using System.Buffers.Binary;

namespace Vitral.Core.Dispositivo;

public enum FormatoCuadro
{
    Jpeg = 2,
    Rgb565 = 3,
}

public sealed record InfoPanel(byte Pm, byte Sub, string Identificador, int Ancho, int Alto, FormatoCuadro Formato, int GiroDeFabrica, string Modelo)
{
    public string ModeloDetectado { get; init; } = Modelo;

    public bool Forzado { get; init; }

    public InfoPanel Con(ModeloPanel forzado) => this with
    {
        Ancho = forzado.Ancho,
        Alto = forzado.Alto,
        Formato = forzado.Formato,
        GiroDeFabrica = forzado.GiroDeFabrica,
        Modelo = forzado.Nombre,
        Forzado = true,
    };
}

public static class Protocolo
{
    public const ushort Vid = 0x87AD;
    public const ushort Pid = 0x70DB;
    public const byte EndpointSalida = 0x01;
    public const byte EndpointEntrada = 0x81;
    public const int TamanoCabecera = 64;
    public const int TamanoRespuesta = 1024;
    public const int TamanoTrozo = 16 * 1024;
    public const int TamanoPaquete = 512;

    public const int RespuestaMinima = 57;

    private static ReadOnlySpan<byte> Magia => [0x12, 0x34, 0x56, 0x78];

    public static byte[] PaqueteHandshake()
    {
        var paquete = new byte[TamanoCabecera];
        Magia.CopyTo(paquete);
        paquete[56] = 1;
        return paquete;
    }

    public static InfoPanel LeerRespuesta(ReadOnlySpan<byte> respuesta)
    {
        if (respuesta.Length < RespuestaMinima)
        {
            throw new ErrorDeProtocolo($"La respuesta del handshake trae {respuesta.Length} bytes; se esperaban al menos {RespuestaMinima}.");
        }
        if (!respuesta[..4].SequenceEqual(Magia))
        {
            throw new ErrorDeProtocolo("La respuesta del handshake no empieza con la magia 12 34 56 78.");
        }

        var pm = respuesta[24];
        if (pm == 0)
        {
            throw new ErrorDeProtocolo("El panel respondió con PM 0: no es un dispositivo válido.");
        }

        var sub = respuesta[36];
        var identificador = respuesta[56] == 0x81
            ? Convert.ToHexString(respuesta.Slice(48, 4))
            : string.Empty;
        var modelo = Catalogo.Buscar(pm, sub);
        return new InfoPanel(pm, sub, identificador, modelo.Ancho, modelo.Alto, modelo.Formato, modelo.GiroDeFabrica, modelo.Nombre);
    }

    public static byte[] ArmarCuadro(ReadOnlySpan<byte> imagen, int ancho, int alto, FormatoCuadro formato)
    {
        if (imagen.IsEmpty)
        {
            throw new ArgumentException("El cuadro no puede ir vacío.", nameof(imagen));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ancho);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alto);

        var cuadro = new byte[TamanoCabecera + imagen.Length];
        var c = cuadro.AsSpan();
        Magia.CopyTo(c);
        BinaryPrimitives.WriteUInt32LittleEndian(c[4..], (uint)formato);
        BinaryPrimitives.WriteUInt32LittleEndian(c[8..], (uint)ancho);
        BinaryPrimitives.WriteUInt32LittleEndian(c[12..], (uint)alto);
        BinaryPrimitives.WriteUInt32LittleEndian(c[56..], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(c[60..], (uint)imagen.Length);
        imagen.CopyTo(c[TamanoCabecera..]);
        return cuadro;
    }

    public static IEnumerable<ReadOnlyMemory<byte>> Trozos(ReadOnlyMemory<byte> cuadro)
    {
        for (var i = 0; i < cuadro.Length; i += TamanoTrozo)
        {
            yield return cuadro.Slice(i, Math.Min(TamanoTrozo, cuadro.Length - i));
        }
        if (cuadro.Length % TamanoPaquete == 0)
        {
            yield return ReadOnlyMemory<byte>.Empty;
        }
    }
}

public sealed class ErrorDeProtocolo(string mensaje) : Exception(mensaje);
