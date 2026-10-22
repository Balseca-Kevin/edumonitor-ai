using EduMonitor.Domain.Entities;

namespace EduMonitor.Application.Abstractions;

/// <summary>Emite el JWT de sesión con los claims <c>sub</c> (id), <c>name</c> (usuario) y <c>role</c> (DOCENTE o ADMIN).</summary>
public interface IJwtService
{
    TokenEmitido Emitir(Usuario usuario);
}

public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn);
