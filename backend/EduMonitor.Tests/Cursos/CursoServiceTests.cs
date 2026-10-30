using EduMonitor.Application.Cursos;
using EduMonitor.Domain.Enums;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Tests.Cursos;

public class CursoServiceTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private readonly int _doc01;
    private readonly int _admin;
    private readonly int _agil;
    private readonly int _bdd;

    public CursoServiceTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), "clave-de-prueba").GetAwaiter().GetResult();
        _doc01 = db.Usuarios.Single(u => u.NombreUsuario == "doc01").Id;
        _admin = db.Usuarios.Single(u => u.NombreUsuario == "admin").Id;
        _agil = db.Cursos.Single(c => c.NombreCorto == "AGIL-4B").Id;
        _bdd = db.Cursos.Single(c => c.NombreCorto == "BDD-4B").Id;
    }

    private CursoService Servicio(EduMonitorDbContext db) => new(db);

    [Fact]
    public async Task Un_docente_solo_lista_sus_cursos_y_un_admin_lista_todos()
    {
        await using var db = _bd.CrearContexto();

        var delDocente = await Servicio(db).ListarAsync(new UsuarioActual(_doc01, RolSistema.DOCENTE), null, null);
        var delAdmin = await Servicio(db).ListarAsync(new UsuarioActual(_admin, RolSistema.ADMIN), null, null);

        var curso = Assert.Single(delDocente);
        Assert.Equal("AGIL-4B", curso.NombreCorto);
        Assert.Null(curso.UltimaSincronizacion);
        Assert.Equal(new[] { "AGIL-4B", "BDD-4B" }, delAdmin.Select(c => c.NombreCorto));
    }

    [Fact]
    public async Task Filtra_por_busqueda_y_periodo()
    {
        await using var db = _bd.CrearContexto();
        var admin = new UsuarioActual(_admin, RolSistema.ADMIN);

        Assert.Equal("BDD-4B", Assert.Single(await Servicio(db).ListarAsync(admin, "Datos", null)).NombreCorto);
        Assert.Equal("AGIL-4B", Assert.Single(await Servicio(db).ListarAsync(admin, "AGIL", null)).NombreCorto);
        Assert.Equal(2, (await Servicio(db).ListarAsync(admin, null, "2026-2")).Count);
        Assert.Empty(await Servicio(db).ListarAsync(admin, null, "2025-1"));
    }

    [Fact]
    public async Task TieneAcceso_solo_al_curso_asignado_salvo_admin()
    {
        await using var db = _bd.CrearContexto();
        var docente = new UsuarioActual(_doc01, RolSistema.DOCENTE);

        Assert.True(await Servicio(db).TieneAccesoAsync(docente, _agil));
        Assert.False(await Servicio(db).TieneAccesoAsync(docente, _bdd));
        Assert.True(await Servicio(db).TieneAccesoAsync(new UsuarioActual(_admin, RolSistema.ADMIN), _bdd));
    }

    public void Dispose() => _bd.Dispose();
}
