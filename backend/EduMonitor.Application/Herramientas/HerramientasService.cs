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
    public Task<RespuestaAlertasHerramienta?> AlertasAsync(int cursoId, CancellationToken ct = default) =>
        AlertasAsync(cursoId, Tramo.Todo, ct);

    /// <summary>
    /// C7 · Las alertas del <paramref name="tramo"/> que la pantalla tiene elegido (R.2), con la evidencia del tramo, para que el texto
    /// del asistente y las tarjetas digan lo mismo. <see cref="TramoInexistente"/> si el curso no tiene ese parcial o esa unidad.
    /// </summary>
    public async Task<RespuestaAlertasHerramienta?> AlertasAsync(int cursoId, Tramo tramo, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return null;
        }

        var consulta = await alertas.ObtenerAlertasAsync(cursoId, EstadosAlerta.Activa, tramo, ct);
        if (consulta.Estado == EstadoConsultaAlertas.TramoInvalido)
        {
            throw new TramoInexistente(tramo.NoExisteDetalle);
        }

        // Forma corta para el contexto del modelo: sin titulo, estado, generada ni revisada, que son iguales en todas; con el
        // nombre de la regla para que la respuesta diga «Promedio bajo» y no un código.
        var lista = consulta.Alertas
            .Select(a => new AlertaHerramientaDto(a.Id, a.Regla, DefinicionesRegla.PorCodigo(a.Regla)?.Nombre ?? a.Regla, a.Estudiante, EvidenciaCorta(a.Regla, a.Evidencia)))
            .ToList();
        return new RespuestaAlertasHerramienta(HerramientaAlertas, reloj.GetLocalNow(), new CursoAlertasHerramientaDto(curso.Id, curso.NombreCorto), lista)
        {
            Tramo = tramo.Codigo,
        };
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
            DefinicionesRegla.CodigoR1 => EvidenciaR1Corta(evidencia),
            _ => null,
        };
        return corta is null ? evidencia : System.Text.Json.JsonSerializer.SerializeToElement(corta);
    }

    /// <summary>
    /// C7 · Descenso reciente en la forma que se dice en palabras: las últimas notas de la más reciente a la más antigua, el promedio
    /// de las últimas y el de las anteriores. La evidencia guardada sigue en orden cronológico.
    /// </summary>
    public static object EvidenciaR1Corta(System.Text.Json.JsonElement evidencia)
    {
        static List<decimal> Numeros(System.Text.Json.JsonElement e, string nombre) =>
            e.ValueKind == System.Text.Json.JsonValueKind.Object && e.TryGetProperty(nombre, out var lista) && lista.ValueKind == System.Text.Json.JsonValueKind.Array
                ? lista.EnumerateArray().Where(v => v.ValueKind == System.Text.Json.JsonValueKind.Number).Select(v => v.GetDecimal()).ToList()
                : new List<decimal>();
        static decimal? Promedio(List<decimal> notas) => notas.Count == 0 ? null : Math.Round(notas.Average(), 2, MidpointRounding.AwayFromZero);
        static object? Valor(System.Text.Json.JsonElement e, string nombre) =>
            e.ValueKind == System.Text.Json.JsonValueKind.Object && e.TryGetProperty(nombre, out var v) ? v : null;

        var ultimas = Numeros(evidencia, "ultimas");
        var anteriores = Numeros(evidencia, "anteriores");
        return new
        {
            ultimasNotas = Enumerable.Reverse(ultimas).ToList(),
            promedioUltimas = Promedio(ultimas),
            promedioAnterior = Promedio(anteriores),
            diferencia = Valor(evidencia, "diferencia"),
            umbral = Valor(evidencia, "umbral"),
        };
    }

    /// <summary>
    /// TA-13.1 · Sin <paramref name="estudianteId"/>: resumen por estudiante (promedio ponderado, parcial y conteos de notas), que cabe
    /// en el contexto del modelo. Con <paramref name="estudianteId"/>: notas y estados por actividad (escala 0 a 10) de ese estudiante.
    /// Null si el curso no existe; <see cref="ResultadoCalificaciones.EstudianteNoMatriculado"/> si se pidió un estudiante que no está en el curso.
    /// Un curso sin sincronizar responde con la lista vacía y <c>ultimaSincronizacion</c> en null.
    /// </summary>
    public async Task<ResultadoCalificaciones?> CalificacionesAsync(
        int cursoId, int? estudianteId, string? nombre = null, CancellationToken ct = default) =>
        await CalificacionesAsync(cursoId, estudianteId, nombre, new FiltroCalificaciones(), ct);

    /// <summary>
    /// HU-23 · Igual que la sobrecarga sin filtro, pero el resumen (sin <paramref name="estudianteId"/>) puede quedarse solo con los
    /// promedios menores a <see cref="FiltroCalificaciones.PromedioMenorA"/> (los que no tienen promedio quedan fuera) y ordenarse por
    /// actividades sin entrega, de mayor a menor con empate por apellido. Así el modelo no filtra ni ordena una lista de 32 filas.
    /// </summary>
    public async Task<ResultadoCalificaciones?> CalificacionesAsync(
        int cursoId, int? estudianteId, string? nombre, FiltroCalificaciones filtro, CancellationToken ct = default)
    {
        var curso = await db.Cursos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cursoId, ct);
        if (curso is null)
        {
            return null;
        }

        var sincronizacion = await indicadores.UltimaSincronizacionConDatosAsync(cursoId, ct);
        var cursoDto = new CursoHerramientaDto(curso.Id, curso.NombreCorto, sincronizacion?.Inicio);
        var tramo = filtro.Tramo ?? Tramo.Todo;
        var completos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (completos is null)
        {
            return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, Array.Empty<object>()) { Tramo = tramo.Codigo });
        }

        // C7 · Con el tramo de la pantalla, los promedios y las notas salen solo de las actividades de ese tramo (el mismo cálculo del
        // panel con ?tramo=), así el texto del asistente y las tarjetas dicen el mismo número.
        if (!tramo.ExisteEn(completos))
        {
            throw new TramoInexistente(tramo.NoExisteDetalle);
        }

        var datos = tramo.Filtrar(completos);

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
            var filtrados = estudiantes
                .Where(e => calculados.ContainsKey(e.Id))
                .Select(e => calculados[e.Id])
                .Where(e => filtro.PromedioMenorA is not decimal limite || e.PromedioPonderado < limite);
            var ordenados = filtro.Orden == OrdenCalificaciones.SinEntrega
                ? filtrados.OrderByDescending(e => e.Notas.SinEntrega).ThenBy(e => e.Apellido).ThenBy(e => e.Nombre)
                : filtrados.OrderBy(e => e.PromedioPonderado is null).ThenBy(e => e.PromedioPonderado).ThenBy(e => e.Apellido);
            var resumen = ordenados
                .Select(e => (object)new EstudianteResumenCalificacionesDto(e.Id, e.Nombre, e.Apellido, e.PromedioPonderado, e.Parcial, e.Notas))
                .ToList();
            return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, resumen) { Tramo = tramo.Codigo }, Coincidencias: estudiantes.Count);
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
                        Libro.LibroService.EstadoNota(x.Calificacion, x.Actividad, datos.Ahora),
                        x.Actividad.Id))
                    .ToList()))
            .ToList();

        return new ResultadoCalificaciones(new RespuestaCalificacionesHerramienta(HerramientaCalificaciones, reloj.GetLocalNow(), cursoDto, respuesta) { Tramo = tramo.Codigo }, Coincidencias: 1);
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

/// <summary>Filtros del resumen de <c>GET /api/tools/calificaciones</c> (HU-23): <c>promedioMenorA</c> y <c>orden</c>.</summary>
public sealed record FiltroCalificaciones(decimal? PromedioMenorA = null, OrdenCalificaciones Orden = OrdenCalificaciones.Promedio, Tramo? Tramo = null);

/// <summary>C7 · El curso no tiene el parcial o la unidad del tramo pedido: la herramienta responde 400.</summary>
public sealed class TramoInexistente(string detalle) : Exception(detalle);

public enum OrdenCalificaciones
{
    Promedio,
    SinEntrega,
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
    IReadOnlyList<object> Estudiantes)
{
    /// <summary>C7 · Tramo con el que se calcularon los promedios y las notas (<c>todo</c>, <c>p1</c>, <c>u2</c>…).</summary>
    public string Tramo { get; init; } = "todo";
}

public sealed record CursoHerramientaDto(int Id, string NombreCorto, DateTimeOffset? UltimaSincronizacion);

/// <summary>Respuesta de <c>GET /api/tools/alertas</c> (herramienta <c>identificar_alertas</c>, docs/contratos-api.md).</summary>
public sealed record RespuestaAlertasHerramienta(string Herramienta, DateTimeOffset Generado, CursoAlertasHerramientaDto Curso, IReadOnlyList<AlertaHerramientaDto> Alertas)
{
    /// <summary>C7 · Tramo de las alertas (<c>todo</c>, <c>p1</c>, <c>u2</c>…).</summary>
    public string Tramo { get; init; } = "todo";
}

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
/// <summary>Nota de un estudiante en una actividad. <c>ActividadId</c> (HU-23) deja enlazar cada cifra de la respuesta con su celda del libro.</summary>
public sealed record CalificacionHerramientaDto(string Actividad, string? Categoria, int? Unidad, decimal? Nota, string? Estado, int? ActividadId = null);
