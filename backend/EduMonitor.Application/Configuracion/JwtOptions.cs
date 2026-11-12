namespace EduMonitor.Application.Configuracion;

/// <summary>Emisión y validación del JWT. Sección <c>Jwt</c> (variables <c>Jwt__Secret</c>, <c>Jwt__Issuer</c>, <c>Jwt__Audience</c>, <c>Jwt__ExpiraMinutos</c>).</summary>
public sealed class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "EduMonitor";
    public string Audience { get; set; } = "EduMonitor";
    public int ExpiraMinutos { get; set; } = 120;
}
