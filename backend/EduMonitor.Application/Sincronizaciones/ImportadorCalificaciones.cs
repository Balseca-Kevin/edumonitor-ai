using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// TA-05.3 · Importa los ítems del libro de calificaciones como actividades (con su categoría y peso) y las notas
/// de los estudiantes matriculados. Upsert por id de Moodle: sincronizar dos veces no duplica.
/// </summary>
public sealed class ImportadorCalificaciones(IMoodleClient moodle, IEduMonitorDbContext db)
{
    // Ítems que son actividades calificables. «course» (total) y «category» (subtotales) no se guardan como actividades.
    private static readonly string[] TiposActividad = { "mod", "manual" };

    public sealed record Resultado(int Actividades, int Calificaciones);

    /// <summary>Lanza <see cref="MoodleNoDisponibleException"/> si Moodle falla; en ese caso no toca la base.</summary>
    public async Task<Resultado> ImportarAsync(Curso curso, CancellationToken ct = default)
    {
        var reportes = await moodle.ObtenerItemsCalificacionAsync(curso.MoodleCursoId, ct);

        var todosLosItems = reportes.SelectMany(r => r.Items).ToList();
        var categorias = todosLosItems
            .Where(i => i.TipoItem == "category" && i.Instancia is not null)
            .GroupBy(i => i.Instancia!.Value)
            .ToDictionary(g => g.Key, g => g.First().Nombre);
        var itemsActividad = todosLosItems
            .Where(i => TiposActividad.Contains(i.TipoItem))
            .GroupBy(i => i.Id)
            .Select(g => g.First())
            .ToList();

        var actividades = await ActualizarActividadesAsync(curso, itemsActividad, categorias, ct);
        var calificaciones = await ActualizarCalificacionesAsync(curso, reportes, actividades, ct);

        await db.SaveChangesAsync(ct);
        return new Resultado(actividades.Count, calificaciones);
    }

    private async Task<Dictionary<int, Actividad>> ActualizarActividadesAsync(
        Curso curso, List<ItemCalificacionMoodle> items, Dictionary<int, string> categorias, CancellationToken ct)
    {
        var ids = items.Select(i => i.Id).ToList();
        var actividades = await db.Actividades.Where(a => ids.Contains(a.MoodleItemId)).ToDictionaryAsync(a => a.MoodleItemId, ct);

        foreach (var item in items)
        {
            if (!actividades.TryGetValue(item.Id, out var actividad))
            {
                actividad = new Actividad { MoodleItemId = item.Id, CursoId = curso.Id };
                db.Actividades.Add(actividad);
                actividades[item.Id] = actividad;
            }

            actividad.Nombre = item.Nombre;
            actividad.Categoria = item.CategoriaId is int categoriaId && categorias.TryGetValue(categoriaId, out var nombre) ? nombre : null;
            actividad.Peso = item.Peso;
            actividad.NotaMaxima = item.NotaMaxima;
            actividad.Modulo = item.Modulo;
            actividad.Instancia = item.Instancia;
            // TA-05.1 (Sebastián): asignar actividad.UnidadId a partir de las secciones de core_course_get_contents.
        }

        return actividades;
    }

    private async Task<int> ActualizarCalificacionesAsync(
        Curso curso, IReadOnlyList<CalificacionesUsuarioMoodle> reportes, Dictionary<int, Actividad> actividades, CancellationToken ct)
    {
        // Solo estudiantes matriculados en este curso (los importa TA-05.2). Un docente o un usuario sin matrícula no tiene notas aquí.
        var estudiantes = await db.Matriculas
            .Where(m => m.CursoId == curso.Id)
            .Select(m => m.Estudiante)
            .ToDictionaryAsync(e => e.MoodleUsuarioId, ct);

        var idsActividad = actividades.Values.Where(a => a.Id != 0).Select(a => a.Id).ToList();
        var existentes = await db.Calificaciones
            .Where(c => idsActividad.Contains(c.ActividadId))
            .ToDictionaryAsync(c => (c.ActividadId, c.EstudianteId), ct);

        var total = 0;
        foreach (var reporte in reportes)
        {
            if (!estudiantes.TryGetValue(reporte.UsuarioId, out var estudiante))
            {
                continue;
            }

            foreach (var item in reporte.Items.Where(i => actividades.ContainsKey(i.Id)))
            {
                var actividad = actividades[item.Id];
                if (actividad.Id == 0 || !existentes.TryGetValue((actividad.Id, estudiante.Id), out var calificacion))
                {
                    calificacion = new Calificacion { Actividad = actividad, Estudiante = estudiante };
                    db.Calificaciones.Add(calificacion);
                }

                calificacion.Nota = item.Nota;
                calificacion.Estado = DerivarEstado(item.Nota, calificacion.Estado);
                total++;
            }
        }

        return total;
    }

    /// <summary>
    /// Con nota (incluida la 0) es CALIFICADO. Sin nota se conserva lo que haya derivado TA-05.4 (SIN_ENTREGA o PENDIENTE);
    /// si antes estaba CALIFICADO y Moodle ya no tiene nota, vuelve a null (no disponible) hasta que TA-05.4 lo derive.
    /// </summary>
    public static EstadoCalificacion? DerivarEstado(decimal? nota, EstadoCalificacion? actual) =>
        nota is not null ? EstadoCalificacion.CALIFICADO
        : actual == EstadoCalificacion.CALIFICADO ? null
        : actual;
}
