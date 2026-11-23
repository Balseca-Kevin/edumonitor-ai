using EduMonitor.Application.Tematicas;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Tests.Tematicas;

/// <summary>
/// HU-08 · Desempeño por tema con casos conocidos. Tema «Historias de usuario»: en la Tarea 1 lo evalúa un criterio de rúbrica de
/// 2,5 puntos; en la Tarea 2 la nota de la tarea; la Tarea 3 vence después de hoy; la Tarea 4 tiene nota pero ningún tema.
/// </summary>
public class CalculadoraTematicaTests
{
    private static readonly DateTimeOffset Hoy = new(2026, 11, 22, 10, 0, 0, TimeSpan.FromHours(-5));

    private static Estudiante E(int id, string nombre, string apellido) => new() { Id = id, Nombre = nombre, Apellido = apellido };

    private static Calificacion C(int actividad, int estudiante, decimal? nota, EstadoCalificacion estado) =>
        new() { ActividadId = actividad, EstudianteId = estudiante, Nota = nota, Estado = estado };

    private static DatosTematica Datos()
    {
        var actividades = new[]
        {
            new Actividad { Id = 1, Nombre = "Tarea 1", FechaLimite = Hoy.AddDays(-60) },
            new Actividad { Id = 2, Nombre = "Tarea 2", FechaLimite = Hoy.AddDays(-30) },
            new Actividad { Id = 3, Nombre = "Tarea 3", FechaLimite = Hoy.AddDays(6) },
            new Actividad { Id = 4, Nombre = "Tarea 4", FechaLimite = Hoy.AddDays(-10) },
        };
        var estudiantes = new[] { E(10, "Ana", "Acosta"), E(11, "Diego", "Díaz"), E(12, "Elena", "Espinoza"), E(13, "Luis", "Mora") };
        return new DatosTematica(
            new[] { new TemaCalculo(100, "Historias de usuario", 1, "Principios fundamentales de las metodologías ágiles"), new TemaCalculo(101, "Sin usar", 1, "U1") },
            actividades,
            new Dictionary<int, IReadOnlyCollection<int>> { [1] = new[] { 100 }, [2] = new[] { 100 }, [3] = new[] { 100 } },
            new[]
            {
                new CriterioCalculo(500, 1, "Redacción de historias de usuario", 2.5m, 100),
                new CriterioCalculo(501, 1, "Formato y entrega", 2.5m, null),
            },
            new[]
            {
                new NivelCalculo(500, 10, 2.5m), new NivelCalculo(501, 10, 0m),
                new NivelCalculo(500, 11, 1m), new NivelCalculo(501, 11, 2.5m),
            },
            estudiantes,
            new[]
            {
                C(1, 10, 2.5m, EstadoCalificacion.CALIFICADO), C(2, 10, 8m, EstadoCalificacion.CALIFICADO),
                C(1, 11, 3.5m, EstadoCalificacion.CALIFICADO), C(2, 11, 5m, EstadoCalificacion.CALIFICADO),
                C(1, 12, null, EstadoCalificacion.SIN_ENTREGA), C(2, 12, 7m, EstadoCalificacion.CALIFICADO),
                C(1, 13, null, EstadoCalificacion.PENDIENTE), C(2, 13, null, EstadoCalificacion.SIN_ENTREGA),
                C(4, 10, 9m, EstadoCalificacion.CALIFICADO),
            },
            Hoy);
    }

    [Fact]
    public void Calcula_la_nota_del_tema_por_criterios_o_por_la_nota_y_sin_inventar_las_que_faltan()
    {
        var tema = Assert.Single(CalculadoraTematica.Calcular(1, Datos(), null).Temas);

        // Ana: criterio 2,5/2,5 → 10 y Tarea 2 → 8: 9. Diego: 1/2,5 → 4 y 5: 4,5. Elena: sin entrega en la Tarea 1, 7 en la Tarea 2.
        // Luis no tiene ninguna nota en el tema: no cuenta, ni como 0.
        Assert.Equal(new[] { ("Díaz", 4.5m), ("Espinoza", 7m), ("Acosta", 9m) }, tema.Estudiantes.Select(e => (e.Apellido, e.Nota)));
        Assert.Equal(new[] { 10m, 8m }, tema.Estudiantes.Single(e => e.Id == 10).Notas.Select(n => n.Nota));
        Assert.Equal(3, tema.EstudiantesConNota);
        Assert.Equal(6.83m, tema.Promedio);
        Assert.Equal(0.6667m, tema.Domina);
        Assert.Equal(new[] { (11, 4.5m) }, tema.Bajo6.Select(b => (b.Id, b.Nota)));
    }

    [Fact]
    public void Dice_la_fuente_de_cada_evaluacion_donde_se_vuelve_a_evaluar_y_que_actividades_no_tienen_tema()
    {
        var resultado = CalculadoraTematica.Calcular(1, Datos(), null);
        var tema = Assert.Single(resultado.Temas);

        Assert.Equal(
            new[] { ("Tarea 1", CalculadoraTematica.FuenteCriterios), ("Tarea 2", CalculadoraTematica.FuenteNota), ("Tarea 3", CalculadoraTematica.FuenteNota) },
            tema.Evaluaciones.Select(e => (e.Actividad, e.Fuente)));
        Assert.Equal(new[] { "Redacción de historias de usuario" }, tema.Evaluaciones[0].Criterios);
        Assert.Equal(new[] { "Tarea 3" }, tema.Proximas.Select(p => p.Nombre));
        Assert.Equal(new[] { "Tarea 4" }, resultado.SinTemas.Select(s => s.Nombre));
    }

    [Fact]
    public void Con_una_actividad_solo_cuentan_sus_notas()
    {
        var tema = Assert.Single(CalculadoraTematica.Calcular(1, Datos(), 1).Temas);

        Assert.Equal(new[] { ("Díaz", 4m), ("Acosta", 10m) }, tema.Estudiantes.Select(e => (e.Apellido, e.Nota)));
        Assert.Equal(0.5m, tema.Domina);
        Assert.Equal(7m, tema.Promedio);
        Assert.Single(tema.Evaluaciones);
    }

    [Fact]
    public void Un_tema_sin_notas_va_con_promedio_y_dominio_nulos()
    {
        var tema = Assert.Single(CalculadoraTematica.Calcular(1, Datos(), 3).Temas);

        Assert.Equal(0, tema.EstudiantesConNota);
        Assert.Null(tema.Promedio);
        Assert.Null(tema.Domina);
        Assert.Empty(tema.Bajo6);
    }
}
