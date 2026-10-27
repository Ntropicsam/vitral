using Vitral.App.Editor;
using Vitral.Core.Dispositivo;
using Vitral.Core.Ejecucion;

namespace Vitral.App;

internal sealed class Bandeja : ApplicationContext
{
    private static readonly int[] Brillos = [100, 75, 50, 25];
    private static readonly int[] Giros = [0, 90, 180, 270];

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
    private EstadoMotor? _estado;
    private bool _avisoTrcc;
    private bool _avisoCpu;
    private bool _instalando;

    public Bandeja(Nucleo nucleo, Actualizador actualizador, bool abrirEditor)
    {
        _nucleo = nucleo;
        _actualizador = actualizador;
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _icono = new NotifyIcon { Icon = _apagado, Text = "Vitral", ContextMenuStrip = _menu, Visible = true };
        _icono.DoubleClick += (_, _) => AbrirEditor();
        _menu.Opening += (_, e) =>
        {
            ArmarMenu();
            e.Cancel = false;
        };
        ArmarMenu();
        _icono.BalloonTipClicked += (_, _) =>
        {
            if (_avisoCpu)
            {
                _avisoCpu = false;
                InstalarServicio(actualizar: false);
            }
        };
        _nucleo.Iniciado.ContinueWith(_ => _ui.Post(_ => SugerirLectorCpu(), null), TaskScheduler.Default);
        _nucleo.LectorCpuListo += () => _ui.Post(_ => _icono.ShowBalloonTip(5000, "Listo", "Vitral ya lee la temperatura de la CPU.", ToolTipIcon.Info), null);
        _nucleo.Motor.EstadoCambio += e => _ui.Post(_ => MostrarEstado(e), null);
        _actualizador.Lista += v => _ui.Post(_ => _icono.ShowBalloonTip(6000, "Hay una versión nueva de Vitral", $"La {v} ya está descargada. Se instala la próxima vez que abras Vitral, o ya mismo desde el menú.", ToolTipIcon.Info), null);
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
            _editor.Activate();
            return;
        }
        Eficiencia.Activo();
        _editor = new VentanaEditor(_nucleo);
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
        _estado = estado;
        var conectada = estado.Pantalla.Conexion == EstadoConexion.Conectada;
        _icono.Icon = conectada ? _prendido : _apagado;
        var texto = conectada ? $"Vitral · {estado.Pantalla.Info?.Modelo}" : $"Vitral · {TextoConexion(estado.Pantalla)}";
        _icono.Text = texto.Length > 63 ? texto[..63] : texto;

        if (estado.Pantalla.Conexion == EstadoConexion.Ocupada && !_avisoTrcc)
        {
            _avisoTrcc = true;
            _icono.ShowBalloonTip(8000, "La pantalla está ocupada", "Otro programa la tiene abierta, casi siempre TRCC. Ciérralo desde su ícono de la bandeja.", ToolTipIcon.Info);
        }
    }

    private void ArmarMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add(new ToolStripMenuItem("Abrir el editor", null, (_, _) => AbrirEditor()) { Font = new Font(_menu.Font, FontStyle.Bold) });
        _menu.Items.Add(new ToolStripSeparator());

        var temas = new ToolStripMenuItem("Tema");
        var activo = _nucleo.Activo()?.Id;
        foreach (var tema in _nucleo.Temas.Listar())
        {
            var id = tema.Id;
            temas.DropDownItems.Add(new ToolStripMenuItem(tema.Tema.Nombre, null, (_, _) => _nucleo.ActivarTema(id)) { Checked = id == activo });
        }
        temas.Enabled = temas.DropDownItems.Count > 0;
        _menu.Items.Add(temas);

        var brillo = new ToolStripMenuItem("Brillo");
        foreach (var b in Brillos)
        {
            brillo.DropDownItems.Add(new ToolStripMenuItem($"{b} %", null, (_, _) => _nucleo.CambiarAjustes(a => a with { Brillo = b })) { Checked = _nucleo.Ajustes.Brillo == b });
        }
        _menu.Items.Add(brillo);

        var giro = new ToolStripMenuItem("Girar la imagen");
        foreach (var g in Giros)
        {
            giro.DropDownItems.Add(new ToolStripMenuItem(g == 0 ? "Sin girar" : $"{g}°", null, (_, _) => _nucleo.CambiarAjustes(a => a with { Rotacion = g })) { Checked = _nucleo.Ajustes.Rotacion == g });
        }
        _menu.Items.Add(giro);
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripMenuItem("Arrancar con Windows", null, (_, _) => AlternarArranque()) { Checked = Arranque.Activo() });
        _menu.Items.Add(new ToolStripMenuItem("Un cuadro por segundo durante los juegos", null, (_, _) => _nucleo.CambiarAjustes(a => a with { ModoJuego = !a.ModoJuego })) { Checked = _nucleo.Ajustes.ModoJuego });
        AgregarServicio();
        if (Arranque.TrccArrancaSolo())
        {
            _menu.Items.Add(new ToolStripMenuItem("Evitar que TRCC se abra solo…", null, (_, _) => DesactivarTrcc()));
        }
        if (_actualizador.VersionLista is { } nueva)
        {
            _menu.Items.Add(new ToolStripMenuItem($"Reiniciar con la versión {nueva}", null, (_, _) => _actualizador.ReiniciarYActualizar()));
        }
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripMenuItem(TextoEstado()) { Enabled = false });
        _menu.Items.Add(new ToolStripMenuItem("Salir", null, (_, _) => Salir()));
    }

    private void AgregarServicio()
    {
        if (!ServicioSensores.Incluido || _instalando)
        {
            return;
        }
        if (!_nucleo.ConServicio)
        {
            _menu.Items.Add(new ToolStripMenuItem("Leer la temperatura de la CPU…", null, (_, _) => InstalarServicio(actualizar: false)));
        }
        else if (_nucleo.VersionServicio is { } version && version != Nucleo.Version)
        {
            _menu.Items.Add(new ToolStripMenuItem("Actualizar el lector de la CPU", null, (_, _) => InstalarServicio(actualizar: true)));
        }
    }

    private void InstalarServicio(bool actualizar)
    {
        if (!actualizar)
        {
            var respuesta = MessageBox.Show(
                "Windows solo deja leer la temperatura de la CPU con permisos de administrador.\n\nVitral instala un servicio pequeño en Archivos de programa que solo lee los sensores y se los pasa a la pantalla. El resto de Vitral sigue sin permisos.\n\n¿Lo instalo?",
                "Vitral",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (respuesta != DialogResult.Yes)
            {
                return;
            }
        }
        _instalando = true;
        _nucleo.InstalarLectorCpuAsync().ContinueWith(_ => _ui.Post(_ => _instalando = false, null), TaskScheduler.Default);
    }

    private void SugerirLectorCpu()
    {
        if (_nucleo.PuedeLeerCpu)
        {
            return;
        }
        _avisoCpu = true;
        _icono.ShowBalloonTip(10_000, "Falta la temperatura de la CPU", "Windows solo la deja leer con permisos. Haz clic aquí para instalar el lector de Vitral.", ToolTipIcon.Info);
    }

    private string TextoEstado()
    {
        if (_estado is not { } e)
        {
            return "Buscando la pantalla…";
        }
        return e.Pantalla.Conexion == EstadoConexion.Conectada
            ? $"{e.Pantalla.Info?.Modelo} · {e.Fps:0} cuadros por segundo"
            : TextoConexion(e.Pantalla);
    }

    private static string TextoConexion(EstadoPantalla p) => p.Conexion switch
    {
        EstadoConexion.Conectada => "Conectada",
        EstadoConexion.NoConectada => "No encuentro la pantalla",
        EstadoConexion.Ocupada => "Otro programa tiene la pantalla",
        EstadoConexion.NoSoportada => $"{p.Info?.Modelo}: todavía no es compatible",
        EstadoConexion.Buscando => "Buscando la pantalla…",
        _ => "La pantalla no responde",
    };

    private static void AlternarArranque()
    {
        if (Arranque.Activo())
        {
            Arranque.Desactivar();
        }
        else
        {
            Arranque.Activar();
        }
    }

    private static void DesactivarTrcc()
    {
        var respuesta = MessageBox.Show(
            "TRCC se abre solo al iniciar Windows y se queda con la pantalla.\n\n¿Desactivo su arranque? Windows te va a pedir permiso. Puedes volver a activarlo desde el Programador de tareas (tarea «TRCCAppStartup»).",
            "Vitral",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (respuesta == DialogResult.Yes && !Arranque.DesactivarTrcc())
        {
            MessageBox.Show("No se pudo desactivar el arranque de TRCC.", "Vitral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Salir()
    {
        _editor?.Close();
        _icono.Visible = false;
        ExitThread();
    }
}
