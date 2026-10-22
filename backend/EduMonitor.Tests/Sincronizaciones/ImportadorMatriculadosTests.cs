using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Sincronizaciones;
using EduMonitor.Domain.Entities;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Tests.Sincronizaciones;

public class ImportadorMatriculadosTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();

    public ImportadorMatriculadosTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), "clave-de-prueba").GetAwaiter().GetResult();
    }

    private async Task<int> Importar(MoodleClientFalso moodle, string nombreCorto = "AGIL-4B")
    {
        await using var db = _bd.CrearContexto();
        var curso = await db.Cursos.SingleAsync(c => c.NombreCorto == nombreCorto);
        return await new ImportadorMatriculados(moodle, db).ImportarAsync(curso);
    }

    [Fact]
    public async Task Importa_solo_estudiantes_con_nombre_y_apellido()
    {
        var cantidad = await Importar(MoodleClientFalso.ConCursoDeEjemplo());

        Assert.Equal(2, cantidad);
        await using var db = _bd.CrearContexto();
        var estudiantes = await db.Estudiantes.OrderBy(e => e.MoodleUsuarioId).ToListAsync();
        Assert.Equal(new[] { 101, 102 }, estudiantes.Select(e => e.MoodleUsuarioId));
        Assert.Equal(("Ana", "Pérez"), (estudiantes[0].Nombre, estudiantes[0].Apellido));
        Assert.DoesNotContain(estudiantes, e => e.MoodleUsuarioId == 5);
        Assert.Equal(2, await db.Matriculas.CountAsync());
    }

    [Fact]
    public void Estudiante_no_tiene_campos_de_contacto()
    {
        var propiedades = typeof(Estudiante).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(propiedades, p => p.Contains("Correo") || p.Contains("Email") || p.Contains("Telefono"));
    }

    [Fact]
    public async Task Importar_dos_veces_no_duplica_y_actualiza_el_nombre()
    {
        var moodle = MoodleClientFalso.ConCursoDeEjemplo();
        await Importar(moodle);
        moodle.Matriculados[0] = moodle.Matriculados[0] with { Nombre = "Ana María" };

        await Importar(moodle);

        await using var db = _bd.CrearContexto();
        Assert.Equal(2, await db.Estudiantes.CountAsync());
        Assert.Equal(2, await db.Matriculas.CountAsync());
        Assert.Equal("Ana María", (await db.Estudiantes.SingleAsync(e => e.MoodleUsuarioId == 101)).Nombre);
    }

    [Fact]
    public async Task Un_estudiante_en_dos_cursos_se_guarda_una_vez_con_dos_matriculas()
    {
        var moodle = MoodleClientFalso.ConCursoDeEjemplo();
        await Importar(moodle, "AGIL-4B");
        await Importar(moodle, "BDD-4B");

        await using var db = _bd.CrearContexto();
        Assert.Equal(2, await db.Estudiantes.CountAsync());
        Assert.Equal(4, await db.Matriculas.CountAsync());
    }

    [Fact]
    public async Task Si_Moodle_falla_lanza_la_excepcion_y_no_toca_la_base()
    {
        var moodle = MoodleClientFalso.ConCursoDeEjemplo();
        moodle.Fallar.Add("core_enrol_get_enrolled_users");

        var error = await Assert.ThrowsAsync<MoodleNoDisponibleException>(() => Importar(moodle));

        Assert.Equal("core_enrol_get_enrolled_users", error.Funcion);
        await using var db = _bd.CrearContexto();
        Assert.Equal(0, await db.Estudiantes.CountAsync());
    }

    public void Dispose() => _bd.Dispose();
}
