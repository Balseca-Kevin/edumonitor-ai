using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Herramientas;
using EduMonitor.Application.Indicadores;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Libro;

/// <summary>
/// TA-21.1 · Libro de calificaciones del curso: la nota de cada estudiante en cada actividad, agrupada por parcial y unidad.
/// Se arma con lo que guardó la última sincronización con datos; nunca llama a Moodle. Las notas van en escala 0 a 10 con la
/// misma conversión del panel, y el promedio ponderado es el mismo de <c>GET /api/cursos/{id}/indicadores</c>.
/// </summary>
public sealed class LibroService(IEduMonitorDbContext db, IndicadorService indicadores)
{
    public const string NoDisponibleEstadosEntrega = "estadosEntrega";
    public const string NombreSinUnidad = "Sin unidad";

    private static readonly string[] FuncionesEntregas = { "mod_assign_get_assignments", "mod_assign_get_submissions" };

    public async Task<ConsultaLibro> ConsultarAsync(int cursoId, string? busqueda, OrdenLibro orden, CancellationToken ct = default) =>
        await ConsultarAsync(cursoId, busqueda, orden, Tramo.Todo, ct);

    /// <summary>R.2 · Con un tramo, el libro trae solo las columnas de ese tramo y el promedio de esas notas, el mismo de los indicadores del tramo.</summary>
    public async Task<ConsultaLibro> ConsultarAsync(int cursoId, string? busqueda, OrdenLibro orden, Tramo tramo, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return new ConsultaLibro(EstadoConsultaLibro.CursoNoExiste);
        }

        var sincronizacion = await indicadores.UltimaSincronizacionConDatosAsync(cursoId, ct);
        var datos = sincronizacion is null ? null : await indicadores.CargarDatosAsync(cursoId, ct);
        if (sincronizacion is null || datos is null)
        {
            return new ConsultaLibro(EstadoConsultaLibro.SinSincronizar);
        }

        if (!tramo.ExisteEn(datos))
        {
            return new ConsultaLibro(EstadoConsultaLibro.TramoInvalido, Detalle: tramo.NoExisteDetalle);
        }

        datos = tramo.Filtrar(datos);
        var parciales = Columnas(datos);
        var actividades = parciales.SelectMany(p => p.Unidades).SelectMany(u => u.Actividades).ToList();
        var porId = datos.Actividades.ToDictionary(a => a.Id);
        var calificaciones = datos.Calificaciones
            .GroupBy(c => (c.EstudianteId, c.ActividadId))
            .ToDictionary(g => g.Key, g => g.First());
        var promedios = CalculadoraIndicadores.Calcular(datos).Estudiantes.ToDictionary(e => e.Id);

        var estudiantes = datos.Estudiantes
            .Where(e => string.IsNullOrWhiteSpace(busqueda) || HerramientasService.CoincideNombre(busqueda, e.Nombre, e.Apellido))
            .Select(e =>
            {
                var calculado = promedios.GetValueOrDefault(e.Id);
                var notas = actividades
                    .Select(a =>
                    {
                        var calificacion = calificaciones.GetValueOrDefault((e.Id, a.Id));
                        var nota = CalculadoraIndicadores.Redondear(CalculadoraIndicadores.NotaEnEscala(calificacion?.Nota, porId[a.Id].NotaMaxima));
                        return new NotaLibroDto(a.Id, nota, calificacion?.Estado?.ToString());
                    })
                    .ToList();
                return new EstudianteLibroDto(e.Id, e.Nombre, e.Apellido, calculado?.PromedioPonderado, calculado?.Parcial ?? false, notas);
            });

        var ordenados = orden == OrdenLibro.Promedio
            ? estudiantes.OrderBy(e => e.PromedioPonderado is null).ThenBy(e => e.PromedioPonderado).ThenBy(e => e.Apellido).ThenBy(e => e.Nombre)
            : estudiantes.OrderBy(e => e.Apellido).ThenBy(e => e.Nombre);

        var noDisponibles = sincronizacion.Errores.Any(e => FuncionesEntregas.Contains(e.Funcion))
            ? new List<string> { NoDisponibleEstadosEntrega }
            : new List<string>();

        return new ConsultaLibro(
            EstadoConsultaLibro.Armado,
            new LibroCalificacionesDto(
                cursoId,
                CalculadoraIndicadores.Escala,
                CalculadoraIndicadores.NotaAprobacion,
                new SincronizacionLibroDto(sincronizacion.Id, sincronizacion.Inicio, sincronizacion.Estado.ToString(), sincronizacion.Origen),
                parciales,
                ordenados.ToList(),
                noDisponibles,
                tramo.Codigo));
    }

    /// <summary>
    /// Parciales y unidades con actividades, en el orden del curso. Las actividades que no están en una sección de Moodle van al
    /// final, en el parcial 0 y la unidad 0 «Sin unidad».
    /// </summary>
    public static IReadOnlyList<ParcialLibroDto> Columnas(DatosCurso datos)
    {
        var ordenadas = CalculadoraIndicadores.OrdenarActividades(datos.Actividades, datos.Unidades);
        var unidades = datos.Unidades.ToDictionary(u => u.Id);
        static ActividadLibroDto Columna(Domain.Entities.Actividad a) => new(a.Id, a.Nombre, a.Categoria, a.FechaLimite);

        var parciales = ordenadas
            .Where(a => a.UnidadId is int id && unidades.ContainsKey(id))
            .GroupBy(a => unidades[a.UnidadId!.Value])
            .GroupBy(g => g.Key.Parcial)
            .OrderBy(p => p.Key)
            .Select(p => new ParcialLibroDto(
                p.Key,
                p.OrderBy(u => u.Key.Numero)
                    .Select(u => new UnidadLibroDto(u.Key.Numero, u.Key.Nombre, u.Select(Columna).ToList()))
                    .ToList()))
            .ToList();

        var sinUnidad = ordenadas.Where(a => a.UnidadId is not int id || !unidades.ContainsKey(id)).Select(Columna).ToList();
        if (sinUnidad.Count > 0)
        {
            parciales.Add(new ParcialLibroDto(0, new[] { new UnidadLibroDto(0, NombreSinUnidad, sinUnidad) }));
        }

        return parciales;
    }
}
