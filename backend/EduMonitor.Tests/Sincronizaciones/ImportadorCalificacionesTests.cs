using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Application.Sincronizaciones;
using EduMonitor.Domain.Enums;
using EduMonitor.Infrastructure.Persistencia;
using EduMonitor.Infrastructure.Seguridad;
using EduMonitor.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Tests.Sincronizaciones;

public class ImportadorCalificacionesTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private readonly MoodleClientFalso _moodle = MoodleClientFalso.ConCursoDeEjemplo();

    public ImportadorCalificacionesTests()
    {
        using var db = _bd.CrearContexto();
        SembradorDatos.SembrarAsync(db, new HasherContrasenas(), "clave-de-prueba").GetAwaiter().GetResult();
        var curso = db.Cursos.Single(c => c.NombreCorto == "AGIL-4B");
        new ImportadorMatriculados(_moodle, db).ImportarAsync(curso).GetAwaiter().GetResult();
    }

    private async Task<ImportadorCalificaciones.Resultado> Importar()
    {
        await using var db = _bd.CrearContexto();
        var curso = await db.Cursos.SingleAsync(c => c.NombreCorto == "AGIL-4B");
        return await new ImportadorCalificaciones(_moodle, db).ImportarAsync(curso);
    }

    [Fact]
    public async Task Guarda_solo_los_items_de_actividad_con_su_categoria_y_peso()
    {
        var resultado = await Importar();

        Assert.Equal(2, resultado.Actividades);
        await using var db = _bd.CrearContexto();
        var actividades = await db.Actividades.OrderBy(a => a.MoodleItemId).ToListAsync();
        Assert.Equal(new[] { "Tarea 1", "Examen P1" }, actividades.Select(a => a.Nombre));
        Assert.Equal(new[] { "Tareas", "Exámenes" }, actividades.Select(a => a.Categoria));
        Assert.All(actividades, a => Assert.Equal(1m, a.Peso));
        Assert.Equal(("assign", 7), (actividades[0].Modulo, actividades[0].Instancia));
    }

    [Fact]
    public async Task Las_notas_coinciden_con_Moodle_y_la_nota_cero_es_distinta_de_sin_nota()
    {
        var resultado = await Importar();

        Assert.Equal(4, resultado.Calificaciones);
        await using var db = _bd.CrearContexto();
        var notas = await db.Calificaciones
            .Select(c => new { c.Estudiante.MoodleUsuarioId, c.Actividad.Nombre, c.Nota, c.Estado })
            .ToListAsync();

        var examenAna = notas.Single(n => n.MoodleUsuarioId == 101 && n.Nombre == "Examen P1");
        Assert.Equal(0m, examenAna.Nota);
        Assert.Equal(EstadoCalificacion.CALIFICADO, examenAna.Estado);

        var tareaAna = notas.Single(n => n.MoodleUsuarioId == 101 && n.Nombre == "Tarea 1");
        Assert.Equal(8.5m, tareaAna.Nota);

        var tareaLuis = notas.Single(n => n.MoodleUsuarioId == 102 && n.Nombre == "Tarea 1");
        Assert.Null(tareaLuis.Nota);
        Assert.Null(tareaLuis.Estado); // no disponible hasta que TA-05.4 lo derive; nunca 0
    }

    [Fact]
    public async Task Importar_dos_veces_no_duplica_y_actualiza_la_nota()
    {
        await Importar();
        _moodle.Calificaciones[1] = _moodle.Calificaciones[1] with
        {
            Items = _moodle.Calificaciones[1].Items.Select(i => i.Id == 30 ? i with { Nota = 6m } : i).ToList(),
        };

        await Importar();

        await using var db = _bd.CrearContexto();
        Assert.Equal(2, await db.Actividades.CountAsync());
        Assert.Equal(4, await db.Calificaciones.CountAsync());
        var tareaLuis = await db.Calificaciones.SingleAsync(c => c.Estudiante.MoodleUsuarioId == 102 && c.Actividad.MoodleItemId == 30);
        Assert.Equal(6m, tareaLuis.Nota);
        Assert.Equal(EstadoCalificacion.CALIFICADO, tareaLuis.Estado);
    }

    [Fact]
    public async Task Ignora_notas_de_usuarios_que_no_son_estudiantes_matriculados()
    {
        _moodle.Calificaciones.Add(new CalificacionesUsuarioMoodle(5, _moodle.Calificaciones[0].Items));

        var resultado = await Importar();

        Assert.Equal(4, resultado.Calificaciones);
    }

    [Fact]
    public async Task Si_Moodle_falla_lanza_la_excepcion_y_no_toca_la_base()
    {
        _moodle.Fallar.Add("gradereport_user_get_grade_items");

        await Assert.ThrowsAsync<MoodleNoDisponibleException>(Importar);

        await using var db = _bd.CrearContexto();
        Assert.Equal(0, await db.Actividades.CountAsync());
    }

    [Theory]
    [InlineData(0d, null, EstadoCalificacion.CALIFICADO)]
    [InlineData(null, EstadoCalificacion.PENDIENTE, EstadoCalificacion.PENDIENTE)]
    [InlineData(null, EstadoCalificacion.SIN_ENTREGA, EstadoCalificacion.SIN_ENTREGA)]
    [InlineData(null, EstadoCalificacion.CALIFICADO, null)]
    [InlineData(null, null, null)]
    public void DerivarEstado(double? nota, EstadoCalificacion? actual, EstadoCalificacion? esperado)
    {
        Assert.Equal(esperado, ImportadorCalificaciones.DerivarEstado((decimal?)nota, actual));
    }

    public void Dispose() => _bd.Dispose();
}
