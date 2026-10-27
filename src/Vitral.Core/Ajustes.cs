using System.Text.Json;
using Vitral.Core.Render;

namespace Vitral.Core;

public sealed record Ajustes
{
    public string? TemaActivo { get; init; }
    public int Brillo { get; init; } = 100;
    public int Rotacion { get; init; }

    public bool ModoJuego { get; init; } = true;

    public string? ModeloForzado { get; init; }

    public string Idioma { get; init; } = "auto";

    public AjustesSalida Salida => new AjustesSalida(Brillo, Rotacion).Normalizado();

    public Ajustes Normalizado() => this with
    {
        Brillo = Math.Clamp(Brillo, 0, 100),
        Rotacion = AjustesSalida.NormalizarRotacion(Rotacion),
        TemaActivo = Temas.AlmacenTemas.EsIdValido(TemaActivo) ? TemaActivo : null,
        ModeloForzado = Dispositivo.Catalogo.PorClave(ModeloForzado) is null ? null : ModeloForzado,
        Idioma = Idioma is "es" or "en" ? Idioma : "auto",
    };
}

public sealed class AlmacenAjustes(string ruta)
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public Ajustes Leer()
    {
        try
        {
            if (!File.Exists(ruta))
            {
                return new Ajustes();
            }
            var ajustes = JsonSerializer.Deserialize<Ajustes>(File.ReadAllText(ruta), Opciones);
            return (ajustes ?? new Ajustes()).Normalizado();
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return new Ajustes();
        }
    }

    public void Guardar(Ajustes ajustes)
    {
        ArgumentNullException.ThrowIfNull(ajustes);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        var temporal = ruta + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(ajustes.Normalizado(), Opciones));
        File.Move(temporal, ruta, overwrite: true);
    }
}
