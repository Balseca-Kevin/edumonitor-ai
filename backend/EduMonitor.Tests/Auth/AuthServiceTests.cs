using EduMonitor.Application.Auth;
using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduMonitor.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private const string ContrasenaPrueba = "clave-de-prueba";
    private readonly BaseDatosPrueba _bd = new();
    private readonly HasherContrasenas _hasher = new();

    public AuthServiceTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, _hasher, ContrasenaPrueba).GetAwaiter().GetResult();
    }

    private AuthService CrearServicio(EduMonitorDbContext db)
    {
        var jwt = new JwtService(Options.Create(new JwtOptions { Secret = ApiFactory.SecretoJwtPrueba, ExpiraMinutos = 120 }), TimeProvider.System);
        return new AuthService(db, _hasher, jwt);
    }

    [Fact]
    public async Task Credenciales_correctas_devuelven_token_y_datos_del_usuario()
    {
        await using var db = _bd.CrearContexto();

        var respuesta = await CrearServicio(db).LoginAsync(new SolicitudLogin("doc01", ContrasenaPrueba));

        Assert.NotNull(respuesta);
        Assert.False(string.IsNullOrEmpty(respuesta.Token));
        Assert.Equal("doc01", respuesta.Usuario.Usuario);
        Assert.Equal("Docente Uno", respuesta.Usuario.Nombre);
        Assert.Equal("DOCENTE", respuesta.Usuario.Rol);
        Assert.True(respuesta.ExpiraEn > DateTimeOffset.Now.AddMinutes(119));
    }

    [Theory]
    [InlineData("doc01", "otra-clave")]
    [InlineData("no-existe", ContrasenaPrueba)]
    public async Task Credenciales_incorrectas_devuelven_null(string usuario, string contrasena)
    {
        await using var db = _bd.CrearContexto();

        var respuesta = await CrearServicio(db).LoginAsync(new SolicitudLogin(usuario, contrasena));

        Assert.Null(respuesta);
    }

    [Fact]
    public async Task Las_contrasenas_se_guardan_con_hash_y_no_en_texto_plano()
    {
        await using var db = _bd.CrearContexto();

        var hashes = await db.Usuarios.Select(u => u.HashContrasena).ToListAsync();

        Assert.Equal(3, hashes.Count);
        Assert.All(hashes, h => Assert.DoesNotContain(ContrasenaPrueba, h));
        Assert.All(hashes, h => Assert.True(_hasher.Verificar(h, ContrasenaPrueba)));
    }

    public void Dispose() => _bd.Dispose();
}
