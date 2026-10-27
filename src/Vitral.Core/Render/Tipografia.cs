using SkiaSharp;
using Vitral.Core.Temas;

namespace Vitral.Core.Render;

internal static class Tipografia
{
    public static readonly SKTypeface Cifras = Cargar("BarlowSemiCondensed-SemiBold.ttf");
    public static readonly SKTypeface Etiquetas = Cargar("BarlowSemiCondensed-Medium.ttf");

    private static SKTypeface Cargar(string archivo)
    {
        var ensamblado = typeof(Tipografia).Assembly;
        var nombre = ensamblado.GetManifestResourceNames().Single(n => n.EndsWith(archivo, StringComparison.Ordinal));
        using var flujo = ensamblado.GetManifestResourceStream(nombre)
            ?? throw new InvalidOperationException($"Falta la fuente {archivo}.");
        return SKTypeface.FromStream(flujo) ?? throw new InvalidOperationException($"La fuente {archivo} está dañada.");
    }
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
