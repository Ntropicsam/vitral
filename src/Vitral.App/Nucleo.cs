using System.Reflection;
using Vitral.Core;
using Vitral.Core.Dispositivo;
using Vitral.Core.Dispositivo.WinUsb;
using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;
using Vitral.Sensores;

namespace Vitral.App;

internal sealed class Nucleo : IDisposable
{
    public static readonly string Version =
        typeof(Nucleo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0";

    private readonly AlmacenAjustes _almacenAjustes = new(Rutas.Ajustes);
    private readonly SensoresIntercambiables _sensores;
    private readonly TaskCompletionSource _iniciado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private InfoPanel? _panel;

    public Nucleo()
    {
        Temas = new AlmacenTemas(Rutas.Temas);
        Ajustes = _almacenAjustes.Leer();
        _sensores = new SensoresIntercambiables(CrearFuente());
        Motor = new Motor(new Pantalla(new BuscadorWinUsb()), _sensores, new RelojSistema());
        Motor.CambiarSalida(Ajustes.Salida);
        Motor.EstadoCambio += e => _panel = e.Pantalla.Info ?? _panel;
    }

    public AlmacenTemas Temas { get; }

    public Ajustes Ajustes { get; private set; }

    public Motor Motor { get; }

    public Task Iniciado => _iniciado.Task;

    public bool Editando { get; set; }

    public bool ConServicio => _sensores.Actual is SensoresServicio;

    public string? VersionServicio => (_sensores.Actual as SensoresServicio)?.VersionServicio;

    public IReadOnlyList<SensorInfo> Sensores => _sensores.Sensores;

    public event Action? TemasCambiaron;

    public event Action? AjustesCambiaron;

    public async Task IniciarAsync()
    {
        try
        {
            Motor.Iniciar();
            if (Activo() is { } activo)
            {
                Motor.UsarTema(activo.Tema, activo.Carpeta);
                return;
            }
            var sensores = await EsperarSensoresAsync().ConfigureAwait(true);
            var tema = Temas.Primero() ?? Temas.Crear(TemasIniciales.Plantilla(sensores, _panel?.Ancho ?? TemasIniciales.AnchoBase, _panel?.Alto ?? TemasIniciales.AltoBase));
            CambiarAjustes(a => a with { TemaActivo = tema.Id });
            Motor.UsarTema(tema.Tema, tema.Carpeta);
            TemasCambiaron?.Invoke();
        }
        finally
        {
            _iniciado.TrySetResult();
        }
    }

    public async Task<IReadOnlyList<SensorInfo>> EsperarSensoresAsync()
    {
        for (var i = 0; i < 80 && _sensores.Sensores.Count == 0; i++)
        {
            if (_sensores.Actual is SensoresServicio servicio)
            {
                await Task.Run(servicio.Leer).ConfigureAwait(true);
            }
            if (_sensores.Sensores.Count == 0)
            {
                await Task.Delay(250).ConfigureAwait(true);
            }
        }
        return _sensores.Sensores;
    }

    public bool PuedeLeerCpu => ConServicio || !ServicioSensores.Incluido;

    public event Action? LectorCpuListo;

    public async Task<bool> InstalarLectorCpuAsync()
    {
        var instalado = await Task.Run(ServicioSensores.PedirInstalacion).ConfigureAwait(true);
        if (instalado && !ConServicio)
        {
            _sensores.Cambiar(new SensoresServicio());
            await EsperarSensoresAsync().ConfigureAwait(true);
            LectorCpuListo?.Invoke();
        }
        return instalado;
    }

    public Tema TemaNuevo() =>
        TemasIniciales.Plantilla(_sensores.Sensores, _panel?.Ancho ?? TemasIniciales.AnchoBase, _panel?.Alto ?? TemasIniciales.AltoBase) with { Nombre = "Tema nuevo" };

    public TemaGuardado? Activo() =>
        Ajustes.TemaActivo is { } id ? Temas.Intentar(id) ?? Temas.Primero() : Temas.Primero();

    public void ActivarTema(string id)
    {
        if (Temas.Intentar(id) is not { } tema)
        {
            return;
        }
        CambiarAjustes(a => a with { TemaActivo = id });
        if (!Editando)
        {
            Motor.UsarTema(tema.Tema, tema.Carpeta);
        }
    }

    public void VolverAlActivo()
    {
        if (Activo() is { } activo)
        {
            Motor.UsarTema(activo.Tema, activo.Carpeta);
        }
        Motor.Pausar(false);
    }

    public void CambiarAjustes(Func<Ajustes, Ajustes> cambio)
    {
        Ajustes = cambio(Ajustes).Normalizado();
        _almacenAjustes.Guardar(Ajustes);
        Motor.CambiarSalida(Ajustes.Salida);
        AjustesCambiaron?.Invoke();
    }

    public void AvisarTemas() => TemasCambiaron?.Invoke();

    public void Dispose()
    {
        Motor.Dispose();
        _sensores.Dispose();
    }

    private static IFuenteSensores CrearFuente() =>
        ServicioSensores.Instalado()
            ? new SensoresServicio()
            : new SensoresDiferidos(() => new SensoresLhm());
}
