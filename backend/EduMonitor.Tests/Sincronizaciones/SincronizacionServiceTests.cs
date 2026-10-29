using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Configuracion;
using EduMonitor.Application.Cursos;
using EduMonitor.Application.Sincronizaciones;
using EduMonitor.Domain.Enums;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduMonitor.Tests.Sincronizaciones;

/// <summary>TA-05.5. Registro de sincronización, idempotencia y estados COMPLETADA, PARCIAL y FALLIDA, con un cliente de Moodle falso.</summary>
public class SincronizacionServiceTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private readonly MoodleClientFalso _moodle = MoodleClientFalso.ConCursoDeEjemplo();
    private readonly int _cursoId;
    private readonly UsuarioActual _doc01;

    public SincronizacionServiceTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), "clave-de-prueba").GetAwaiter().GetResult();
        _cursoId = db.Cursos.Single(c => c.NombreCorto == "AGIL-4B").Id;
        _doc01 = new UsuarioActual(db.Usuarios.Single(u => u.NombreUsuario == "doc01").Id, RolSistema.DOCENTE);
    }

    private async Task<SincronizacionDto?> Sincronizar(int? cursoId = null)
    {
        await using var db = _bd.CrearContexto();
        var servicio = new SincronizacionService(
            db,
            new ImportadorUnidades(_moodle, db),
            new ImportadorMatriculados(_moodle, db),
            new ImportadorCalificaciones(_moodle, db),
            new ImportadorEntregas(_moodle, db),
            Options.Create(new MoodleOptions { Url = "http://localhost:8080" }),
            TimeProvider.System);
        return await servicio.SincronizarAsync(cursoId ?? _cursoId, _doc01);
    }

    [Fact]
    public async Task Sin_errores_queda_COMPLETADA_con_origen_fechas_y_registros()
    {
        var resultado = await Sincronizar();

        Assert.NotNull(resultado);
        Assert.Equal("COMPLETADA", resultado.Estado);
        Assert.Equal("http://localhost:8080", resultado.Origen);
        Assert.True(resultado.Fin >= resultado.Inicio);
        Assert.Equal(new RegistrosSincronizacion(4, 2, 2, 4), resultado.Registros);
        Assert.Empty(resultado.Errores);
    }

    [Fact]
    public async Task Sincronizar_dos_veces_no_duplica_registros_y_guarda_dos_ejecuciones()
    {
        var primera = await Sincronizar();
        var segunda = await Sincronizar();

        Assert.Equal(primera!.Registros, segunda!.Registros);
        await using var db = _bd.CrearContexto();
        Assert.Equal(2, await db.Estudiantes.CountAsync());
        Assert.Equal(2, await db.Matriculas.CountAsync());
        Assert.Equal(2, await db.Actividades.CountAsync());
        Assert.Equal(4, await db.Calificaciones.CountAsync());
        Assert.Equal(4, await db.Unidades.CountAsync());
        Assert.Equal(2, await db.Sincronizaciones.CountAsync());
    }

    [Fact]
    public async Task Si_falla_una_funcion_y_otra_no_queda_PARCIAL_con_el_error()
    {
        _moodle.Fallar.Add("gradereport_user_get_grade_items");

        var resultado = await Sincronizar();

        Assert.Equal("PARCIAL", resultado!.Estado);
        var error = Assert.Single(resultado.Errores);
        Assert.Equal("gradereport_user_get_grade_items", error.Funcion);
        Assert.Equal("Acceso denegado para el usuario técnico", error.Mensaje);
        Assert.Equal(2, resultado.Registros.Estudiantes);
        Assert.Equal(0, resultado.Registros.Calificaciones);

        await using var db = _bd.CrearContexto();
        Assert.Equal(0, await db.Calificaciones.CountAsync()); // no disponible: no se inventan notas
        var guardada = await db.Sincronizaciones.SingleAsync();
        Assert.Equal(EstadoSincronizacion.PARCIAL, guardada.Estado);
        Assert.Equal("gradereport_user_get_grade_items", Assert.Single(guardada.Errores).Funcion);
    }

    [Fact]
    public async Task Si_fallan_todas_las_funciones_queda_FALLIDA_y_se_registra()
    {
        _moodle.Fallar.UnionWith(new[]
        {
            "core_course_get_contents",
            "core_enrol_get_enrolled_users",
            "gradereport_user_get_grade_items",
            "mod_assign_get_assignments",
        });

        var resultado = await Sincronizar();

        Assert.Equal("FALLIDA", resultado!.Estado);
        // Sin estructura de unidades la finalización no se intenta: 4 pasos, 4 errores.
        Assert.Equal(4, resultado.Errores.Count);
        await using var db = _bd.CrearContexto();
        Assert.Equal(EstadoSincronizacion.FALLIDA, (await db.Sincronizaciones.SingleAsync()).Estado);
        Assert.Equal(0, await db.Estudiantes.CountAsync());
    }

    [Fact]
    public async Task Una_PARCIAL_conserva_los_datos_de_la_sincronizacion_anterior()
    {
        await Sincronizar();
        _moodle.Fallar.Add("gradereport_user_get_grade_items");

        await Sincronizar();

        await using var db = _bd.CrearContexto();
        Assert.Equal(4, await db.Calificaciones.CountAsync());
    }

    [Fact]
    public async Task Importa_las_secciones_como_unidades_con_su_parcial_y_liga_las_actividades()
    {
        await Sincronizar();

        await using var db = _bd.CrearContexto();
        var unidades = await db.Unidades.OrderBy(u => u.Numero).ToListAsync();
        Assert.Equal(new[] { (1, 1, "Unidad 1"), (2, 1, "Unidad 2"), (3, 2, "Unidad 3"), (4, 2, "Unidad 4") },
            unidades.Select(u => (u.Numero, u.Parcial, u.Nombre)));
        Assert.All(unidades, u => Assert.Equal(_cursoId, u.CursoId));

        var actividades = await db.Actividades.Include(a => a.Unidad).ToDictionaryAsync(a => a.Nombre);
        Assert.Equal("Unidad 1", actividades["Tarea 1"].Unidad!.Nombre);
        Assert.Equal("Unidad 2", actividades["Examen P1"].Unidad!.Nombre);
    }

    [Fact]
    public async Task Sin_nota_y_sin_entrega_es_SIN_ENTREGA_y_la_nota_cero_es_CALIFICADO()
    {
        await Sincronizar();

        await using var db = _bd.CrearContexto();
        var estados = await EstadosAsync(db);
        Assert.Equal(EstadoCalificacion.CALIFICADO, estados[(101, "Tarea 1")]);
        Assert.Equal(EstadoCalificacion.SIN_ENTREGA, estados[(102, "Tarea 1")]);
        Assert.Equal(EstadoCalificacion.CALIFICADO, estados[(101, "Examen P1")]); // nota 0
        Assert.Equal(new DateTimeOffset(2026, 9, 8, 23, 59, 0, TimeSpan.FromHours(-5)),
            (await db.Actividades.SingleAsync(a => a.Nombre == "Tarea 1")).FechaLimite);
    }

    [Fact]
    public async Task Sin_nota_y_entregada_es_PENDIENTE()
    {
        _moodle.Entregas.Add(new EntregaMoodle(7, 102, "submitted", "notgraded"));

        await Sincronizar();

        await using var db = _bd.CrearContexto();
        Assert.Equal(EstadoCalificacion.PENDIENTE, (await EstadosAsync(db))[(102, "Tarea 1")]);
    }

    [Fact]
    public async Task Un_borrador_no_cuenta_como_entrega()
    {
        _moodle.Entregas.Add(new EntregaMoodle(7, 102, "draft", "notgraded"));

        await Sincronizar();

        await using var db = _bd.CrearContexto();
        Assert.Equal(EstadoCalificacion.SIN_ENTREGA, (await EstadosAsync(db))[(102, "Tarea 1")]);
    }

    [Fact]
    public async Task Importa_la_finalizacion_y_deja_no_disponible_lo_que_no_tiene_seguimiento()
    {
        await Sincronizar();

        await using var db = _bd.CrearContexto();
        var finalizada = await db.Calificaciones.Include(c => c.Actividad).Include(c => c.Estudiante)
            .ToDictionaryAsync(c => (c.Estudiante.MoodleUsuarioId, c.Actividad.Nombre), c => c.Finalizada);
        Assert.True(finalizada[(101, "Tarea 1")]);
        Assert.False(finalizada[(102, "Tarea 1")]);
        Assert.Null(finalizada[(101, "Examen P1")]);
    }

    [Fact]
    public async Task Si_falla_la_finalizacion_queda_PARCIAL_y_el_dato_no_disponible()
    {
        _moodle.Fallar.Add("core_completion_get_activities_completion_status");

        var resultado = await Sincronizar();

        Assert.Equal("PARCIAL", resultado!.Estado);
        Assert.Equal("core_completion_get_activities_completion_status", Assert.Single(resultado.Errores).Funcion);
        await using var db = _bd.CrearContexto();
        Assert.All(await db.Calificaciones.ToListAsync(), c => Assert.Null(c.Finalizada));
        Assert.Equal(4, await db.Calificaciones.CountAsync()); // las notas sí se importaron
    }

    [Fact]
    public async Task Si_fallan_las_secciones_las_notas_se_importan_sin_unidad()
    {
        _moodle.Fallar.Add("core_course_get_contents");

        var resultado = await Sincronizar();

        Assert.Equal("PARCIAL", resultado!.Estado);
        Assert.Equal(0, resultado.Registros.Unidades);
        Assert.DoesNotContain("core_completion_get_activities_completion_status", _moodle.Llamadas);
        await using var db = _bd.CrearContexto();
        Assert.All(await db.Actividades.ToListAsync(), a => Assert.Null(a.UnidadId));
        Assert.Equal(4, await db.Calificaciones.CountAsync());
    }

    [Fact]
    public async Task Si_fallan_las_entregas_las_notas_vacias_quedan_no_disponibles()
    {
        _moodle.Fallar.Add("mod_assign_get_assignments");

        var resultado = await Sincronizar();

        Assert.Equal("PARCIAL", resultado!.Estado);
        await using var db = _bd.CrearContexto();
        Assert.Null((await EstadosAsync(db))[(102, "Tarea 1")]);
    }

    private static Task<Dictionary<(int, string), EstadoCalificacion?>> EstadosAsync(EduMonitorDbContext db) =>
        db.Calificaciones.Include(c => c.Actividad).Include(c => c.Estudiante)
            .ToDictionaryAsync(c => (c.Estudiante.MoodleUsuarioId, c.Actividad.Nombre), c => c.Estado);

    [Fact]
    public async Task Curso_inexistente_devuelve_null_y_no_registra()
    {
        Assert.Null(await Sincronizar(9999));

        await using var db = _bd.CrearContexto();
        Assert.Equal(0, await db.Sincronizaciones.CountAsync());
    }

    public void Dispose() => _bd.Dispose();
}
