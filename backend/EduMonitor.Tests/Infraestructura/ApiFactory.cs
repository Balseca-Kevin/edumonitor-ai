using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>API en memoria para pruebas de integración. Entorno "Testing": no carga el .env del repo.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
