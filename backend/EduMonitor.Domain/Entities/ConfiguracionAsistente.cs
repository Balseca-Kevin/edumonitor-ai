namespace EduMonitor.Domain.Entities;

/// <summary>HU-23 · Configuración del asistente (una fila): el interruptor «Usar solo el modelo local» de «Estado del sistema».</summary>
public class ConfiguracionAsistente
{
    public int Id { get; set; }

    public bool SoloModeloLocal { get; set; }

    public DateTimeOffset Actualizado { get; set; }
}
