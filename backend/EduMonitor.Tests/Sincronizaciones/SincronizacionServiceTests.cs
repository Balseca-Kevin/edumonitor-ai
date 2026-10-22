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
            new ImportadorMatriculados(_moodle, db),
            new ImportadorCalificaciones(_moodle, db),
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
        Assert.Equal(new RegistrosSincronizacion(0, 2, 2, 4), resultado.Registros);
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
        _moodle.Fallar.Add("core_enrol_get_enrolled_users");
        _moodle.Fallar.Add("gradereport_user_get_grade_items");

        var resultado = await Sincronizar();

        Assert.Equal("FALLIDA", resultado!.Estado);
        Assert.Equal(2, resultado.Errores.Count);
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
    public async Task Curso_inexistente_devuelve_null_y_no_registra()
    {
        Assert.Null(await Sincronizar(9999));

        await using var db = _bd.CrearContexto();
        Assert.Equal(0, await db.Sincronizaciones.CountAsync());
    }

    public void Dispose() => _bd.Dispose();
}
