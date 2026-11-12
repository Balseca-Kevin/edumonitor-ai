using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Configuracion;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Infrastructure.Moodle;

/// <summary>
/// TA-02.2 · Cliente REST del Moodle de laboratorio (<c>/webservice/rest/server.php</c>). Solo llama a las 8 funciones permitidas.
/// El token va en el cuerpo del POST, nunca en la URL ni en los registros. Si Moodle no responde o devuelve un error,
/// lanza <see cref="MoodleNoDisponibleException"/>; los fallos de red se reintentan antes de rendirse.
/// </summary>
internal sealed partial class MoodleClient(HttpClient http, IConfiguracionMoodle configuracion, ILogger<MoodleClient> logger) : IMoodleClient
{
    /// <summary>Reintentos ante fallos de red, tiempo de espera o 5xx. Un error de Moodle (token, permisos) no se reintenta.</summary>
    internal const int Reintentos = 2;

    /// <summary>Matriculados por página de <c>core_enrol_get_enrolled_users</c>.</summary>
    internal const int TamanoPagina = 100;

    /// <summary>Espera entre reintentos; las pruebas la ponen en cero.</summary>
    internal TimeSpan EsperaEntreReintentos { get; init; } = TimeSpan.FromMilliseconds(500);

    public async Task<InfoSitioMoodle> ObtenerInfoSitioAsync(CancellationToken ct = default)
    {
        using var json = await LlamarAsync("core_webservice_get_site_info", new(), ct);
        var raiz = json.RootElement;
        var funciones = raiz.TryGetProperty("functions", out var lista)
            ? lista.EnumerateArray().Select(f => Texto(f, "name")).ToList()
            : new List<string>();
        return new InfoSitioMoodle(Texto(raiz, "sitename"), VersionCorta(Texto(raiz, "release")), Texto(raiz, "username"), funciones);
    }

    public async Task<IReadOnlyList<CursoMoodle>> ObtenerCursosDeUsuarioAsync(int moodleUsuarioId, CancellationToken ct = default)
    {
        using var json = await LlamarAsync("core_enrol_get_users_courses", new() { ["userid"] = Num(moodleUsuarioId) }, ct);
        return json.RootElement.EnumerateArray()
            .Select(c => new CursoMoodle(Entero(c, "id"), Texto(c, "shortname"), Texto(c, "fullname")))
            .ToList();
    }

    public async Task<IReadOnlyList<SeccionMoodle>> ObtenerContenidoCursoAsync(int moodleCursoId, CancellationToken ct = default)
    {
        var parametros = new Dictionary<string, string>
        {
            ["courseid"] = Num(moodleCursoId),
            // Sin el contenido de archivos y páginas: solo hacen falta secciones y módulos.
            ["options[0][name]"] = "excludecontents",
            ["options[0][value]"] = "1",
        };
        using var json = await LlamarAsync("core_course_get_contents", parametros, ct);
        return json.RootElement.EnumerateArray()
            .Select(s => new SeccionMoodle(
                Entero(s, "id"),
                Entero(s, "section"),
                Texto(s, "name"),
                s.TryGetProperty("modules", out var modulos)
                    ? modulos.EnumerateArray().Select(m => new ModuloMoodle(Entero(m, "id"), Entero(m, "instance"), Texto(m, "name"), Texto(m, "modname"))).ToList()
                    : new List<ModuloMoodle>()))
            .ToList();
    }

    public async Task<IReadOnlyList<MatriculadoMoodle>> ObtenerMatriculadosAsync(int moodleCursoId, CancellationToken ct = default)
    {
        var resultado = new List<MatriculadoMoodle>();
        for (var desde = 0; ; desde += TamanoPagina)
        {
            var parametros = new Dictionary<string, string>
            {
                ["courseid"] = Num(moodleCursoId),
                // Solo los campos mínimos: Moodle no envía el correo ni otros datos de contacto.
                ["options[0][name]"] = "userfields",
                ["options[0][value]"] = "id,firstname,lastname,roles",
                ["options[1][name]"] = "limitfrom",
                ["options[1][value]"] = Num(desde),
                ["options[2][name]"] = "limitnumber",
                ["options[2][value]"] = Num(TamanoPagina),
            };
            using var json = await LlamarAsync("core_enrol_get_enrolled_users", parametros, ct);
            var pagina = json.RootElement.EnumerateArray()
                .Select(u => new MatriculadoMoodle(
                    Entero(u, "id"),
                    Texto(u, "firstname"),
                    Texto(u, "lastname"),
                    u.TryGetProperty("roles", out var roles) ? roles.EnumerateArray().Select(r => Texto(r, "shortname")).ToList() : new List<string>()))
                .ToList();
            resultado.AddRange(pagina);
            if (pagina.Count < TamanoPagina)
            {
                return resultado;
            }
        }
    }

    public async Task<IReadOnlyList<CalificacionesUsuarioMoodle>> ObtenerItemsCalificacionAsync(int moodleCursoId, CancellationToken ct = default)
    {
        // userid = 0: todos los estudiantes calificables del curso en una sola llamada.
        using var json = await LlamarAsync("gradereport_user_get_grade_items", new() { ["courseid"] = Num(moodleCursoId), ["userid"] = "0" }, ct);
        return json.RootElement.GetProperty("usergrades").EnumerateArray()
            .Select(u => new CalificacionesUsuarioMoodle(
                Entero(u, "userid"),
                u.GetProperty("gradeitems").EnumerateArray().Select(ItemCalificacion).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<TareaMoodle>> ObtenerTareasAsync(int moodleCursoId, CancellationToken ct = default)
    {
        var parametros = new Dictionary<string, string>
        {
            ["courseids[0]"] = Num(moodleCursoId),
            // El usuario técnico ve el curso por su rol de sistema, no por matrícula.
            ["includenotenrolledcourses"] = "1",
        };
        using var json = await LlamarAsync("mod_assign_get_assignments", parametros, ct);
        var raiz = json.RootElement;
        var cursos = raiz.GetProperty("courses").EnumerateArray().ToList();
        if (cursos.Count == 0 && Advertencia(raiz) is { } advertencia)
        {
            // Sin cursos y con advertencia es falta de permisos: no es lo mismo que un curso sin tareas.
            throw new MoodleNoDisponibleException("mod_assign_get_assignments", advertencia);
        }

        return cursos
            .SelectMany(c => c.GetProperty("assignments").EnumerateArray())
            .Select(a => new TareaMoodle(Entero(a, "id"), Entero(a, "cmid"), Texto(a, "name"), Fecha(a, "duedate")))
            .ToList();
    }

    public async Task<IReadOnlyList<EntregaMoodle>> ObtenerEntregasAsync(IReadOnlyCollection<int> moodleTareaIds, CancellationToken ct = default)
    {
        if (moodleTareaIds.Count == 0)
        {
            return Array.Empty<EntregaMoodle>();
        }

        var parametros = moodleTareaIds
            .Select((id, i) => (Clave: $"assignmentids[{i}]", Valor: Num(id)))
            .ToDictionary(p => p.Clave, p => p.Valor);
        using var json = await LlamarAsync("mod_assign_get_submissions", parametros, ct);
        return json.RootElement.GetProperty("assignments").EnumerateArray()
            .SelectMany(a => a.GetProperty("submissions").EnumerateArray()
                .Select(s => new EntregaMoodle(Entero(a, "assignmentid"), Entero(s, "userid"), Texto(s, "status"), TextoOpcional(s, "gradingstatus"))))
            .ToList();
    }

    public async Task<IReadOnlyList<FinalizacionActividadMoodle>> ObtenerFinalizacionAsync(int moodleCursoId, int moodleUsuarioId, CancellationToken ct = default)
    {
        using var json = await LlamarAsync(
            "core_completion_get_activities_completion_status",
            new() { ["courseid"] = Num(moodleCursoId), ["userid"] = Num(moodleUsuarioId) },
            ct);
        return json.RootElement.GetProperty("statuses").EnumerateArray()
            .Select(s => new FinalizacionActividadMoodle(Entero(s, "cmid"), Entero(s, "state")))
            .ToList();
    }

    /// <summary>
    /// POST a server.php con el token y la función. Devuelve el JSON si Moodle respondió sin error; si no, lanza
    /// <see cref="MoodleNoDisponibleException"/> con un mensaje que nunca incluye el token.
    /// </summary>
    private async Task<JsonDocument> LlamarAsync(string funcion, Dictionary<string, string> parametros, CancellationToken ct)
    {
        var moodle = configuracion.Actual;
        if (!moodle.TokenConfigurado || string.IsNullOrWhiteSpace(moodle.Url))
        {
            throw new MoodleNoDisponibleException(funcion, "La conexión con Moodle no está configurada (Moodle__Url y Moodle__Token).");
        }

        var url = $"{moodle.Url.TrimEnd('/')}/webservice/rest/server.php";
        var reloj = Stopwatch.StartNew();
        for (var intento = 0; ; intento++)
        {
            var cuerpo = new Dictionary<string, string>(parametros)
            {
                ["wstoken"] = moodle.Token,
                ["wsfunction"] = funcion,
                ["moodlewsrestformat"] = "json",
            };

            string? fallo;
            try
            {
                using var respuesta = await http.PostAsync(url, new FormUrlEncodedContent(cuerpo), ct);
                if ((int)respuesta.StatusCode >= 500)
                {
                    fallo = $"Moodle respondió HTTP {(int)respuesta.StatusCode}.";
                }
                else if (!respuesta.IsSuccessStatusCode)
                {
                    throw new MoodleNoDisponibleException(funcion, $"Moodle respondió HTTP {(int)respuesta.StatusCode}.");
                }
                else
                {
                    var json = await LeerJsonAsync(funcion, respuesta, ct);
                    logger.LogInformation("Moodle {Funcion} respondió en {DuracionMs} ms", funcion, reloj.ElapsedMilliseconds);
                    return json;
                }
            }
            catch (HttpRequestException e)
            {
                fallo = $"No se pudo conectar con Moodle: {e.Message}";
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                fallo = $"Moodle no respondió en {http.Timeout.TotalSeconds:0} s.";
            }

            if (intento >= Reintentos)
            {
                logger.LogWarning("Moodle {Funcion} falló tras {Intentos} intentos: {Fallo}", funcion, intento + 1, fallo);
                throw new MoodleNoDisponibleException(funcion, fallo);
            }

            await Task.Delay(EsperaEntreReintentos * (intento + 1), ct);
        }
    }

    private static async Task<JsonDocument> LeerJsonAsync(string funcion, HttpResponseMessage respuesta, CancellationToken ct)
    {
        JsonDocument json;
        try
        {
            json = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        }
        catch (JsonException)
        {
            throw new MoodleNoDisponibleException(funcion, "Moodle devolvió una respuesta que no es JSON.");
        }

        // Moodle informa sus errores con HTTP 200 y un objeto {exception, errorcode, message}.
        var raiz = json.RootElement;
        if (raiz.ValueKind == JsonValueKind.Object && (raiz.TryGetProperty("exception", out _) || raiz.TryGetProperty("errorcode", out _)))
        {
            var codigo = TextoOpcional(raiz, "errorcode");
            var mensaje = TextoOpcional(raiz, "message") ?? "error sin descripción";
            json.Dispose();
            throw new MoodleNoDisponibleException(funcion, codigo is null ? mensaje : $"{mensaje} ({codigo})");
        }

        return json;
    }

    private static ItemCalificacionMoodle ItemCalificacion(JsonElement i)
    {
        var tipo = Texto(i, "itemtype");
        // Moodle no da nombre a los ítems de categoría ni al total: la categoría lleva su nombre en el idnumber
        // (moodle-lab/scripts/cargar_datos_laboratorio.php).
        var nombre = TextoOpcional(i, "itemname")
            ?? (tipo == "course" ? "Total del curso" : TextoOpcional(i, "idnumber"))
            ?? $"Categoría {EnteroOpcional(i, "iteminstance")}";
        return new ItemCalificacionMoodle(
            Entero(i, "id"),
            nombre,
            tipo,
            TextoOpcional(i, "itemmodule"),
            EnteroOpcional(i, "iteminstance"),
            EnteroOpcional(i, "categoryid"),
            DecimalOpcional(i, "weightraw"),
            DecimalOpcional(i, "graderaw"),
            DecimalOpcional(i, "grademax"));
    }

    private static string? Advertencia(JsonElement raiz) =>
        raiz.TryGetProperty("warnings", out var advertencias) && advertencias.GetArrayLength() > 0
            ? TextoOpcional(advertencias[0], "message")
            : null;

    /// <summary>«4.5.15+ (Build: 20251003)» → «4.5».</summary>
    internal static string VersionCorta(string release)
    {
        var coincidencia = VersionRegex().Match(release);
        return coincidencia.Success ? coincidencia.Value : release;
    }

    [GeneratedRegex(@"^\d+\.\d+")]
    private static partial Regex VersionRegex();

    private static string Num(int valor) => valor.ToString(CultureInfo.InvariantCulture);

    private static string Texto(JsonElement e, string propiedad) => TextoOpcional(e, propiedad) ?? string.Empty;

    private static string? TextoOpcional(JsonElement e, string propiedad) =>
        e.TryGetProperty(propiedad, out var valor) && valor.ValueKind != JsonValueKind.Null
            ? valor.ValueKind == JsonValueKind.String ? valor.GetString() : valor.GetRawText()
            : null;

    private static int Entero(JsonElement e, string propiedad) => EnteroOpcional(e, propiedad) ?? 0;

    private static int? EnteroOpcional(JsonElement e, string propiedad) =>
        e.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.Number ? valor.GetInt32()
        : e.TryGetProperty(propiedad, out valor) && valor.ValueKind == JsonValueKind.String && int.TryParse(valor.GetString(), CultureInfo.InvariantCulture, out var n) ? n
        : null;

    private static decimal? DecimalOpcional(JsonElement e, string propiedad) =>
        e.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.Number ? valor.GetDecimal() : null;

    /// <summary>Fechas de Moodle en segundos Unix; 0 significa «sin fecha».</summary>
    private static DateTimeOffset? Fecha(JsonElement e, string propiedad) =>
        EnteroOpcional(e, propiedad) is int segundos and > 0 ? DateTimeOffset.FromUnixTimeSeconds(segundos) : null;
}
