using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>API en memoria para pruebas de integración. Entorno "Testing": no carga el .env del repo.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Secreto solo para pruebas; no es el de ningún entorno.</summary>
    public const string SecretoJwtPrueba = "secreto-de-pruebas-edumonitor-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", SecretoJwtPrueba);
        builder.UseSetting("Jwt:Issuer", "EduMonitor");
        builder.UseSetting("Jwt:Audience", "EduMonitor");
        builder.UseSetting("Jwt:ExpiraMinutos", "120");
    }
}
