using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using EduMonitor.Domain.Enums;

namespace EduMonitor.Application.Alertas;

/// <summary>
/// TA-12.1 · Evalúa las reglas activas de un curso sobre los datos guardados de la última sincronización.
/// Código determinista en Application, sin acceso a la base ni a Moodle; el modelo de IA nunca calcula alertas.
/// </summary>
public static class MotorAlertas
{
    /// <summary>Todas las alertas detectadas: una por cada regla evaluada y estudiante que la cumple.</summary>
    public static IReadOnlyList<AlertaDetectada> Evaluar(
        DatosCurso datos,
        IReadOnlyList<ReglaEvaluada> reglas,
        DateTimeOffset ahora)
    {
        var contextos = datos.Estudiantes.Select(estudiante => ConstruirContexto(datos, estudiante)).ToList();
        var detectadas = new List<AlertaDetectada>();

        foreach (var regla in reglas)
        {
            var instancia = Crear(regla.Definicion.Codigo);
            foreach (var contexto in contextos)
            {
                if (instancia.Evaluar(contexto, regla.Umbral, ahora) is { } detectada)
                {
                    detectadas.Add(detectada);
                }
            }
        }

        return detectadas;
    }

    /// <summary>Los datos de un estudiante que necesitan las reglas: notas ordenadas, categorías y entregas faltantes.</summary>
    public static ContextoEstudiante ConstruirContexto(DatosCurso datos, Estudiante estudiante)
    {
        var notas = CalculadoraIndicadores.NotasOrdenadas(datos, estudiante.Id);
        var porActividad = datos.Actividades.ToDictionary(a => a.Id);

        var sinEntregas = datos.Calificaciones
            .Where(c => c.EstudianteId == estudiante.Id && c.Estado == EstadoCalificacion.SIN_ENTREGA)
            .Where(c => porActividad.ContainsKey(c.ActividadId))
            .Select(c => new ActividadSinEntrega(porActividad[c.ActividadId].Nombre, porActividad[c.ActividadId].FechaLimite))
            .ToList();

        return new ContextoEstudiante(
            estudiante.Id,
            notas.Select(n => n.Nota).ToList(),
            notas.Select(n => (n.Actividad.Categoria, n.Nota)).ToList(),
            sinEntregas,
            notas.Count);
    }

    private static IReglaAlerta Crear(string codigo) => codigo switch
    {
        DefinicionesRegla.CodigoR1 => new ReglaR1DescensoReciente(),
        DefinicionesRegla.CodigoR2 => new ReglaR2Atrasos(),
        DefinicionesRegla.CodigoR4 => new ReglaR4PromedioBajo(),
        _ => throw new NotSupportedException($"La regla {codigo} no está implementada."),
    };
}
