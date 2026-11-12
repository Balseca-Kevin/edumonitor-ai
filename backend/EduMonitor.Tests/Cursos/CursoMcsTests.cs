using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Enums;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;

namespace EduMonitor.Tests.Cursos;

/// <summary>
/// Curso adicional del laboratorio (12 nov 2026). DADO una base que ya tiene los datos semilla, CUANDO la API arranca, ENTONCES
/// agrega MCS-4B con el id de Moodle 4, solo para doc01, sin tocar AGIL-4B ni BDD-4B; doc02 no lo ve y una segunda vez no duplica.
/// </summary>
public class CursoMcsTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();

    public CursoMcsTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), "clave-de-prueba").GetAwaiter().GetResult();
    }

    private static CursoService Servicio(EduMonitorDbContext db) => new(db, TimeProvider.System);

    [Fact]
    public async Task Agrega_MCS_4B_solo_a_doc01_y_no_cambia_los_otros_cursos()
    {
        await using var db = _bd.CrearContexto();
        var antes = db.Cursos.OrderBy(c => c.Id).Select(c => new { c.Id, c.MoodleCursoId, c.NombreCorto }).ToList();

        Assert.True(await SembradorDatos.AsegurarCursoMcsAsync(db));

        var doc01 = new UsuarioActual(db.Usuarios.Single(u => u.NombreUsuario == "doc01").Id, RolSistema.DOCENTE);
        var doc02 = new UsuarioActual(db.Usuarios.Single(u => u.NombreUsuario == "doc02").Id, RolSistema.DOCENTE);
        var mcs = db.Cursos.Single(c => c.NombreCorto == "MCS-4B");

        Assert.Equal(SembradorDatos.MoodleIdMcs, mcs.MoodleCursoId);
        Assert.Equal("Manejo y Configuración del Software (lab)", mcs.Nombre);
        Assert.Equal(new[] { "AGIL-4B", "MCS-4B" }, (await Servicio(db).ListarAsync(doc01, null, null)).Select(c => c.NombreCorto));
        Assert.Equal("BDD-4B", Assert.Single(await Servicio(db).ListarAsync(doc02, null, null)).NombreCorto);
        Assert.True(await Servicio(db).TieneAccesoAsync(doc01, mcs.Id));
        Assert.False(await Servicio(db).TieneAccesoAsync(doc02, mcs.Id));
        Assert.Equal(new[] { "AGIL-4B", "MCS-4B" }, (await Servicio(db).CursosAutorizadosAsync(doc01)).Cursos.Select(c => c.NombreCorto));

        // AGIL-4B y BDD-4B siguen con su id y su id de Moodle.
        var despues = db.Cursos.Where(c => c.NombreCorto != "MCS-4B").OrderBy(c => c.Id).Select(c => new { c.Id, c.MoodleCursoId, c.NombreCorto }).ToList();
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task Es_idempotente_y_no_hace_nada_sin_doc01()
    {
        await using (var db = _bd.CrearContexto())
        {
            Assert.True(await SembradorDatos.AsegurarCursoMcsAsync(db));
            Assert.False(await SembradorDatos.AsegurarCursoMcsAsync(db));
            Assert.Single(db.Cursos.Where(c => c.NombreCorto == "MCS-4B"));
        }

        using var vacia = new BaseDatosPrueba();
        await using var sinUsuarios = vacia.CrearContexto();
        Assert.False(await SembradorDatos.AsegurarCursoMcsAsync(sinUsuarios));
        Assert.Empty(sinUsuarios.Cursos);
    }

    public void Dispose()
    {
        _bd.Dispose();
        GC.SuppressFinalize(this);
    }
}
