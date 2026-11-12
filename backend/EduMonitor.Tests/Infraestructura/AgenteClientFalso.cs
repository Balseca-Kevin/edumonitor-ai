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

    /// <summary>Estado de Ollama que informa <c>GET /salud</c>.</summary>
    public string Ollama { get; set; } = "OK";

    /// <summary>Simula un agente que no contesta /salud: la espera se corta con el tiempo máximo de la API.</summary>
    public bool Lento { get; set; }

    public async Task<SaludAgente> SaludAsync(CancellationToken ct = default)
    {
        if (Caido)
        {
            throw new AgenteNoDisponibleException("No se pudo conectar con el agente.");
        }

        if (Lento)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }

        return new SaludAgente("OK", "qwen2.5:7b", Ollama);
    }

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
