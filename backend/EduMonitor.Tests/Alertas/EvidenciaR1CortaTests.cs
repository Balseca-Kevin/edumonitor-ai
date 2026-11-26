using System.Text.Json;
using EduMonitor.Application.Herramientas;

namespace EduMonitor.Tests.Alertas;

/// <summary>
/// C7 · DADO un descenso reciente guardado en orden cronológico (anteriores 7,5 y 8,0; últimas 5,0, 4,5 y 4,0), CUANDO la herramienta
/// lo resume para el asistente, ENTONCES las últimas notas van de la más reciente a la más antigua, con el promedio de las últimas y
/// el de las anteriores, para que el texto diga lo mismo que la tarjeta.
/// </summary>
public class EvidenciaR1CortaTests
{
    [Fact]
    public void Las_ultimas_notas_van_de_la_mas_reciente_a_la_mas_antigua_con_el_promedio_anterior()
    {
        var guardada = JsonDocument.Parse("""{ "ultimas": [5.0, 4.5, 4.0], "anteriores": [7.5, 8.0], "diferencia": -3.25, "umbral": 1.5 }""").RootElement;

        var corta = JsonSerializer.SerializeToElement(HerramientasService.EvidenciaR1Corta(guardada));

        Assert.Equal(new[] { 4.0m, 4.5m, 5.0m }, corta.GetProperty("ultimasNotas").EnumerateArray().Select(v => v.GetDecimal()));
        Assert.Equal(4.5m, corta.GetProperty("promedioUltimas").GetDecimal());
        Assert.Equal(7.75m, corta.GetProperty("promedioAnterior").GetDecimal());
        Assert.Equal(-3.25m, corta.GetProperty("diferencia").GetDecimal());
        Assert.Equal(1.5m, corta.GetProperty("umbral").GetDecimal());
    }

    [Fact]
    public void Sin_notas_anteriores_el_promedio_anterior_va_en_null_nunca_0()
    {
        var guardada = JsonDocument.Parse("""{ "ultimas": [5.0, 4.0], "diferencia": -2, "umbral": 1.5 }""").RootElement;

        var corta = JsonSerializer.SerializeToElement(HerramientasService.EvidenciaR1Corta(guardada));

        Assert.Equal(JsonValueKind.Null, corta.GetProperty("promedioAnterior").ValueKind);
    }
}
