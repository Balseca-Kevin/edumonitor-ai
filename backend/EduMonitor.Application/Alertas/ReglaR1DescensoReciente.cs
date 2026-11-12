using EduMonitor.Application.Indicadores;

namespace EduMonitor.Application.Alertas;

/// <summary>
/// R1 · Descenso reciente (TA-12.1): se activa cuando la tendencia —promedio de las últimas 3 notas calificadas
/// menos el promedio de las anteriores— es menor o igual a −umbral. Se compara el valor redondeado a 2 decimales,
/// el mismo que muestra el panel, para que lo que ve el docente y lo que dispara la alerta coincidan.
/// Sin al menos 4 notas no hay tendencia y la regla no se activa.
/// Evidencia: las últimas notas, las anteriores, la diferencia y el umbral.
/// </summary>
public sealed class ReglaR1DescensoReciente : IReglaAlerta
{
    public DefinicionRegla Definicion => DefinicionesRegla.R1;

    public AlertaDetectada? Evaluar(ContextoEstudiante contexto, decimal umbral, DateTimeOffset ahora)
    {
        var tendencia = CalculadoraIndicadores.Tendencia(contexto.NotasOrdenadas);
        if (tendencia is null || CalculadoraIndicadores.Redondear(tendencia) > -umbral)
        {
            return null;
        }

        static decimal Redondear(decimal nota) => CalculadoraIndicadores.Redondear(nota)!.Value;

        var n = CalculadoraIndicadores.NTendencia;
        var ultimas = contexto.NotasOrdenadas.Skip(contexto.NotasOrdenadas.Count - n).Select(Redondear).ToList();
        var anteriores = contexto.NotasOrdenadas.Take(contexto.NotasOrdenadas.Count - n).Select(Redondear).ToList();

        return new AlertaDetectada(
            Definicion.Codigo,
            contexto.EstudianteId,
            EvidenciaJson.Serializar(new
            {
                ultimas,
                anteriores,
                diferencia = CalculadoraIndicadores.Redondear(tendencia),
                umbral = CalculadoraIndicadores.Redondear(umbral),
            }));
    }
}
