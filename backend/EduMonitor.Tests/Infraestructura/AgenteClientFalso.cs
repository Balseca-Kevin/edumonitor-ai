using System.Text.Json;
using EduMonitor.Application.Agente;

namespace EduMonitor.Tests.Infraestructura;

/// <summary>
/// Agente en memoria para las pruebas del proxy del chat. Guarda cada petición recibida; con <see cref="Caido"/> responde
/// como un agente que no contestó a tiempo.
/// </summary>
public sealed class AgenteClientFalso : IAgenteClient
{
    public List<PeticionAgente> Peticiones { get; } = new();

    public bool Caido { get; set; }

    public Task<RespuestaAgente> ConsultarAsync(PeticionAgente peticion, CancellationToken ct = default)
    {
        Peticiones.Add(peticion);
        if (Caido)
        {
            throw new AgenteNoDisponibleException("El asistente no respondió en 60 segundos.");
        }

        var parametros = JsonSerializer.SerializeToElement(new { cursoId = peticion.CursoId });
        return Task.FromResult(new RespuestaAgente(
            "Hechos: 1 estudiante tiene una nota cero en Examen P1.",
            new[] { new TrazaAgente("obtener_calificaciones", parametros, "OK", 84) },
            "qwen2.5:7b",
            5400));
    }
}
