using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>
/// Cliente de Moodle en memoria para las pruebas de sincronización. Los datos se fijan en cada prueba;
/// <see cref="Fallar"/> hace que una función responda como Moodle caído.
/// </summary>
public sealed class MoodleClientFalso : IMoodleClient
{
    public List<MatriculadoMoodle> Matriculados { get; } = new();
    public List<CalificacionesUsuarioMoodle> Calificaciones { get; } = new();
    public HashSet<string> Fallar { get; } = new();
    public List<string> Llamadas { get; } = new();

    /// <summary>Curso de ejemplo: 2 estudiantes y 1 docente; categorías Exámenes y Tareas; est02 sin nota en la tarea.</summary>
    public static MoodleClientFalso ConCursoDeEjemplo()
    {
        var falso = new MoodleClientFalso();
        falso.Matriculados.AddRange(new[]
        {
            new MatriculadoMoodle(101, "Ana", "Pérez", new[] { "student" }),
            new MatriculadoMoodle(102, "Luis", "Mora", new[] { "student" }),
            new MatriculadoMoodle(5, "Docente", "Uno", new[] { "editingteacher" }),
        });

        IReadOnlyList<ItemCalificacionMoodle> Items(decimal? notaTarea, decimal? notaExamen) => new[]
        {
            new ItemCalificacionMoodle(1, "Total del curso", "course", null, 1, null, null, 9m, 10m),
            new ItemCalificacionMoodle(10, "Exámenes", "category", null, 20, 1, 0.4m, null, 10m),
            new ItemCalificacionMoodle(11, "Tareas", "category", null, 21, 1, 0.3m, null, 10m),
            new ItemCalificacionMoodle(30, "Tarea 1", "mod", "assign", 7, 21, 1m, notaTarea, 10m),
            new ItemCalificacionMoodle(31, "Examen P1", "mod", "quiz", 3, 20, 1m, notaExamen, 10m),
        };

        falso.Calificaciones.Add(new CalificacionesUsuarioMoodle(101, Items(8.5m, 0m)));
        falso.Calificaciones.Add(new CalificacionesUsuarioMoodle(102, Items(null, 7m)));
        return falso;
    }

    private Task<T> Responder<T>(string funcion, T valor)
    {
        Llamadas.Add(funcion);
        return Fallar.Contains(funcion)
            ? Task.FromException<T>(new MoodleNoDisponibleException(funcion, "Acceso denegado para el usuario técnico"))
            : Task.FromResult(valor);
    }

    public Task<InfoSitioMoodle> ObtenerInfoSitioAsync(CancellationToken ct = default) =>
        Responder("core_webservice_get_site_info", new InfoSitioMoodle("Moodle-Lab-UTA", "4.5", "edumonitor_ws_user", Array.Empty<string>()));

    public Task<IReadOnlyList<CursoMoodle>> ObtenerCursosDeUsuarioAsync(int moodleUsuarioId, CancellationToken ct = default) =>
        Responder<IReadOnlyList<CursoMoodle>>("core_enrol_get_users_courses", Array.Empty<CursoMoodle>());

    public Task<IReadOnlyList<SeccionMoodle>> ObtenerContenidoCursoAsync(int moodleCursoId, CancellationToken ct = default) =>
        Responder<IReadOnlyList<SeccionMoodle>>("core_course_get_contents", Array.Empty<SeccionMoodle>());

    public Task<IReadOnlyList<MatriculadoMoodle>> ObtenerMatriculadosAsync(int moodleCursoId, CancellationToken ct = default) =>
        Responder<IReadOnlyList<MatriculadoMoodle>>("core_enrol_get_enrolled_users", Matriculados.ToList());

    public Task<IReadOnlyList<CalificacionesUsuarioMoodle>> ObtenerItemsCalificacionAsync(int moodleCursoId, CancellationToken ct = default) =>
        Responder<IReadOnlyList<CalificacionesUsuarioMoodle>>("gradereport_user_get_grade_items", Calificaciones.ToList());

    public Task<IReadOnlyList<TareaMoodle>> ObtenerTareasAsync(int moodleCursoId, CancellationToken ct = default) =>
        Responder<IReadOnlyList<TareaMoodle>>("mod_assign_get_assignments", Array.Empty<TareaMoodle>());

    public Task<IReadOnlyList<EntregaMoodle>> ObtenerEntregasAsync(IReadOnlyCollection<int> moodleTareaIds, CancellationToken ct = default) =>
        Responder<IReadOnlyList<EntregaMoodle>>("mod_assign_get_submissions", Array.Empty<EntregaMoodle>());

    public Task<IReadOnlyList<FinalizacionActividadMoodle>> ObtenerFinalizacionAsync(int moodleCursoId, int moodleUsuarioId, CancellationToken ct = default) =>
        Responder<IReadOnlyList<FinalizacionActividadMoodle>>("core_completion_get_activities_completion_status", Array.Empty<FinalizacionActividadMoodle>());
}
