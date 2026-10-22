using EduMonitor.Api.Errores;
using EduMonitor.Application;
using EduMonitor.Application.Configuracion;
using EduMonitor.Infrastructure;
using EduMonitor.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// En desarrollo los secretos llegan del .env de la raíz del repo (nunca versionado). En CI y en pruebas, de variables de entorno.
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.TraversePath().Load();
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        bearer.MapInboundClaims = false; // conserva sub, name y role tal como se emiten
        bearer.TokenValidationParameters = JwtService.ParametrosValidacion(jwt.Value);
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = TitulosProblemDetails.Aplicar);
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

const string PoliticaFrontend = "Frontend";
builder.Services.AddCors(opciones => opciones.AddPolicy(PoliticaFrontend, politica => politica
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo { Title = "EduMonitor API", Version = "v1" });
    var esquemaBearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT obtenido en POST /api/auth/login. Escribe solo el token, sin «Bearer».",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    opciones.AddSecurityDefinition("Bearer", esquemaBearer);
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquemaBearer] = Array.Empty<string>() });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(PoliticaFrontend);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Visible para WebApplicationFactory<Program> en EduMonitor.Tests.
public partial class Program;
