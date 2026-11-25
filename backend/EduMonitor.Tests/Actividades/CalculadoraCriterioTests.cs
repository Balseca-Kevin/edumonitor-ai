using EduMonitor.Application.Actividades;

namespace EduMonitor.Tests.Actividades;

/// <summary>TA-22.A1 · Separación y aviso por criterio con casos calculados a mano.</summary>
public class CalculadoraCriterioTests
{
    [Fact]
    public void Pearson_de_un_caso_conocido_da_0_7746()
    {
        // Medias 3 y 4; Σdx·dy = 6, Σdx² = 10, Σdy² = 6 → 6 / √60 = 0,7746.
        Assert.Equal(0.7746m, CalculadoraCriterio.Pearson(new[] { 1m, 2m, 3m, 4m, 5m }, new[] { 2m, 4m, 5m, 4m, 5m }));
    }

    [Fact]
    public void Pearson_da_1_con_series_identicas_y_menos_1_con_series_invertidas()
    {
        var x = new[] { 1m, 2m, 3m, 4m, 5m };
        Assert.Equal(1m, CalculadoraCriterio.Pearson(x, x));
        Assert.Equal(-1m, CalculadoraCriterio.Pearson(x, x.Reverse().ToArray()));
    }

    [Fact]
    public void Pearson_es_null_con_una_serie_constante()
    {
        Assert.Null(CalculadoraCriterio.Pearson(new[] { 1m, 2m, 3m, 4m, 5m }, new[] { 2.5m, 2.5m, 2.5m, 2.5m, 2.5m }));
    }

    [Fact]
    public void Separacion_es_null_con_menos_de_8_estudiantes_o_con_un_solo_criterio()
    {
        var siete = Enumerable.Range(1, 7).Select(i => ((decimal)i, (decimal)i)).ToList();
        var ocho = Enumerable.Range(1, 8).Select(i => ((decimal)i, (decimal)i)).ToList();

        Assert.Null(CalculadoraCriterio.Separacion(siete, 4));
        Assert.Null(CalculadoraCriterio.Separacion(ocho, 1));
        Assert.Equal(1m, CalculadoraCriterio.Separacion(ocho, 4));
    }

    [Fact]
    public void Separacion_es_null_si_todos_sacaron_lo_mismo_en_el_criterio()
    {
        var pares = Enumerable.Range(1, 10).Select(i => (2.5m, (decimal)i)).ToList();
        Assert.Null(CalculadoraCriterio.Separacion(pares, 4));
    }

    [Fact]
    public void Aviso_de_ensenanza_con_dominio_bajo_aunque_la_separacion_sea_alta()
    {
        Assert.Equal(CalculadoraCriterio.RevisarEnsenanza, CalculadoraCriterio.Aviso(0.25m, 0.9m));
        Assert.Equal(CalculadoraCriterio.RevisarEnsenanza, CalculadoraCriterio.Aviso(0.30m, null)); // el umbral entra
    }

    [Fact]
    public void Aviso_de_enunciado_con_separacion_baja_y_ninguno_en_los_demas_casos()
    {
        Assert.Equal(CalculadoraCriterio.RevisarEnunciado, CalculadoraCriterio.Aviso(0.8m, 0.20m)); // el umbral entra
        Assert.Equal(CalculadoraCriterio.RevisarEnunciado, CalculadoraCriterio.Aviso(0.8m, -0.3m));
        Assert.Null(CalculadoraCriterio.Aviso(0.8m, 0.21m));
        Assert.Null(CalculadoraCriterio.Aviso(0.8m, null));
        Assert.Null(CalculadoraCriterio.Aviso(null, 0.1m));
    }

    [Fact]
    public void Criterio_suma_la_separacion_y_el_aviso()
    {
        var criterio = CalculadoraActividad.Criterio(1, 2, "Comparación de enfoques", 2.5m, null, new[] { 2.5m, 2.5m, 2.5m, 1m }, 0.1m);
        var sinNiveles = CalculadoraActividad.Criterio(2, 3, "Formato", 2.5m, null, Array.Empty<decimal>(), 0.1m);

        Assert.Equal(0.1m, criterio.Separacion);
        Assert.Equal(CalculadoraCriterio.RevisarEnunciado, criterio.Aviso);
        Assert.Null(sinNiveles.Separacion);
        Assert.Null(sinNiveles.Aviso);
    }
}
