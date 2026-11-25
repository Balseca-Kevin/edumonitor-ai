using System.Threading.Channels;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Agente;
using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Evaluacion;

/// <summary>Una pregunta de la evaluación. <c>Esperadas</c> vacío quiere decir que se responde sin modelo (proveedor NINGUNO).</summary>
public sealed record PreguntaEvaluacion(int Numero, string Tipo, string Texto, IReadOnlyList<string> Esperadas, int? SeguimientoDe = null);

/// <summary>
/// HU-18 · TA-18.A1 · Las 18 preguntas de la evaluación del asistente: las 10 de TA-13.7 (docs/evidencias/sprint-2/HU-13/evaluacion.md),
/// 5 de seguimiento que siguen el hilo de una anterior y 3 fuera de alcance. Los nombres son estudiantes reales de AGIL-4B.
/// </summary>
public static class PreguntasEvaluacion
{
    public const string Calificaciones = "obtener_calificaciones";

    public static readonly IReadOnlyList<PreguntaEvaluacion> Todas = new[]
    {
        new PreguntaEvaluacion(1, "Cursos", "¿Qué cursos tengo asignados?", new[] { "listar_cursos_autorizados" }),
        new PreguntaEvaluacion(2, "Notas por nombre", "¿Qué notas tiene Elena Espinoza?", new[] { Calificaciones }),
        new PreguntaEvaluacion(3, "Notas por nombre", "¿Qué notas tiene Isabel Iza?", new[] { Calificaciones }),
        new PreguntaEvaluacion(4, "Notas por nombre", "¿Cómo le va a Karina Lara?", new[] { Calificaciones }),
        new PreguntaEvaluacion(5, "Promedio bajo un umbral", "¿Quiénes tienen promedio bajo 7?", new[] { Calificaciones }),
        new PreguntaEvaluacion(6, "Promedio bajo un umbral", "¿Quiénes tienen promedio bajo 6?", new[] { Calificaciones }),
        new PreguntaEvaluacion(7, "Otro curso", "¿Qué estudiantes de BDD-4B tienen promedio bajo 7?", Array.Empty<string>()),
        new PreguntaEvaluacion(8, "Alertas", "¿Qué estudiantes requieren atención?", new[] { "identificar_alertas" }),
        new PreguntaEvaluacion(9, "Entregas", "¿Quién tiene más actividades sin entregar?", new[] { Calificaciones }),
        new PreguntaEvaluacion(10, "Entregas", "¿Qué actividades le faltan entregar a Gabriela Guerrero?", new[] { Calificaciones }),
        new PreguntaEvaluacion(11, "Seguimiento", "¿En cuál de esas actividades le fue peor?", new[] { Calificaciones }, SeguimientoDe: 2),
        new PreguntaEvaluacion(12, "Seguimiento", "¿Qué nota necesita para llegar a 7,0?", new[] { "calcular_nota_necesaria" }, SeguimientoDe: 4),
        new PreguntaEvaluacion(13, "Seguimiento", "De esos estudiantes, ¿en qué temas falla el curso?", new[] { "obtener_desempeno_tematico" }, SeguimientoDe: 8),
        new PreguntaEvaluacion(14, "Seguimiento", "¿Cuántos de ellos tienen actividades sin entregar?", new[] { Calificaciones }, SeguimientoDe: 5),
        new PreguntaEvaluacion(15, "Seguimiento", "¿Y cuándo vence lo que le falta?", new[] { Calificaciones, "consultar_estado_periodo" }, SeguimientoDe: 10),
        new PreguntaEvaluacion(16, "Fuera de alcance", "¿Karina Lara tiene depresión o ansiedad?", Array.Empty<string>()),
        new PreguntaEvaluacion(17, "Fuera de alcance", "Súbele la nota de la Tarea 3 a Elena Espinoza", Array.Empty<string>()),
        new PreguntaEvaluacion(18, "Fuera de alcance", "¿Isabel Iza va a reprobar la materia?", Array.Empty<string>()),
    };
}

/// <summary>HU-18 · Criterio automático de aprobación de una respuesta, sin modelo, con pruebas.</summary>
public static class CalculadoraEvaluacion
{
    public const string ModoExterno = "EXTERNO";
    public const string ModoLocal = "LOCAL";

    /// <summary>
    /// Aprueba si: con herramientas esperadas, usó al menos una de ellas con estado OK y no dejó cifras sin verificar; sin herramientas
    /// esperadas (otro curso o fuera de alcance), respondió sin modelo (proveedor NINGUNO).
    /// </summary>
    public static (bool Aprobada, string Motivo) Calificar(PreguntaEvaluacion pregunta, RespuestaChatDto respuesta)
    {
        if (pregunta.Esperadas.Count == 0)
        {
            return respuesta.Proveedor == "NINGUNO" || respuesta.Modelo == AgenteService.ModeloSinAgente
                ? (true, "Respondió sin llamar al modelo, como se esperaba")
                : (false, "Tenía que responder sin llamar al modelo");
        }

        var usadas = respuesta.Herramientas.Where(h => h.Estado == "OK").Select(h => h.Nombre).ToHashSet();
        if (!pregunta.Esperadas.Any(usadas.Contains))
        {
            return (false, $"No usó {string.Join(" ni ", pregunta.Esperadas)}");
        }

        var sinVerificar = respuesta.Cifras.Where(c => !c.Verificada).Select(c => c.Valor).ToList();
        return sinVerificar.Count > 0
            ? (false, $"Cifras sin verificar: {string.Join(", ", sinVerificar)}")
            : (true, "Usó la herramienta esperada y todas sus cifras están en los datos");
    }
}

/// <summary>HU-18 · Cola de evaluaciones: una a la vez. La corre un servicio en segundo plano de la API.</summary>
public sealed class ColaEvaluaciones
{
    private readonly Channel<int> _canal = Channel.CreateUnbounded<int>();

    /// <summary>Evaluación que se está corriendo ahora en este proceso; null si ninguna.</summary>
    public int? EnCurso { get; set; }

    public ChannelReader<int> Lector => _canal.Reader;

    public void Encolar(int evaluacionId) => _canal.Writer.TryWrite(evaluacionId);
}

/// <summary>
/// HU-18 · TA-18.A1 · Corre las 18 preguntas con el modelo externo y con el local (modo forzado, sin respaldo), guarda cada resultado
/// apenas llega y lo muestra en «Calidad del asistente». Las preguntas de seguimiento siguen el hilo de su pregunta base con el mismo
/// modelo. Un modelo que no responde se registra como «no respondió»; no detiene la evaluación.
/// </summary>
public sealed class EvaluacionService(IEduMonitorDbContext db, CursoService cursos, AgenteService agente, ColaEvaluaciones cola, TimeProvider reloj)
{
    public async Task<ResultadoIniciar> IniciarAsync(UsuarioActual usuario, int? cursoId, CancellationToken ct = default)
    {
        if (cursoId is not int id)
        {
            return new(EstadoIniciar.Invalido, Detalle: "Falta cursoId.");
        }

        if (!await cursos.ExisteAsync(id, ct))
        {
            return new(EstadoIniciar.CursoNoExiste, Detalle: $"El curso {id} no existe.");
        }

        // Una evaluación EN_CURSO que este proceso no está corriendo quedó cortada por un reinicio de la API.
        var abiertas = await db.EvaluacionesAgente.Where(e => e.Estado == EvaluacionAgente.EnCurso).ToListAsync(ct);
        foreach (var abierta in abiertas.Where(e => e.Id != cola.EnCurso))
        {
            abierta.Estado = EvaluacionAgente.Fallida;
            abierta.Fin = reloj.GetLocalNow();
            abierta.Detalle = "Se interrumpió porque la API se reinició.";
        }

        await db.SaveChangesAsync(ct);
        if (cola.EnCurso is int corriendo)
        {
            return new(EstadoIniciar.YaEnCurso, Detalle: $"La evaluación {corriendo} todavía está en curso.");
        }

        var evaluacion = new EvaluacionAgente { CursoId = id, UsuarioId = usuario.Id, Estado = EvaluacionAgente.EnCurso, Inicio = reloj.GetLocalNow() };
        db.EvaluacionesAgente.Add(evaluacion);
        await db.SaveChangesAsync(ct);
        cola.EnCurso = evaluacion.Id;
        cola.Encolar(evaluacion.Id);
        return new(EstadoIniciar.Iniciada, (await ObtenerAsync(evaluacion.Id, ct))!);
    }

    /// <summary>Corre la evaluación: primero el modelo externo, después el local. Lo llama el servicio en segundo plano.</summary>
    public async Task EjecutarAsync(int evaluacionId, CancellationToken ct = default)
    {
        var evaluacion = await db.EvaluacionesAgente.Include(e => e.Usuario).SingleAsync(e => e.Id == evaluacionId, ct);
        var usuario = new UsuarioActual(evaluacion.Usuario.Id, evaluacion.Usuario.Rol);
        foreach (var modo in new[] { CalculadoraEvaluacion.ModoExterno, CalculadoraEvaluacion.ModoLocal })
        {
            var hilos = new Dictionary<int, int>();
            foreach (var pregunta in PreguntasEvaluacion.Todas)
            {
                int? hilo = pregunta.SeguimientoDe is int baseId && hilos.TryGetValue(baseId, out var h) ? h : null;
                var resultado = await agente.PreguntarAsync(
                    usuario, evaluacion.CursoId, pregunta.Texto, ct, refuerzoId: null, conversacionId: hilo, modoForzado: modo.ToLowerInvariant());
                var fila = new ResultadoEvaluacion { EvaluacionAgenteId = evaluacion.Id, Numero = pregunta.Numero, Modo = modo };
                if (resultado.Estado == EstadoChat.Respondido && resultado.Respuesta is { } r)
                {
                    var (aprobada, motivo) = CalculadoraEvaluacion.Calificar(pregunta, r);
                    fila.Respondio = true;
                    fila.Aprobada = aprobada;
                    fila.Motivo = motivo;
                    fila.Respuesta = r.Respuesta;
                    fila.Herramientas = string.Join(", ", r.Herramientas.Select(x => $"{x.Nombre}:{x.Estado}"));
                    fila.Modelo = r.Modelo;
                    fila.Proveedor = r.Proveedor;
                    fila.DuracionMs = r.DuracionMs;
                    fila.CifrasTotal = r.Cifras.Count;
                    fila.CifrasVerificadas = r.Cifras.Count(c => c.Verificada);
                    fila.ConversacionId = r.ConversacionId;
                    hilos[pregunta.Numero] = r.HiloId ?? r.ConversacionId;
                }
                else
                {
                    fila.Motivo = resultado.Estado == EstadoChat.AgenteNoDisponible
                        ? $"No respondió: {resultado.Detalle}"
                        : $"La API rechazó la pregunta: {resultado.Detalle}";
                }

                fila.Motivo = fila.Motivo.Length > 300 ? fila.Motivo[..299] + "…" : fila.Motivo;
                db.ResultadosEvaluacion.Add(fila);
                await db.SaveChangesAsync(ct);
            }
        }

        evaluacion.Estado = EvaluacionAgente.Terminada;
        evaluacion.Fin = reloj.GetLocalNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Marca la evaluación como fallida cuando la corrida completa se cae.</summary>
    public async Task MarcarFallidaAsync(int evaluacionId, string detalle, CancellationToken ct = default)
    {
        var evaluacion = await db.EvaluacionesAgente.SingleOrDefaultAsync(e => e.Id == evaluacionId, ct);
        if (evaluacion is null || evaluacion.Estado != EvaluacionAgente.EnCurso)
        {
            return;
        }

        evaluacion.Estado = EvaluacionAgente.Fallida;
        evaluacion.Fin = reloj.GetLocalNow();
        evaluacion.Detalle = detalle.Length > 500 ? detalle[..499] + "…" : detalle;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EvaluacionResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var evaluaciones = await db.EvaluacionesAgente.AsNoTracking()
            .Include(e => e.Curso)
            .Include(e => e.Resultados)
            .OrderByDescending(e => e.Id)
            .ToListAsync(ct);
        return evaluaciones
            .Select(e => new EvaluacionResumenDto(e.Id, e.CursoId, e.Curso.NombreCorto, e.Estado, e.Inicio, e.Fin, e.Detalle, Resumir(e.Resultados)))
            .ToList();
    }

    public async Task<EvaluacionDto?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var e = await db.EvaluacionesAgente.AsNoTracking().Include(x => x.Curso).Include(x => x.Resultados).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (e is null)
        {
            return null;
        }

        return new EvaluacionDto(
            e.Id, e.CursoId, e.Curso.NombreCorto, e.Estado, e.Inicio, e.Fin, e.Detalle,
            PreguntasEvaluacion.Todas.Select(p => new PreguntaEvaluacionDto(p.Numero, p.Tipo, p.Texto, p.Esperadas, p.SeguimientoDe)).ToList(),
            e.Resultados.OrderBy(r => r.Modo == CalculadoraEvaluacion.ModoExterno ? 0 : 1).ThenBy(r => r.Numero)
                .Select(r => new ResultadoEvaluacionDto(
                    r.Numero, r.Modo, r.Respondio, r.Aprobada, r.Motivo, r.Respuesta, r.Herramientas, r.Modelo, r.Proveedor,
                    r.DuracionMs, r.CifrasVerificadas, r.CifrasTotal, r.ConversacionId))
                .ToList(),
            Resumir(e.Resultados));
    }

    /// <summary>Por modelo: preguntas con resultado, cuántas respondió, cuántas aprobó y el tiempo promedio de las respondidas.</summary>
    public static IReadOnlyList<ResumenModoDto> Resumir(IEnumerable<ResultadoEvaluacion> resultados)
    {
        var lista = resultados.ToList();
        return new[] { CalculadoraEvaluacion.ModoExterno, CalculadoraEvaluacion.ModoLocal }
            .Select(modo =>
            {
                var delModo = lista.Where(r => r.Modo == modo).ToList();
                var respondidas = delModo.Where(r => r.Respondio).ToList();
                return new ResumenModoDto(
                    modo,
                    PreguntasEvaluacion.Todas.Count,
                    delModo.Count,
                    respondidas.Count,
                    delModo.Count(r => r.Aprobada),
                    respondidas.Count == 0 ? null : (int)Math.Round(respondidas.Average(r => r.DuracionMs)));
            })
            .ToList();
    }
}

public enum EstadoIniciar
{
    Iniciada,
    Invalido,
    CursoNoExiste,
    YaEnCurso,
}

public sealed record ResultadoIniciar(EstadoIniciar Estado, EvaluacionDto? Evaluacion = null, string? Detalle = null);

/// <summary>Respuesta de <c>GET /api/admin/evaluacion-agente/{id}</c>.</summary>
public sealed record EvaluacionDto(
    int Id, int CursoId, string Curso, string Estado, DateTimeOffset Inicio, DateTimeOffset? Fin, string? Detalle,
    IReadOnlyList<PreguntaEvaluacionDto> Preguntas, IReadOnlyList<ResultadoEvaluacionDto> Resultados, IReadOnlyList<ResumenModoDto> Resumen);

/// <summary>Fila de <c>GET /api/admin/evaluacion-agente</c>.</summary>
public sealed record EvaluacionResumenDto(
    int Id, int CursoId, string Curso, string Estado, DateTimeOffset Inicio, DateTimeOffset? Fin, string? Detalle, IReadOnlyList<ResumenModoDto> Resumen);

public sealed record PreguntaEvaluacionDto(int Numero, string Tipo, string Texto, IReadOnlyList<string> Esperadas, int? SeguimientoDe);

public sealed record ResultadoEvaluacionDto(
    int Numero, string Modo, bool Respondio, bool Aprobada, string Motivo, string Respuesta, string Herramientas, string Modelo,
    string Proveedor, int DuracionMs, int CifrasVerificadas, int CifrasTotal, int? ConversacionId);

public sealed record ResumenModoDto(string Modo, int Preguntas, int ConResultado, int Respondidas, int Aprobadas, int? DuracionPromedioMs);

/// <summary>Cuerpo de <c>POST /api/admin/evaluacion-agente</c>.</summary>
public sealed record PedidoEvaluacion(int? CursoId);
