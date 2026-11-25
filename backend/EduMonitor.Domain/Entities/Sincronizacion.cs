using EduMonitor.Domain.Enums;

namespace EduMonitor.Domain.Entities;

/// <summary>Registro de cada sincronización manual de un curso (SINCRONIZACION en el Diagrama 5).</summary>
public class Sincronizacion
{
    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    /// <summary>Usuario que pulsó Sincronizar.</summary>
    public int UsuarioId { get; set; }

    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public EstadoSincronizacion Estado { get; set; }

    /// <summary>URL del Moodle de donde vienen los datos.</summary>
    public string Origen { get; set; } = string.Empty;

    public int RegistrosUnidades { get; set; }
    public int RegistrosActividades { get; set; }
    public int RegistrosEstudiantes { get; set; }
    public int RegistrosCalificaciones { get; set; }

    public List<ErrorSincronizacion> Errores { get; set; } = new();

    /// <summary>
    /// TA-21.A1 · True cuando la sincronización guardó sus cambios frente a la anterior. Las hechas antes de existir el registro
    /// quedan en false: sus cambios no se conocen, que no es lo mismo que no haber cambios.
    /// </summary>
    public bool CambiosRegistrados { get; set; }
}

/// <summary>Función de Moodle que falló en una sincronización y su mensaje.</summary>
public sealed record ErrorSincronizacion(string Funcion, string Mensaje);
