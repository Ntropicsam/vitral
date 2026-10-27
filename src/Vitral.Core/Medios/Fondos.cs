using SkiaSharp;
using Vitral.Core.Temas;

namespace Vitral.Core.Medios;

public interface IFondoCargado : IDisposable
{
    int Cantidad { get; }

    int DuracionMs(int cuadro);

    SKImage? Imagen(int cuadro);
}

public static class Fondos
{
    public static IFondoCargado Cargar(Fondo fondo, string carpeta)
    {
        ArgumentNullException.ThrowIfNull(fondo);
        var archivo = ValidadorTemas.ArchivoSeguro(fondo.Archivo);
        var ruta = archivo is null ? null : Path.Combine(carpeta, archivo);
        if (ruta is null || !File.Exists(ruta))
        {
            return new FondoColor();
        }
        try
        {
            return fondo.Tipo switch
            {
                TipoFondo.Imagen => new FondoImagen(ruta),
                TipoFondo.Animacion => CargarAnimacion(ruta),
                _ => new FondoColor(),
            };
        }
        catch (Exception e) when (e is FormatoDeMedioException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            Ejecucion.Registro.Escribir($"Fondo no válido en {carpeta}: {e.Message}");
            return new FondoColor();
        }
    }

    private static FondoAnimacion CargarAnimacion(string ruta)
    {
        var animacion = ArchivoCuadros.Leer(ruta);
        Medidas.Exigir(animacion);
        return new FondoAnimacion(animacion);
    }
}

public sealed class FondoColor : IFondoCargado
{
    public int Cantidad => 1;

    public int DuracionMs(int cuadro) => 1000;

    public SKImage? Imagen(int cuadro) => null;

    public void Dispose()
    {
    }
}

public sealed class FondoImagen : IFondoCargado
{
    private readonly SKImage _imagen;

    public FondoImagen(string ruta)
    {
        using var datos = SKData.Create(ruta) ?? throw new FormatoDeMedioException("No se pudo leer la imagen de fondo.");
        Medidas.ExigirCodificada(datos.AsSpan(), Medidas.PixelesImagen);
        _imagen = SKImage.FromEncodedData(datos)?.ToRasterImage()
            ?? throw new FormatoDeMedioException("La imagen de fondo está dañada.");
    }

    public int Cantidad => 1;

    public int DuracionMs(int cuadro) => 1000;

    public SKImage? Imagen(int cuadro) => _imagen;

    public void Dispose() => _imagen.Dispose();
}

public sealed class FondoAnimacion(Animacion animacion) : IFondoCargado
{
    private SKImage? _ultima;
    private int _indiceUltima = -1;

    public int Cantidad => animacion.Cantidad;

    public int DuracionMs(int cuadro) => animacion.DuracionesMs[Indice(cuadro)];

    public SKImage? Imagen(int cuadro)
    {
        var i = Indice(cuadro);
        if (i == _indiceUltima)
        {
            return _ultima;
        }
        _ultima?.Dispose();
        _ultima = SKImage.FromEncodedData(animacion.Cuadros[i])?.ToRasterImage();
        _indiceUltima = i;
        return _ultima;
    }

    public void Dispose()
    {
        _ultima?.Dispose();
        _ultima = null;
        _indiceUltima = -1;
    }

    private int Indice(int cuadro) => ((cuadro % animacion.Cantidad) + animacion.Cantidad) % animacion.Cantidad;
}
