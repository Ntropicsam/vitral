using System.Text.Json;
using System.Text.Json.Nodes;
using Vitral.Core.Ejecucion;
using Vitral.Core.Medios;
using Vitral.Core.Temas;

namespace Vitral.App.Editor;

internal sealed class Puente : IDisposable
{
    private const int EsperaGuardadoMs = 500;
    private const long MaximoVideoBytes = 200L * 1024 * 1024;

    private readonly Nucleo _nucleo;
    private readonly Action<string> _enviar;
    private readonly Func<byte[]?> _ultimaVista;
    private readonly System.Windows.Forms.Timer _guardar = new() { Interval = EsperaGuardadoMs };
    private TemaGuardado? _edicion;
    private Tema? _pendiente;
    private List<byte[]>? _videoCuadros;
    private List<int>? _videoDuraciones;
    private long _videoBytes;

    private readonly PuenteAjustes _ajustes;

    public Puente(Nucleo nucleo, Actualizador actualizador, Action<string> enviar, Func<byte[]?> ultimaVista)
    {
        _nucleo = nucleo;
        _enviar = enviar;
        _ultimaVista = ultimaVista;
        _ajustes = new PuenteAjustes(nucleo, actualizador, enviar);
        _guardar.Tick += (_, _) => GuardarPendiente();
        _nucleo.LectorCpuListo += AlLectorCpu;
    }

    private void AlLectorCpu() => _enviar(Mensajes.Sensores(_nucleo.Sensores, _nucleo.PuedeLeerCpu));

    public void Recibir(string json)
    {
        JsonObject? m;
        try
        {
            m = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return;
        }
        if (m is null)
        {
            return;
        }

        try
        {
            Atender(Mensajes.Texto(m, "tipo"), m);
        }
        catch (Exception e) when (e is TemaInvalidoException or FormatoDeMedioException or IOException or KeyNotFoundException or FormatException or ArgumentException)
        {
            Registro.Escribir($"Editor: {e.Message}");
            var clave = e switch
            {
                FormatoDeMedioException => "error.medio",
                TemaInvalidoException => "error.tema",
                IOException => "error.disco",
                _ => "error.otro",
            };
            _enviar(Mensajes.Error(Textos.T(clave, e.Message)));
        }
    }

    public void Dispose()
    {
        _ajustes.Dispose();
        _nucleo.LectorCpuListo -= AlLectorCpu;
        GuardarPendiente();
        _guardar.Dispose();
        _nucleo.Editando = false;
        _nucleo.Motor.VistaPrevia = false;
        _nucleo.VolverAlActivo();
    }

    private void Atender(string? tipo, JsonObject m)
    {
        switch (tipo)
        {
            case "listo":
                Arrancar();
                break;
            case "tema.abrir":
                Abrir(Mensajes.Texto(m, "id") ?? string.Empty);
                break;
            case "tema.cambiar":
                Cambiar(m["tema"]?.ToJsonString() ?? string.Empty);
                break;
            case "tema.nuevo":
                Nuevo(Enum.TryParse<Distribucion>(Mensajes.Texto(m, "distribucion"), out var d) ? d : Distribucion.Completa);
                break;
            case "tema.duplicar":
                GuardarPendiente();
                AbrirGuardado(_nucleo.Temas.Duplicar(Mensajes.Texto(m, "id") ?? string.Empty));
                break;
            case "tema.borrar":
                Borrar(Mensajes.Texto(m, "id") ?? string.Empty);
                break;
            case "tema.aplicar" when _edicion is not null:
                GuardarPendiente();
                _nucleo.ActivarTema(_edicion.Id);
                EnviarTemas();
                break;
            case "fondo.archivo":
                ImportarArchivo(Mensajes.Texto(m, "nombre") ?? "archivo", Mensajes.Texto(m, "datos") ?? string.Empty);
                break;
            case "fondo.video.inicio":
                (_videoCuadros, _videoDuraciones, _videoBytes) = ([], [], 0);
                break;
            case "fondo.video.cuadro":
                RecibirCuadroDeVideo(Mensajes.Texto(m, "jpeg") ?? string.Empty, Mensajes.Entero(m, "duracion") ?? 0);
                break;
            case "fondo.video.fin":
                TerminarVideo();
                break;
            case "animacion.pausar":
                _nucleo.Motor.Pausar(Mensajes.Logico(m, "pausado") ?? false);
                break;
            case "animacion.ir":
                _nucleo.Motor.IrACuadro(Mensajes.Entero(m, "cuadro") ?? 0);
                break;
            case "legibilidad":
                _nucleo.Motor.MedirLegibilidad(Mensajes.Texto(m, "capaId") ?? string.Empty);
                break;
            default:
                if (!_ajustes.Atender(tipo, m))
                {
                    Registro.Escribir($"Editor: mensaje desconocido «{tipo}».");
                }
                break;
        }
    }

    private void Arrancar()
    {
        _nucleo.Editando = true;
        _nucleo.Motor.VistaPrevia = true;
        var ui = TaskScheduler.FromCurrentSynchronizationContext();
        _nucleo.EsperarSensoresAsync().ContinueWith(t => _enviar(Mensajes.Sensores(t.Result, _nucleo.PuedeLeerCpu)), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, ui);
        _ajustes.Arrancar();
        _nucleo.Iniciado.ContinueWith(_ => AbrirInicial(), CancellationToken.None, TaskContinuationOptions.None, ui);
    }

    private void AbrirInicial()
    {
        if (_edicion is not null)
        {
            return;
        }
        EnviarTemas();
        if (_nucleo.Activo() is { } activo)
        {
            AbrirGuardado(activo);
        }
        else
        {
            Nuevo(Distribucion.SoloDatos);
        }
    }

    private void Abrir(string id)
    {
        GuardarPendiente();
        AbrirGuardado(_nucleo.Temas.Intentar(id) ?? throw new KeyNotFoundException("Ese tema ya no existe."));
    }

    private void AbrirGuardado(TemaGuardado tema)
    {
        _edicion = tema;
        _nucleo.Motor.UsarTema(tema.Tema, tema.Carpeta);
        _nucleo.Motor.Pausar(false);
        _enviar(Mensajes.Tema(tema.Id, tema.Tema));
        EnviarTemas();
    }

    private void Cambiar(string json)
    {
        if (_edicion is null)
        {
            return;
        }
        var tema = SerializadorTemas.Leer(json);
        tema = tema with { Fondo = tema.Fondo with { Tipo = _edicion.Tema.Fondo.Tipo, Archivo = _edicion.Tema.Fondo.Archivo } };
        _edicion = _edicion with { Tema = tema };
        _pendiente = tema;
        _nucleo.Motor.UsarTema(tema, _edicion.Carpeta);
        _guardar.Stop();
        _guardar.Start();
    }

    private void GuardarPendiente()
    {
        _guardar.Stop();
        if (_edicion is null || _pendiente is null)
        {
            return;
        }
        var nombreAnterior = _nucleo.Temas.Intentar(_edicion.Id)?.Tema.Nombre;
        _edicion = _nucleo.Temas.Guardar(_edicion.Id, _pendiente);
        _pendiente = null;
        if (_ultimaVista() is { } vista)
        {
            _nucleo.Temas.GuardarMiniatura(_edicion.Id, vista);
        }
        _enviar(Mensajes.Guardado(_edicion.Id));
        EnviarTemas();
        if (nombreAnterior != _edicion.Tema.Nombre)
        {
            _nucleo.AvisarTemas();
        }
    }

    private void Nuevo(Distribucion distribucion)
    {
        GuardarPendiente();
        AbrirGuardado(_nucleo.Temas.Crear(_nucleo.CrearTema(distribucion)));
        _nucleo.AvisarTemas();
    }

    private void Borrar(string id)
    {
        if (_edicion?.Id == id)
        {
            _pendiente = null;
            _guardar.Stop();
        }
        _nucleo.Temas.Borrar(id);
        var restante = _nucleo.Temas.Primero();
        if (_nucleo.Ajustes.TemaActivo == id && restante is not null)
        {
            _nucleo.ActivarTema(restante.Id);
        }
        if (_edicion?.Id == id)
        {
            if (restante is null)
            {
                Nuevo(Distribucion.SoloDatos);
                return;
            }
            AbrirGuardado(restante);
        }
        EnviarTemas();
        _nucleo.AvisarTemas();
    }

    private void ImportarArchivo(string nombre, string base64)
    {
        if (_edicion is null)
        {
            return;
        }
        GuardarPendiente();
        var datos = Convert.FromBase64String(base64);
        var tema = _nucleo.Temas.ImportarFondo(_edicion.Id, _edicion.Tema, Path.GetFileName(nombre), datos);
        AbrirGuardado(_nucleo.Temas.Guardar(_edicion.Id, tema));
    }

    private void RecibirCuadroDeVideo(string base64, int duracion)
    {
        if (_videoCuadros is null || _videoDuraciones is null)
        {
            throw new FormatException("Llegó un cuadro de video sin que empezara la importación.");
        }
        var jpeg = Convert.FromBase64String(base64);
        _videoBytes += jpeg.Length;
        if (_videoCuadros.Count >= Animacion.MaximoCuadros || _videoBytes > MaximoVideoBytes)
        {
            (_videoCuadros, _videoDuraciones) = (null, null);
            throw new FormatException("El video es demasiado largo para la pantalla. Recórtalo a menos de cinco minutos.");
        }
        _videoCuadros.Add(jpeg);
        _videoDuraciones.Add(duracion);
    }

    private void TerminarVideo()
    {
        if (_edicion is null || _videoCuadros is not { Count: > 0 } cuadros || _videoDuraciones is null)
        {
            throw new FormatException("El video no trajo cuadros.");
        }
        var medio = Importador.GuardarAnimacion(new Animacion(cuadros, _videoDuraciones));
        (_videoCuadros, _videoDuraciones) = (null, null);
        GuardarPendiente();
        var tema = _nucleo.Temas.ConFondo(_edicion.Id, _edicion.Tema, medio);
        AbrirGuardado(_nucleo.Temas.Guardar(_edicion.Id, tema));
    }

    private void EnviarTemas() => _enviar(Mensajes.Temas(_nucleo.Temas.Listar(), _nucleo.Ajustes.TemaActivo));
}
