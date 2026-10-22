using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;

namespace EduMonitor.Infrastructure.Moodle;

/// <summary>
/// Marcador hasta que llegue el cliente real con HttpClient tipado (TA-02.2, Sebastián).
/// Cada llamada falla como "Moodle no disponible" para que la API responda 502 y nunca datos inventados.
/// </summary>
internal sealed class MoodleClientPendiente : IMoodleClient
{
    private static Task<T> Pendiente<T>(string funcion) =>
        Task.FromException<T>(new MoodleNoDisponibleException(funcion, "El cliente de Moodle aún no está implementado (TA-02.2)."));

    public Task<InfoSitioMoodle> ObtenerInfoSitioAsync(CancellationToken ct = default) =>
        Pendiente<InfoSitioMoodle>("core_webservice_get_site_info");

    public Task<IReadOnlyList<CursoMoodle>> ObtenerCursosDeUsuarioAsync(int moodleUsuarioId, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<CursoMoodle>>("core_enrol_get_users_courses");

    public Task<IReadOnlyList<SeccionMoodle>> ObtenerContenidoCursoAsync(int moodleCursoId, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<SeccionMoodle>>("core_course_get_contents");

    public Task<IReadOnlyList<MatriculadoMoodle>> ObtenerMatriculadosAsync(int moodleCursoId, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<MatriculadoMoodle>>("core_enrol_get_enrolled_users");

    public Task<IReadOnlyList<CalificacionesUsuarioMoodle>> ObtenerItemsCalificacionAsync(int moodleCursoId, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<CalificacionesUsuarioMoodle>>("gradereport_user_get_grade_items");

    public Task<IReadOnlyList<TareaMoodle>> ObtenerTareasAsync(int moodleCursoId, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<TareaMoodle>>("mod_assign_get_assignments");

    public Task<IReadOnlyList<EntregaMoodle>> ObtenerEntregasAsync(IReadOnlyCollection<int> moodleTareaIds, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<EntregaMoodle>>("mod_assign_get_submissions");

    public Task<IReadOnlyList<FinalizacionActividadMoodle>> ObtenerFinalizacionAsync(int moodleCursoId, int moodleUsuarioId, CancellationToken ct = default) =>
        Pendiente<IReadOnlyList<FinalizacionActividadMoodle>>("core_completion_get_activities_completion_status");
}
