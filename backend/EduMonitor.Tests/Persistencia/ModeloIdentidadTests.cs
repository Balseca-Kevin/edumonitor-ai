using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Tests.Persistencia;

public class ModeloIdentidadTests
{
    [Fact]
    public async Task Guarda_usuario_con_rol_como_texto_y_su_curso_asignado()
    {
        using var bd = new BaseDatosPrueba();
        await using (var contexto = bd.CrearContexto())
        {
            var curso = new Curso { MoodleCursoId = 2, NombreCorto = "AGIL-4B", Nombre = "Metodologías Ágiles (lab)", Periodo = "2026-2" };
            contexto.Usuarios.Add(new Usuario
            {
                NombreUsuario = "doc01",
                Nombre = "Docente Uno",
                HashContrasena = "hash",
                Rol = RolSistema.DOCENTE,
                Cursos = { new CursoDocente { Curso = curso } },
            });
            await contexto.SaveChangesAsync();
        }

        await using var lectura = bd.CrearContexto();
        var usuario = await lectura.Usuarios.Include(u => u.Cursos).ThenInclude(c => c.Curso).SingleAsync();
        Assert.Equal(RolSistema.DOCENTE, usuario.Rol);
        Assert.Equal("AGIL-4B", Assert.Single(usuario.Cursos).Curso.NombreCorto);

        await using var comando = bd.Conexion.CreateCommand();
        comando.CommandText = "SELECT Rol FROM Usuario";
        Assert.Equal("DOCENTE", await comando.ExecuteScalarAsync());
    }

    [Fact]
    public async Task NombreUsuario_y_MoodleCursoId_son_unicos()
    {
        using var bd = new BaseDatosPrueba();
        await using var contexto = bd.CrearContexto();
        contexto.Usuarios.Add(new Usuario { NombreUsuario = "doc01", Nombre = "A", HashContrasena = "h", Rol = RolSistema.DOCENTE });
        contexto.Usuarios.Add(new Usuario { NombreUsuario = "doc01", Nombre = "B", HashContrasena = "h", Rol = RolSistema.DOCENTE });
        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        await using var otro = bd.CrearContexto();
        otro.Cursos.Add(new Curso { MoodleCursoId = 2, NombreCorto = "A", Nombre = "A", Periodo = "2026-2" });
        otro.Cursos.Add(new Curso { MoodleCursoId = 2, NombreCorto = "B", Nombre = "B", Periodo = "2026-2" });
        await Assert.ThrowsAsync<DbUpdateException>(() => otro.SaveChangesAsync());
    }
}
