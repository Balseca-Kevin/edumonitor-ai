using EduMonitor.Tests.Infraestructura;

namespace EduMonitor.Tests.Moodle;

/// <summary>
/// TA-02.2 · El cliente contra el Moodle de laboratorio real, con los datos de moodle-lab/scripts/cargar_datos_laboratorio.php.
/// Se omiten si no hay .env con el token (por ejemplo, en CI).
/// </summary>
public class MoodleClientIntegracionTests
{
    private const int AgilMoodleId = 2;

    private static readonly string[] FuncionesPermitidas =
    {
        "core_webservice_get_site_info",
        "core_enrol_get_users_courses",
        "core_course_get_contents",
        "core_enrol_get_enrolled_users",
        "gradereport_user_get_grade_items",
        "mod_assign_get_assignments",
        "mod_assign_get_submissions",
        "core_completion_get_activities_completion_status",
    };

    [FactMoodleReal]
    public async Task El_servicio_tiene_exactamente_las_8_funciones_permitidas()
    {
        var info = await MoodleReal.Cliente().ObtenerInfoSitioAsync();

        Assert.Equal("edumonitor_ws_user", info.UsuarioTecnico);
        Assert.Matches(@"^\d+\.\d+$", info.Version);
        Assert.Equal(FuncionesPermitidas.Order(), info.Funciones.Order());
    }

    [FactMoodleReal]
    public async Task Lee_las_cuatro_unidades_y_las_16_tareas_de_AGIL_4B()
    {
        var cliente = MoodleReal.Cliente();

        var secciones = await cliente.ObtenerContenidoCursoAsync(AgilMoodleId);
        var tareas = await cliente.ObtenerTareasAsync(AgilMoodleId);

        Assert.Equal(new[] { "Unidad 1", "Unidad 2", "Unidad 3", "Unidad 4" }, secciones.Where(s => s.Numero > 0).Select(s => s.Nombre));
        Assert.Equal(16, tareas.Count);
        Assert.All(tareas, t => Assert.NotNull(t.FechaLimite));
    }

    [FactMoodleReal]
    public async Task Lee_32_estudiantes_y_las_categorias_40_30_30()
    {
        var cliente = MoodleReal.Cliente();

        var matriculados = await cliente.ObtenerMatriculadosAsync(AgilMoodleId);
        var reportes = await cliente.ObtenerItemsCalificacionAsync(AgilMoodleId);

        Assert.Equal(32, matriculados.Count(m => m.Roles.Contains("student")));
        var categorias = reportes[0].Items.Where(i => i.TipoItem == "category").ToDictionary(i => i.Nombre, i => i.Peso);
        Assert.Equal(new Dictionary<string, decimal?> { ["Exámenes"] = 0.4m, ["Tareas"] = 0.3m, ["APE"] = 0.3m }, categorias);
    }

    [FactMoodleReal]
    public async Task Lee_entregas_y_finalizacion()
    {
        var cliente = MoodleReal.Cliente();
        var tareas = await cliente.ObtenerTareasAsync(AgilMoodleId);
        var estudiante = (await cliente.ObtenerMatriculadosAsync(AgilMoodleId)).First(m => m.Roles.Contains("student"));

        var entregas = await cliente.ObtenerEntregasAsync(tareas.Select(t => t.Id).ToList());
        var finalizacion = await cliente.ObtenerFinalizacionAsync(AgilMoodleId, estudiante.Id);

        Assert.Contains(entregas, e => e.Estado == "submitted" && e.EstadoCalificacion == "notgraded");
        Assert.Contains(entregas, e => e.EstadoCalificacion == "graded");
        Assert.Equal(16, finalizacion.Count);
    }
}
