using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>
/// API en memoria con EduMonitorDB en SQLite y los datos semilla de laboratorio:
/// admin (ADMIN), doc01 (AGIL-4B) y doc02 (BDD-4B), todos con <see cref="ContrasenaPrueba"/>.
/// </summary>
public class ApiConDatosFactory : ApiFactory
{
    public const string ContrasenaPrueba = "clave-de-prueba";

    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public LogsEnMemoria Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _conexion.Open();

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<EduMonitorDbContext>>();
            services.AddDbContext<EduMonitorDbContext>(opciones => opciones.UseSqlite(_conexion));
            ConfigurarServicios(services);
        });
    }

    /// <summary>Punto para que una prueba reemplace servicios (p. ej. el cliente de Moodle por uno falso).</summary>
    protected virtual void ConfigurarServicios(IServiceCollection services)
    {
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var alcance = host.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        db.Database.EnsureCreated();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), ContrasenaPrueba).GetAwaiter().GetResult();
        return host;
    }

    public int IdCurso(string nombreCorto)
    {
        using var alcance = Services.CreateScope();
        return alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>().Cursos.Single(c => c.NombreCorto == nombreCorto).Id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conexion.Dispose();
    }
}

internal static class ServiceCollectionPruebasExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
