using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduMonitor.Domain.Enums;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Tests.Infraestructura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace EduMonitor.Tests.Sincronizaciones;

/// <summary>
/// TA-05.6 · Sincronización de AGIL-4B de punta a punta: API en memoria (EduMonitorDB en SQLite) contra el Moodle de
/// laboratorio real, con los datos de moodle-lab/scripts/cargar_datos_laboratorio.php. Compara 10 notas con Moodle.
/// Se omite si no hay .env con el token (por ejemplo, en CI).
/// </summary>
public class SincronizacionMoodleRealTests(ITestOutputHelper salida)
{
    private const int AgilMoodleId = 2;

    private sealed class ApiConMoodleReal : ApiConDatosFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Moodle:Url", MoodleReal.Configuracion.Url);
            builder.UseSetting("Moodle:Token", MoodleReal.Configuracion.Token);
        }
    }

    // Estudiante (nombre del script de carga) y actividad. Incluye nota 0, sin entrega, pendiente, descenso y promedio bajo.
    private static readonly (string Estudiante, string Actividad)[] Muestra =
    {
        ("Ana Acosta", "Tarea 1"),
        ("Carla Cevallos", "Tarea 2"),          // est03: descenso, unidad 1 alta
        ("Carla Cevallos", "Tarea 5"),          // est03: descenso, unidad 3 baja
        ("Elena Espinoza", "Tarea 3"),          // est05: nota 0 calificada
        ("Gabriela Guerrero", "Tarea 5"),       // est07: sin entrega
        ("Isabel Iza", "Tarea 6"),              // est09: pendiente de calificar
        ("Karina Lara", "Examen Parcial 1"),    // est11: promedio bajo
        ("María Naranjo", "APE 2"),             // est13: alejado del grupo
        ("Quilla Ríos", "Taller 1"),           // est17: temática crítica en la unidad 3
        ("Francisco Haro", "Examen Parcial 1"),
    };

    [FactMoodleReal]
    public async Task Sincroniza_AGIL_4B_y_10_notas_coinciden_con_Moodle()
    {
        await using var api = new ApiConMoodleReal();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");

        var respuesta = await doc01.PostAsync($"/api/cursos/{curso}/sync", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sync = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("COMPLETADA", sync.GetProperty("estado").GetString());
        var registros = sync.GetProperty("registros");
        Assert.Equal(4, registros.GetProperty("unidades").GetInt32());
        Assert.Equal(16, registros.GetProperty("actividades").GetInt32());
        Assert.Equal(32, registros.GetProperty("estudiantes").GetInt32());
        Assert.Equal(16 * 32, registros.GetProperty("calificaciones").GetInt32());

        // Lo que tiene Moodle, leído aparte con el cliente.
        var moodle = MoodleReal.Cliente();
        var matriculados = (await moodle.ObtenerMatriculadosAsync(AgilMoodleId)).ToDictionary(m => $"{m.Nombre} {m.Apellido}", m => m.Id);
        var notasMoodle = (await moodle.ObtenerItemsCalificacionAsync(AgilMoodleId))
            .SelectMany(r => r.Items.Where(i => i.TipoItem == "mod").Select(i => (r.UsuarioId, i.Nombre, i.Nota)))
            .ToDictionary(n => (n.UsuarioId, n.Nombre), n => n.Nota);

        using var alcance = api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var notasEduMonitor = await db.Calificaciones.Include(c => c.Actividad).Include(c => c.Estudiante)
            .Where(c => c.Actividad.CursoId == curso)
            .ToDictionaryAsync(c => (c.Estudiante.MoodleUsuarioId, c.Actividad.Nombre));

        salida.WriteLine("| # | Estudiante | Actividad | Nota en Moodle | Nota en EduMonitor | Estado en EduMonitor | ¿Coincide? |");
        salida.WriteLine("|---|---|---|---|---|---|---|");
        var numero = 0;
        foreach (var (estudiante, actividad) in Muestra)
        {
            var clave = (matriculados[estudiante], actividad);
            var enMoodle = notasMoodle[clave];
            var enEduMonitor = notasEduMonitor[clave];
            var coincide = enMoodle == enEduMonitor.Nota;
            salida.WriteLine($"| {++numero} | {estudiante} | {actividad} | {Texto(enMoodle)} | {Texto(enEduMonitor.Nota)} | {enEduMonitor.Estado?.ToString() ?? "no disponible"} | {(coincide ? "Sí" : "No")} |");
            Assert.True(coincide, $"{estudiante} · {actividad}: Moodle {Texto(enMoodle)}, EduMonitor {Texto(enEduMonitor.Nota)}");
        }

        // Los tres estados quedan diferenciados y la nota 0 no se confunde con sin entrega.
        Assert.Equal((0m, EstadoCalificacion.CALIFICADO), Par(notasEduMonitor[(matriculados["Elena Espinoza"], "Tarea 3")]));
        Assert.Equal((null, EstadoCalificacion.SIN_ENTREGA), Par(notasEduMonitor[(matriculados["Gabriela Guerrero"], "Tarea 5")]));
        Assert.Equal((null, EstadoCalificacion.PENDIENTE), Par(notasEduMonitor[(matriculados["Isabel Iza"], "Tarea 6")]));

        // Unidad, fecha límite y finalización también llegan.
        var tarea5 = await db.Actividades.Include(a => a.Unidad).SingleAsync(a => a.CursoId == curso && a.Nombre == "Tarea 5");
        Assert.Equal((3, 2), (tarea5.Unidad!.Numero, tarea5.Unidad.Parcial));
        Assert.NotNull(tarea5.FechaLimite);
        Assert.True(notasEduMonitor[(matriculados["Ana Acosta"], "Tarea 1")].Finalizada);

        // El token no aparece en los registros de la API.
        Assert.DoesNotContain(api.Logs.Mensajes, m => m.Contains(MoodleReal.Configuracion.Token, StringComparison.Ordinal));
    }

    [FactMoodleReal]
    public async Task Sincronizar_dos_veces_no_duplica()
    {
        await using var api = new ApiConMoodleReal();
        var doc01 = await api.ClienteConSesionAsync("doc01");
        var curso = api.IdCurso("AGIL-4B");

        await doc01.PostAsync($"/api/cursos/{curso}/sync", null);
        await doc01.PostAsync($"/api/cursos/{curso}/sync", null);

        using var alcance = api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        Assert.Equal(4, await db.Unidades.CountAsync(u => u.CursoId == curso));
        Assert.Equal(32, await db.Matriculas.CountAsync(m => m.CursoId == curso));
        Assert.Equal(16 * 32, await db.Calificaciones.CountAsync(c => c.Actividad.CursoId == curso));
        Assert.Equal(2, await db.Sincronizaciones.CountAsync(s => s.CursoId == curso));
    }

    private static (decimal?, EstadoCalificacion?) Par(Domain.Entities.Calificacion c) => (c.Nota, c.Estado);

    private static string Texto(decimal? nota) => nota?.ToString("0.00", CultureInfo.InvariantCulture) ?? "—";
}
