using EduMonitor.Application.Sincronizaciones;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Tests.Sincronizaciones;

/// <summary>TA-21.A1 · Tipo de cada celda que cambia entre dos sincronizaciones, con casos conocidos.</summary>
public class ComparadorCalificacionesTests
{
    private static EstadoCelda Calificada(decimal nota) => new(nota, EstadoCalificacion.CALIFICADO);
    private static readonly EstadoCelda SinEntrega = new(null, EstadoCalificacion.SIN_ENTREGA);
    private static readonly EstadoCelda Pendiente = new(null, EstadoCalificacion.PENDIENTE);
    private static readonly EstadoCelda SinDato = new(null, null);

    public static TheoryData<EstadoCelda?, EstadoCelda, string?> Casos => new()
    {
        { SinEntrega, Calificada(7m), ComparadorCalificaciones.NotaNueva },
        { Pendiente, Calificada(0m), ComparadorCalificaciones.NotaNueva }, // un 0 calificado también es nota nueva
        { null, Calificada(8.5m), ComparadorCalificaciones.NotaNueva },
        { Calificada(6.25m), Calificada(7m), ComparadorCalificaciones.NotaCambiada },
        { Calificada(5m), Calificada(5m), null },
        { SinEntrega, Pendiente, ComparadorCalificaciones.EntregaNueva },
        { null, Pendiente, ComparadorCalificaciones.EntregaNueva },
        { SinDato, Pendiente, ComparadorCalificaciones.EntregaNueva },
        { Pendiente, Pendiente, null },
        { SinDato, SinEntrega, null },
        { null, SinEntrega, null },
        { null, SinDato, null },
        { Calificada(9m), SinDato, ComparadorCalificaciones.Otro },
        { Pendiente, SinEntrega, ComparadorCalificaciones.Otro },
    };

    [Theory]
    [MemberData(nameof(Casos))]
    public void Clasifica_cada_cambio_de_celda(EstadoCelda? antes, EstadoCelda ahora, string? esperado) =>
        Assert.Equal(esperado, ComparadorCalificaciones.Clasificar(antes, ahora));

    [Fact]
    public void Compara_solo_las_celdas_que_existen_despues_y_omite_las_que_no_cambiaron()
    {
        var antes = new Dictionary<(int, int), EstadoCelda>
        {
            [(1, 10)] = Calificada(6m),
            [(1, 11)] = SinEntrega,
            [(2, 10)] = Calificada(9m), // el estudiante 10 ya no está en la actividad 2: no cuenta
        };
        var despues = new Dictionary<(int, int), EstadoCelda>
        {
            [(1, 10)] = Calificada(6m),
            [(1, 11)] = Calificada(4.5m),
            [(3, 10)] = SinEntrega, // actividad nueva sin entrega: no cuenta
        };

        var cambio = Assert.Single(ComparadorCalificaciones.Comparar(antes, despues));

        Assert.Equal((1, 11, ComparadorCalificaciones.NotaNueva), (cambio.ActividadId, cambio.EstudianteId, cambio.Tipo));
        Assert.Equal(SinEntrega, cambio.Antes);
        Assert.Equal(Calificada(4.5m), cambio.Ahora);
    }
}
