using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// TA-05.2 · Importa los estudiantes matriculados de un curso con datos mínimos (id de Moodle, nombre y apellido).
/// Upsert por id de Moodle: sincronizar dos veces no duplica estudiantes ni matrículas.
/// </summary>
public sealed class ImportadorMatriculados(IMoodleClient moodle, IEduMonitorDbContext db)
{
    /// <summary>Rol de Moodle de los estudiantes. Docentes y demás roles no se importan como estudiantes.</summary>
    public const string RolEstudiante = "student";

    /// <summary>Devuelve cuántos estudiantes hay matriculados en el curso según Moodle. Lanza <see cref="MoodleNoDisponibleException"/> si Moodle falla.</summary>
    public async Task<int> ImportarAsync(Curso curso, CancellationToken ct = default)
    {
        // Primero se lee Moodle: si falla, no se toca la base.
        var matriculados = await moodle.ObtenerMatriculadosAsync(curso.MoodleCursoId, ct);
        var estudiantesMoodle = matriculados
            .Where(m => m.Roles.Contains(RolEstudiante, StringComparer.OrdinalIgnoreCase))
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .ToList();

        var ids = estudiantesMoodle.Select(m => m.Id).ToList();
        var existentes = await db.Estudiantes
            .Where(e => ids.Contains(e.MoodleUsuarioId))
            .ToDictionaryAsync(e => e.MoodleUsuarioId, ct);
        var matriculadosYa = (await db.Matriculas
                .Where(m => m.CursoId == curso.Id)
                .Select(m => m.Estudiante.MoodleUsuarioId)
                .ToListAsync(ct))
            .ToHashSet();

        foreach (var dato in estudiantesMoodle)
        {
            if (!existentes.TryGetValue(dato.Id, out var estudiante))
            {
                estudiante = new Estudiante { MoodleUsuarioId = dato.Id };
                db.Estudiantes.Add(estudiante);
                existentes[dato.Id] = estudiante;
            }

            estudiante.Nombre = dato.Nombre;
            estudiante.Apellido = dato.Apellido;

            if (!matriculadosYa.Contains(dato.Id))
            {
                db.Matriculas.Add(new Matricula { CursoId = curso.Id, Estudiante = estudiante });
            }
        }

        await db.SaveChangesAsync(ct);
        return estudiantesMoodle.Count;
    }
}
