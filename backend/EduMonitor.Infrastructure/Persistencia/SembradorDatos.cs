using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EduMonitor.Infrastructure.Persistencia;

/// <summary>
/// Solo en Development: aplica las migraciones y carga los datos semilla de laboratorio si la base está vacía.
/// La contraseña de los usuarios semilla llega de <c>Semillas__Contrasena</c> (.env), nunca del código.
/// </summary>
public static class SembradorDatos
{
    public static async Task InicializarAsync(IServiceProvider servicios, CancellationToken ct = default)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EduMonitorDbContext>();
        var logger = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SembradorDatos));

        await db.Database.MigrateAsync(ct);

        if (await db.Usuarios.AnyAsync(ct))
        {
            return;
        }

        var contrasena = alcance.ServiceProvider.GetRequiredService<IConfiguration>()["Semillas:Contrasena"];
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            logger.LogWarning("Semillas__Contrasena no está configurada; no se crean los usuarios semilla.");
            return;
        }

        var hasher = alcance.ServiceProvider.GetRequiredService<IHasherContrasenas>();
        await SembrarAsync(db, hasher, contrasena, ct);
        logger.LogInformation("Datos semilla cargados: admin, doc01 (AGIL-4B) y doc02 (BDD-4B).");
    }

    /// <summary>1 administrador, 2 docentes y 2 cursos, cada docente con un curso distinto. También lo usan las pruebas.</summary>
    public static async Task SembrarAsync(EduMonitorDbContext db, IHasherContrasenas hasher, string contrasena, CancellationToken ct = default)
    {
        // Ids de Moodle provisionales: se ajustan cuando TA-01.2 cree los cursos en el Moodle de laboratorio.
        var agil = new Curso { MoodleCursoId = 2, NombreCorto = "AGIL-4B", Nombre = "Metodologías Ágiles (lab)", Periodo = "2026-2" };
        var bdd = new Curso { MoodleCursoId = 3, NombreCorto = "BDD-4B", Nombre = "Bases de Datos (lab)", Periodo = "2026-2" };

        var hash = hasher.Hashear(contrasena);
        db.Usuarios.AddRange(
            new Usuario { NombreUsuario = "admin", Nombre = "Administrador", HashContrasena = hash, Rol = RolSistema.ADMIN },
            new Usuario { NombreUsuario = "doc01", Nombre = "Docente Uno", HashContrasena = hash, Rol = RolSistema.DOCENTE, Cursos = { new CursoDocente { Curso = agil } } },
            new Usuario { NombreUsuario = "doc02", Nombre = "Docente Dos", HashContrasena = hash, Rol = RolSistema.DOCENTE, Cursos = { new CursoDocente { Curso = bdd } } });

        await db.SaveChangesAsync(ct);
    }
}
