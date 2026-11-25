using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Libro;
using EduMonitor.Application.Tematicas;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Fichas;

/// <summary>
/// HU-10 · Ficha del estudiante con lo que guardó la última sincronización: sus notas y estados por actividad (los mismos del libro),
/// los criterios de rúbrica y el comentario del docente, la evolución de su promedio frente a 7,0, los temas donde queda bajo 7,0 y
/// sus alertas activas. No llama a Moodle ni calcula fechas en la pantalla.
/// </summary>
public sealed class FichaEstudianteService(IEduMonitorDbContext db, IndicadorService indicadores, TematicaService tematicas)
{
    public const decimal Aprobacion = 7.0m;

    public async Task<ConsultaFicha> ConsultarAsync(int cursoId, int estudianteId, CancellationToken ct = default)
    {
        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            return new ConsultaFicha(EstadoConsultaFicha.SinSincronizar);
        }

        var estudiante = datos.Estudiantes.FirstOrDefault(e => e.Id == estudianteId);
        if (estudiante is null)
        {
            return new ConsultaFicha(EstadoConsultaFicha.EstudianteNoExiste);
        }

        var calculados = CalculadoraIndicadores.Calcular(datos);
        var fila = calculados.Estudiantes.Single(e => e.Id == estudianteId);
        var orden = CalculadoraIndicadores.OrdenarActividades(datos.Actividades, datos.Unidades);
        var unidades = datos.Unidades.ToDictionary(u => u.Id);
        var calificaciones = datos.Calificaciones.Where(c => c.EstudianteId == estudianteId)
            .GroupBy(c => c.ActividadId).ToDictionary(g => g.Key, g => g.First());

        var ids = orden.Select(a => a.Id).ToList();
        var criterios = (await db.CalificacionesCriterio.AsNoTracking()
                .Where(c => c.EstudianteId == estudianteId && ids.Contains(c.CriterioRubrica.ActividadId))
                .Select(c => new
                {
                    c.CriterioRubrica.ActividadId, c.CriterioRubrica.Orden, c.CriterioRubrica.Descripcion, c.CriterioRubrica.PuntajeMaximo, c.Nivel, c.Puntaje,
                })
                .ToListAsync(ct))
            .GroupBy(c => c.ActividadId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CriterioFichaDto>)g.OrderBy(c => c.Orden).Select(c => new CriterioFichaDto(c.Descripcion, c.Nivel, c.Puntaje, c.PuntajeMaximo)).ToList());

        var actividades = orden.Select(a =>
        {
            var cal = calificaciones.GetValueOrDefault(a.Id);
            var unidad = a.UnidadId is int uid && unidades.TryGetValue(uid, out var u) ? u : null;
            var nota = cal?.Estado == Domain.Enums.EstadoCalificacion.CALIFICADO
                ? CalculadoraIndicadores.Redondear(CalculadoraIndicadores.NotaEnEscala(cal.Nota, a.NotaMaxima))
                : null;
            return new ActividadFichaDto(
                a.Id, a.Nombre, a.Categoria, unidad?.Numero ?? 0, unidad?.Nombre ?? "Sin unidad", a.FechaLimite,
                nota, LibroService.EstadoNota(cal, a, datos.Ahora), cal?.Comentario,
                criterios.GetValueOrDefault(a.Id) ?? Array.Empty<CriterioFichaDto>());
        }).ToList();

        var evolucion = CalculadoraFicha.Evolucion(CalculadoraIndicadores.NotasOrdenadas(datos, estudianteId));

        var desempeno = await tematicas.DesempenoAsync(cursoId, null, ct);
        var temasDebiles = (desempeno.Valor?.Temas ?? Array.Empty<TemaDesempenoDto>())
            .Select(t => (Tema: t, Estudiante: t.Estudiantes.FirstOrDefault(e => e.Id == estudianteId)))
            .Where(x => x.Estudiante is not null && x.Estudiante.Nota < Aprobacion)
            .OrderBy(x => x.Estudiante!.Nota)
            .Select(x => new TemaDebilDto(
                x.Tema.Id, x.Tema.Nombre, x.Tema.Unidad.Numero, x.Estudiante!.Nota, x.Tema.Promedio,
                x.Estudiante.Notas.Select(n => new NotaTemaFichaDto(n.ActividadId, actividades.FirstOrDefault(a => a.Id == n.ActividadId)?.Nombre ?? string.Empty, n.Nota)).ToList(),
                x.Tema.Proximas))
            .ToList();

        var alertas = (await db.Alertas.AsNoTracking()
                .Where(a => a.EstudianteId == estudianteId && a.Regla.CursoId == cursoId && a.Estado == "ACTIVA")
                .Select(a => new { a.Id, a.Regla.Codigo, a.Evidencia, a.Generada })
                .ToListAsync(ct))
            .OrderBy(a => a.Codigo)
            .Select(a => new AlertaFichaDto(a.Id, a.Codigo, LeerJson(a.Evidencia), a.Generada))
            .ToList();

        return new ConsultaFicha(EstadoConsultaFicha.Encontrada, new FichaEstudianteDto(
            cursoId,
            new EstudianteFichaDto(estudiante.Id, estudiante.Nombre, estudiante.Apellido),
            Aprobacion,
            fila.PromedioPonderado is null ? null : CalculadoraIndicadores.Redondear(fila.PromedioPonderado),
            fila.Parcial,
            fila.Grupo,
            fila.Notas,
            fila.Categorias ?? Array.Empty<CategoriaPromedioDto>(),
            actividades,
            evolucion,
            temasDebiles,
            alertas,
            datos.FinalizacionDisponible ? Array.Empty<string>() : new[] { "tasaFinalizacion" }));
    }

    private static JsonElement LeerJson(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return documento.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var vacio = JsonDocument.Parse("{}");
            return vacio.RootElement.Clone();
        }
    }
}

/// <summary>HU-10 · Cálculos de la ficha, sin base de datos, con pruebas de casos conocidos.</summary>
public static class CalculadoraFicha
{
    /// <summary>
    /// Evolución del promedio frente a 7,0: por cada nota calificada, en el orden del curso, la nota y el promedio ponderado 40/30/30
    /// con las notas hasta esa actividad (normalizado si todavía falta una categoría, como el promedio del panel).
    /// </summary>
    public static IReadOnlyList<PuntoEvolucionDto> Evolucion(IReadOnlyList<NotaCalculo> notas)
    {
        var puntos = new List<PuntoEvolucionDto>();
        for (var i = 0; i < notas.Count; i++)
        {
            var hasta = notas.Take(i + 1).Select(n => (n.Actividad.Categoria, n.Nota));
            var (promedio, _) = CalculadoraIndicadores.PromedioPonderado(hasta);
            var actividad = notas[i].Actividad;
            puntos.Add(new PuntoEvolucionDto(actividad.Id, actividad.Nombre, actividad.FechaLimite,
                CalculadoraIndicadores.Redondear(notas[i].Nota)!.Value, CalculadoraIndicadores.Redondear(promedio)));
        }

        return puntos;
    }
}

public enum EstadoConsultaFicha
{
    Encontrada,
    SinSincronizar,
    EstudianteNoExiste,
}

public sealed record ConsultaFicha(EstadoConsultaFicha Estado, FichaEstudianteDto? Ficha = null);

/// <summary>Respuesta de <c>GET /api/cursos/{id}/estudiantes/{eid}</c> (HU-10).</summary>
public sealed record FichaEstudianteDto(
    int CursoId,
    EstudianteFichaDto Estudiante,
    decimal Aprobacion,
    decimal? PromedioPonderado,
    bool Parcial,
    string? Grupo,
    ConteoNotasDto Notas,
    IReadOnlyList<CategoriaPromedioDto> Categorias,
    IReadOnlyList<ActividadFichaDto> Actividades,
    IReadOnlyList<PuntoEvolucionDto> Evolucion,
    IReadOnlyList<TemaDebilDto> TemasDebiles,
    IReadOnlyList<AlertaFichaDto> Alertas,
    IReadOnlyList<string> NoDisponibles);

public sealed record EstudianteFichaDto(int Id, string Nombre, string Apellido);

/// <summary>Nota y estado del estudiante en una actividad, los mismos del libro, con su comentario y sus criterios de rúbrica.</summary>
public sealed record ActividadFichaDto(
    int Id, string Nombre, string? Categoria, int Unidad, string NombreUnidad, DateTimeOffset? FechaLimite,
    decimal? Nota, string? Estado, string? Comentario, IReadOnlyList<CriterioFichaDto> Criterios);

public sealed record CriterioFichaDto(string Descripcion, string Nivel, decimal Puntaje, decimal PuntajeMaximo);

public sealed record PuntoEvolucionDto(int ActividadId, string Actividad, DateTimeOffset? FechaLimite, decimal Nota, decimal? Promedio);

public sealed record TemaDebilDto(
    int Id, string Nombre, int Unidad, decimal Nota, decimal? PromedioCurso, IReadOnlyList<NotaTemaFichaDto> Notas, IReadOnlyList<ProximaEvaluacionDto> Proximas);

public sealed record NotaTemaFichaDto(int ActividadId, string Actividad, decimal Nota);

public sealed record AlertaFichaDto(int Id, string Regla, JsonElement Evidencia, DateTimeOffset Generada);
