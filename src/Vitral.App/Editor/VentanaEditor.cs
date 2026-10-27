using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Vitral.Core.Ejecucion;
using Vitral.Core.Render;

namespace Vitral.App.Editor;

internal sealed class VentanaEditor : Form
{
    public const string HostApp = "app.vitral.local";
    public const string HostTemas = "temas.vitral.local";

    private readonly Nucleo _nucleo;
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(0x12, 0x14, 0x19) };
    private Puente? _puente;
    private CuadroRenderizado? _ultimo;
    private int _vistaEnCamino;

    public VentanaEditor(Nucleo nucleo)
    {
        _nucleo = nucleo;
        Text = "Vitral";
        Icon = IconoVitral.Icono(32);
        MinimumSize = new Size(1040, 640);
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(0x12, 0x14, 0x19);
        Controls.Add(_web);
        Load += async (_, _) => await IniciarAsync().ConfigureAwait(true);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _nucleo.Motor.CuadroListo -= AlCuadro;
        _nucleo.Motor.EstadoCambio -= AlEstado;
        _nucleo.Motor.LegibilidadLista -= AlLegibilidad;
        _nucleo.Motor.SensoresLeidos -= AlSensores;
        _nucleo.AjustesCambiaron -= AlAjustes;
        _puente?.Dispose();
        _web.Dispose();
        base.OnFormClosed(e);
    }

    private async Task IniciarAsync()
    {
        try
        {
            var entorno = await CoreWebView2Environment.CreateAsync(null, Rutas.WebView).ConfigureAwait(true);
            await _web.EnsureCoreWebView2Async(entorno).ConfigureAwait(true);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show("Para el editor hace falta WebView2 de Microsoft, que viene con Windows 11. Instálalo desde go.microsoft.com/fwlink/p/?LinkId=2124703.", "Vitral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
        }

        var web = _web.CoreWebView2;
        web.SetVirtualHostNameToFolderMapping(HostApp, Rutas.Editor, CoreWebView2HostResourceAccessKind.DenyCors);
        web.SetVirtualHostNameToFolderMapping(HostTemas, Rutas.Temas, CoreWebView2HostResourceAccessKind.DenyCors);
        var ajustes = web.Settings;
        ajustes.AreDevToolsEnabled = System.Diagnostics.Debugger.IsAttached;
        ajustes.AreDefaultContextMenusEnabled = false;
        ajustes.IsStatusBarEnabled = false;
        ajustes.IsZoomControlEnabled = false;
        ajustes.AreBrowserAcceleratorKeysEnabled = false;
        ajustes.IsGeneralAutofillEnabled = false;
        ajustes.IsPasswordAutosaveEnabled = false;
        web.NavigationStarting += (_, e) => e.Cancel = !e.Uri.StartsWith($"https://{HostApp}/", StringComparison.OrdinalIgnoreCase);
        web.NewWindowRequested += (_, e) => e.Handled = true;
        web.WebMessageReceived += AlMensaje;

        _puente = new Puente(_nucleo, Enviar, () => _ultimo?.Vista);
        _nucleo.Motor.CuadroListo += AlCuadro;
        _nucleo.Motor.EstadoCambio += AlEstado;
        _nucleo.Motor.LegibilidadLista += AlLegibilidad;
        _nucleo.Motor.SensoresLeidos += AlSensores;
        _nucleo.AjustesCambiaron += AlAjustes;
        web.Navigate($"https://{HostApp}/index.html");
    }

    private void AlMensaje(object? remitente, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!e.Source.StartsWith($"https://{HostApp}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        string json;
        try
        {
            json = e.WebMessageAsJson;
        }
        catch (ArgumentException)
        {
            return;
        }
        _puente?.Recibir(json);
    }

    private void Enviar(string json)
    {
        if (!IsDisposed && _web.CoreWebView2 is { } web)
        {
            web.PostWebMessageAsJson(json);
        }
    }

    private void AlCuadro(CuadroRenderizado cuadro)
    {
        Volatile.Write(ref _ultimo, cuadro);
        if (Interlocked.Exchange(ref _vistaEnCamino, 1) == 1 || IsDisposed)
        {
            return;
        }
        Despachar(() =>
        {
            Volatile.Write(ref _vistaEnCamino, 0);
            if (Volatile.Read(ref _ultimo) is { } ultimo)
            {
                Enviar(Mensajes.Vista(ultimo));
            }
        });
    }

    private void AlEstado(EstadoMotor estado) => Despachar(() => Enviar(Mensajes.Estado(estado)));

    private void AlLegibilidad(ResultadoLegibilidad? r) => Despachar(() => Enviar(Mensajes.Legibilidad(r)));

    private void AlSensores(Core.Sensores.LecturaSensores l) => Despachar(() => Enviar(Mensajes.Valores(l)));

    private void AlAjustes() => Enviar(Mensajes.Ajustes(_nucleo.Ajustes));

    private void Despachar(Action accion)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }
        try
        {
            BeginInvoke(accion);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
