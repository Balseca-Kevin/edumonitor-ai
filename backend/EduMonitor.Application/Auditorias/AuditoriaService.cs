using System.Text.RegularExpressions;
using EduMonitor.Application.Abstractions;
using EduMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Auditorias;

/// <summary>
/// HU-17 · Registro de auditoría: quién hizo qué, cuándo y con qué resultado. Lo escribe un filtro de la API después de cada acción
/// marcada; lo lee la pantalla «Auditoría» (ADMIN). El detalle pasa por <see cref="SinSecretos"/> antes de guardarse.
/// </summary>
public sealed class AuditoriaService(IEduMonitorDbContext db, TimeProvider reloj)
{
    public const int LimitePorDefecto = 200;
    public const int LimiteMaximo = 500;

    // Tokens JWT, «Bearer …», claves con forma de API key y pares clave=valor sensibles.
    private static readonly Regex Secretos = new(
        @"eyJ[\w-]+\.[\w-]+\.[\w-]+|Bearer\s+\S+|sk-[\w-]{8,}|(?i:(token|contrasena|contraseña|password|clave|secret|api_?key)\s*[=:]\s*)\S+",
        RegexOptions.CultureInvariant);

    public async Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        db.Auditorias.Add(new Auditoria
        {
            UsuarioId = registro.UsuarioId,
            Accion = Recortar(registro.Accion, 50),
            Entidad = Recortar(SinSecretos(registro.Entidad), 250),
            CursoId = registro.CursoId,
            Detalle = Recortar(SinSecretos(registro.Detalle), 500),
            Resultado = Recortar(registro.Resultado, 10),
            Fecha = reloj.GetLocalNow(),
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Del más reciente al más antiguo. <paramref name="usuario"/> filtra por nombre de usuario (sin distinguir mayúsculas).</summary>
    public async Task<RespuestaAuditoria> ListarAsync(string? accion, string? usuario, int? limite, CancellationToken ct = default)
    {
        var consulta = db.Auditorias.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(accion))
        {
            var buscada = accion.Trim().ToUpperInvariant();
            consulta = consulta.Where(a => a.Accion == buscada);
        }

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            var buscado = usuario.Trim().ToLower();
            consulta = consulta.Where(a => a.Usuario != null && a.Usuario.NombreUsuario.ToLower().Contains(buscado));
        }

        var total = await consulta.CountAsync(ct);
        var cuantos = Math.Clamp(limite ?? LimitePorDefecto, 1, LimiteMaximo);
        var registros = await consulta
            .OrderByDescending(a => a.Id) // el id crece con la fecha; SQLite (pruebas) no ordena por DateTimeOffset
            .Take(cuantos)
            .Select(a => new RegistroAuditoriaDto(
                a.Id, a.Fecha,
                a.Usuario == null ? null : new UsuarioAuditoriaDto(a.Usuario.Id, a.Usuario.NombreUsuario, a.Usuario.Nombre),
                a.Accion, a.Entidad, a.CursoId, a.Detalle, a.Resultado))
            .ToListAsync(ct);
        var acciones = await db.Auditorias.AsNoTracking().Select(a => a.Accion).Distinct().OrderBy(a => a).ToListAsync(ct);
        return new RespuestaAuditoria(total, acciones, registros);
    }

    public static string SinSecretos(string? texto) =>
        string.IsNullOrEmpty(texto) ? string.Empty : Secretos.Replace(texto, m => m.Groups[1].Success ? $"{m.Groups[1].Value}=••••" : "••••");

    private static string Recortar(string? texto, int largo) =>
        string.IsNullOrEmpty(texto) ? string.Empty : texto.Length <= largo ? texto : texto[..(largo - 1)] + "…";
}

public sealed record RegistroAuditoria(int? UsuarioId, string Accion, string Entidad, int? CursoId, string Detalle, string Resultado);

/// <summary>Respuesta de <c>GET /api/auditoria</c>.</summary>
public sealed record RespuestaAuditoria(int Total, IReadOnlyList<string> Acciones, IReadOnlyList<RegistroAuditoriaDto> Registros);

public sealed record RegistroAuditoriaDto(
    int Id, DateTimeOffset Fecha, UsuarioAuditoriaDto? Usuario, string Accion, string Entidad, int? CursoId, string Detalle, string Resultado);

public sealed record UsuarioAuditoriaDto(int Id, string Usuario, string Nombre);
