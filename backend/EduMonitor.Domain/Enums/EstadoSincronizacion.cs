namespace EduMonitor.Domain.Enums;

/// <summary>Resultado de una sincronización con Moodle.</summary>
public enum EstadoSincronizacion
{
    /// <summary>Todas las funciones respondieron.</summary>
    COMPLETADA = 1,

    /// <summary>Al menos una función falló y otras no. Lo afectado se muestra como no disponible.</summary>
    PARCIAL = 2,

    /// <summary>No se importó nada.</summary>
    FALLIDA = 3,
}
