using System.Security.Principal;
using LibreHardwareMonitor.Hardware;
using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;

namespace Vitral.Sensores;

public sealed class SensoresLhm : IFuenteSensores
{
    private const int MaximoFallas = 5;

    private readonly Computer _computador;
    private readonly Lock _candado = new();
    private readonly Dictionary<IHardware, int> _fallas = [];
    private readonly bool _conPermisos;
    private List<Entrada> _entradas = [];
    private Dictionary<string, Entrada> _porId = [];
    private IReadOnlySet<string>? _interes;

    public SensoresLhm()
    {
        _conPermisos = EsAdministrador();
        _computador = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = _conPermisos,
            IsStorageEnabled = true,
            IsControllerEnabled = true,
        };
        _computador.Open();
        Reindexar();
        LeerSinCandado(null);
    }

    public IReadOnlyList<SensorInfo> Sensores { get; private set; } = [];

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
            return LeerSinCandado(_interes);
        }
    }

    public LecturaSensores Leer(IReadOnlyList<string>? ids)
    {
        lock (_candado)
        {
            return LeerSinCandado(ids?.ToHashSet(StringComparer.Ordinal));
        }
    }

    public string? Sugerido(bool gpu)
    {
        var candidatos = _entradas
            .Where(e => e.Sensor.SensorType == SensorType.Temperature && EsGpu(e.Sensor.Hardware.HardwareType) == gpu
                && (gpu || e.Sensor.Hardware.HardwareType == HardwareType.Cpu))
            .ToList();
        string[] preferidos = gpu ? ["GPU Core", "Core"] : ["Tctl", "Package", "Tdie", "Core"];
        foreach (var nombre in preferidos)
        {
            var hallado = candidatos.Find(e => e.Sensor.Name.Contains(nombre, StringComparison.OrdinalIgnoreCase));
            if (hallado is not null)
            {
                return hallado.Info.Id;
            }
        }
        return candidatos.Count > 0 ? candidatos[0].Info.Id : null;
    }

    public void Dispose() => _computador.Close();

    private LecturaSensores LeerSinCandado(IReadOnlySet<string>? ids)
    {
        var elegidas = ids is null
            ? _entradas
            : ids.Select(id => _porId.GetValueOrDefault(id)).OfType<Entrada>().ToList();
        foreach (var raiz in elegidas.Select(e => e.Raiz).Distinct())
        {
            if (_fallas.GetValueOrDefault(raiz) >= MaximoFallas)
            {
                continue;
            }
            try
            {
                Actualizar(raiz);
                _fallas.Remove(raiz);
            }
            catch (Exception e) when (e is NullReferenceException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                var fallas = _fallas[raiz] = _fallas.GetValueOrDefault(raiz) + 1;
                if (fallas == MaximoFallas)
                {
                    Registro.Escribir($"Se deja de leer {raiz.Name}: {e.Message}");
                }
            }
        }
        var valores = new Dictionary<string, float>(elegidas.Count, StringComparer.Ordinal);
        foreach (var e in elegidas)
        {
            if (e.Sensor.Value is { } v && float.IsFinite(v))
            {
                valores[e.Info.Id] = e.Sensor.SensorType == SensorType.SmallData ? v / 1024 : v;
            }
        }
        return new LecturaSensores(valores);
    }

    private void Reindexar()
    {
        var lista = new List<Entrada>();
        foreach (var hardware in _computador.Hardware)
        {
            Agregar(hardware, hardware, lista);
        }
        _entradas = lista;
        _porId = lista.ToDictionary(e => e.Info.Id, StringComparer.Ordinal);
        Sensores = lista.Select(e => e.Info).ToList();
        Registro.Escribir($"Sensores: {Sensores.Count} en {_computador.Hardware.Count} dispositivos{(_conPermisos ? "" : ", sin permisos para la CPU")}.");
    }

    private void Agregar(IHardware raiz, IHardware hardware, List<Entrada> lista)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (!_conPermisos && sensor.SensorType == SensorType.Temperature && hardware.HardwareType == HardwareType.Cpu)
            {
                continue;
            }
            if (Tipo(sensor, hardware.HardwareType) is { } tipo)
            {
                lista.Add(new Entrada(sensor, raiz, new SensorInfo(sensor.Identifier.ToString(), sensor.Name, raiz.Name, tipo)));
            }
        }
        foreach (var sub in hardware.SubHardware)
        {
            Agregar(raiz, sub, lista);
        }
    }

    private static void Actualizar(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
        {
            Actualizar(sub);
        }
    }

    private static TipoSensor? Tipo(ISensor sensor, HardwareType hardware) => sensor.SensorType switch
    {
        SensorType.Temperature => TipoSensor.Temperatura,
        SensorType.Load => TipoSensor.Carga,
        SensorType.Clock => TipoSensor.Reloj,
        SensorType.Fan => TipoSensor.Ventilador,
        SensorType.Power => TipoSensor.Potencia,
        SensorType.Voltage => TipoSensor.Voltaje,
        SensorType.Data or SensorType.SmallData => hardware == HardwareType.Memory ? TipoSensor.Memoria : TipoSensor.Datos,
        SensorType.Control or SensorType.Level => TipoSensor.Carga,
        _ => null,
    };

    private static bool EsGpu(HardwareType tipo) => tipo is HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel;

    private static bool EsAdministrador()
    {
        using var identidad = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identidad).IsInRole(WindowsBuiltInRole.Administrator) || identidad.IsSystem;
    }

    private sealed record Entrada(ISensor Sensor, IHardware Raiz, SensorInfo Info);
}
