using System.Text.Json;
using EduMonitor.Application.Alertas;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Tests.Alertas;

/// <summary>
/// TA-12.1 y TA-12.5 · Reglas R1, R2 y R4: por cada una un caso que la activa y uno que no, la forma de la evidencia
/// del contrato (docs/contratos-api.md) y el motor que evalúa las reglas activas de un curso.
/// </summary>
public class ReglasAlertaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private static ContextoEstudiante Contexto(
        IReadOnlyList<decimal>? notas = null,
        IReadOnlyList<(string? Categoria, decimal Nota)>? notasConCategoria = null,
        IReadOnlyList<ActividadSinEntrega>? sinEntregas = null)
    {
        notas ??= new List<decimal>();
        return new ContextoEstudiante(
            EstudianteId: 7,
            NotasOrdenadas: notas,
            NotasConCategoria: notasConCategoria ?? notas.Select(n => ((string?)"Tareas", n)).ToList(),
            SinEntregas: sinEntregas ?? new List<ActividadSinEntrega>(),
            NotasCalificadas: notas.Count);
    }

    private static JsonDocument Evidencia(AlertaDetectada detectada) => JsonDocument.Parse(detectada.EvidenciaJson);

    // --- R1 · Descenso reciente ---

    [Fact]
    public void R1_se_activa_cuando_la_tendencia_es_menor_o_igual_al_umbral_negativo()
    {
        // Últimas 3: 6, 5, 4 → 5,00 · Anteriores: 9, 9, 9 → 9,00 · tendencia −4,00 ≤ −1,50.
        var regla = new ReglaR1DescensoReciente();
        var contexto = Contexto(new List<decimal> { 9m, 9m, 9m, 6m, 5m, 4m });

        var detectada = regla.Evaluar(contexto, DefinicionesRegla.R1.UmbralPorDefecto, Ahora);

        Assert.NotNull(detectada);
        Assert.Equal("R1", detectada.CodigoRegla);
        Assert.Equal(7, detectada.EstudianteId);

        using var evidencia = Evidencia(detectada);
        var raiz = evidencia.RootElement;
        Assert.Equal(new[] { 6m, 5m, 4m }, raiz.GetProperty("ultimas").EnumerateArray().Select(v => v.GetDecimal()));
        Assert.Equal(new[] { 9m, 9m, 9m }, raiz.GetProperty("anteriores").EnumerateArray().Select(v => v.GetDecimal()));
        Assert.Equal(-4m, raiz.GetProperty("diferencia").GetDecimal());
        Assert.Equal(1.5m, raiz.GetProperty("umbral").GetDecimal());
    }

    [Fact]
    public void R1_no_se_activa_con_tendencia_dentro_del_umbral_o_sin_suficientes_notas()
    {
        var regla = new ReglaR1DescensoReciente();

        // Tendencia 0,00 > −1,50: notas planas, sin descenso.
        Assert.Null(regla.Evaluar(Contexto(new List<decimal> { 7m, 7m, 7m, 7m }), 1.5m, Ahora));

        // Con 4 notas la tendencia es exactamente −1,50 y el umbral es inclusive (≤ −umbral).
        Assert.NotNull(regla.Evaluar(Contexto(new List<decimal> { 9m, 8m, 8m, 6.5m }), 1.5m, Ahora));

        // Con menos de 4 notas no hay tendencia: la regla queda callada.
        Assert.Null(regla.Evaluar(Contexto(new List<decimal> { 9m, 3m, 3m }), 1.5m, Ahora));
    }

    // --- R2 · Atrasos ---

    [Fact]
    public void R2_se_activa_con_al_menos_umbral_actividades_vencidas_sin_entrega()
    {
        var regla = new ReglaR2Atrasos();
        var contexto = Contexto(sinEntregas: new List<ActividadSinEntrega>
        {
            new("Tarea 1", new DateTimeOffset(2026, 10, 1, 23, 59, 0, TimeSpan.Zero)),
            new("Tarea 2", new DateTimeOffset(2026, 10, 15, 23, 59, 0, TimeSpan.Zero)),
        });

        var detectada = regla.Evaluar(contexto, DefinicionesRegla.R2.UmbralPorDefecto, Ahora);

        Assert.NotNull(detectada);
        Assert.Equal("R2", detectada.CodigoRegla);

        using var evidencia = Evidencia(detectada);
        var actividades = evidencia.RootElement.GetProperty("actividades");
        Assert.Equal(2, actividades.GetArrayLength());
        Assert.Equal("Tarea 1", actividades[0].GetProperty("nombre").GetString());
        Assert.Equal("2026-10-01T23:59:00+00:00", actividades[0].GetProperty("fechaLimite").GetString());
        Assert.Equal(2m, evidencia.RootElement.GetProperty("umbral").GetDecimal());
    }

    [Fact]
    public void R2_no_se_activa_con_menos_de_umbral_vencidas_o_con_entregas_aun_no_vencidas()
    {
        var regla = new ReglaR2Atrasos();

        // Solo 1 vencida con umbral 2.
        var unaVencida = Contexto(sinEntregas: new List<ActividadSinEntrega>
        {
            new("Tarea 1", new DateTimeOffset(2026, 10, 1, 23, 59, 0, TimeSpan.Zero)),
        });
        Assert.Null(regla.Evaluar(unaVencida, 2m, Ahora));

        // Dos sin entrega, pero una todavía no vence: solo cuenta 1.
        var unaPorVencer = Contexto(sinEntregas: new List<ActividadSinEntrega>
        {
            new("Tarea 1", new DateTimeOffset(2026, 10, 1, 23, 59, 0, TimeSpan.Zero)),
            new("Tarea 2", new DateTimeOffset(2026, 12, 1, 23, 59, 0, TimeSpan.Zero)),
        });
        Assert.Null(regla.Evaluar(unaPorVencer, 2m, Ahora));

        // Sin fecha límite no se puede considerar vencida.
        var sinFecha = Contexto(sinEntregas: new List<ActividadSinEntrega> { new("Tarea sin fecha", null) });
        Assert.Null(regla.Evaluar(sinFecha, 1m, Ahora));
    }

    // --- R4 · Promedio bajo ---

    [Fact]
    public void R4_se_activa_con_promedio_ponderado_menor_al_umbral_y_al_menos_cuatro_notas()
    {
        var regla = new ReglaR4PromedioBajo();
        // Cuatro notas de 6 en Tareas: el promedio ponderado (peso único) es 6,00 < 7,0.
        var notas = new List<decimal> { 6m, 6m, 6m, 6m };
        var contexto = Contexto(notas, notas.Select(n => ((string?)"Tareas", n)).ToList());

        var detectada = regla.Evaluar(contexto, DefinicionesRegla.R4.UmbralPorDefecto, Ahora);

        Assert.NotNull(detectada);
        Assert.Equal("R4", detectada.CodigoRegla);

        using var evidencia = Evidencia(detectada);
        Assert.Equal(6m, evidencia.RootElement.GetProperty("promedio").GetDecimal());
        Assert.Equal(4, evidencia.RootElement.GetProperty("notas").GetInt32());
        Assert.Equal(7m, evidencia.RootElement.GetProperty("umbral").GetDecimal());
    }

    [Fact]
    public void R4_no_se_activa_con_promedio_suficiente_o_con_menos_de_cuatro_notas()
    {
        var regla = new ReglaR4PromedioBajo();

        // Promedio 8,00 ≥ 7,0.
        var suficientes = new List<decimal> { 8m, 8m, 8m, 8m };
        Assert.Null(regla.Evaluar(Contexto(suficientes, suficientes.Select(n => ((string?)"Tareas", n)).ToList()), 7m, Ahora));

        // Promedio 6,995 redondeado a 2 decimales es 7,00: se compara el valor que ve el panel, no activa.
        var enLimite = new List<decimal> { 7m, 7m, 7m, 6.98m };
        Assert.Null(regla.Evaluar(Contexto(enLimite, enLimite.Select(n => ((string?)"Tareas", n)).ToList()), 7m, Ahora));

        // Tres notas bajas no alcanzan las 4 notas mínimas.
        var pocas = new List<decimal> { 5m, 5m, 5m };
        Assert.Null(regla.Evaluar(Contexto(pocas, pocas.Select(n => ((string?)"Tareas", n)).ToList()), 7m, Ahora));
    }

    // --- Motor ---

    [Fact]
    public void Motor_evalua_solo_las_reglas_recibidas_y_por_cada_estudiante()
    {
        var datos = DatosDeEjemplo();
        var reglas = new List<ReglaEvaluada> { new(DefinicionesRegla.R2, 2m) };

        var detectadas = MotorAlertas.Evaluar(datos, reglas, Ahora);

        // Solo Ana tiene 2 actividades vencidas sin entrega; Luis tiene 1.
        var unica = Assert.Single(detectadas);
        Assert.Equal("R2", unica.CodigoRegla);
        Assert.Equal(1, unica.EstudianteId);

        // Sin reglas activas no se detecta nada, aunque los datos tengan atrasos.
        Assert.Empty(MotorAlertas.Evaluar(datos, new List<ReglaEvaluada>(), Ahora));

        // R4 sobre un curso sin notas calificadas: menos de 4 notas, sin alerta.
        var r4 = MotorAlertas.Evaluar(datos, new List<ReglaEvaluada> { new(DefinicionesRegla.R4, 7m) }, Ahora);
        Assert.Empty(r4);
    }

    /// <summary>Curso de ejemplo: Ana (id 1) con 2 atrasos vencidos y Luis (id 2) con 1, sin notas calificadas.</summary>
    private static DatosCurso DatosDeEjemplo()
    {
        var unidades = new List<Unidad>
        {
            new() { Id = 1, CursoId = 1, MoodleSeccionId = 10, Numero = 1, Parcial = 1, Nombre = "Unidad 1" },
        };

        var actividades = new List<Actividad>
        {
            new() { Id = 1, CursoId = 1, UnidadId = 1, MoodleItemId = 101, Nombre = "Tarea 1", Categoria = "Tareas", NotaMaxima = 10m, FechaLimite = new DateTimeOffset(2026, 10, 1, 23, 59, 0, TimeSpan.Zero) },
            new() { Id = 2, CursoId = 1, UnidadId = 1, MoodleItemId = 102, Nombre = "Tarea 2", Categoria = "Tareas", NotaMaxima = 10m, FechaLimite = new DateTimeOffset(2026, 10, 15, 23, 59, 0, TimeSpan.Zero) },
            new() { Id = 3, CursoId = 1, UnidadId = 1, MoodleItemId = 103, Nombre = "Tarea 3", Categoria = "Tareas", NotaMaxima = 10m, FechaLimite = new DateTimeOffset(2026, 12, 1, 23, 59, 0, TimeSpan.Zero) },
        };

        var estudiantes = new List<Estudiante>
        {
            new() { Id = 1, MoodleUsuarioId = 21, Nombre = "Ana", Apellido = "Pérez" },
            new() { Id = 2, MoodleUsuarioId = 22, Nombre = "Luis", Apellido = "Gómez" },
        };

        Calificacion SinEntrega(int id, int actividadId) => new()
        {
            Id = id,
            ActividadId = actividadId,
            EstudianteId = 1,
            Nota = null,
            Estado = EstadoCalificacion.SIN_ENTREGA,
        };

        var calificaciones = new List<Calificacion>
        {
            SinEntrega(1, 1),
            SinEntrega(2, 2),
            new() { Id = 3, ActividadId = 1, EstudianteId = 2, Nota = null, Estado = EstadoCalificacion.SIN_ENTREGA },
            new() { Id = 4, ActividadId = 3, EstudianteId = 2, Nota = null, Estado = EstadoCalificacion.SIN_ENTREGA },
        };

        return new DatosCurso(unidades, actividades, estudiantes, calificaciones);
    }
}
