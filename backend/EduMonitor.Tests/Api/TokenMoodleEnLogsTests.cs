using EduMonitor.Tests.Infraestructura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Tests.Api;

public class TokenMoodleEnLogsTests
{
    private const string TokenPrueba = "0f1e2d3c4b5a69788796a5b4c3d23f9a";

    [Fact]
    public async Task Al_iniciar_la_API_el_log_muestra_el_token_enmascarado_y_nunca_completo()
    {
        var logs = new LogsEnMemoria();
        await using var factory = new ApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Moodle:Url"] = "http://localhost:8080",
                ["Moodle:Token"] = TokenPrueba,
            }));
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
        });

        var cliente = factory.CreateClient();
        await cliente.GetAsync("/api/health");

        Assert.Contains(logs.Mensajes, m => m.Contains("token ••••3f9a"));
        Assert.DoesNotContain(logs.Mensajes, m => m.Contains(TokenPrueba));
    }
}
