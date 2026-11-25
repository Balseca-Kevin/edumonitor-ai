using EduMonitor.Application.Indicadores;
using EduMonitor.Application.Periodo;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Tests.Periodo;

/// <summary>
/// TA-22.A4 · Casos conocidos del estado del período. El curso empieza el martes 1 sep 2026 (02:00 en Ecuador, el startdate de
/// AGIL-4B en el aula virtual): la semana 1 va del 1 al 7 sep y la semana 12, del 17 al 23 nov. El domingo 22 nov 2026 a las 10:00
/// es la semana 12. Sin enddate, el período termina con la última fecha límite (11 dic, semana 15).
/// </summary>
public class CalculadoraPeriodoTests
{
    private static readonly TimeSpan Ecuador = TimeSpan.FromHours(-5);
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 2, 0, 0, Ecuador);
    private static readonly DateTimeOffset Ahora = new(2026, 11, 22, 10, 0, 0, Ecuador);

    private static DateTimeOffset Dia(int mes, int dia) => new(2026, mes, dia, 23, 59, 0, Ecuador);

    /// <summary>
    /// 4 unidades (la 5 sin actividades) y 6 actividades: T1 (U1, todos entregaron), T5 (U3, uno no entregó), APE 4 (U4, vencida el
    /// 20 nov), Examen Parcial 2 (U4, 25 nov: semana 13), Tarea 9 (U4, 28 nov) y Proyecto integrador (U4, 11 dic). Dos estudiantes.
    /// </summary>
    private static DatosCurso Curso(DateTimeOffset? venceExamen = null)
    {
        var unidades = new[]
        {
            new Unidad { Id = 1, Numero = 1, Parcial = 1, Nombre = "Principios fundamentales de las metodologías ágiles" },
            new Unidad { Id = 3, Numero = 3, Parcial = 2, Nombre = "Principales metodologías ágiles del mercado" },
            new Unidad { Id = 4, Numero = 4, Parcial = 2, Nombre = "Herramientas ágiles del mercado" },
            new Unidad { Id = 5, Numero = 5, Parcial = 2, Nombre = "Procesos transaccionales con bloques PL/SQL" },
        };
        var actividades = new[]
        {
            new Actividad { Id = 1, Nombre = "Tarea 1", Categoria = "Tareas", UnidadId = 1, MoodleItemId = 1, FechaLimite = Dia(9, 8) },
            new Actividad { Id = 2, Nombre = "Tarea 5", Categoria = "Tareas", UnidadId = 3, MoodleItemId = 2, FechaLimite = Dia(10, 14) },
            new Actividad { Id = 3, Nombre = "APE 4", Categoria = "APE", UnidadId = 4, MoodleItemId = 3, FechaLimite = Dia(11, 20) },
            new Actividad { Id = 4, Nombre = "Examen Parcial 2", Categoria = "Exámenes", UnidadId = 4, MoodleItemId = 4, FechaLimite = venceExamen ?? Dia(11, 25) },
            new Actividad { Id = 5, Nombre = "Tarea 9", Categoria = "Tareas", UnidadId = 4, MoodleItemId = 5, FechaLimite = Dia(11, 28) },
            new Actividad { Id = 6, Nombre = "Proyecto integrador", Categoria = "Tareas", UnidadId = 4, MoodleItemId = 6, FechaLimite = Dia(12, 11) },
        };
        var estudiantes = new[] { new Estudiante { Id = 1, Nombre = "Ana", Apellido = "Acosta" }, new Estudiante { Id = 2, Nombre = "Diego", Apellido = "Dávila" } };
        Calificacion Nota(int actividad, int estudiante, EstadoCalificacion estado) =>
            new() { ActividadId = actividad, EstudianteId = estudiante, Estado = estado, Nota = estado == EstadoCalificacion.CALIFICADO ? 8 : null };
        var calificaciones = new[]
        {
            Nota(1, 1, EstadoCalificacion.CALIFICADO), Nota(1, 2, EstadoCalificacion.PENDIENTE),
            Nota(2, 1, EstadoCalificacion.CALIFICADO), Nota(2, 2, EstadoCalificacion.SIN_ENTREGA),
            Nota(3, 1, EstadoCalificacion.SIN_ENTREGA), Nota(3, 2, EstadoCalificacion.SIN_ENTREGA),
            Nota(4, 1, EstadoCalificacion.SIN_ENTREGA), Nota(5, 1, EstadoCalificacion.SIN_ENTREGA),
        };
        return new DatosCurso(unidades, actividades, estudiantes, calificaciones);
    }

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<string>> SinTemas = new Dictionary<int, IReadOnlyList<string>>();

    [Theory]
    [InlineData(9, 1, 1)]   // martes, primer día
    [InlineData(9, 7, 1)]   // lunes siguiente: todavía semana 1
    [InlineData(9, 8, 2)]
    [InlineData(11, 22, 12)]
    [InlineData(11, 23, 12)]
    [InlineData(11, 24, 13)]
    [InlineData(12, 11, 15)]
    public void La_semana_cuenta_desde_el_dia_de_inicio_en_fechas_de_Ecuador(int mes, int dia, int semana) =>
        Assert.Equal(semana, CalculadoraPeriodo.NumeroSemana(Inicio, new DateTimeOffset(2026, mes, dia, 12, 0, 0, Ecuador)));

    [Fact]
    public void Una_fecha_limite_de_las_23_59_de_Ecuador_no_cambia_de_dia_aunque_llegue_en_UTC()
    {
        var enUtc = Dia(9, 7).ToUniversalTime(); // 8 sep 04:59 UTC
        Assert.Equal(1, CalculadoraPeriodo.NumeroSemana(Inicio, enUtc));
    }

    [Theory]
    [InlineData("Tarea 1", "T1")]
    [InlineData("Taller 2", "TL2")]
    [InlineData("APE 3", "APE3")]
    [InlineData("Examen Parcial 1", "EX P1")]
    [InlineData("Examen P2", "EX P2")]
    [InlineData("Proyecto integrador", "PI")]
    public void La_sigla_de_cada_actividad(string nombre, string sigla) => Assert.Equal(sigla, CalculadoraPeriodo.Sigla(nombre));

    [Fact]
    public void Semana_estado_y_unidad_actual_con_las_fechas_del_aula_virtual()
    {
        var p = CalculadoraPeriodo.Calcular(1, Curso(), Inicio, null, Ahora, SinTemas);

        Assert.Equal(new SemanaDto(12, 15), p.Semana);
        Assert.Equal("EN_CURSO", p.Estado);
        Assert.Equal("ULTIMA_FECHA_LIMITE", p.FinOrigen);
        Assert.Equal(Dia(12, 11), p.Fin);
        Assert.Equal(new UnidadActualDto(4, "Herramientas ágiles del mercado", 2), p.UnidadActual);
        Assert.Empty(p.NoDisponibles);
        Assert.Equal(Ecuador, p.Hoy.Offset);
    }

    [Fact]
    public void Cada_actividad_lleva_su_semana_su_estado_y_quienes_entregaron()
    {
        var p = CalculadoraPeriodo.Calcular(1, Curso(), Inicio, null, Ahora, SinTemas);
        var a = p.Actividades.ToDictionary(x => x.Nombre);

        Assert.Equal((2, CalculadoraPeriodo.Vencida, 2, 0), (a["Tarea 1"].Semana, a["Tarea 1"].Estado, a["Tarea 1"].Entregaron, a["Tarea 1"].Faltan));
        Assert.Equal((CalculadoraPeriodo.Vencida, 1, 1), (a["Tarea 5"].Estado, a["Tarea 5"].Entregaron, a["Tarea 5"].Faltan));
        Assert.Equal((12, CalculadoraPeriodo.Vencida, 2), (a["APE 4"].Semana, a["APE 4"].Estado, a["APE 4"].Faltan));
        Assert.Equal((13, CalculadoraPeriodo.Proxima, 0), (a["Examen Parcial 2"].Semana, a["Examen Parcial 2"].Estado, a["Examen Parcial 2"].Faltan));
        Assert.Equal("EX P2", a["Examen Parcial 2"].Sigla);
        Assert.All(p.Actividades, x => Assert.Equal(2, x.Matriculados));
    }

    [Fact]
    public void Bloques_vencidas_vence_esta_semana_y_siguiente()
    {
        var temas = new Dictionary<int, IReadOnlyList<string>> { [4] = new[] { "1.3 Manifiesto Ágil", "4.1 Herramientas para Scrum" } };
        var p = CalculadoraPeriodo.Calcular(1, Curso(), Inicio, null, Ahora, temas);

        Assert.Equal(3, p.Vencidas.Actividades);
        Assert.Equal(3, p.Vencidas.EntregasQueFaltan);
        Assert.Equal(new[] { ("Tarea 5", 1), ("APE 4", 2) }, p.Vencidas.ConFaltas.Select(v => (v.Nombre, v.Faltan)));
        Assert.Empty(p.VenceEstaSemana); // el examen vence el miércoles 25: semana 13
        Assert.Equal("Examen Parcial 2", p.Siguiente!.Nombre);
        Assert.Equal(13, p.Siguiente.Semana);
        Assert.Equal(new[] { "1.3 Manifiesto Ágil", "4.1 Herramientas para Scrum" }, p.Siguiente.Temas);
    }

    [Fact]
    public void Una_actividad_que_vence_en_la_semana_actual_y_todavia_no_pasa_va_en_vence_esta_semana()
    {
        var p = CalculadoraPeriodo.Calcular(1, Curso(venceExamen: Dia(11, 23)), Inicio, null, Ahora, SinTemas);

        var estaSemana = Assert.Single(p.VenceEstaSemana);
        Assert.Equal(("Examen Parcial 2", 0, 2), (estaSemana.Nombre, estaSemana.Entregaron, estaSemana.Matriculados));
        Assert.Equal("Tarea 9", p.Siguiente!.Nombre);
    }

    [Fact]
    public void Las_unidades_van_de_la_semana_siguiente_a_la_anterior_hasta_su_ultima_fecha_limite()
    {
        var p = CalculadoraPeriodo.Calcular(1, Curso(), Inicio, null, Ahora, SinTemas);

        Assert.Equal(new[]
        {
            new UnidadPeriodoDto(1, "Principios fundamentales de las metodologías ágiles", 1, 1, 2),
            new UnidadPeriodoDto(3, "Principales metodologías ágiles del mercado", 2, 3, 7),
            new UnidadPeriodoDto(4, "Herramientas ágiles del mercado", 2, 8, 15),
            new UnidadPeriodoDto(5, "Procesos transaccionales con bloques PL/SQL", 2, null, null), // sin actividades: sin semanas, nunca 0
        }, p.Unidades);
    }

    [Fact]
    public void Con_el_fin_de_Moodle_el_total_sale_de_esa_fecha_y_despues_del_fin_el_periodo_termino()
    {
        var finMoodle = new DateTimeOffset(2026, 12, 18, 23, 59, 0, Ecuador);
        var p = CalculadoraPeriodo.Calcular(1, Curso(), Inicio, finMoodle, new DateTimeOffset(2027, 1, 10, 9, 0, 0, Ecuador), SinTemas);

        Assert.Equal("MOODLE", p.FinOrigen);
        Assert.Equal(new SemanaDto(16, 16), p.Semana); // acotada al total
        Assert.Equal("TERMINADO", p.Estado);
        Assert.Null(p.Siguiente);
        Assert.Equal(4, p.UnidadActual!.Numero); // todas vencieron: la última con actividades
    }

    [Fact]
    public void Sin_fecha_de_inicio_no_hay_semanas_pero_los_estados_siguen()
    {
        var p = CalculadoraPeriodo.Calcular(1, Curso(), null, null, Ahora, SinTemas);

        Assert.Null(p.Semana);
        Assert.Null(p.Estado);
        Assert.Equal(new[] { CalculadoraPeriodo.NoDisponibleInicio }, p.NoDisponibles);
        Assert.All(p.Actividades, a => Assert.Null(a.Semana));
        Assert.All(p.Unidades, u => Assert.Null(u.SemanaHasta));
        Assert.Equal(3, p.Vencidas.Actividades);
        Assert.Equal(new[] { "Examen Parcial 2", "Tarea 9" }, p.VenceEstaSemana.Select(v => v.Nombre)); // vencen del 22 al 28 nov: los próximos 7 días
    }
}
