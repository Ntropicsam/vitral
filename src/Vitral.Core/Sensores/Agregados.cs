using System.Text.RegularExpressions;

namespace Vitral.Core.Sensores;

public sealed record Agrupacion(IReadOnlyList<SensorInfo> Sensores, IReadOnlyDictionary<string, IReadOnlyList<string>> Miembros)
{
    public static readonly Agrupacion Vacia = new([], new Dictionary<string, IReadOnlyList<string>>());

    public IEnumerable<string> Expandir(IEnumerable<string> ids) =>
        ids.SelectMany(id => Miembros.TryGetValue(id, out var m) ? m.Append(id) : [id]).Distinct(StringComparer.Ordinal);
}

public static partial class Agregados
{
    public const string Promedio = "promedio";
    public const string Maximo = "maximo";

    public static Agrupacion Agrupar(IReadOnlyList<SensorInfo> sensores)
    {
        ArgumentNullException.ThrowIfNull(sensores);
        var ordenados = new List<SensorInfo>(sensores.Count + 16);
        var miembros = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var dispositivos = sensores.Select(s => s.Hardware).Distinct(StringComparer.Ordinal);
        foreach (var hardware in dispositivos)
        {
            foreach (var tipo in Enum.GetValues<TipoSensor>())
            {
                var grupo = sensores.Where(s => s.Hardware == hardware && s.Tipo == tipo && s.Agregado is null).ToList();
                if (grupo.Count == 0)
                {
                    continue;
                }
                ordenados.AddRange(grupo);
                foreach (var serie in grupo.GroupBy(s => BaseDeSerie(s.Nombre)).Where(g => g.Key is not null && g.Count() >= 2))
                {
                    var ids = serie.Select(s => s.Id).ToList();
                    var raiz = RaizDeId(ids[0]);
                    foreach (var clase in new[] { Promedio, Maximo })
                    {
                        var id = $"{raiz}/{clase}/{tipo}/{serie.Key}";
                        ordenados.Add(new SensorInfo(id, serie.Key!, hardware, tipo, clase));
                        miembros[id] = ids;
                    }
                }
            }
        }
        return new Agrupacion(ordenados, miembros);
    }

    public static void Completar(IDictionary<string, float> valores, Agrupacion agrupacion, IEnumerable<string> pedidos)
    {
        ArgumentNullException.ThrowIfNull(valores);
        ArgumentNullException.ThrowIfNull(agrupacion);
        foreach (var id in pedidos)
        {
            if (!agrupacion.Miembros.TryGetValue(id, out var ids))
            {
                continue;
            }
            var datos = ids.Select(m => valores.TryGetValue(m, out var v) ? v : float.NaN).Where(float.IsFinite).ToList();
            if (datos.Count == 0)
            {
                continue;
            }
            valores[id] = id.Contains($"/{Maximo}/", StringComparison.Ordinal) ? datos.Max() : datos.Average();
        }
    }

    public static string? BaseDeSerie(string nombre)
    {
        var m = Serie().Match(nombre ?? string.Empty);
        if (!m.Success)
        {
            return null;
        }
        var baseNombre = $"{m.Groups["a"].Value.Trim()} {m.Groups["b"].Value.Trim()}".Trim();
        return baseNombre.Length == 0 ? null : baseNombre;
    }

    private static string RaizDeId(string id)
    {
        var partes = id.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 2 ? "/" + string.Join('/', partes[..^2]) : id;
    }

    [GeneratedRegex(@"^(?<a>.*?)\s*#\s*\d+(?<b>.*)$")]
    private static partial Regex Serie();
}
