using EduMonitor.Application.Abstractions.Moodle;
using EduMonitor.Domain.Entities;

namespace EduMonitor.Application.Sincronizaciones;

/// <summary>
/// Secciones y módulos del curso leídos en una sincronización. Relaciona cada actividad del libro de calificaciones
/// (tipo de módulo e instancia) con su unidad y con su id de módulo de curso (cmid), que usa la finalización.
/// </summary>
public sealed class EstructuraCurso
{
    private readonly Dictionary<(string Modulo, int Instancia), (Unidad Unidad, int ModuloCursoId)> _modulos = new();

    public int Unidades { get; private set; }

    public void Agregar(Unidad unidad, IEnumerable<ModuloMoodle> modulos)
    {
        Unidades++;
        foreach (var modulo in modulos)
        {
            _modulos[(modulo.TipoModulo, modulo.Instancia)] = (unidad, modulo.Id);
        }
    }

    /// <summary>Unidad de la actividad, o null si el módulo no está en ninguna unidad.</summary>
    public Unidad? UnidadDe(string? modulo, int? instancia) =>
        modulo is not null && instancia is int i && _modulos.TryGetValue((modulo, i), out var dato) ? dato.Unidad : null;

    /// <summary>Tipo de módulo e instancia a partir del cmid que devuelve core_completion_get_activities_completion_status.</summary>
    public (string Modulo, int Instancia)? ActividadDeModulo(int moduloCursoId) =>
        _modulos.Where(m => m.Value.ModuloCursoId == moduloCursoId).Select(m => ((string, int)?)m.Key).FirstOrDefault();
}
