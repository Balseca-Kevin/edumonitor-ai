using EduMonitor.Tests.Infraestructura;

namespace EduMonitor.Tests.Persistencia;

/// <summary>HU-03 · El modelo crea todas las tablas del Diagrama 5 de la planificación.</summary>
public class ModeloCompletoTests
{
    private static readonly string[] TablasDiagrama5 =
    {
        "Curso", "Unidad", "Estudiante", "Matricula", "Actividad", "Calificacion",
        "Tema", "ContenidoMinimo", "ActividadTema", "ReglaAlerta", "Alerta", "TareaRefuerzo",
        "Sincronizacion", "Conversacion", "TrazaHerramienta",
        "Usuario", "DocenteCurso", "Auditoria",
    };

    [Fact]
    public async Task Crea_todas_las_tablas_del_diagrama_5()
    {
        using var bd = new BaseDatosPrueba();
        await using var comando = bd.Conexion.CreateCommand();
        comando.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        var tablas = new List<string>();
        await using (var lector = await comando.ExecuteReaderAsync())
        {
            while (await lector.ReadAsync())
            {
                tablas.Add(lector.GetString(0));
            }
        }

        Assert.All(TablasDiagrama5, tabla => Assert.Contains(tabla, tablas));
    }
}
