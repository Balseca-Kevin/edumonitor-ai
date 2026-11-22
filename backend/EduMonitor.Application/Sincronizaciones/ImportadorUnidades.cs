using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// TA-05.1 · Importa las secciones del curso como unidades (core_course_get_contents) y devuelve la estructura
/// módulo → unidad que usan los demás pasos. Upsert por id de sección: sincronizar dos veces no duplica.
/// TA-08.A1 · El nombre de la unidad es el de la sección (el del sílabo en el aula virtual). Si la unidad todavía no tiene temas,
/// los temas del resumen de la sección (<see cref="TemasSilabo"/>) quedan como sus temas iniciales; después los administra el
/// docente en el mapeo de HU-08 y la sincronización ya no los toca.
/// </summary>
public sealed class ImportadorUnidades(IMoodleClient moodle, IEduMonitorDbContext db)
{
    /// <summary>Lanza <see cref="MoodleNoDisponibleException"/> si Moodle falla; en ese caso no toca la base.</summary>
    public async Task<EstructuraCurso> ImportarAsync(Curso curso, CancellationToken ct = default)
    {
        var secciones = await moodle.ObtenerContenidoCursoAsync(curso.MoodleCursoId, ct);
        // La sección 0 es la cabecera «General» del curso, no una unidad.
        var seccionesUnidad = secciones.Where(s => s.Numero > 0).ToList();

        var ids = seccionesUnidad.Select(s => s.Id).ToList();
        var existentes = await db.Unidades.Where(u => ids.Contains(u.MoodleSeccionId)).ToDictionaryAsync(u => u.MoodleSeccionId, ct);

        var estructura = new EstructuraCurso();
        foreach (var seccion in seccionesUnidad)
        {
            if (!existentes.TryGetValue(seccion.Id, out var unidad))
            {
                unidad = new Unidad { MoodleSeccionId = seccion.Id, CursoId = curso.Id };
                db.Unidades.Add(unidad);
            }

            unidad.Numero = seccion.Numero;
            unidad.Parcial = ParcialDe(seccion.Numero);
            unidad.Nombre = string.IsNullOrWhiteSpace(seccion.Nombre) ? $"Unidad {seccion.Numero}" : seccion.Nombre;
            estructura.Agregar(unidad, seccion.Modulos);
        }

        await db.SaveChangesAsync(ct);
        await AgregarTemasInicialesAsync(seccionesUnidad, ct);
        return estructura;
    }

    private async Task AgregarTemasInicialesAsync(IReadOnlyList<SeccionMoodle> secciones, CancellationToken ct)
    {
        var ids = secciones.Select(s => s.Id).ToList();
        var unidades = await db.Unidades.Where(u => ids.Contains(u.MoodleSeccionId)).ToDictionaryAsync(u => u.MoodleSeccionId, ct);
        var idsUnidad = unidades.Values.Select(u => u.Id).ToList();
        var conTemas = (await db.Temas.Where(t => idsUnidad.Contains(t.UnidadId)).Select(t => t.UnidadId).Distinct().ToListAsync(ct)).ToHashSet();

        foreach (var seccion in secciones)
        {
            var unidad = unidades[seccion.Id];
            if (conTemas.Contains(unidad.Id))
            {
                continue;
            }

            foreach (var tema in TemasSilabo.Leer(seccion.Resumen))
            {
                db.Temas.Add(new Tema { UnidadId = unidad.Id, Nombre = tema });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Modelo UTA: el Parcial 1 tiene las unidades 1 y 2; el Parcial 2, las unidades 3 y 4.</summary>
    public static int ParcialDe(int numeroUnidad) => numeroUnidad <= 2 ? 1 : 2;
}
