using SkiaSharp;
using Vitral.Core.Temas;

namespace Vitral.Core.Render;

public sealed record ResultadoLegibilidad(string CapaId, double Contraste, bool Suficiente)
{
    public const double Minimo = 4.5;
}

public static class Legibilidad
{
    private const int CuadrosDeMuestra = 12;

    public static ResultadoLegibilidad? Medir(EntradaEscena entrada, string capaId)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        var tema = entrada.Tema;
        var capa = tema.Capas.FirstOrDefault(c => c.Id == capaId);
        var (color, velo, tamano) = capa switch
        {
            CapaSensor s => (s.Color, s.Velo, s.Tamano),
            CapaTexto t => (t.Color, t.Velo, t.Tamano),
            _ => (string.Empty, false, 0f),
        };
        var caja = capa is null ? null : Escena.Medir(capa, entrada);
        if (caja is null || caja.Ancho <= 0 || caja.Alto <= 0)
        {
            return null;
        }

        var texto = Luminancia(SKColor.Parse(color));
        using var superficie = SKSurface.Create(new SKImageInfo(tema.Ancho, tema.Alto, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var mapa = new SKBitmap(new SKImageInfo(tema.Ancho, tema.Alto, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        var rect = SKRectI.Intersect(
            SKRectI.Round(new SKRect(caja.X, caja.Y, caja.X + caja.Ancho, caja.Y + caja.Alto)),
            new SKRectI(0, 0, tema.Ancho, tema.Alto));
        if (rect.IsEmpty)
        {
            return null;
        }

        var peor = double.MaxValue;
        foreach (var cuadro in Muestras(entrada.Fondo.Cantidad))
        {
            Escena.DibujarFondo(superficie.Canvas, tema, entrada.Fondo, cuadro);
            if (velo)
            {
                var margen = Math.Max(6, tamano * 0.14f);
                using var pintura = new SKPaint { Color = new SKColor(0x0d, 0x0f, 0x14, 184) };
                superficie.Canvas.DrawRect(caja.X - margen, caja.Y - margen, caja.Ancho + (margen * 2), caja.Alto + (margen * 2), pintura);
            }
            superficie.ReadPixels(mapa.Info, mapa.GetPixels(), mapa.RowBytes, 0, 0);
            peor = Math.Min(peor, ContrastePeor(mapa, rect, texto));
        }
        var redondeado = Math.Round(peor, 1);
        return new ResultadoLegibilidad(capaId, redondeado, peor >= ResultadoLegibilidad.Minimo);
    }

    public static double Luminancia(SKColor c)
    {
        static double Canal(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return (0.2126 * Canal(c.Red)) + (0.7152 * Canal(c.Green)) + (0.0722 * Canal(c.Blue));
    }

    public static double Contraste(double l1, double l2) => (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);

    internal static double ContrastePeor(SKBitmap mapa, SKRectI rect, double luminanciaTexto)
    {
        var luminancias = new List<double>(rect.Width * rect.Height / 4);
        var paso = Math.Max(1, Math.Min(rect.Width, rect.Height) / 60);
        for (var y = rect.Top; y < rect.Bottom; y += paso)
        {
            for (var x = rect.Left; x < rect.Right; x += paso)
            {
                luminancias.Add(Luminancia(mapa.GetPixel(x, y)));
            }
        }
        if (luminancias.Count == 0)
        {
            return double.MaxValue;
        }
        luminancias.Sort();
        var mediana = luminancias[luminancias.Count / 2];
        var indice = luminanciaTexto >= mediana
            ? (int)Math.Floor((luminancias.Count - 1) * 0.95)
            : (int)Math.Ceiling((luminancias.Count - 1) * 0.05);
        return Contraste(luminanciaTexto, luminancias[indice]);
    }

    private static IEnumerable<int> Muestras(int cantidad)
    {
        if (cantidad <= CuadrosDeMuestra)
        {
            return Enumerable.Range(0, cantidad);
        }
        return Enumerable.Range(0, CuadrosDeMuestra).Select(i => i * cantidad / CuadrosDeMuestra);
    }
}
