namespace EduMonitor.Application.Abstractions;

/// <summary>Hash de contraseñas. Nunca se guardan ni se comparan en texto plano.</summary>
public interface IHasherContrasenas
{
    string Hashear(string contrasena);

    bool Verificar(string hash, string contrasena);
}
