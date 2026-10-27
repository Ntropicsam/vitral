using Vitral.Core.Dispositivo;
using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;

namespace Vitral.Core.Tests;

internal static class Datos
{
    public static readonly byte[] RespuestaMjolnir = Convert.FromHexString(
        "1234567853534352" + "4d2d563100000000" + "000000005733389b" + "0700000020000000"
        + "0600000002000000" + "0000000001000000" + "32240ae200000000" + "8100000000000000");
}

internal sealed class PuertoFalso : IPuertoUsb
{
    public List<byte[]> Escrituras { get; } = [];
    public byte[] Respuesta { get; set; } = Datos.RespuestaMjolnir;
    public int RespuestasMalas { get; set; }
    public bool FallarEscritura { get; set; }
    public bool EscrituraCorta { get; set; }
    public bool Liberado { get; private set; }

    public int Escribir(ReadOnlySpan<byte> datos)
    {
        if (FallarEscritura)
        {
            throw new IOException("desconectado");
        }
        Escrituras.Add(datos.ToArray());
        return EscrituraCorta && datos.Length > 0 ? datos.Length - 1 : datos.Length;
    }

    public int Leer(Span<byte> destino)
    {
        if (RespuestasMalas > 0)
        {
            RespuestasMalas--;
            return 0;
        }
        Respuesta.CopyTo(destino);
        return Respuesta.Length;
    }

    public void Dispose() => Liberado = true;

    public int Cuadros => Escrituras.Count(e => e.Length > 64 && e[0] == 0x12);
}

internal sealed class BuscadorFalso : IBuscadorUsb
{
    public PuertoFalso Puerto { get; set; } = new();
    public MotivoSinPanel? Falla { get; set; }
    public int Aperturas { get; private set; }

    public ResultadoApertura Abrir()
    {
        Aperturas++;
        if (Falla is { } motivo)
        {
            return ResultadoApertura.Fallo(motivo, "falso");
        }
        Puerto = new PuertoFalso { Respuesta = Puerto.Respuesta };
        return ResultadoApertura.Abierto(Puerto);
    }
}

internal sealed class RelojFalso : IReloj
{
    public long Milisegundos { get; set; }
}

internal sealed class SensoresFalsos : IFuenteSensores
{
    public Dictionary<string, float> Valores { get; } = new() { ["cpu"] = 52, ["gpu"] = 61 };

    public IReadOnlyList<SensorInfo> Sensores { get; } =
    [
        new("cpu", "Tctl/Tdie", "Ryzen 5 5600T", TipoSensor.Temperatura),
        new("gpu", "GPU Core", "RX 9060 XT", TipoSensor.Temperatura),
    ];

    public int Lecturas { get; private set; }

    public IReadOnlySet<string>? UltimoInteres { get; private set; }

    public void Interes(IReadOnlySet<string>? ids) => UltimoInteres = ids;

    public LecturaSensores Leer()
    {
        Lecturas++;
        return new LecturaSensores(new Dictionary<string, float>(Valores));
    }

    public void Dispose()
    {
    }
}
