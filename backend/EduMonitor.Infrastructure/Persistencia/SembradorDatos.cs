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

        if (!await db.Usuarios.AnyAsync(ct))
        {
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

        // En cada arranque, también sobre una base que ya tiene datos: el curso adicional del laboratorio.
        if (await AsegurarCursoMcsAsync(db, ct))
        {
            logger.LogInformation("Curso de laboratorio agregado: MCS-4B (Moodle {MoodleId}) asignado a doc01.", MoodleIdMcs);
        }
    }

    /// <summary>Id de MCS-4B en el Moodle de laboratorio: el script lo crea después de AGIL-4B (2) y BDD (3).</summary>
    public const int MoodleIdMcs = 4;

    /// <summary>
    /// Curso adicional del 12 nov 2026: MCS-4B «Manejo y Configuración del Software (lab)», asignado solo a doc01. Idempotente:
    /// no hace nada si el curso ya existe o si doc01 no existe. No toca usuarios, AGIL-4B ni BDD-4B. El docente lo ve en «Mis cursos»
    /// y lo sincroniza con el botón, como los otros.
    /// </summary>
    public static async Task<bool> AsegurarCursoMcsAsync(EduMonitorDbContext db, CancellationToken ct = default)
    {
        if (await db.Cursos.AnyAsync(c => c.NombreCorto == "MCS-4B" || c.MoodleCursoId == MoodleIdMcs, ct))
        {
            return false;
        }

        var doc01 = await db.Usuarios.SingleOrDefaultAsync(u => u.NombreUsuario == "doc01", ct);
        if (doc01 is null)
        {
            return false;
        }

        var mcs = new Curso { MoodleCursoId = MoodleIdMcs, NombreCorto = "MCS-4B", Nombre = "Manejo y Configuración del Software (lab)", Periodo = "2026-2" };
        db.CursosDocentes.Add(new CursoDocente { Curso = mcs, Usuario = doc01 });
        await db.SaveChangesAsync(ct);
        return true;
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
