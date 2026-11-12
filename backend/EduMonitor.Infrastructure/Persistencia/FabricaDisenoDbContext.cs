using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduMonitor.Infrastructure.Persistencia;

/// <summary>
/// Solo para <c>dotnet ef migrations add</c>: crea el contexto sin arrancar la API ni leer secretos.
/// <c>dotnet ef database update</c> toma la cadena real de <c>ConnectionStrings__EduMonitorDB</c> si está definida.
/// </summary>
internal sealed class FabricaDisenoDbContext : IDesignTimeDbContextFactory<EduMonitorDbContext>
{
    public EduMonitorDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__EduMonitorDB")
            ?? "Server=localhost,1433;Database=EduMonitorDB;Integrated Security=false;TrustServerCertificate=True";
        var opciones = new DbContextOptionsBuilder<EduMonitorDbContext>().UseSqlServer(cadena).Options;
        return new EduMonitorDbContext(opciones);
    }
}
