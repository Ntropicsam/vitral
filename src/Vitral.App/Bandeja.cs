using Vitral.App.Editor;
using Vitral.Core.Dispositivo;
using Vitral.Core.Ejecucion;

namespace Vitral.App;

internal sealed class Bandeja : ApplicationContext
{
    private readonly Nucleo _nucleo;
    private readonly Actualizador _actualizador;
    private readonly NotifyIcon _icono;
    private readonly ContextMenuStrip _menu = new();
    private readonly System.Windows.Forms.Timer _juegos = new() { Interval = 3000 };
    private readonly System.Windows.Forms.Timer _reposo = new() { Interval = 20_000 };
    private readonly Icon _prendido = IconoVitral.Icono(32);
    private readonly Icon _apagado = IconoVitral.Icono(32, apagado: true);
    private readonly SynchronizationContext _ui;
    private VentanaEditor? _editor;
    private bool _avisoTrcc;
    private bool _avisoCpu;

    public Bandeja(Nucleo nucleo, Actualizador actualizador, bool abrirEditor)
    {
        _nucleo = nucleo;
        _actualizador = actualizador;
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _icono = new NotifyIcon { Icon = _apagado, Text = "Vitral", ContextMenuStrip = _menu, Visible = true };
        _icono.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                AbrirEditor();
            }
        };
        _icono.BalloonTipClicked += (_, _) =>
        {
            if (_avisoCpu)
            {
                _avisoCpu = false;
                _ = _nucleo.InstalarLectorCpuAsync();
            }
            else
            {
                AbrirEditor();
            }
        };
        _menu.Opening += (_, e) =>
        {
            ArmarMenu();
            e.Cancel = false;
        };
        ArmarMenu();
        _nucleo.Iniciado.ContinueWith(_ => _ui.Post(_ => SugerirLectorCpu(), null), TaskScheduler.Default);
        _nucleo.LectorCpuListo += () => _ui.Post(_ => _icono.ShowBalloonTip(5000, Textos.T("listo"), Textos.T("cpu.listo"), ToolTipIcon.Info), null);
        _nucleo.Motor.EstadoCambio += e => _ui.Post(_ => MostrarEstado(e), null);
        _actualizador.Lista += v => _ui.Post(_ => _icono.ShowBalloonTip(6000, Textos.T("nuevaVersion.titulo"), Textos.T("nuevaVersion.texto", v), ToolTipIcon.Info), null);
        _juegos.Tick += (_, _) => _nucleo.Motor.ModoLento = _nucleo.Ajustes.ModoJuego && DetectorJuego.HayJuego();
        _juegos.Start();
        _reposo.Tick += (_, _) =>
        {
            _reposo.Stop();
            if (_editor is null)
            {
                Eficiencia.Reposo();
            }
        };
        if (abrirEditor)
        {
            _ui.Post(_ => AbrirEditor(), null);
        }
        else
        {
            _reposo.Start();
        }
    }

    public void AbrirEditor()
    {
        if (_editor is { IsDisposed: false })
        {
            if (_editor.WindowState == FormWindowState.Minimized)
            {
                _editor.WindowState = FormWindowState.Normal;
            }
            _editor.Activate();
            return;
        }
        Eficiencia.Activo();
        _editor = new VentanaEditor(_nucleo, _actualizador);
        _editor.FormClosed += (_, _) =>
        {
            _editor = null;
            _reposo.Stop();
            _reposo.Start();
        };
        _editor.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _juegos.Dispose();
            _reposo.Dispose();
            _icono.Visible = false;
            _icono.Dispose();
            _menu.Dispose();
            _prendido.Dispose();
            _apagado.Dispose();
        }
        base.Dispose(disposing);
    }

    private void MostrarEstado(EstadoMotor estado)
    {
        var conectada = estado.Pantalla.Conexion == EstadoConexion.Conectada;
        _icono.Icon = conectada ? _prendido : _apagado;
        var texto = conectada ? $"Vitral · {estado.Pantalla.Info?.Modelo}" : $"Vitral · {TextoConexion(estado.Pantalla)}";
        _icono.Text = texto.Length > 63 ? texto[..63] : texto;
        if (estado.Pantalla.Conexion == EstadoConexion.Ocupada && !_avisoTrcc)
        {
            _avisoTrcc = true;
            _icono.ShowBalloonTip(8000, Textos.T("ocupada.titulo"), Textos.T("ocupada.texto"), ToolTipIcon.Info);
        }
    }

    private void ArmarMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add(new ToolStripMenuItem(Textos.T("abrir"), null, (_, _) => AbrirEditor()) { Font = new Font(_menu.Font, FontStyle.Bold) });
        if (_actualizador.VersionLista is { } nueva)
        {
            _menu.Items.Add(new ToolStripMenuItem(Textos.T("reiniciarVersion", nueva), null, (_, _) => _actualizador.ReiniciarYActualizar()));
        }
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Textos.T("salir"), null, (_, _) => Salir()));
    }

    private void SugerirLectorCpu()
    {
        if (_nucleo.PuedeLeerCpu)
        {
            return;
        }
        _avisoCpu = true;
        _icono.ShowBalloonTip(10_000, Textos.T("cpu.titulo"), Textos.T("cpu.texto"), ToolTipIcon.Info);
    }

    private static string TextoConexion(EstadoPantalla p) => p.Conexion switch
    {
        EstadoConexion.Conectada => Textos.T("estado.conectada"),
        EstadoConexion.NoConectada => Textos.T("estado.noConectada"),
        EstadoConexion.Ocupada => Textos.T("estado.ocupada"),
        EstadoConexion.NoSoportada => Textos.T("estado.noSoportada", p.Info?.Modelo ?? "?"),
        EstadoConexion.Buscando => Textos.T("estado.buscando"),
        _ => Textos.T("estado.error"),
    };

    private void Salir()
    {
        _editor?.Close();
        _icono.Visible = false;
        ExitThread();
    }
}
