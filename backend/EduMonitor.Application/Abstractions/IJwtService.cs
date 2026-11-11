using EduMonitor.Domain.Entities;

namespace EduMonitor.Application.Abstractions;

/// <summary>Emite el JWT de sesión con los claims <c>sub</c> (id), <c>name</c> (usuario) y <c>role</c> (DOCENTE o ADMIN).</summary>
public interface IJwtService
{
    TokenEmitido Emitir(Usuario usuario);

    /// <summary>
    /// TA-14.2 · Token de servicio para el agente: <c>role=AGENTE</c>, <c>docente</c> (id del docente que preguntó) y <c>curso</c>,
    /// con vencimiento de <see cref="TokenAgente.Minutos"/> minutos. Solo sirve en <c>/api/tools/*</c>.
    /// </summary>
    TokenEmitido EmitirTokenAgente(int docenteId, int cursoId);
}

/// <summary>Claims del token de servicio del agente (docs/contratos-api.md, HU-13 y HU-14).</summary>
public static class TokenAgente
{
    public const string Rol = "AGENTE";
    public const string ClaimDocente = "docente";
    public const string ClaimCurso = "curso";
    public const int Minutos = 2;
}

public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn);
