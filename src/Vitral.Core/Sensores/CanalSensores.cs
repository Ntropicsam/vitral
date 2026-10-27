using System.Buffers.Binary;
using System.Text.Json;

namespace Vitral.Core.Sensores;

public sealed record PedidoSensores(bool Lista, IReadOnlyList<string>? Ids);

public sealed record RespuestaSensores(string Version, IReadOnlyList<SensorInfo>? Sensores, IReadOnlyDictionary<string, float> Valores);

public static class CanalSensores
{
    public const string NombreTuberia = "Vitral.Sensores";
    public const string NombreServicio = "VitralSensores";
    public const int MaximoMensaje = 1024 * 1024;
    public const int MaximoIds = 512;
    public const int LargoMaximoId = 200;

    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    public static void Escribir<T>(Stream flujo, T mensaje)
    {
        ArgumentNullException.ThrowIfNull(flujo);
        var cuerpo = JsonSerializer.SerializeToUtf8Bytes(mensaje, Opciones);
        if (cuerpo.Length > MaximoMensaje)
        {
            throw new InvalidDataException("El mensaje de sensores es demasiado grande.");
        }
        Span<byte> largo = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(largo, cuerpo.Length);
        flujo.Write(largo);
        flujo.Write(cuerpo);
        flujo.Flush();
    }

    public static T? Leer<T>(Stream flujo)
    {
        ArgumentNullException.ThrowIfNull(flujo);
        Span<byte> cabecera = stackalloc byte[4];
        if (!LeerExacto(flujo, cabecera))
        {
            return default;
        }
        var largo = BinaryPrimitives.ReadInt32LittleEndian(cabecera);
        if (largo <= 0 || largo > MaximoMensaje)
        {
            throw new InvalidDataException("Mensaje de sensores con largo inválido.");
        }
        var cuerpo = new byte[largo];
        if (!LeerExacto(flujo, cuerpo))
        {
            throw new EndOfStreamException("El mensaje de sensores llegó cortado.");
        }
        return JsonSerializer.Deserialize<T>(cuerpo, Opciones);
    }

    public static IReadOnlyList<string>? IdsValidos(IReadOnlyList<string>? ids) =>
        ids?.Where(id => !string.IsNullOrEmpty(id) && id.Length <= LargoMaximoId).Take(MaximoIds).ToList();

    private static bool LeerExacto(Stream flujo, Span<byte> destino)
    {
        var leidos = 0;
        while (leidos < destino.Length)
        {
            var n = flujo.Read(destino[leidos..]);
            if (n == 0)
            {
                if (leidos == 0)
                {
                    return false;
                }
                throw new EndOfStreamException("El mensaje de sensores llegó cortado.");
            }
            leidos += n;
        }
        return true;
    }
}
