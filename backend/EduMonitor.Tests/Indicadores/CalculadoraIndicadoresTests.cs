using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Tests.Indicadores;

/// <summary>TA-09.2 · Casos conocidos de la planificación (tabla 5): 9, 8, 6, 3, 3 → 5,80 y 2,77; categoría faltante; cero frente a sin entrega.</summary>
public class CalculadoraIndicadoresTests
{
    [Fact]
    public void Caso_de_clase_9_8_6_3_3_da_promedio_5_80_desviacion_2_77_y_mediana_6()
    {
        var notas = new[] { 9m, 8m, 6m, 3m, 3m };

        Assert.Equal(5.80m, CalculadoraIndicadores.Redondear(CalculadoraIndicadores.Media(notas)));
        Assert.Equal(2.77m, CalculadoraIndicadores.Redondear(CalculadoraIndicadores.DesviacionMuestral(notas)));
        Assert.Equal(6m, CalculadoraIndicadores.Mediana(notas));
    }

    [Fact]
    public void Desviacion_con_un_solo_valor_es_null_y_mediana_de_cantidad_par_promedia_los_centrales()
    {
        Assert.Null(CalculadoraIndicadores.DesviacionMuestral(new[] { 7m }));
        Assert.Equal(7.5m, CalculadoraIndicadores.Mediana(new[] { 9m, 6m, 8m, 7m }));
        Assert.Null(CalculadoraIndicadores.Media(Array.Empty<decimal>()));
    }

    [Fact]
    public void Promedio_ponderado_con_las_tres_categorias_usa_40_30_30()
    {
        var (valor, parcial) = CalculadoraIndicadores.PromedioPonderado(new (string?, decimal)[]
        {
            ("Exámenes", 8m), ("Tareas", 6m), ("Tareas", 8m), ("APE", 9m),
        });

        Assert.Equal(8.00m, CalculadoraIndicadores.Redondear(valor)); // 0,4·8 + 0,3·7 + 0,3·9
        Assert.False(parcial);
    }

    [Fact]
    public void Si_falta_una_categoria_normaliza_los_pesos_y_marca_parcial()
    {
        var (valor, parcial) = CalculadoraIndicadores.PromedioPonderado(new (string?, decimal)[] { ("Exámenes", 8m), ("Tareas", 6m) });

        Assert.Equal(7.14m, CalculadoraIndicadores.Redondear(valor)); // (0,4·8 + 0,3·6) / 0,7
        Assert.True(parcial);
    }

    [Fact]
    public void Categorias_fuera_de_la_ponderacion_no_entran_y_sin_categorias_no_hay_promedio()
    {
        var (valor, parcial) = CalculadoraIndicadores.PromedioPonderado(new (string?, decimal)[] { (null, 9m), ("Extra", 10m) });

        Assert.Null(valor);
        Assert.False(parcial);
        Assert.Equal("EXAMENES", CalculadoraIndicadores.ClaveCategoria(" Exámenes "));
    }

    [Fact]
    public void Tendencia_con_N_3_resta_el_promedio_de_las_anteriores_y_es_null_con_menos_de_4_notas()
    {
        Assert.Equal(-3.25m, CalculadoraIndicadores.Tendencia(new[] { 7.5m, 8m, 5m, 4.5m, 4m }));
        Assert.Null(CalculadoraIndicadores.Tendencia(new[] { 7m, 6m, 5m }));
    }

    [Theory]
    [InlineData(8.5, "ALTO")]
    [InlineData(8.49, "MEDIO")]
    [InlineData(6.0, "MEDIO")]
    [InlineData(5.99, "BAJO")]
    public void Grupos_alto_desde_8_5_medio_desde_6_y_bajo_debajo_de_6(double promedio, string grupo)
    {
        Assert.Equal(grupo, CalculadoraIndicadores.Grupo((decimal)promedio));
    }

    [Fact]
    public void Puntuacion_z_usa_la_media_y_la_desviacion_del_curso_y_es_null_sin_desviacion()
    {
        Assert.Equal(-0.98m, CalculadoraIndicadores.Redondear(CalculadoraIndicadores.PuntuacionZ(6.1m, 7.42m, 1.35m)));
        Assert.Null(CalculadoraIndicadores.PuntuacionZ(6.1m, 6.1m, 0m));
        Assert.Null(CalculadoraIndicadores.PuntuacionZ(null, 7m, 1m));
    }

    [Fact]
    public void Distribucion_tiene_10_tramos_de_1_punto_y_el_10_cae_en_el_ultimo()
    {
        var tramos = CalculadoraIndicadores.Distribucion(new[] { 0m, 0.99m, 5.5m, 9.2m, 10m });

        Assert.Equal(10, tramos.Count);
        Assert.Equal(new TramoDistribucionDto(0, 1, 2), tramos[0]);
        Assert.Equal(1, tramos[5].Cantidad);
        Assert.Equal(new TramoDistribucionDto(9, 10, 2), tramos[9]);
    }

    [Fact]
    public void Nota_se_lleva_a_escala_10_con_la_nota_maxima_de_la_actividad()
    {
        Assert.Equal(8.5m, CalculadoraIndicadores.NotaEnEscala(85m, 100m));
        Assert.Equal(7m, CalculadoraIndicadores.NotaEnEscala(7m, null));
        Assert.Null(CalculadoraIndicadores.NotaEnEscala(null, 10m));
    }

    [Fact]
    public void Cero_calificado_entra_al_promedio_y_sin_entrega_o_pendiente_no()
    {
        var datos = Curso(
            (1, "Tareas", 1, 0m, EstadoCalificacion.CALIFICADO),
            (1, "Tareas", 1, null, EstadoCalificacion.SIN_ENTREGA),
            (1, "Tareas", 1, null, EstadoCalificacion.PENDIENTE),
            (1, "Exámenes", 1, 8m, EstadoCalificacion.CALIFICADO));

        var estudiante = CalculadoraIndicadores.Calcular(datos).Estudiantes.Single();

        Assert.Equal(new ConteoNotasDto(Calificadas: 2, Ceros: 1, SinEntrega: 1, Pendientes: 1), estudiante.Notas);
        Assert.Equal(4.57m, estudiante.PromedioPonderado); // (0,4·8 + 0,3·0) / 0,7: la entrega pendiente y la no entregada no cuentan como 0
        Assert.True(estudiante.Parcial);
        Assert.Equal("BAJO", estudiante.Grupo);
    }

    [Fact]
    public void Curso_completo_calcula_resumen_grupos_unidades_y_finalizacion()
    {
        var datos = Curso(
            (1, "Exámenes", 1, 9m, EstadoCalificacion.CALIFICADO),
            (1, "Tareas", 1, 9m, EstadoCalificacion.CALIFICADO),
            (1, "APE", 1, 9m, EstadoCalificacion.CALIFICADO),
            (2, "Exámenes", 2, 6m, EstadoCalificacion.CALIFICADO),
            (2, "Tareas", 2, 6m, EstadoCalificacion.CALIFICADO),
            (2, "APE", 2, 6m, EstadoCalificacion.CALIFICADO));

        var resultado = CalculadoraIndicadores.Calcular(datos);

        Assert.Equal(2, resultado.Curso.Estudiantes);
        Assert.Equal(7.5m, resultado.Curso.Promedio);
        Assert.Equal(2.12m, resultado.Curso.Desviacion);
        Assert.Equal(new GruposRendimientoDto(Alto: 1, Medio: 1, Bajo: 0), resultado.Grupos);
        Assert.Equal(0.5m, resultado.Curso.TasaFinalizacion);
        Assert.Equal(1, resultado.Unidades.Single().Numero);
        Assert.Equal(3, resultado.Unidades.Single().Actividades);
        Assert.Equal(3, resultado.Unidades.Single().Calificadas);
        Assert.All(resultado.Estudiantes, e => Assert.False(e.Parcial));
    }

    [Fact]
    public void Sin_finalizacion_disponible_la_tasa_es_null_no_cero()
    {
        var datos = Curso((1, "Tareas", 1, 7m, EstadoCalificacion.CALIFICADO)) with { FinalizacionDisponible = false };

        var resultado = CalculadoraIndicadores.Calcular(datos);

        Assert.Null(resultado.Curso.TasaFinalizacion);
        Assert.Null(resultado.Estudiantes.Single().TasaFinalizacion);
    }

    [Fact]
    public void Tendencia_del_estudiante_sigue_el_orden_de_las_fechas_limite()
    {
        var unidad = new Unidad { Id = 1, Numero = 1, Parcial = 1, Nombre = "Unidad 1" };
        var estudiante = new Estudiante { Id = 1, Nombre = "Ana", Apellido = "Pérez" };
        var notas = new[] { 7.5m, 8m, 5m, 4.5m, 4m };
        var actividades = notas.Select((_, i) => new Actividad
        {
            Id = i + 1, MoodleItemId = 100 - i, UnidadId = 1, Categoria = "Tareas", NotaMaxima = 10,
            FechaLimite = new DateTimeOffset(2026, 9, 1 + i, 23, 59, 0, TimeSpan.FromHours(-5)),
        }).ToList();
        var calificaciones = notas.Select((n, i) => new Calificacion { ActividadId = i + 1, EstudianteId = 1, Nota = n, Estado = EstadoCalificacion.CALIFICADO }).ToList();
        var datos = new DatosCurso(new[] { unidad }, actividades, new[] { estudiante }, calificaciones);

        Assert.Equal(-3.25m, CalculadoraIndicadores.Calcular(datos).Estudiantes.Single().Tendencia);
        Assert.Equal(notas, CalculadoraIndicadores.NotasOrdenadas(datos, 1).Select(n => n.Nota));
    }

    /// <summary>Una unidad con una actividad por fila; cada fila es (estudiante, categoría, actividad dentro del estudiante, nota, estado).</summary>
    private static DatosCurso Curso(params (int Estudiante, string Categoria, int Unidad, decimal? Nota, EstadoCalificacion Estado)[] filas)
    {
        var unidad = new Unidad { Id = 1, Numero = 1, Parcial = 1, Nombre = "Unidad 1" };
        var estudiantes = filas.Select(f => f.Estudiante).Distinct()
            .Select(id => new Estudiante { Id = id, Nombre = $"Est{id}", Apellido = "Prueba" }).ToList();
        var actividades = new List<Actividad>();
        var calificaciones = new List<Calificacion>();
        foreach (var (fila, indice) in filas.Select((f, i) => (f, i)))
        {
            var porEstudiante = filas.Take(indice).Count(f => f.Estudiante == fila.Estudiante);
            var nombre = $"{fila.Categoria} {porEstudiante + 1}";
            var actividad = actividades.FirstOrDefault(a => a.Nombre == nombre);
            if (actividad is null)
            {
                actividad = new Actividad { Id = actividades.Count + 1, MoodleItemId = actividades.Count + 1, UnidadId = unidad.Id, Nombre = nombre, Categoria = fila.Categoria, NotaMaxima = 10 };
                actividades.Add(actividad);
            }

            calificaciones.Add(new Calificacion
            {
                ActividadId = actividad.Id,
                EstudianteId = fila.Estudiante,
                Nota = fila.Nota,
                Estado = fila.Estado,
                Finalizada = fila.Estudiante == 1,
            });
        }

        return new DatosCurso(new[] { unidad }, actividades, estudiantes, calificaciones);
    }
}
