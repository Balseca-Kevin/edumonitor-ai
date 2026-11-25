using EduMonitor.Application.Refuerzos;

namespace EduMonitor.Tests.Refuerzos;

/// <summary>
/// TA-15.B3 · Elección de los ejemplos de estilo del docente: solo los planes donde el docente cambió el borrador, los 2 más recientes,
/// y ninguno si hay menos de 2.
/// </summary>
public class EjemplosEstiloTests
{
    private static EjemploEstilo Plan(int id, string generado, string aprobado) => new(id, 1, generado, aprobado);

    [Fact]
    public void Con_un_solo_plan_editado_no_hay_ejemplos()
    {
        Assert.Empty(EjemplosEstilo.Elegir(new[] { Plan(1, "Borrador", "Borrador corregido") }));
    }

    [Fact]
    public void Un_plan_aprobado_tal_cual_no_cuenta_aunque_cambien_los_espacios()
    {
        var elegidos = EjemplosEstilo.Elegir(new[]
        {
            Plan(1, "Repasa  el tema.\n", "Repasa el tema."),
            Plan(2, "Borrador", "Borrador corregido"),
            Plan(3, "   ", "Texto"),
        });

        Assert.Empty(elegidos);
    }

    [Fact]
    public void Elige_los_dos_planes_editados_mas_recientes()
    {
        var elegidos = EjemplosEstilo.Elegir(new[]
        {
            Plan(4, "A", "A corregido"),
            Plan(9, "B", "B corregido"),
            Plan(7, "C", "C corregido"),
            Plan(12, "D", "D"),
        });

        Assert.Equal(new[] { 9, 7 }, elegidos.Select(e => e.PlanId));
    }

    [Fact]
    public void Recortar_deja_el_texto_corto_igual_y_corta_el_largo_en_un_espacio()
    {
        Assert.Equal("Repasa el tema.", EjemplosEstilo.Recortar("  Repasa el tema.  "));

        var largo = string.Join(' ', Enumerable.Repeat("palabra", 400));
        var recortado = EjemplosEstilo.Recortar(largo);

        Assert.True(recortado.Length <= EjemplosEstilo.LargoMaximo + 2);
        Assert.EndsWith("palabra …", recortado);
    }
}
