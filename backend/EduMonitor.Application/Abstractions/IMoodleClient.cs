using EduMonitor.Application.Abstractions.Moodle;

namespace EduMonitor.Application.Abstractions;

/// <summary>
/// Lectura del Moodle de laboratorio por servicios web REST. Un método por cada función permitida
/// (ver docs/CONTEXTO_IA.md); ninguna otra función de Moodle se usa. Solo lectura.
/// Los métodos lanzan <see cref="MoodleNoDisponibleException"/> si Moodle no responde o devuelve un error.
/// </summary>
public interface IMoodleClient
{
    /// <summary><c>core_webservice_get_site_info</c>: sitio, versión, usuario técnico y funciones habilitadas.</summary>
    Task<InfoSitioMoodle> ObtenerInfoSitioAsync(CancellationToken ct = default);

    /// <summary><c>core_enrol_get_users_courses</c>: cursos en los que está matriculado un usuario de Moodle.</summary>
    Task<IReadOnlyList<CursoMoodle>> ObtenerCursosDeUsuarioAsync(int moodleUsuarioId, CancellationToken ct = default);

    /// <summary><c>core_course_get_contents</c>: secciones (unidades) y módulos (actividades) del curso.</summary>
    Task<IReadOnlyList<SeccionMoodle>> ObtenerContenidoCursoAsync(int moodleCursoId, CancellationToken ct = default);

    /// <summary><c>core_enrol_get_enrolled_users</c>: matriculados con sus roles, sin correo ni datos de contacto.</summary>
    Task<IReadOnlyList<MatriculadoMoodle>> ObtenerMatriculadosAsync(int moodleCursoId, CancellationToken ct = default);

    /// <summary><c>gradereport_user_get_grade_items</c>: ítems, categorías, pesos y notas de cada estudiante del curso.</summary>
    Task<IReadOnlyList<CalificacionesUsuarioMoodle>> ObtenerItemsCalificacionAsync(int moodleCursoId, CancellationToken ct = default);

    /// <summary><c>mod_assign_get_assignments</c>: tareas del curso con su fecha límite.</summary>
    Task<IReadOnlyList<TareaMoodle>> ObtenerTareasAsync(int moodleCursoId, CancellationToken ct = default);

    /// <summary><c>mod_assign_get_submissions</c>: estado de entrega por estudiante de las tareas indicadas.</summary>
    Task<IReadOnlyList<EntregaMoodle>> ObtenerEntregasAsync(IReadOnlyCollection<int> moodleTareaIds, CancellationToken ct = default);

    /// <summary><c>core_completion_get_activities_completion_status</c>: estado de finalización de las actividades para un estudiante.</summary>
    Task<IReadOnlyList<FinalizacionActividadMoodle>> ObtenerFinalizacionAsync(int moodleCursoId, int moodleUsuarioId, CancellationToken ct = default);
}
