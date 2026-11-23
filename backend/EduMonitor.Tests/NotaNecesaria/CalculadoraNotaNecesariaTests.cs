using EduMonitor.Application.NotaNecesaria;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Tests.NotaNecesaria;

/// <summary>TA-10.A1 · Nota necesaria para llegar a 7,0 con 40/30/30, con casos calculados a mano.</summary>
public class CalculadoraNotaNecesariaTests
{
    private static readonly DateTimeOffset Hoy = new(2026, 11, 22, 10, 0, 0, TimeSpan.FromHours(-5));
    private static readonly Estudiante Ana = new() { Id = 1, Nombre = "Ana", Apellido = "Acosta" };

    private static Actividad A(int id, string nombre, string categoria, int dias) =>
        new() { Id = id, Nombre = nombre, Categoria = categoria, FechaLimite = Hoy.AddDays(dias) };

    private static Calificacion C(int actividad, decimal? nota, EstadoCalificacion estado) =>
        new() { ActividadId = actividad, EstudianteId = 1, Nota = nota, Estado = estado };

    [Fact]
    public void Calcula_la_nota_igual_en_todas_las_pendientes_para_llegar_a_7()
    {
        // Exámenes: 6 y uno pendiente. Tareas: 8 y 6, y una que aún no vence. APE: 7.
        // Final(x) = 0,4·(6 + x)/2 + 0,3·(14 + x)/3 + 0,3·7 = 4,7 + 0,3·x → x = 7,67; con 10 en todo, 7,70.
        var actividades = new[]
        {
            A(1, "Examen Parcial 1", "Exámenes", -40), A(2, "Examen Parcial 2", "Exámenes", 3),
            A(3, "Tarea 1", "Tareas", -50), A(4, "Tarea 2", "Tareas", -30), A(5, "Tarea 9", "Tareas", 6),
            A(6, "APE 1", "APE", -20),
        };
        var notas = new[]
        {
            C(1, 6m, EstadoCalificacion.CALIFICADO), C(2, null, EstadoCalificacion.SIN_ENTREGA),
            C(3, 8m, EstadoCalificacion.CALIFICADO), C(4, 6m, EstadoCalificacion.CALIFICADO), C(5, null, EstadoCalificacion.SIN_ENTREGA),
            C(6, 7m, EstadoCalificacion.CALIFICADO),
        };

        var r = CalculadoraNotaNecesaria.Calcular(1, Ana, actividades, notas, Hoy);

        Assert.Equal(CalculadoraNotaNecesaria.Necesita, r.Estado);
        Assert.Equal(7.67m, r.NotaNecesaria);
        Assert.Equal(7.70m, r.MaximoPosible);
        Assert.Equal(6.60m, r.PromedioActual); // 0,4·6 + 0,3·7 + 0,3·7, solo con las calificadas
        Assert.Equal(2, r.Pendientes);
        Assert.Equal(new[] { "Examen Parcial 2" }, r.Categorias[0].Pendientes.Select(p => p.Nombre));
        Assert.Equal("AUN_NO_VENCE", r.Categorias[1].Pendientes.Single().Estado);
    }

    [Fact]
    public void Si_con_cero_en_las_pendientes_ya_llega_dice_que_ya_alcanza()
    {
        var actividades = new[] { A(1, "Examen", "Exámenes", -10), A(2, "Tarea 1", "Tareas", -10), A(3, "Tarea 2", "Tareas", 5), A(4, "APE 1", "APE", -5) };
        var notas = new[]
        {
            C(1, 9m, EstadoCalificacion.CALIFICADO), C(2, 10m, EstadoCalificacion.CALIFICADO),
            C(3, null, EstadoCalificacion.SIN_ENTREGA), C(4, 9m, EstadoCalificacion.CALIFICADO),
        };

        var r = CalculadoraNotaNecesaria.Calcular(1, Ana, actividades, notas, Hoy);

        // Con 0 en la Tarea 2: 0,4·9 + 0,3·5 + 0,3·9 = 7,8.
        Assert.Equal(CalculadoraNotaNecesaria.YaAlcanza, r.Estado);
        Assert.Null(r.NotaNecesaria);
        Assert.Equal(9.30m, r.MaximoPosible);
    }

    [Fact]
    public void Si_ni_con_10_llega_dice_que_ya_no_alcanza_y_cuanto_es_lo_maximo()
    {
        var actividades = new[] { A(1, "Examen", "Exámenes", -10), A(2, "Tarea 1", "Tareas", -10), A(3, "Tarea 2", "Tareas", 5), A(4, "APE 1", "APE", -5) };
        var notas = new[]
        {
            C(1, 2m, EstadoCalificacion.CALIFICADO), C(2, 2m, EstadoCalificacion.CALIFICADO),
            C(3, null, EstadoCalificacion.PENDIENTE), C(4, 2m, EstadoCalificacion.CALIFICADO),
        };

        var r = CalculadoraNotaNecesaria.Calcular(1, Ana, actividades, notas, Hoy);

        // 0,4·2 + 0,3·(2 + x)/2 + 0,3·2 = 1,7 + 0,15·x → x = 35: no alcanza; lo máximo es 3,20.
        Assert.Equal(CalculadoraNotaNecesaria.NoAlcanza, r.Estado);
        Assert.Null(r.NotaNecesaria);
        Assert.Equal(3.20m, r.MaximoPosible);
        Assert.Equal("PENDIENTE", r.Categorias[1].Pendientes.Single().Estado); // por calificar también cuenta
    }

    [Fact]
    public void Una_vencida_sin_entrega_no_cuenta_como_pendiente_y_sin_pendientes_no_hay_nota_necesaria()
    {
        var actividades = new[] { A(1, "Examen", "Exámenes", -10), A(2, "Tarea 1", "Tareas", -10) };
        var notas = new[] { C(1, 5m, EstadoCalificacion.CALIFICADO), C(2, null, EstadoCalificacion.SIN_ENTREGA) };

        var r = CalculadoraNotaNecesaria.Calcular(1, Ana, actividades, notas, Hoy);

        Assert.Equal(CalculadoraNotaNecesaria.SinPendientes, r.Estado);
        Assert.Null(r.NotaNecesaria);
        Assert.Equal(5m, r.PromedioActual);
        Assert.True(r.Parcial);
        Assert.Equal(0, r.Pendientes);
    }

    [Fact]
    public void Sin_notas_ni_pendientes_no_hay_datos()
    {
        var r = CalculadoraNotaNecesaria.Calcular(1, Ana, new[] { A(1, "Tarea 1", "Tareas", -10) }, Array.Empty<Calificacion>(), Hoy);

        Assert.Equal(CalculadoraNotaNecesaria.SinDatos, r.Estado);
        Assert.Null(r.PromedioActual);
    }
}
