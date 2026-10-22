using EduMonitor.Infrastructure.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>EduMonitorDB en SQLite en memoria: vive mientras la conexión siga abierta. No usa SQL Server ni datos reales.</summary>
public sealed class BaseDatosPrueba : IDisposable
{
    private readonly SqliteConnection _conexion;

    public BaseDatosPrueba()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        Opciones = new DbContextOptionsBuilder<EduMonitorDbContext>().UseSqlite(_conexion).Options;
        using var contexto = CrearContexto();
        contexto.Database.EnsureCreated();
    }

    public DbContextOptions<EduMonitorDbContext> Opciones { get; }

    public SqliteConnection Conexion => _conexion;

    public EduMonitorDbContext CrearContexto() => new(Opciones);

    public void Dispose() => _conexion.Dispose();
}
