using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Sincronizaciones;
using EduMonitor.Domain.Entities;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Tests.Sincronizaciones;

/// <summary>
/// TA-08.A1 · DADO el aula virtual con el sílabo cargado (nombre de la sección y temas en su resumen), CUANDO se sincroniza,
/// ENTONCES cada unidad lleva el nombre del sílabo y sus temas iniciales, sin duplicarlos al volver a sincronizar y sin tocar los
/// temas que el docente ya registró.
/// </summary>
public class TemasSilaboTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private readonly MoodleClientFalso _moodle = MoodleClientFalso.ConCursoDeEjemplo();
    private readonly Curso _curso;

    public TemasSilaboTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), "clave-de-prueba").GetAwaiter().GetResult();
        _curso = db.Cursos.AsNoTracking().Single(c => c.NombreCorto == "AGIL-4B");

        _moodle.Secciones.Clear();
        _moodle.Secciones.AddRange(new[]
        {
            new SeccionMoodle(1, 0, "General", Array.Empty<ModuloMoodle>(), "<p>Bienvenida</p>"),
            new SeccionMoodle(11, 1, "Principios fundamentales de las metodologías ágiles", Array.Empty<ModuloMoodle>(),
                "<ul><li>1.1 Evolución del software</li><li>1.2 Etapas del proceso de desarrollo de software</li><li>1.3 Manifiesto &Aacute;gil</li></ul>"),
            new SeccionMoodle(12, 2, "Filosofía y enfoques ágiles de la actualidad", Array.Empty<ModuloMoodle>(), "<p>2.1 Filosofía ágil</p><p>2.2 Enfoques ágiles</p>"),
            new SeccionMoodle(13, 3, "", Array.Empty<ModuloMoodle>(), null),
        });
    }

    private async Task ImportarAsync()
    {
        await using var db = _bd.CrearContexto();
        await new ImportadorUnidades(_moodle, db).ImportarAsync(_curso);
    }

    private Dictionary<int, List<string>> TemasPorUnidad()
    {
        using var db = _bd.CrearContexto();
        return db.Unidades.Where(u => u.CursoId == _curso.Id).ToList()
            .ToDictionary(u => u.Numero, u => db.Temas.Where(t => t.UnidadId == u.Id).OrderBy(t => t.Id).Select(t => t.Nombre).ToList());
    }

    [Theory]
    [InlineData("<ul><li>1.1 Evolución del software</li><li> 1.2  Etapas </li></ul>", new[] { "1.1 Evolución del software", "1.2 Etapas" })]
    [InlineData("<p>2.1 Filosofía ágil</p><p>2.2 Enfoques ágiles</p>", new[] { "2.1 Filosofía ágil", "2.2 Enfoques ágiles" })]
    [InlineData("3.1 XP<br>3.2 Scrum<br/>3.2 scrum", new[] { "3.1 XP", "3.2 Scrum" })]
    [InlineData("<ul><li>5.1 Bloques <strong>PL/SQL</strong> &amp; triggers</li></ul>", new[] { "5.1 Bloques PL/SQL & triggers" })]
    [InlineData("", new string[0])]
    [InlineData(null, new string[0])]
    public void Lee_los_temas_del_resumen_de_la_seccion(string? resumen, string[] esperados)
    {
        Assert.Equal(esperados, TemasSilabo.Leer(resumen));
    }

    [Fact]
    public async Task Cada_unidad_lleva_el_nombre_y_los_temas_del_silabo_sin_duplicarlos()
    {
        await ImportarAsync();
        await ImportarAsync();

        using var db = _bd.CrearContexto();
        var unidades = db.Unidades.Where(u => u.CursoId == _curso.Id).OrderBy(u => u.Numero).ToList();
        Assert.Equal(new[] { "Principios fundamentales de las metodologías ágiles", "Filosofía y enfoques ágiles de la actualidad", "Unidad 3" },
            unidades.Select(u => u.Nombre));

        var temas = TemasPorUnidad();
        Assert.Equal(new[] { "1.1 Evolución del software", "1.2 Etapas del proceso de desarrollo de software", "1.3 Manifiesto Ágil" }, temas[1]);
        Assert.Equal(new[] { "2.1 Filosofía ágil", "2.2 Enfoques ágiles" }, temas[2]);
        Assert.Empty(temas[3]); // sin temas en el resumen: no se inventan
    }

    [Fact]
    public async Task Los_temas_que_registro_el_docente_no_se_tocan_al_volver_a_sincronizar()
    {
        await ImportarAsync();
        using (var db = _bd.CrearContexto())
        {
            var unidad1 = db.Unidades.Single(u => u.CursoId == _curso.Id && u.Numero == 1);
            db.Temas.RemoveRange(db.Temas.Where(t => t.UnidadId == unidad1.Id && t.Nombre.StartsWith("1.2")));
            db.Temas.Add(new Tema { UnidadId = unidad1.Id, Nombre = "Tema del docente" });
            db.SaveChanges();
        }

        await ImportarAsync();

        Assert.Equal(new[] { "1.1 Evolución del software", "1.3 Manifiesto Ágil", "Tema del docente" }, TemasPorUnidad()[1]);
    }

    public void Dispose() => _bd.Dispose();
}
