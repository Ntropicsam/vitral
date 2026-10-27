using SkiaSharp;

namespace Vitral.Core.Medios;

public static class Medidas
{
    public const int LadoMaximoEntrada = 8192;

    public const long PixelesImagen = 32L * 1024 * 1024;

    public const long PixelesCuadro = 4L * 1024 * 1024;

    public const long PixelesAnimacion = 4L * 1024 * 1024 * 1024;

    public static void Exigir(int ancho, int alto, long topePixeles)
    {
        if (ancho <= 0 || alto <= 0)
        {
            throw new FormatoDeMedioException("La imagen no trae medidas válidas.");
        }
        if (ancho > LadoMaximoEntrada || alto > LadoMaximoEntrada || (long)ancho * alto > topePixeles)
        {
            throw new FormatoDeMedioException($"La imagen es demasiado grande ({ancho} × {alto}).");
        }
    }

    public static SKImageInfo ExigirCodificada(ReadOnlySpan<byte> datos, long topePixeles)
    {
        using var codec = SKCodec.Create(SKData.CreateCopy(datos))
            ?? throw new FormatoDeMedioException("Un cuadro no es una imagen válida.");
        Exigir(codec.Info.Width, codec.Info.Height, topePixeles);
        return codec.Info;
    }

    public static void Exigir(Animacion animacion)
    {
        ArgumentNullException.ThrowIfNull(animacion);
        long total = 0;
        foreach (var cuadro in animacion.Cuadros)
        {
            var info = ExigirCodificada(cuadro, PixelesCuadro);
            total += (long)info.Width * info.Height;
            if (total > PixelesAnimacion)
            {
                throw new FormatoDeMedioException("La animación es demasiado pesada para la pantalla.");
            }
        }
    }
}
