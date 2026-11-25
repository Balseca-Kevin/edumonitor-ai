using System.Globalization;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Documentos;
using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Periodo;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Informes;

/// <summary>
/// HU-16 · Informe del curso en PDF: las cifras del panel (las mismas de los indicadores) y el resumen que redactó el asistente en una
/// conversación del docente (por la línea de corte, el informe exporta el resumen del asistente). No llama a Moodle ni al agente.
/// </summary>
public sealed class InformeService(
    IEduMonitorDbContext db, IndicadorService indicadores, PeriodoService periodo, IGeneradorDocumentos documentos, TimeProvider reloj)
{
    private static readonly CultureInfo Ecuador = CultureInfo.GetCultureInfo("es-EC");
    private const string NoDisponible = "no disponible";

    public async Task<ResultadoInforme> GenerarAsync(UsuarioActual usuario, int cursoId, string? formato, int? conversacionId, CancellationToken ct = default)
    {
        if (!string.Equals(formato ?? "pdf", "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new(EstadoInforme.Invalido, Detalle: "El informe se descarga en formato pdf.");
        }

        if (conversacionId is not int cid)
        {
            return new(EstadoInforme.Invalido, Detalle: "Falta conversacionId: el informe lleva el resumen de una respuesta del asistente.");
        }

        var conversacion = await db.Conversaciones.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cid, ct);
        if (conversacion is null || conversacion.CursoId != cursoId || (conversacion.UsuarioId != usuario.Id && !usuario.EsAdmin))
        {
            return new(EstadoInforme.NoEncontrado, Detalle: $"La conversación {cid} no es tuya en el curso {cursoId}.");
        }

        var consulta = await indicadores.ConsultarAsync(cursoId, ct);
        if (consulta.Estado == EstadoConsultaIndicadores.SinSincronizar || consulta.Indicadores is null)
        {
            return new(EstadoInforme.SinSincronizar, Detalle: "El curso todavía no tiene datos de Moodle.");
        }

        var curso = await db.Cursos.AsNoTracking().SingleAsync(c => c.Id == cursoId, ct);
        var i = consulta.Indicadores;
        var p = (await periodo.ConsultarAsync(cursoId, ct)).Periodo;
        var textoPeriodo = p?.Semana is { } semana
            ? $"Semana {semana.Actual} de {semana.Total}{(p.UnidadActual is { } u ? $" · U{u.Numero} · {u.Nombre}" : string.Empty)}"
            : "Período no disponible";
        var conAlertas = await db.Alertas.AsNoTracking()
            .Where(a => a.Regla.CursoId == cursoId && a.Estado == "ACTIVA")
            .Select(a => a.EstudianteId)
            .Distinct()
            .CountAsync(ct);

        var cifras = new List<(string, string)>
        {
            ("Estudiantes", i.Curso.Estudiantes.ToString(Ecuador)),
            ("Actividades evaluadas", i.Curso.ActividadesEvaluadas.ToString(Ecuador)),
            ("Promedio del curso (40/30/30)", Numero(i.Curso.Promedio)),
            ("La mitad del curso tiene más de", Numero(i.Curso.Mediana)),
            ("Actividades completadas", i.Curso.TasaFinalizacion is decimal t ? $"{(t * 100).ToString("0", Ecuador)} %" : NoDisponible),
            ("Requieren atención", conAlertas.ToString(Ecuador)),
            ("8,5 o más", i.Grupos.Alto.ToString(Ecuador)),
            ("Entre 6,0 y 8,4", i.Grupos.Medio.ToString(Ecuador)),
            ("Menos de 6,0", i.Grupos.Bajo.ToString(Ecuador)),
        };

        var respondio = conversacion.Proveedor switch
        {
            "EXTERNO" => $"Respondió: modelo externo · {conversacion.Modelo}",
            "NINGUNO" => "Respondió: EduMonitor, sin llamar al modelo",
            _ => conversacion.Respaldo ? $"Respondió: modelo local (respaldo) · {conversacion.Modelo}" : $"Respondió: modelo local · {conversacion.Modelo}",
        };
        var documento = new DocumentoInforme(
            $"{curso.NombreCorto} · {curso.Nombre}",
            textoPeriodo,
            CalculadoraPeriodo.EnEcuador(i.Sincronizacion.Fecha),
            i.Sincronizacion.Estado,
            cifras,
            conversacion.Pregunta,
            conversacion.Respuesta,
            $"{respondio} · {CalculadoraPeriodo.EnEcuador(conversacion.Fecha).ToString("dd/MM/yyyy HH:mm", Ecuador)}",
            CalculadoraPeriodo.EnEcuador(reloj.GetUtcNow()));
        return new(EstadoInforme.Generado, documentos.Informe(documento), $"informe-{curso.NombreCorto}-{cid}.pdf");
    }

    private static string Numero(decimal? valor) => valor is decimal v ? v.ToString("0.00", Ecuador) : NoDisponible;
}

public enum EstadoInforme
{
    Generado,
    Invalido,
    NoEncontrado,
    SinSincronizar,
}

public sealed record ResultadoInforme(EstadoInforme Estado, byte[]? Contenido = null, string? Nombre = null, string? Detalle = null);
