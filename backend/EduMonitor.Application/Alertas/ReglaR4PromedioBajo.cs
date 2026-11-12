using EduMonitor.Application.Indicadores;

namespace EduMonitor.Application.Alertas;

/// <summary>
/// R4 · Promedio bajo (TA-12.1): se activa cuando el promedio ponderado UTA 40/30/30 del estudiante es menor
/// que el umbral y tiene al menos <see cref="NotasMinimas"/> notas calificadas. Se compara el promedio redondeado
/// a 2 decimales, el mismo que muestra el panel. Con menos de 4 notas el promedio no es representativo,
/// por eso R4 pide ese mínimo antes de decir que el promedio es bajo.
/// Evidencia: el promedio, la cantidad de notas y el umbral.
/// </summary>
public sealed class ReglaR4PromedioBajo : IReglaAlerta
{
    /// <summary>Notas mínimas para que R4 concluya que el promedio es bajo.</summary>
    public const int NotasMinimas = 4;

    public DefinicionRegla Definicion => DefinicionesRegla.R4;

    public AlertaDetectada? Evaluar(ContextoEstudiante contexto, decimal umbral, DateTimeOffset ahora)
    {
        if (contexto.NotasCalificadas < NotasMinimas)
        {
            return null;
        }

        var (promedio, _) = CalculadoraIndicadores.PromedioPonderado(contexto.NotasConCategoria);
        var redondeado = CalculadoraIndicadores.Redondear(promedio);
        if (redondeado is null || redondeado >= umbral)
        {
            return null;
        }

        return new AlertaDetectada(
            Definicion.Codigo,
            contexto.EstudianteId,
            EvidenciaJson.Serializar(new
            {
                promedio = redondeado,
                notas = contexto.NotasCalificadas,
                umbral = CalculadoraIndicadores.Redondear(umbral),
            }));
    }
}
