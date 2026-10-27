using System.IO.Pipes;
using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;

namespace Vitral.Sensores;

public sealed class SensoresServicio : IFuenteSensores
{
    private const int EsperaConexionMs = 300;
    private const int ReintentoMs = 5000;
    private const int RefrescoListaMs = 60_000;

    private readonly Lock _candado = new();
    private NamedPipeClientStream? _tuberia;
    private IReadOnlySet<string>? _interes;
    private long _proximoIntento;
    private long _proximaLista;

    public IReadOnlyList<SensorInfo> Sensores { get; private set; } = [];

    public string? VersionServicio { get; private set; }

    public static bool Disponible()
    {
        using var prueba = new NamedPipeClientStream(".", CanalSensores.NombreTuberia, PipeDirection.InOut);
        try
        {
            prueba.Connect(EsperaConexionMs);
            return true;
        }
        catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void Interes(IReadOnlySet<string>? ids)
    {
        lock (_candado)
        {
            _interes = ids;
        }
    }

    public LecturaSensores Leer()
    {
        lock (_candado)
        {
            var ahora = Environment.TickCount64;
            if (_tuberia is null && !Conectar(ahora))
            {
                return LecturaSensores.Vacia;
            }
            try
            {
                var lista = Sensores.Count == 0 || ahora >= _proximaLista;
                CanalSensores.Escribir(_tuberia!, new PedidoSensores(lista, _interes?.ToList()));
                var respuesta = CanalSensores.Leer<RespuestaSensores>(_tuberia!)
                    ?? throw new EndOfStreamException("El servicio de sensores cerró la conexión.");
                VersionServicio = respuesta.Version;
                if (respuesta.Sensores is { Count: > 0 } sensores)
                {
                    Sensores = sensores;
                    _proximaLista = ahora + RefrescoListaMs;
                }
                return new LecturaSensores(respuesta.Valores);
            }
            catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException or ObjectDisposedException)
            {
                Registro.Escribir($"Servicio de sensores: {e.Message}");
                Soltar();
                _proximoIntento = ahora + ReintentoMs;
                return LecturaSensores.Vacia;
            }
        }
    }

    public void Dispose()
    {
        lock (_candado)
        {
            Soltar();
        }
    }

    private bool Conectar(long ahora)
    {
        if (ahora < _proximoIntento)
        {
            return false;
        }
        var tuberia = new NamedPipeClientStream(".", CanalSensores.NombreTuberia, PipeDirection.InOut);
        try
        {
            tuberia.Connect(EsperaConexionMs);
            _tuberia = tuberia;
            _proximaLista = 0;
            return true;
        }
        catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException)
        {
            tuberia.Dispose();
            _proximoIntento = ahora + ReintentoMs;
            return false;
        }
    }

    private void Soltar()
    {
        _tuberia?.Dispose();
        _tuberia = null;
    }
}
