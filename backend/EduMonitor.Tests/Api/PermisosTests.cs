using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Tests.Infraestructura;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EduMonitor.Tests.Api;

/// <summary>
/// TA-04.4. Criterio de HU-04: DADO dos docentes con cursos distintos, CUANDO el docente A pide un curso del docente B,
/// ENTONCES recibe 403; sin token, 401; con credenciales válidas, 200 con un JWT que lleva su rol y vence según la configuración.
/// </summary>
public class PermisosTests(ApiConDatosFactory factory) : IClassFixture<ApiConDatosFactory>
{
    // --- Login ---

    [Fact]
    public async Task Login_con_credenciales_validas_responde_200_con_JWT_que_lleva_rol_y_vencimiento()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario = "doc01", contrasena = ApiConDatosFactory.ContrasenaPrueba });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("doc01", cuerpo.GetProperty("usuario").GetProperty("usuario").GetString());
        Assert.Equal("DOCENTE", cuerpo.GetProperty("usuario").GetProperty("rol").GetString());

        var token = new JsonWebToken(cuerpo.GetProperty("token").GetString());
        Assert.Equal("DOCENTE", token.GetClaim("role").Value);
        Assert.Equal(cuerpo.GetProperty("usuario").GetProperty("id").GetInt32().ToString(), token.Subject);
        var minutos = (token.ValidTo - token.IssuedAt).TotalMinutes;
        Assert.Equal(120, minutos, precision: 0);
        Assert.Equal(token.ValidTo, cuerpo.GetProperty("expiraEn").GetDateTimeOffset().UtcDateTime, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("doc01", "contrasena-equivocada")]
    [InlineData("no-existe", ApiConDatosFactory.ContrasenaPrueba)]
    public async Task Login_con_credenciales_incorrectas_responde_401_generico(string usuario, string contrasena)
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario, contrasena });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Credenciales incorrectas", problema.GetProperty("title").GetString());
        Assert.Equal("El usuario o la contraseña no son válidos.", problema.GetProperty("detail").GetString());
        Assert.False(problema.TryGetProperty("token", out _));
    }

    [Fact]
    public async Task Login_sin_contrasena_responde_400()
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario = "doc01" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // --- 401 ---

    [Theory]
    [InlineData("/api/cursos")]
    [InlineData("/api/cursos/1")]
    public async Task Sin_token_responde_401_con_ProblemDetails(string ruta)
    {
        var cliente = factory.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://httpstatuses.io/401", problema.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Con_token_invalido_responde_401()
    {
        var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "no.es.un-jwt");

        var respuesta = await cliente.GetAsync("/api/cursos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    // --- 403 y acceso correcto ---

    [Fact]
    public async Task Docente_que_pide_un_curso_ajeno_recibe_403()
    {
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var cursoDeDoc02 = factory.IdCurso("BDD-4B");

        var respuesta = await doc01.GetAsync($"/api/cursos/{cursoDeDoc02}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Acceso denegado", problema.GetProperty("title").GetString());
        Assert.Equal($"El curso {cursoDeDoc02} no está asignado a este docente.", problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Docente_que_pide_su_curso_recibe_200()
    {
        var doc01 = await factory.ClienteConSesionAsync("doc01");
        var cursoPropio = factory.IdCurso("AGIL-4B");

        var respuesta = await doc01.GetAsync($"/api/cursos/{cursoPropio}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var curso = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AGIL-4B", curso.GetProperty("nombreCorto").GetString());
        Assert.Equal(JsonValueKind.Null, curso.GetProperty("ultimaSincronizacion").ValueKind);
    }

    [Fact]
    public async Task Admin_puede_ver_cualquier_curso()
    {
        var admin = await factory.ClienteConSesionAsync("admin");

        var respuesta = await admin.GetAsync($"/api/cursos/{factory.IdCurso("BDD-4B")}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Curso_inexistente_responde_404()
    {
        var doc01 = await factory.ClienteConSesionAsync("doc01");

        var respuesta = await doc01.GetAsync("/api/cursos/9999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Lista_de_cursos_solo_trae_los_del_docente()
    {
        var doc02 = await factory.ClienteConSesionAsync("doc02");

        var cursos = await doc02.GetFromJsonAsync<JsonElement>("/api/cursos");

        var curso = Assert.Single(cursos.EnumerateArray());
        Assert.Equal("BDD-4B", curso.GetProperty("nombreCorto").GetString());
    }

}
