using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Alertas;
using EduMonitor.Application.Indicadores;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Herramientas;

/// <summary>
/// Datos de las herramientas del agente (HU-13). Lee lo guardado en EduMonitorDB; nunca llama a Moodle.
/// La autorización por curso ya la resolvió la política CursoAutorizado.
/// </summary>
public sealed class HerramientasService(IEduMonitorDbContext db, IndicadorService indicadores, AlertasService alertas, TimeProvider reloj)
{
    public const string HerramientaCalificaciones = "obtener_calificaciones";
    public const string HerramientaAlertas = "identificar_alertas";

    /// <summary>
    /// TA-13.2 · Alertas ACTIVAS del curso, ya calculadas por el backend (HU-12), con el mismo objeto de
    /// GET /api/cursos/{id}/alertas. Null si el curso no existe. El agente solo las lee: nunca calcula alertas.
    /// </summary>
    public async Task<RespuestaAlertasHerramienta?> AlertasAsync(int cursoId, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return null;
        }

        var consulta = await alertas.ObtenerAlertasAsync(cursoId, EstadosAlerta.Activa, ct);
        // Forma corta para el contexto del modelo: sin titulo, estado, generada ni revisada, que son iguales en todas; con el
        // nombre de la regla para que la respuesta diga «Promedio bajo» y no un código.
        var lista = consulta.Alertas
            .Select(a => new AlertaHerramientaDto(a.Id, a.Regla, DefinicionesRegla.PorCodigo(a.Regla)?.Nombre ?? a.Regla, a.Estudiante, EvidenciaCorta(a.Regla, a.Evidencia)))
            .ToList();
        return new RespuestaAlertasHerramienta(HerramientaAlertas, reloj.GetLocalNow(), new CursoAlertasHerramientaDto(curso.Id, curso.NombreCorto), lista);
    }

    /// <summary>
    /// Evidencia corta para el agente (la pantalla «Alertas» sigue con la completa): atrasos sin fechas límite, descenso sin la
    /// lista de notas anteriores. Promedio bajo queda igual. Así 16 alertas caben en el contexto y el modelo responde a tiempo.
    /// </summary>
    public static System.Text.Json.JsonElement EvidenciaCorta(string regla, System.Text.Json.JsonElement evidencia)
    {
        static System.Text.Json.JsonElement? Campo(System.Text.Json.JsonElement e, string nombre) =>
            e.ValueKind == System.Text.Json.JsonValueKind.Object && e.TryGetProperty(nombre, out var valor) ? valor : null;

        object? corta = regla.ToUpperInvariant() switch
        {
            DefinicionesRegla.CodigoR2 when Campo(evidencia, "actividades") is { ValueKind: System.Text.Json.JsonValueKind.Array } actividades => new
            {
                actividades = actividades.EnumerateArray()
                    .Select(a => Campo(a, "nombre")?.GetString())
                    .Where(n => n is not null)
                    .ToList(),
                umbral = Campo(evidencia, "umbral"),
            },
            DefinicionesRegla.CodigoR1 => new
            {
                ultimas = Campo(evidencia, "ultimas"),
                diferencia = Campo(evidencia, "diferencia"),
                umbral = Campo(evidencia, "umbral"),
            },
            _ => null,
        };
        return corta is null ? evidencia : System.Text.Json.JsonSerializer.SerializeToElement(corta);
    }

    /// <summary>
    /// TA-13.1 · Sin <paramref name="estudianteId"/>: resumen por estudiante (promedio ponderado, parcial y conteos de notas), que cabe
    /// en el contexto del modelo. Con <paramref name="estudianteId"/>: notas y estados por actividad (escala 0 a 10) de ese estudiante.
    /// Null si el curso no existe; <see cref="ResultadoCalificaciones.EstudianteNoMatriculado"/> si se pidió un estudiante que no está en el curso.
    /// Un curso sin sincronizar responde con la lista vacía y <c>ultimaSincronizacion</c> en null.
    /// </summary>
    public async Task<ResultadoCalificaciones?> CalificacionesAsync(int cursoId, int? estudianteId, string? nombre = null, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return null;
        }

        var sincronizacion = await indicadores.UltimaSincronizacionConDatosAsync(cursoId, ct);
        var cursoDto = new CursoHerramientaDto(curso.Id, curso.NombreCorto, sincronizacion?.Inicio);
        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, Array.Empty<object>()));
        }

        var calculados = CalculadoraIndicadores.Calcular(datos).Estudiantes.ToDictionary(e => e.Id);

        // Qué estudiantes entran: uno por id, los que coinciden con el nombre, o todo el curso.
        var buscaPorNombre = estudianteId is null && !string.IsNullOrWhiteSpace(nombre);
        var estudiantes = estudianteId is not null
            ? datos.Estudiantes.Where(e => e.Id == estudianteId).ToList()
            : buscaPorNombre
                ? datos.Estudiantes.Where(e => CoincideNombre(nombre!, e.Nombre, e.Apellido)).ToList()
                : datos.Estudiantes.ToList();
        if (estudiantes.Count == 0 && (estudianteId is not null || buscaPorNombre))
        {
            return new ResultadoCalificaciones(null, EstudianteNoMatriculado: true);
        }

        // Todo el curso, o varias coincidencias de nombre: el resumen, de menor a mayor promedio (sin promedio al final), para que
        // los que están bajo un umbral queden juntos al principio y el modelo no tenga que buscarlos entre todas las filas.
        if (estudianteId is null && (!buscaPorNombre || estudiantes.Count > 1))
        {
            var resumen = estudiantes
                .Where(e => calculados.ContainsKey(e.Id))
                .Select(e => calculados[e.Id])
                .OrderBy(e => e.PromedioPonderado is null)
                .ThenBy(e => e.PromedioPonderado)
                .ThenBy(e => e.Apellido)
                .Select(e => (object)new EstudianteResumenCalificacionesDto(e.Id, e.Nombre, e.Apellido, e.PromedioPonderado, e.Parcial, e.Notas))
                .ToList();
            return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, resumen), Coincidencias: estudiantes.Count);
        }

        var promedios = calculados.ToDictionary(e => e.Key, e => e.Value.PromedioPonderado);
        var unidades = datos.Unidades.ToDictionary(u => u.Id, u => u.Numero);
        var orden = CalculadoraIndicadores.OrdenarActividades(datos.Actividades, datos.Unidades);
        var calificaciones = datos.Calificaciones.GroupBy(c => (c.EstudianteId, c.ActividadId)).ToDictionary(g => g.Key, g => g.First());

        var respuesta = estudiantes.Select(e => (object)new EstudianteCalificacionesDto(
                e.Id,
                e.Nombre,
                e.Apellido,
                promedios.GetValueOrDefault(e.Id),
                orden
                    .Select(a => (Actividad: a, Calificacion: calificaciones.GetValueOrDefault((e.Id, a.Id))))
                    .Where(x => x.Calificacion is not null)
                    .Select(x => new CalificacionHerramientaDto(
                        x.Actividad.Nombre,
                        x.Actividad.Categoria,
                        x.Actividad.UnidadId is int id && unidades.TryGetValue(id, out var numero) ? numero : null,
                        CalculadoraIndicadores.Redondear(CalculadoraIndicadores.NotaEnEscala(x.Calificacion!.Nota, x.Actividad.NotaMaxima)),
                        x.Calificacion.Estado?.ToString()))
                    .ToList()))
            .ToList();

        return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, respuesta), Coincidencias: 1);
    }

    /// <summary>
    /// Busca por nombre o apellido sin distinguir tildes ni mayúsculas: cada palabra de la búsqueda tiene que ser el inicio de alguna
    /// palabra del nombre completo («ana» encuentra a Ana Acosta pero no a Adriana; «espinoza» y «elena espi» encuentran a Elena Espinoza).
    /// </summary>
    public static bool CoincideNombre(string busqueda, string nombre, string apellido)
    {
        var palabrasBuscadas = (CalculadoraIndicadores.ClaveCategoria(busqueda) ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var palabrasDelNombre = (CalculadoraIndicadores.ClaveCategoria($"{nombre} {apellido}") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palabrasBuscadas.Length > 0
            && palabrasBuscadas.All(buscada => palabrasDelNombre.Any(palabra => palabra.StartsWith(buscada, StringComparison.Ordinal)));
    }
}

/// <summary><see cref="Coincidencias"/>: estudiantes que entraron en la respuesta (1 cuando va el detalle de uno solo).</summary>
public sealed record ResultadoCalificaciones(RespuestaCalificacionesHerramienta? Respuesta, bool EstudianteNoMatriculado = false, int Coincidencias = 0);

/// <summary>
/// Respuesta de <c>GET /api/tools/calificaciones</c> (herramienta <c>obtener_calificaciones</c>). Cada elemento de
/// <see cref="Estudiantes"/> es un <see cref="EstudianteResumenCalificacionesDto"/> sin <c>estudianteId</c> o un
/// <see cref="EstudianteCalificacionesDto"/> con él.
/// </summary>
public sealed record RespuestaCalificacionesHerramienta(
    string Herramienta,
    DateTimeOffset Generado,
    CursoHerramientaDto Curso,
    IReadOnlyList<object> Estudiantes);

public sealed record CursoHerramientaDto(int Id, string NombreCorto, DateTimeOffset? UltimaSincronizacion);

/// <summary>Respuesta de <c>GET /api/tools/alertas</c> (herramienta <c>identificar_alertas</c>, docs/contratos-api.md).</summary>
public sealed record RespuestaAlertasHerramienta(string Herramienta, DateTimeOffset Generado, CursoAlertasHerramientaDto Curso, IReadOnlyList<AlertaHerramientaDto> Alertas);

/// <summary>Alerta activa para el agente: la de GET /api/cursos/{id}/alertas sin los campos que son iguales en todas.</summary>
public sealed record AlertaHerramientaDto(int Id, string Regla, string NombreRegla, EstudianteAlertaDto Estudiante, System.Text.Json.JsonElement Evidencia);

public sealed record CursoAlertasHerramientaDto(int Id, string NombreCorto);

/// <summary>Resumen de un estudiante: los mismos promedio, marca de parcial y conteos que el panel de indicadores.</summary>
public sealed record EstudianteResumenCalificacionesDto(
    int Id,
    string Nombre,
    string Apellido,
    decimal? PromedioPonderado,
    bool Parcial,
    ConteoNotasDto Notas);

public sealed record EstudianteCalificacionesDto(
    int Id,
    string Nombre,
    string Apellido,
    decimal? PromedioPonderado,
    IReadOnlyList<CalificacionHerramientaDto> Calificaciones);

/// <summary>Nota en escala 0 a 10 (null si no hay nota) y estado: CALIFICADO, SIN_ENTREGA, PENDIENTE o null si no está disponible.</summary>
public sealed record CalificacionHerramientaDto(string Actividad, string? Categoria, int? Unidad, decimal? Nota, string? Estado);
