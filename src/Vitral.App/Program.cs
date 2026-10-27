using Velopack;
using Vitral.Core.Ejecucion;

namespace Vitral.App;

internal static class Program
{
    private const string NombreMutex = @"Local\Vitral.Instancia";
    private const string NombreAviso = @"Local\Vitral.AbrirEditor";

    [STAThread]
    private static int Main(string[] args)
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ =>
            {
                Arranque.Desactivar();
                if (ServicioSensores.Instalado())
                {
                    ServicioSensores.PedirRetiro();
                }
            })
            .Run();

        Registro.Configurar(Rutas.Registro);
        ApplicationConfiguration.Initialize();

        if (args.Length == 2 && args[0] == ServicioSensores.Argumento)
        {
            return ServicioSensores.Ejecutar(args[1]);
        }

        using var mutex = new Mutex(initiallyOwned: true, NombreMutex, out var primera);
        if (!primera)
        {
            if (EventWaitHandle.TryOpenExisting(NombreAviso, out var aviso))
            {
                aviso.Set();
                aviso.Dispose();
            }
            return 0;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Registro.Escribir($"Error en la interfaz: {e.Exception}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Registro.Escribir($"Error fatal: {e.ExceptionObject}");
        Registro.Escribir($"Vitral {Nucleo.Version} arranca.");

        using var nucleo = new Nucleo();
        using var actualizador = new Actualizador();
        using var bandeja = new Bandeja(nucleo, actualizador, abrirEditor: !args.Contains("--bandeja"));
        using var avisoEditor = new EventWaitHandle(false, EventResetMode.AutoReset, NombreAviso);
        var ui = SynchronizationContext.Current!;
        ThreadPool.RegisterWaitForSingleObject(avisoEditor, (_, _) => ui.Post(_ => bandeja.AbrirEditor(), null), null, Timeout.Infinite, executeOnlyOnce: false);

        ui.Post(async _ =>
        {
            try
            {
                await nucleo.IniciarAsync().ConfigureAwait(true);
            }
            catch (Exception e)
            {
                Registro.Escribir($"No se pudo arrancar: {e}");
                MessageBox.Show($"Vitral no pudo leer sus temas: {e.Message}", "Vitral", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }, null);

        Application.Run(bandeja);
        actualizador.AplicarAlSalir();
        Registro.Escribir("Vitral se cierra.");
        return 0;
    }
}
