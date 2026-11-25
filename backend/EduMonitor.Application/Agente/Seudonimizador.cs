using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EduMonitor.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EduMonitor.Application.Agente;

/// <summary>
/// HU-23 · Seudonimización: antes de que los datos lleguen al modelo, cada estudiante del curso se cambia por un código (E-001,
/// E-002…). Los códigos se asignan por curso en el orden del id del estudiante, así que son estables entre preguntas. La tabla
/// código-nombre queda en el backend; la pantalla siempre recibe nombres. No salen nombres, correos ni ids de Moodle.
/// </summary>
public sealed class Seudonimizador(IEduMonitorDbContext db)
{
    private static readonly Regex Codigo = new(@"\bE-(\d{3})\b", RegexOptions.CultureInvariant);
    private static readonly string[] CamposQueNoSalen = { "moodleId", "moodleUsuarioId", "correo", "email" };

    public async Task<MapaSeudonimos> MapaAsync(int cursoId, CancellationToken ct = default)
    {
        var estudiantes = await db.Matriculas.AsNoTracking()
            .Where(m => m.CursoId == cursoId)
            .Select(m => new { m.Estudiante.Id, m.Estudiante.Nombre, m.Estudiante.Apellido })
            .ToListAsync(ct);
        return new MapaSeudonimos(estudiantes
            .OrderBy(e => e.Id)
            .Select((e, i) => new Seudonimo(e.Id, $"E-{i + 1:000}", e.Nombre.Trim(), e.Apellido.Trim()))
            .ToList());
    }

    /// <summary>Cambia los nombres de un texto (la pregunta o un turno anterior) por sus códigos.</summary>
    public static string Ocultar(string texto, MapaSeudonimos mapa)
    {
        if (string.IsNullOrEmpty(texto) || mapa.Estudiantes.Count == 0)
        {
            return texto;
        }

        // Primero las formas completas, de la más larga a la más corta; después nombre o apellido sueltos si son únicos en el curso.
        var formas = new List<(string Forma, string Codigo)>();
        foreach (var e in mapa.Estudiantes)
        {
            formas.Add(($"{e.Nombre} {e.Apellido}", e.Codigo));
            formas.Add(($"{e.Apellido} {e.Nombre}", e.Codigo));
            formas.Add(($"{e.Apellido}, {e.Nombre}", e.Codigo));
        }

        foreach (var grupo in mapa.Estudiantes.GroupBy(e => Normalizar(e.Apellido)).Where(g => g.Count() == 1 && g.Key.Length >= 3))
        {
            formas.Add((grupo.Single().Apellido, grupo.Single().Codigo));
        }

        foreach (var grupo in mapa.Estudiantes.GroupBy(e => Normalizar(e.Nombre)).Where(g => g.Count() == 1 && g.Key.Length >= 3))
        {
            formas.Add((grupo.Single().Nombre, grupo.Single().Codigo));
        }

        var resultado = texto;
        foreach (var (forma, codigo) in formas.Where(f => f.Forma.Trim().Length > 0).OrderByDescending(f => f.Forma.Length))
        {
            resultado = Reemplazar(resultado, forma, codigo);
        }

        return resultado;
    }

    /// <summary>Cambia los códigos que escribió el modelo por el nombre del estudiante, para la pantalla.</summary>
    public static string Mostrar(string texto, MapaSeudonimos mapa)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return texto;
        }

        return Codigo.Replace(texto, m => mapa.PorCodigo(m.Value) is { } e ? $"{e.Nombre} {e.Apellido}" : m.Value);
    }

    /// <summary>
    /// Respuesta de una herramienta para el modelo: cada objeto de estudiante (con <c>apellido</c>) cambia <c>nombre</c> y
    /// <c>apellido</c> por <c>codigo</c>; los textos con nombres se seudonimizan; los ids de Moodle y los correos no salen.
    /// </summary>
    public static JsonNode? OcultarJson(JsonNode? nodo, MapaSeudonimos mapa)
    {
        switch (nodo)
        {
            case JsonObject objeto:
                foreach (var campo in CamposQueNoSalen)
                {
                    objeto.Remove(campo);
                }

                if (objeto.ContainsKey("apellido"))
                {
                    var id = (objeto["id"] as JsonValue)?.TryGetValue<int>(out var valor) == true ? valor : (int?)null;
                    var nombre = objeto["nombre"]?.GetValue<string>() ?? string.Empty;
                    var apellido = objeto["apellido"]?.GetValue<string>() ?? string.Empty;
                    var estudiante = (id is int eid ? mapa.PorId(eid) : null) ?? mapa.PorNombre(nombre, apellido);
                    objeto.Remove("nombre");
                    objeto.Remove("apellido");
                    objeto["codigo"] = estudiante?.Codigo ?? "E-???";
                }

                foreach (var clave in objeto.Select(p => p.Key).ToList())
                {
                    var hijo = objeto[clave];
                    var nuevo = OcultarJson(hijo, mapa);
                    if (!ReferenceEquals(nuevo, hijo))
                    {
                        objeto[clave] = nuevo;
                    }
                }

                return objeto;
            case JsonArray arreglo:
                for (var i = 0; i < arreglo.Count; i++)
                {
                    var hijo = arreglo[i];
                    var nuevo = OcultarJson(hijo, mapa);
                    if (!ReferenceEquals(nuevo, hijo))
                    {
                        arreglo[i] = nuevo;
                    }
                }

                return arreglo;
            case JsonValue valorTexto when valorTexto.TryGetValue<string>(out var texto):
                var oculto = Ocultar(texto, mapa);
                return oculto == texto ? valorTexto : JsonValue.Create(oculto);
            default:
                return nodo;
        }
    }

    private static string Reemplazar(string texto, string forma, string codigo)
    {
        var comparador = CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions opciones = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        var salida = new StringBuilder();
        var desde = 0;
        while (desde < texto.Length)
        {
            var relativo = comparador.IndexOf(texto.AsSpan(desde), forma.AsSpan(), opciones, out var largo);
            if (relativo < 0)
            {
                break;
            }

            var indice = desde + relativo;

            var antes = indice == 0 || !char.IsLetter(texto[indice - 1]);
            var despues = indice + largo >= texto.Length || !char.IsLetter(texto[indice + largo]);
            if (antes && despues)
            {
                salida.Append(texto, desde, indice - desde).Append(codigo);
            }
            else
            {
                salida.Append(texto, desde, indice + largo - desde);
            }

            desde = indice + Math.Max(largo, 1);
        }

        return salida.Append(texto, desde, texto.Length - desde).ToString();
    }

    internal static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder();
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sinTildes.Append(char.ToLowerInvariant(c));
            }
        }

        return sinTildes.ToString();
    }
}

public sealed record Seudonimo(int EstudianteId, string Codigo, string Nombre, string Apellido);

/// <summary>Tabla código-nombre de un curso. Nunca sale del backend.</summary>
public sealed class MapaSeudonimos(IReadOnlyList<Seudonimo> estudiantes)
{
    public IReadOnlyList<Seudonimo> Estudiantes { get; } = estudiantes;

    public Seudonimo? PorId(int id) => Estudiantes.FirstOrDefault(e => e.EstudianteId == id);

    public Seudonimo? PorCodigo(string codigo) => Estudiantes.FirstOrDefault(e => string.Equals(e.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));

    public Seudonimo? PorNombre(string nombre, string apellido) => Estudiantes.FirstOrDefault(e =>
        Seudonimizador.Normalizar(e.Nombre) == Seudonimizador.Normalizar(nombre)
        && Seudonimizador.Normalizar(e.Apellido) == Seudonimizador.Normalizar(apellido));
}
