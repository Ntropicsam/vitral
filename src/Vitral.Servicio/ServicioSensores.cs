using System.IO.Pipes;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;
using Vitral.Sensores;

namespace Vitral.Servicio;

public sealed class ServicioSensores : ServiceBase
{
    private const int MaximoClientes = 4;

    private static readonly string Version =
        typeof(ServicioSensores).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0";

    private readonly CancellationTokenSource _fin = new();
    private Task<SensoresLhm?>? _sensores;
    private Task? _escucha;

    public ServicioSensores()
    {
        ServiceName = CanalSensores.NombreServicio;
        CanStop = true;
        CanShutdown = true;
    }

    protected override void OnStart(string[] args)
    {
        _sensores = Task.Run(AbrirSensores);
        _escucha = Task.Run(() => EscucharAsync(_fin.Token));
        Registro.Escribir($"Servicio de sensores {Version} en marcha.");
    }

    protected override void OnStop() => Detener();

    protected override void OnShutdown() => Detener();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fin.Dispose();
        }
        base.Dispose(disposing);
    }

    private void Detener()
    {
        _fin.Cancel();
        _escucha?.Wait(TimeSpan.FromSeconds(3));
        if (_sensores is { IsCompletedSuccessfully: true, Result: { } lhm })
        {
            lhm.Dispose();
        }
    }

    private static SensoresLhm? AbrirSensores()
    {
        try
        {
            return new SensoresLhm();
        }
        catch (Exception e)
        {
            Registro.Escribir($"LibreHardwareMonitor no arrancó: {e}");
            return null;
        }
    }

    private async Task EscucharAsync(CancellationToken fin)
    {
        var primera = true;
        var clientes = new List<Task>();
        while (!fin.IsCancellationRequested)
        {
            clientes.RemoveAll(c => c.IsCompleted);
            if (clientes.Count >= MaximoClientes)
            {
                await Task.WhenAny(clientes).ConfigureAwait(false);
                continue;
            }
            NamedPipeServerStream tuberia;
            try
            {
                tuberia = Crear(primera);
                primera = false;
            }
            catch (IOException e)
            {
                Registro.Escribir($"No se pudo abrir la tubería de sensores: {e.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), fin).ConfigureAwait(false);
                continue;
            }
            try
            {
                await tuberia.WaitForConnectionAsync(fin).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await tuberia.DisposeAsync().ConfigureAwait(false);
                break;
            }
            clientes.Add(Task.Run(() => Atender(tuberia, fin), fin));
        }
    }

    private void Atender(NamedPipeServerStream tuberia, CancellationToken fin)
    {
        using (tuberia)
        {
            try
            {
                while (!fin.IsCancellationRequested && tuberia.IsConnected)
                {
                    var pedido = CanalSensores.Leer<PedidoSensores>(tuberia);
                    if (pedido is null)
                    {
                        return;
                    }
                    CanalSensores.Escribir(tuberia, Responder(pedido));
                }
            }
            catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException)
            {
                Registro.Escribir($"Cliente de sensores: {e.Message}");
            }
        }
    }

    private RespuestaSensores Responder(PedidoSensores pedido)
    {
        if (_sensores is not { IsCompletedSuccessfully: true, Result: { } lhm })
        {
            return new RespuestaSensores(Version, null, new Dictionary<string, float>());
        }
        var lectura = lhm.Leer(CanalSensores.IdsValidos(pedido.Ids));
        return new RespuestaSensores(Version, pedido.Lista ? lhm.Sensores : null, lectura.Valores);
    }

    private static NamedPipeServerStream Crear(bool primera)
    {
        var seguridad = new PipeSecurity();
        seguridad.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        seguridad.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        seguridad.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.InteractiveSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        seguridad.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        var opciones = PipeOptions.Asynchronous | (primera ? PipeOptions.FirstPipeInstance : PipeOptions.None);
        return NamedPipeServerStreamAcl.Create(
            CanalSensores.NombreTuberia,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            opciones,
            64 * 1024,
            64 * 1024,
            seguridad);
    }
}
