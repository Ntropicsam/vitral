using System.Buffers.Binary;

namespace Vitral.Core.Medios;

public sealed class Animacion
{
    public const int MaximoCuadros = 9000;
    public const int DuracionMinimaMs = 20;

    public const int DuracionPorDefectoMs = 100;

    public Animacion(IReadOnlyList<byte[]> cuadros, IReadOnlyList<int> duracionesMs)
    {
        ArgumentNullException.ThrowIfNull(cuadros);
        ArgumentNullException.ThrowIfNull(duracionesMs);
        if (cuadros.Count == 0)
        {
            throw new ArgumentException("Una animación necesita al menos un cuadro.", nameof(cuadros));
        }
        if (cuadros.Count != duracionesMs.Count)
        {
            throw new ArgumentException("Cada cuadro necesita su duración.", nameof(duracionesMs));
        }
        if (cuadros.Count > MaximoCuadros)
        {
            throw new ArgumentException($"Una animación admite hasta {MaximoCuadros} cuadros.", nameof(cuadros));
        }
        Cuadros = cuadros;
        DuracionesMs = duracionesMs.Select(NormalizarDuracion).ToArray();
    }

    public IReadOnlyList<byte[]> Cuadros { get; }

    public IReadOnlyList<int> DuracionesMs { get; }

    public int Cantidad => Cuadros.Count;

    public static int NormalizarDuracion(int ms) => ms < DuracionMinimaMs ? DuracionPorDefectoMs : Math.Min(ms, 60_000);
}

public static class ArchivoCuadros
{
    public const string Extension = ".cuadros";
    private const int Version = 1;
    private const long TamanoMaximo = 512L * 1024 * 1024;
    private static ReadOnlySpan<byte> Magia => "VTRC"u8;

    public static void Escribir(Animacion animacion, Stream destino)
    {
        ArgumentNullException.ThrowIfNull(animacion);
        using var escritor = new BinaryWriter(destino, System.Text.Encoding.UTF8, leaveOpen: true);
        escritor.Write(Magia);
        escritor.Write(Version);
        escritor.Write(animacion.Cantidad);
        foreach (var d in animacion.DuracionesMs)
        {
            escritor.Write(d);
        }
        foreach (var c in animacion.Cuadros)
        {
            escritor.Write(c.Length);
            escritor.Write(c);
        }
    }

    public static Animacion Leer(ReadOnlySpan<byte> datos)
    {
        if (datos.Length < 12 || !datos[..4].SequenceEqual(Magia))
        {
            throw new FormatoDeMedioException("No es un archivo de cuadros de Vitral.");
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(datos[4..]) != Version)
        {
            throw new FormatoDeMedioException("Versión de archivo de cuadros desconocida.");
        }
        var cantidad = BinaryPrimitives.ReadInt32LittleEndian(datos[8..]);
        var lector = new LectorBinario(datos, 12);
        return LeerCuerpo(ref lector, cantidad, duracionesAcumuladas: false);
    }

    public static Animacion Leer(string ruta)
    {
        var info = new FileInfo(ruta);
        if (info.Length > TamanoMaximo)
        {
            throw new FormatoDeMedioException("El archivo de cuadros es demasiado grande.");
        }
        return Leer(File.ReadAllBytes(ruta));
    }

    internal static Animacion LeerCuerpo(ref LectorBinario lector, int cantidad, bool duracionesAcumuladas)
    {
        if (cantidad <= 0 || cantidad > Animacion.MaximoCuadros)
        {
            throw new FormatoDeMedioException($"Cantidad de cuadros fuera de rango: {cantidad}.");
        }
        var duraciones = new int[cantidad];
        var anterior = 0;
        for (var i = 0; i < cantidad; i++)
        {
            var valor = lector.Int32();
            duraciones[i] = duracionesAcumuladas ? valor - anterior : valor;
            anterior = valor;
        }
        var cuadros = new byte[cantidad][];
        for (var i = 0; i < cantidad; i++)
        {
            var largo = lector.Int32();
            cuadros[i] = lector.Bytes(largo);
        }
        return new Animacion(cuadros, duraciones);
    }
}

internal ref struct LectorBinario(ReadOnlySpan<byte> datos, int posicion)
{
    private readonly ReadOnlySpan<byte> _datos = datos;
    private int _posicion = posicion;

    public int Int32()
    {
        Exigir(4);
        var v = BinaryPrimitives.ReadInt32LittleEndian(_datos[_posicion..]);
        _posicion += 4;
        return v;
    }

    public byte[] Bytes(int largo)
    {
        if (largo <= 0)
        {
            throw new FormatoDeMedioException("Un cuadro trae largo inválido.");
        }
        Exigir(largo);
        var copia = _datos.Slice(_posicion, largo).ToArray();
        _posicion += largo;
        return copia;
    }

    private readonly void Exigir(int largo)
    {
        if (largo > _datos.Length - _posicion)
        {
            throw new FormatoDeMedioException("El archivo está cortado.");
        }
    }
}

public sealed class FormatoDeMedioException(string mensaje) : Exception(mensaje);
