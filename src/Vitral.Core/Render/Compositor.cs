using SkiaSharp;

namespace Vitral.Core.Render;

public sealed record AjustesSalida(int Brillo, int Rotacion)
{
    public static readonly AjustesSalida PorDefecto = new(100, 0);

    public AjustesSalida Normalizado() => new(Math.Clamp(Brillo, 0, 100), NormalizarRotacion(Rotacion));

    public static int NormalizarRotacion(int grados) => (((grados / 90 * 90) % 360) + 360) % 360;
}

public sealed record CuadroRenderizado(byte[] Pantalla, byte[] Vista, IReadOnlyList<CajaCapa> Cajas, int Cuadro);

public sealed class Compositor : IDisposable
{
    public const int CalidadJpeg = 90;

    private static readonly SKSamplingOptions Muestreo = new(SKFilterMode.Linear, SKMipmapMode.None);

    private SKSurface? _tema;
    private SKSurface? _panel;

    public CuadroRenderizado Componer(EntradaEscena entrada, int anchoPanel, int altoPanel, AjustesSalida ajustes, bool conVista = true)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        ArgumentNullException.ThrowIfNull(ajustes);
        var a = ajustes.Normalizado();
        var tema = entrada.Tema;
        _tema = Superficie(_tema, tema.Ancho, tema.Alto);
        var lienzo = _tema.Canvas;
        var cajas = Escena.Dibujar(lienzo, entrada);
        if (a.Brillo < 100)
        {
            using var sombra = new SKPaint { Color = new SKColor(0, 0, 0, (byte)Math.Round(255 * (1 - (a.Brillo / 100f)))) };
            lienzo.DrawRect(0, 0, tema.Ancho, tema.Alto, sombra);
        }

        using var imagenTema = _tema.Snapshot();
        if (a.Rotacion == 0 && tema.Ancho == anchoPanel && tema.Alto == altoPanel)
        {
            var unico = Jpeg(imagenTema);
            return new CuadroRenderizado(unico, unico, cajas, entrada.Cuadro);
        }

        using var girada = AlPanel(imagenTema, anchoPanel, altoPanel, a.Rotacion);
        var pantalla = Jpeg(girada);
        var vista = conVista ? Jpeg(imagenTema) : pantalla;
        return new CuadroRenderizado(pantalla, vista, cajas, entrada.Cuadro);
    }

    public void Dispose()
    {
        _tema?.Dispose();
        _panel?.Dispose();
        _tema = _panel = null;
    }

    private SKImage AlPanel(SKImage imagen, int ancho, int alto, int rotacion)
    {
        _panel = Superficie(_panel, ancho, alto);
        var lienzo = _panel.Canvas;
        lienzo.Clear(SKColors.Black);
        var girada = rotacion is 90 or 270;
        var w = girada ? imagen.Height : imagen.Width;
        var h = girada ? imagen.Width : imagen.Height;
        var escala = Math.Min((float)ancho / w, (float)alto / h);
        lienzo.Save();
        lienzo.Translate(ancho / 2f, alto / 2f);
        lienzo.RotateDegrees(rotacion);
        lienzo.Scale(escala);
        lienzo.DrawImage(imagen, new SKRect(-imagen.Width / 2f, -imagen.Height / 2f, imagen.Width / 2f, imagen.Height / 2f), Muestreo);
        lienzo.Restore();
        return _panel.Snapshot();
    }

    private static byte[] Jpeg(SKImage imagen)
    {
        using var datos = imagen.Encode(SKEncodedImageFormat.Jpeg, CalidadJpeg);
        return datos.ToArray();
    }

    private static SKSurface Superficie(SKSurface? actual, int ancho, int alto)
    {
        if (actual is not null && actual.Canvas.DeviceClipBounds.Width == ancho && actual.Canvas.DeviceClipBounds.Height == alto)
        {
            return actual;
        }
        actual?.Dispose();
        return SKSurface.Create(new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("No se pudo crear la superficie de dibujo.");
    }
}
