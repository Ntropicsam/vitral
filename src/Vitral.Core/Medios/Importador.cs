using SkiaSharp;

namespace Vitral.Core.Medios;

public enum TipoMedio
{
    Imagen,
    Animacion,
}

public sealed record MedioImportado(TipoMedio Tipo, string NombreArchivo, byte[] Contenido);

public static class Importador
{
    public const long TamanoMaximo = 200L * 1024 * 1024;

    public const int LadoMaximo = 1920;

    private const int CalidadJpeg = 90;

    public static MedioImportado Importar(string nombreOriginal, byte[] datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (datos.Length == 0)
        {
            throw new FormatoDeMedioException("El archivo está vacío.");
        }
        if (datos.Length > TamanoMaximo)
        {
            throw new FormatoDeMedioException("El archivo pesa más de 200 MB.");
        }

        if (LectorZt.Parece(datos) && nombreOriginal.EndsWith(".zt", StringComparison.OrdinalIgnoreCase))
        {
            return GuardarAnimacion(LectorZt.Leer(datos));
        }

        using var codec = SKCodec.Create(SKData.CreateCopy(datos))
            ?? throw new FormatoDeMedioException("Ese archivo no es una imagen que Vitral sepa abrir.");

        if (codec.FrameCount > 1)
        {
            Medidas.Exigir(codec.Info.Width, codec.Info.Height, Medidas.PixelesCuadro);
            if ((long)codec.Info.Width * codec.Info.Height * codec.FrameCount > Medidas.PixelesAnimacion)
            {
                throw new FormatoDeMedioException("La animación es demasiado pesada para la pantalla.");
            }
            return GuardarAnimacion(DecodificarAnimacion(codec));
        }
        Medidas.Exigir(codec.Info.Width, codec.Info.Height, Medidas.PixelesImagen);
        return GuardarImagen(codec);
    }

    public static MedioImportado GuardarAnimacion(Animacion animacion)
    {
        Medidas.Exigir(animacion);
        using var memoria = new MemoryStream();
        ArchivoCuadros.Escribir(animacion, memoria);
        return new MedioImportado(TipoMedio.Animacion, "fondo" + ArchivoCuadros.Extension, memoria.ToArray());
    }

    private static Animacion DecodificarAnimacion(SKCodec codec)
    {
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var marcos = codec.FrameInfo;
        var cantidad = Math.Min(marcos.Length, Animacion.MaximoCuadros);
        var cuadros = new byte[cantidad][];
        var duraciones = new int[cantidad];

        using var lienzo = new SKBitmap(info);
        var compuesto = -1;
        for (var i = 0; i < cantidad; i++)
        {
            var requerido = marcos[i].RequiredFrame;
            if (requerido >= 0 && requerido != compuesto)
            {
                lienzo.Erase(SKColors.Transparent);
                for (var j = 0; j <= requerido; j++)
                {
                    Decodificar(codec, lienzo, j, j == 0 ? -1 : j - 1);
                }
            }
            else if (requerido < 0)
            {
                lienzo.Erase(SKColors.Transparent);
            }

            Decodificar(codec, lienzo, i, requerido);
            compuesto = i;
            cuadros[i] = AJpeg(lienzo);
            duraciones[i] = marcos[i].Duration;
        }
        return new Animacion(cuadros, duraciones);
    }

    private static void Decodificar(SKCodec codec, SKBitmap lienzo, int cuadro, int previo)
    {
        var opciones = new SKCodecOptions(cuadro, previo);
        var resultado = codec.GetPixels(lienzo.Info, lienzo.GetPixels(), opciones);
        if (resultado is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            throw new FormatoDeMedioException($"No se pudo leer el cuadro {cuadro + 1} de la animación ({resultado}).");
        }
    }

    private static MedioImportado GuardarImagen(SKCodec codec)
    {
        using var mapa = SKBitmap.Decode(codec)
            ?? throw new FormatoDeMedioException("No se pudo leer la imagen.");
        using var reducido = Reducir(mapa);
        using var imagen = SKImage.FromBitmap(reducido);
        using var png = imagen.Encode(SKEncodedImageFormat.Png, 100);
        return new MedioImportado(TipoMedio.Imagen, "fondo.png", png.ToArray());
    }

    private static byte[] AJpeg(SKBitmap cuadro)
    {
        var escala = Math.Min(1f, (float)LadoMaximo / Math.Max(cuadro.Width, cuadro.Height));
        var ancho = Math.Max(1, (int)(cuadro.Width * escala));
        var alto = Math.Max(1, (int)(cuadro.Height * escala));
        using var superficie = SKSurface.Create(new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul));
        var lienzo = superficie.Canvas;
        lienzo.Clear(SKColors.Black);
        using var imagen = SKImage.FromBitmap(cuadro);
        lienzo.DrawImage(imagen, new SKRect(0, 0, ancho, alto), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        using var foto = superficie.Snapshot();
        using var jpeg = foto.Encode(SKEncodedImageFormat.Jpeg, CalidadJpeg);
        return jpeg.ToArray();
    }

    private static SKBitmap Reducir(SKBitmap mapa)
    {
        var lado = Math.Max(mapa.Width, mapa.Height);
        if (lado <= LadoMaximo)
        {
            return mapa.Copy();
        }
        var escala = (float)LadoMaximo / lado;
        var info = new SKImageInfo(Math.Max(1, (int)(mapa.Width * escala)), Math.Max(1, (int)(mapa.Height * escala)));
        return mapa.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None))
            ?? throw new FormatoDeMedioException("No se pudo reducir la imagen.");
    }
}
