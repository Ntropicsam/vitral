using System.Collections.Concurrent;
using SkiaSharp;
using Vitral.Core.Temas;

namespace Vitral.Core.Render;

public sealed record FuenteVitral(string Id, string Nombre, string Archivo, string ArchivoEtiqueta);

public static class Fuentes
{
    public const string PorDefecto = "barlow";

    public static readonly IReadOnlyList<FuenteVitral> Todas =
    [
        new(PorDefecto, "Barlow Semi Condensed", "BarlowSemiCondensed-SemiBold.ttf", "BarlowSemiCondensed-Medium.ttf"),
        new("saira", "Saira Condensed", "SairaCondensed-SemiBold.ttf", "SairaCondensed-SemiBold.ttf"),
        new("rajdhani", "Rajdhani", "Rajdhani-SemiBold.ttf", "Rajdhani-SemiBold.ttf"),
        new("chakra", "Chakra Petch", "ChakraPetch-SemiBold.ttf", "ChakraPetch-SemiBold.ttf"),
        new("bebas", "Bebas Neue", "BebasNeue-Regular.ttf", "BebasNeue-Regular.ttf"),
        new("anton", "Anton", "Anton-Regular.ttf", "Anton-Regular.ttf"),
        new("plexmono", "IBM Plex Mono", "IBMPlexMono-SemiBold.ttf", "IBMPlexMono-SemiBold.ttf"),
        new("russo", "Russo One", "RussoOne-Regular.ttf", "RussoOne-Regular.ttf"),
    ];

    public static string Normalizar(string? id) =>
        id is not null && Todas.Any(f => f.Id == id) ? id : PorDefecto;
}

internal static class Tipografia
{
    private static readonly ConcurrentDictionary<string, SKTypeface> Cargadas = new(StringComparer.Ordinal);

    public static SKTypeface Cifras(string? fuente) => Cargar(Buscar(fuente).Archivo);

    public static SKTypeface Etiquetas(string? fuente) => Cargar(Buscar(fuente).ArchivoEtiqueta);

    private static FuenteVitral Buscar(string? id)
    {
        var normal = Fuentes.Normalizar(id);
        return Fuentes.Todas.First(f => f.Id == normal);
    }

    private static SKTypeface Cargar(string archivo) => Cargadas.GetOrAdd(archivo, a =>
    {
        var ensamblado = typeof(Tipografia).Assembly;
        var nombre = ensamblado.GetManifestResourceNames().Single(n => n.EndsWith(a, StringComparison.Ordinal));
        using var flujo = ensamblado.GetManifestResourceStream(nombre)
            ?? throw new InvalidOperationException($"Falta la fuente {a}.");
        return SKTypeface.FromStream(flujo) ?? throw new InvalidOperationException($"La fuente {a} está dañada.");
    });
}

internal sealed class LineaTexto : IDisposable
{
    private readonly SKFont _fuente;
    private readonly string _texto;
    private readonly float[] _avances;

    public LineaTexto(string texto, SKTypeface tipo, float tamano)
    {
        _texto = texto;
        _fuente = new SKFont(tipo, tamano) { Subpixel = true, Edging = SKFontEdging.Antialias };
        var glifos = _fuente.GetGlyphs(texto);
        var anchos = _fuente.GetGlyphWidths(glifos);
        var cero = _fuente.MeasureText("0");
        _avances = new float[texto.Length];
        for (var i = 0; i < texto.Length && i < anchos.Length; i++)
        {
            _avances[i] = char.IsAsciiDigit(texto[i]) ? cero : anchos[i];
        }
        Ancho = _avances.Sum();
        AltoMayuscula = _fuente.Metrics.CapHeight > 0 ? _fuente.Metrics.CapHeight : tamano * 0.7f;
    }

    public float Ancho { get; }

    public float AltoMayuscula { get; }

    public float Tamano => _fuente.Size;

    public void Dibujar(SKCanvas lienzo, float izquierda, float baseY, SKPaint pintura)
    {
        var x = izquierda;
        for (var i = 0; i < _texto.Length; i++)
        {
            var c = _texto[i].ToString();
            var natural = _fuente.MeasureText(c);
            var desplazamiento = (_avances[i] - natural) / 2;
            lienzo.DrawText(c, x + desplazamiento, baseY, SKTextAlign.Left, _fuente, pintura);
            x += _avances[i];
        }
    }

    public void Dispose() => _fuente.Dispose();

    public static float Izquierda(float x, float ancho, Alineacion alineacion) => alineacion switch
    {
        Alineacion.Centro => x - (ancho / 2),
        Alineacion.Derecha => x - ancho,
        _ => x,
    };
}
