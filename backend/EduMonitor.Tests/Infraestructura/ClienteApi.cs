using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EduMonitor.Tests.Infraestructura;

public static class ClienteApi
{
    /// <summary>Inicia sesión con un usuario semilla y devuelve un cliente con su JWT en la cabecera Authorization.</summary>
    public static async Task<HttpClient> ClienteConSesionAsync(this ApiConDatosFactory factory, string usuario)
    {
        var cliente = factory.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario, contrasena = ApiConDatosFactory.ContrasenaPrueba });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo.GetProperty("token").GetString());
        return cliente;
    }
}
