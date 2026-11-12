using EduMonitor.Application.Indicadores;

namespace EduMonitor.Application.Alertas;

/// <summary>
/// R2 · Atrasos (TA-12.1): se activa cuando el estudiante tiene al menos «umbral» actividades con fecha límite
/// pasada y estado SIN_ENTREGA. Una entrega pendiente de calificar no es un atraso y una actividad sin fecha
/// límite no se puede considerar vencida.
/// Evidencia: las actividades vencidas con su fecha límite y el umbral.
/// </summary>
public sealed class ReglaR2Atrasos : IReglaAlerta
{
    public DefinicionRegla Definicion => DefinicionesRegla.R2;

    public AlertaDetectada? Evaluar(ContextoEstudiante contexto, decimal umbral, DateTimeOffset ahora)
    {
        var vencidas = contexto.SinEntregas
            .Where(a => a.FechaLimite <= ahora)
            .OrderBy(a => a.FechaLimite)
            .ToList();

        if (vencidas.Count < umbral)
        {
            return null;
        }

        return new AlertaDetectada(
            Definicion.Codigo,
            contexto.EstudianteId,
            EvidenciaJson.Serializar(new
            {
                actividades = vencidas
                    .Select(a => new { nombre = a.Nombre, fechaLimite = a.FechaLimite })
                    .ToList(),
                umbral = CalculadoraIndicadores.Redondear(umbral),
            }));
    }
}
