using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Vitral.Core.Dispositivo;
using Vitral.Core.Medios;
using Vitral.Core.Render;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.Core.Ejecucion;

public sealed record EstadoMotor(EstadoPantalla Pantalla, double Fps, int Cuadro, int Cuadros, bool Pausado);

public sealed class Motor : IDisposable
{
    public const int ReenvioMs = 1000;
    public const int LecturaSensoresMs = 1000;
    public const int ReintentoConexionMs = 2000;
    public const int ReintentoNoSoportadaMs = 60_000;
    public const int CuadroModoLentoMs = 1000;

    private readonly Pantalla _pantalla;
    private readonly IFuenteSensores _sensores;
    private readonly IReloj _reloj;
    private readonly ConcurrentQueue<Action> _ordenes = new();
    private readonly AutoResetEvent _despertar = new(false);
    private readonly Compositor _compositor = new();
    private readonly Queue<long> _envios = new();

    private Thread? _hilo;
    private volatile bool _detener;
    private volatile bool _vistaPrevia;

    private Tema _tema = new();
    private IFondoCargado _fondo = new FondoColor();
    private Dictionary<string, TipoSensor> _tipos = [];
    private HashSet<string> _sensoresDelTema = [];
    private AjustesSalida _salida = AjustesSalida.PorDefecto;
    private LecturaSensores _lectura = LecturaSensores.Vacia;
    private CuadroRenderizado? _ultimo;
    private string? _firma;
    private int _cuadro;
    private bool _pausado;
    private long _proximoCuadro;
    private long _proximaLectura;
    private long _proximoIntento;
    private long _ultimoEnvio = long.MinValue / 2;
    private string? _legibilidadPedida;
    private EstadoMotor? _estadoAnunciado;
    private long _ultimoAnuncio;

    public Motor(Pantalla pantalla, IFuenteSensores sensores, IReloj reloj)
    {
        _pantalla = pantalla;
        _sensores = sensores;
        _reloj = reloj;
        _tipos = sensores.Sensores.ToDictionary(s => s.Id, s => s.Tipo);
    }

    public event Action<CuadroRenderizado>? CuadroListo;

    public event Action<EstadoMotor>? EstadoCambio;

    public event Action<ResultadoLegibilidad?>? LegibilidadLista;

    public event Action<LecturaSensores>? SensoresLeidos;

    public bool VistaPrevia
    {
        get => _vistaPrevia;
        set
        {
            _vistaPrevia = value;
            Encolar(() =>
            {
                _firma = null;
                AvisarInteres();
            });
        }
    }

    public bool ModoLento { get; set; }

    public IReadOnlyList<SensorInfo> Sensores => _sensores.Sensores;

    public void Iniciar()
    {
        if (_hilo is not null)
        {
            return;
        }
        _hilo = new Thread(Bucle) { IsBackground = true, Name = "Vitral · motor", Priority = ThreadPriority.BelowNormal };
        _hilo.Start();
    }

    public void UsarTema(Tema tema, string carpeta) => Encolar(() =>
    {
        if (tema.Fondo != _tema.Fondo)
        {
            var nuevo = Fondos.Cargar(tema.Fondo, carpeta);
            _fondo.Dispose();
            _fondo = nuevo;
            _cuadro = 0;
            _proximoCuadro = _reloj.Milisegundos + _fondo.DuracionMs(0);
        }
        _tema = tema;
        _firma = null;
        _sensoresDelTema = SensoresUsados(tema);
        AvisarInteres();
        _proximaLectura = 0;
    });

    public void CambiarSalida(AjustesSalida salida) => Encolar(() =>
    {
        _salida = salida.Normalizado();
        _firma = null;
    });

    public void Pausar(bool pausado) => Encolar(() =>
    {
        _pausado = pausado;
        _proximoCuadro = _reloj.Milisegundos + _fondo.DuracionMs(_cuadro);
    });

    public void IrACuadro(int cuadro) => Encolar(() =>
    {
        _cuadro = Math.Clamp(cuadro, 0, _fondo.Cantidad - 1);
        _proximoCuadro = _reloj.Milisegundos + _fondo.DuracionMs(_cuadro);
    });

    public void MedirLegibilidad(string capaId)
    {
        Volatile.Write(ref _legibilidadPedida, capaId);
        _despertar.Set();
    }

    public long Paso()
    {
        var ahora = _reloj.Milisegundos;
        while (_ordenes.TryDequeue(out var orden))
        {
            orden();
        }

        Conectar(ahora);
        var alguienMira = _pantalla.Conectada || _vistaPrevia;
        if (alguienMira)
        {
            LeerSensores(ahora);
            Avanzar(ahora);
            var firma = Firma();
            if (_ultimo is null || firma != _firma)
            {
                _ultimo = Dibujar();
                _firma = firma;
                if (_vistaPrevia)
                {
                    CuadroListo?.Invoke(_ultimo);
                }
                Enviar(ahora);
            }
            else if (ahora - _ultimoEnvio >= ReenvioMs)
            {
                Enviar(ahora);
            }
        }

        if (Interlocked.Exchange(ref _legibilidadPedida, null) is { } capaId)
        {
            LegibilidadLista?.Invoke(Legibilidad.Medir(Entrada(), capaId));
        }

        Anunciar(ahora);
        return Siguiente(ahora, alguienMira);
    }

    public void Dispose()
    {
        _detener = true;
        _despertar.Set();
        _hilo?.Join(TimeSpan.FromSeconds(3));
        _pantalla.Dispose();
        _fondo.Dispose();
        _compositor.Dispose();
        _despertar.Dispose();
    }

    private void Bucle()
    {
        while (!_detener)
        {
            long siguiente;
            try
            {
                siguiente = Paso();
            }
            catch (Exception e)
            {
                Registro.Escribir($"Motor: {e}");
                _firma = null;
                siguiente = _reloj.Milisegundos + 500;
            }
            var espera = (int)Math.Clamp(siguiente - _reloj.Milisegundos, 1, 1000);
            _despertar.WaitOne(espera);
        }
    }

    private void Encolar(Action orden)
    {
        _ordenes.Enqueue(orden);
        _despertar.Set();
    }

    private void AvisarInteres() => _sensores.Interes(_vistaPrevia ? null : _sensoresDelTema);

    private static HashSet<string> SensoresUsados(Tema tema) => tema.Capas
        .Where(c => c.Visible)
        .Select(c => c switch
        {
            CapaSensor s => s.SensorId,
            CapaMedidor m => m.SensorId,
            _ => string.Empty,
        })
        .Where(id => id.Length > 0)
        .ToHashSet(StringComparer.Ordinal);

    private void Conectar(long ahora)
    {
        if (_pantalla.Conectada || ahora < _proximoIntento)
        {
            return;
        }
        if (_pantalla.Conectar())
        {
            Registro.Escribir($"Pantalla conectada: {_pantalla.Estado.Info}");
            _ultimoEnvio = long.MinValue / 2;
            _firma = null;
            return;
        }
        var espera = _pantalla.Estado.Conexion == EstadoConexion.NoSoportada ? ReintentoNoSoportadaMs : ReintentoConexionMs;
        _proximoIntento = ahora + espera;
    }

    private void LeerSensores(long ahora)
    {
        if (ahora < _proximaLectura)
        {
            return;
        }
        _proximaLectura = ahora + LecturaSensoresMs;
        if (_sensoresDelTema.Count == 0 && !_vistaPrevia)
        {
            return;
        }
        _lectura = _sensores.Leer();
        if (_tipos.Count != _sensores.Sensores.Count)
        {
            _tipos = _sensores.Sensores.ToDictionary(s => s.Id, s => s.Tipo);
        }
        if (_vistaPrevia)
        {
            SensoresLeidos?.Invoke(_lectura);
        }
    }

    private void Avanzar(long ahora)
    {
        if (_pausado || _fondo.Cantidad <= 1 || ahora < _proximoCuadro)
        {
            return;
        }
        _cuadro = (_cuadro + 1) % _fondo.Cantidad;
        var duracion = ModoLento ? Math.Max(CuadroModoLentoMs, _fondo.DuracionMs(_cuadro)) : _fondo.DuracionMs(_cuadro);
        _proximoCuadro = ahora - _proximoCuadro > 1000 ? ahora + duracion : _proximoCuadro + duracion;
    }

    private string Firma()
    {
        var partes = new StringBuilder();
        partes.Append(_cuadro).Append('|');
        foreach (var id in _sensoresDelTema)
        {
            var valor = _lectura.Valor(id);
            partes.Append(valor is { } v ? Math.Round(v, 1).ToString(CultureInfo.InvariantCulture) : "-").Append('|');
        }
        return partes.ToString();
    }

    private CuadroRenderizado Dibujar()
    {
        var info = _pantalla.Estado.Info;
        var salida = _salida with { Rotacion = _salida.Rotacion + (info?.GiroDeFabrica ?? 0) };
        return _compositor.Componer(Entrada(), info?.Ancho ?? _tema.Ancho, info?.Alto ?? _tema.Alto, salida, _vistaPrevia);
    }

    private EntradaEscena Entrada() =>
        new(_tema, _fondo, _cuadro, _lectura, id => _tipos.TryGetValue(id, out var t) ? t : TipoSensor.Otro);

    private void Enviar(long ahora)
    {
        if (!_pantalla.Conectada || _ultimo is null)
        {
            return;
        }
        try
        {
            _pantalla.Enviar(_ultimo.Pantalla);
            _ultimoEnvio = ahora;
            _envios.Enqueue(ahora);
        }
        catch (IOException e)
        {
            Registro.Escribir($"Se perdió la pantalla: {e.Message}");
            _proximoIntento = ahora + 500;
        }
    }

    private void Anunciar(long ahora)
    {
        while (_envios.Count > 0 && ahora - _envios.Peek() > 1000)
        {
            _envios.Dequeue();
        }
        var estado = new EstadoMotor(_pantalla.Estado, _envios.Count, _cuadro, _fondo.Cantidad, _pausado);
        if (_estadoAnunciado is null
            || _estadoAnunciado with { Cuadro = estado.Cuadro } != estado
            || ahora - _ultimoAnuncio >= 1000)
        {
            _estadoAnunciado = estado;
            _ultimoAnuncio = ahora;
            EstadoCambio?.Invoke(estado);
        }
    }

    private long Siguiente(long ahora, bool alguienMira)
    {
        var siguiente = _pantalla.Conectada ? _ultimoEnvio + ReenvioMs : _proximoIntento;
        if (alguienMira)
        {
            siguiente = Math.Min(siguiente, _proximaLectura);
            if (!_pausado && _fondo.Cantidad > 1)
            {
                siguiente = Math.Min(siguiente, _proximoCuadro);
            }
        }
        return Math.Max(siguiente, ahora + 1);
    }
}
