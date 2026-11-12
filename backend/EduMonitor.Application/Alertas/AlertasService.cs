using System.Text.Json;
using EduMonitor.Application.Abstractions;
using EduMonitor.Application.Indicadores;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Alertas;

/// <summary>
/// TA-12.2 y TA-12.3 · Reglas configurables y alertas de un curso (HU-12). Las reglas se crean con sus umbrales
/// por defecto en la primera consulta; el PUT valida todo el arreglo antes de escribir y luego recalcula.
/// El recálculo es idempotente: crea o actualiza las alertas ACTIVAS detectadas, retira las que ya no aplican y
/// nunca toca las REVISADA (quedan como historial). Sin datos de sincronización se retiran las activas.
/// Todo es código determinista en Application: el modelo de IA no calcula alertas ni lee la base.
/// </summary>
public sealed class AlertasService(IEduMonitorDbContext db, IndicadorService indicadores, TimeProvider reloj)
{
    /// <summary>Texto de toda alerta, tal como lo exige docs/contratos-api.md: nunca «reprobará» ni «en riesgo».</summary>
    public const string TituloAlerta = "Requiere atención";

    /// <summary>TA-12.2 · Las reglas del curso en el orden del catálogo; null si el curso no existe. La primera consulta crea las tres por defecto.</summary>
    public async Task<IReadOnlyList<ReglaDto>?> ObtenerReglasAsync(int cursoId, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return null;
        }

        if (await AsegurarReglasAsync(cursoId, ct))
        {
            await RecalcularAsync(cursoId, ct);
        }

        var reglas = await ReglasDelCursoAsync(cursoId, ct);
        return reglas.OrderBy(r => Orden(r.Codigo)).Select(ADto).ToList();
    }

    /// <summary>
    /// TA-12.2 · Aplica los cambios del PUT y recalcula las alertas. 404 (CursoExiste false) si el curso no existe;
    /// 400 con Detalle si un código no existe o el umbral sale de rango. Si algo falla, no se escribe nada.
    /// </summary>
    public async Task<ResultadoReglas> ActualizarReglasAsync(int cursoId, IReadOnlyList<CambioRegla> cambios, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return new ResultadoReglas(false);
        }

        foreach (var cambio in cambios)
        {
            var definicion = DefinicionesRegla.PorCodigo(cambio.Codigo);
            if (definicion is null)
            {
                return new ResultadoReglas(true, $"La regla {cambio.Codigo} no existe.");
            }

            if (!DefinicionesRegla.UmbralEnRango(definicion, cambio.Umbral))
            {
                return new ResultadoReglas(true,
                    $"El umbral de {definicion.Codigo} debe estar entre {definicion.UmbralMinimo} y {definicion.UmbralMaximo}.");
            }
        }

        var reglas = await ReglasDelCursoAsync(cursoId, ct);
        if (reglas.Count == 0)
        {
            // Si el docente cambia una regla antes de haberlas consultado, nacen las tres por defecto y aplica el cambio.
            reglas = ReglasConDefecto(cursoId);
            db.ReglasAlerta.AddRange(reglas);
        }

        foreach (var cambio in cambios)
        {
            var regla = reglas.FirstOrDefault(r => string.Equals(r.Codigo, cambio.Codigo, StringComparison.OrdinalIgnoreCase));
            if (regla is null)
            {
                regla = new ReglaAlerta { CursoId = cursoId, Codigo = cambio.Codigo.ToUpperInvariant() };
                reglas.Add(regla);
                db.ReglasAlerta.Add(regla);
            }

            regla.Umbral = cambio.Umbral;
            regla.Activa = cambio.Activa;
        }

        await db.SaveChangesAsync(ct);
        await RecalcularAsync(cursoId, ct);
        return new ResultadoReglas(true);
    }

    /// <summary>TA-12.3 · Alertas del curso en un estado (ACTIVA por defecto). La lista viene vacía si el curso todavía no tiene datos.</summary>
    public async Task<ConsultaAlertas> ObtenerAlertasAsync(int cursoId, string? estado, CancellationToken ct = default)
    {
        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, ct))
        {
            return new ConsultaAlertas(EstadoConsultaAlertas.CursoNoExiste, Array.Empty<AlertaDto>());
        }

        var filtro = (estado ?? EstadosAlerta.Activa).ToUpperInvariant();
        if (filtro != EstadosAlerta.Activa && filtro != EstadosAlerta.Revisada)
        {
            return new ConsultaAlertas(EstadoConsultaAlertas.EstadoInvalido, Array.Empty<AlertaDto>(),
                "El estado debe ser ACTIVA o REVISADA.");
        }

        // Un curso sincronizado antes de HU-12 no tiene reglas: nacen por defecto y se calculan sus alertas ahora,
        // para que la pantalla y la tarjeta «Requiere atención» no muestren 0 con datos que sí las tienen.
        if (await AsegurarReglasAsync(cursoId, ct))
        {
            await RecalcularAsync(cursoId, ct);
        }

        var alertas = await db.Alertas.AsNoTracking()
            .Include(a => a.Regla)
            .Include(a => a.Estudiante)
            .Where(a => a.Regla.CursoId == cursoId && a.Estado == filtro)
            .ToListAsync(ct);

        // El orden va en memoria: SQLite (las pruebas) no admite ORDER BY sobre DateTimeOffset.
        var ordenadas = alertas.OrderByDescending(a => a.Generada).ThenByDescending(a => a.Id).ToList();
        return new ConsultaAlertas(EstadoConsultaAlertas.Ok, ordenadas.Select(ADto).ToList());
    }

    /// <summary>TA-12.3 · Marca una alerta como REVISADA con su fecha. 404 si la alerta no es de ese curso; 400 si el estado no es REVISADA.</summary>
    public async Task<ResultadoAlerta> MarcarAsync(int cursoId, int alertaId, string? estado, CancellationToken ct = default)
    {
        var alerta = await db.Alertas
            .Include(a => a.Regla)
            .Include(a => a.Estudiante)
            .FirstOrDefaultAsync(a => a.Id == alertaId && a.Regla.CursoId == cursoId, ct);
        if (alerta is null)
        {
            return new ResultadoAlerta(EstadoResultadoAlerta.NoExiste);
        }

        if (!string.Equals(estado, EstadosAlerta.Revisada, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoAlerta(EstadoResultadoAlerta.EstadoInvalido, Detalle: "El estado debe ser REVISADA.");
        }

        alerta.Estado = EstadosAlerta.Revisada;
        alerta.Revisada = reloj.GetLocalNow();
        await db.SaveChangesAsync(ct);
        return new ResultadoAlerta(EstadoResultadoAlerta.Actualizada, ADto(alerta));
    }

    /// <summary>
    /// TA-12.3 · Recalcula las alertas ACTIVAS del curso con sus reglas activas, sobre los datos de la última
    /// sincronización. Se dispara al terminar una sincronización y al cambiar una regla. Crea o actualiza las
    /// detectadas, retira las que ya no aplican y no toca las REVISADA. Sin datos se retiran las activas.
    /// </summary>
    public async Task RecalcularAsync(int cursoId, CancellationToken ct = default)
    {
        await AsegurarReglasAsync(cursoId, ct);
        var reglas = await db.ReglasAlerta.AsNoTracking()
            .Where(r => r.CursoId == cursoId && r.Activa)
            .ToListAsync(ct);
        var activas = await db.Alertas
            .Where(a => a.Estado == EstadosAlerta.Activa && a.Regla.CursoId == cursoId)
            .ToListAsync(ct);
        // Lo que el docente ya revisó no vuelve a activas mientras la evidencia sea la misma; si cambia, nace una alerta nueva.
        var revisadas = (await db.Alertas.AsNoTracking()
                .Where(a => a.Estado == EstadosAlerta.Revisada && a.Regla.CursoId == cursoId)
                .Select(a => new { a.ReglaId, a.EstudianteId, a.Evidencia })
                .ToListAsync(ct))
            .Select(a => (a.ReglaId, a.EstudianteId, a.Evidencia))
            .ToHashSet();

        var datos = await indicadores.CargarDatosAsync(cursoId, ct);
        if (datos is null)
        {
            if (activas.Count > 0)
            {
                db.Alertas.RemoveRange(activas);
                await db.SaveChangesAsync(ct);
            }

            return;
        }

        var porCodigo = new Dictionary<string, ReglaAlerta>(StringComparer.OrdinalIgnoreCase);
        var evaluables = new List<ReglaEvaluada>();
        foreach (var regla in reglas)
        {
            var definicion = DefinicionesRegla.PorCodigo(regla.Codigo);
            if (definicion is null)
            {
                continue; // códigos fuera del Sprint 2 (R3, G1): se ignoran hasta que existan sus clases.
            }

            porCodigo[regla.Codigo] = regla;
            evaluables.Add(new ReglaEvaluada(definicion, regla.Umbral));
        }

        var detectadas = MotorAlertas.Evaluar(datos, evaluables, reloj.GetLocalNow());
        var existentes = new Dictionary<(int ReglaId, int EstudianteId), Alerta>();
        foreach (var activa in activas)
        {
            existentes.TryAdd((activa.ReglaId, activa.EstudianteId), activa);
        }

        var detectadasAhora = new HashSet<(int, int)>();
        foreach (var detectada in detectadas)
        {
            var clave = (ReglaId: porCodigo[detectada.CodigoRegla].Id, EstudianteId: detectada.EstudianteId);
            if (existentes.TryGetValue(clave, out var existente))
            {
                detectadasAhora.Add(clave);
                // Solo cambia la evidencia: conserva su fecha de generación y su estado ACTIVA.
                if (existente.Evidencia != detectada.EvidenciaJson)
                {
                    existente.Evidencia = detectada.EvidenciaJson;
                }
            }
            else if (revisadas.Contains((clave.ReglaId, clave.EstudianteId, detectada.EvidenciaJson)))
            {
                continue;
            }
            else if (detectadasAhora.Add(clave))
            {
                db.Alertas.Add(new Alerta
                {
                    ReglaId = clave.ReglaId,
                    EstudianteId = clave.EstudianteId,
                    Evidencia = detectada.EvidenciaJson,
                    Estado = EstadosAlerta.Activa,
                    Generada = reloj.GetLocalNow(),
                });
            }
        }

        foreach (var activa in activas)
        {
            if (!detectadasAhora.Contains((activa.ReglaId, activa.EstudianteId)))
            {
                db.Alertas.Remove(activa);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Crea las tres reglas por defecto si el curso todavía no tiene ninguna. True si las creó.</summary>
    private async Task<bool> AsegurarReglasAsync(int cursoId, CancellationToken ct)
    {
        if (await db.ReglasAlerta.AnyAsync(r => r.CursoId == cursoId, ct))
        {
            return false;
        }

        db.ReglasAlerta.AddRange(ReglasConDefecto(cursoId));
        await db.SaveChangesAsync(ct);
        return true;
    }

    private Task<List<ReglaAlerta>> ReglasDelCursoAsync(int cursoId, CancellationToken ct) =>
        db.ReglasAlerta.Where(r => r.CursoId == cursoId).ToListAsync(ct);

    private static List<ReglaAlerta> ReglasConDefecto(int cursoId) =>
        DefinicionesRegla.Todas
            .Select(d => new ReglaAlerta { CursoId = cursoId, Codigo = d.Codigo, Umbral = d.UmbralPorDefecto, Activa = true })
            .ToList();

    /// <summary>Orden del catálogo (R1, R2, R4); los códigos desconocidos van al final.</summary>
    private static int Orden(string codigo)
    {
        for (var i = 0; i < DefinicionesRegla.Todas.Count; i++)
        {
            if (string.Equals(DefinicionesRegla.Todas[i].Codigo, codigo, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static ReglaDto ADto(ReglaAlerta regla)
    {
        var definicion = DefinicionesRegla.PorCodigo(regla.Codigo);
        return new ReglaDto(regla.Codigo, definicion?.Nombre ?? regla.Codigo, regla.Umbral, regla.Activa, definicion?.Condicion ?? string.Empty);
    }

    private static AlertaDto ADto(Alerta alerta)
    {
        using var evidencia = JsonDocument.Parse(string.IsNullOrWhiteSpace(alerta.Evidencia) ? "{}" : alerta.Evidencia);
        return new AlertaDto(
            alerta.Id,
            alerta.Regla.Codigo,
            TituloAlerta,
            new EstudianteAlertaDto(alerta.Estudiante.Id, alerta.Estudiante.Nombre, alerta.Estudiante.Apellido),
            alerta.Estado,
            alerta.Generada,
            alerta.Revisada,
            evidencia.RootElement.Clone());
    }
}
