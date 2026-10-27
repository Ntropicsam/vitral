using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;

namespace Vitral.App;

internal static class ServicioSensores
{
    public const string Argumento = "--servicio";

    private const int CanceladoPorLaPersona = 1223;

    private static readonly string[] Archivos = ["VitralSensores.exe", "MonoPosixHelper.dll", "libMonoPosixHelper.dll"];

    private static readonly string Origen = Path.Combine(AppContext.BaseDirectory, "servicio");
    private static readonly string Destino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Vitral", "Sensores");
    private static readonly string Ejecutable = Path.Combine(Destino, "VitralSensores.exe");

    public static bool Incluido => File.Exists(Path.Combine(Origen, "VitralSensores.exe"));

    public static bool Instalado()
    {
        try
        {
            using var servicio = new ServiceController(CanalSensores.NombreServicio);
            _ = servicio.Status;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool PedirInstalacion() => Elevar("instalar");

    public static bool PedirRetiro() => Elevar("quitar");

    public static int Ejecutar(string accion)
    {
        try
        {
            if (accion == "instalar")
            {
                Instalar();
            }
            else if (accion == "quitar")
            {
                Quitar();
            }
            return 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or System.ServiceProcess.TimeoutException)
        {
            MessageBox.Show($"No se pudo {(accion == "instalar" ? "instalar" : "quitar")} el lector de la CPU: {e.Message}", "Vitral", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 1;
        }
    }

    private static bool Elevar(string accion)
    {
        var inicio = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas" };
        inicio.ArgumentList.Add(Argumento);
        inicio.ArgumentList.Add(accion);
        try
        {
            using var proceso = Process.Start(inicio);
            if (proceso is null)
            {
                return false;
            }
            proceso.WaitForExit(TimeSpan.FromMinutes(2));
            return proceso.HasExited && proceso.ExitCode == 0;
        }
        catch (Win32Exception e)
        {
            if (e.NativeErrorCode != CanceladoPorLaPersona)
            {
                Registro.Escribir($"No se pudo pedir permisos para el lector de la CPU: {e.Message}");
            }
            return false;
        }
    }

    private static void Instalar()
    {
        if (!Incluido)
        {
            throw new IOException("Esta copia de Vitral no trae el lector de la CPU.");
        }
        var existe = Instalado();
        if (existe)
        {
            Parar();
        }
        Directory.CreateDirectory(Destino);
        foreach (var nombre in Archivos)
        {
            var origen = Path.Combine(Origen, nombre);
            var info = new FileInfo(origen);
            if (!info.Exists || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new IOException($"Falta o no es válido el archivo {nombre} del lector.");
            }
            File.Copy(origen, Path.Combine(Destino, nombre), overwrite: true);
        }
        if (!existe)
        {
            Sc("create", CanalSensores.NombreServicio, "binPath=", $"\"{Ejecutable}\"", "start=", "auto", "DisplayName=", "Vitral · Sensores");
            Sc("description", CanalSensores.NombreServicio, "Lee los sensores del equipo para la pantalla de Vitral. Solo entrega lecturas.");
            Sc("failure", CanalSensores.NombreServicio, "reset=", "86400", "actions=", "restart/5000/restart/5000//");
        }
        using var servicio = new ServiceController(CanalSensores.NombreServicio);
        servicio.Start();
        servicio.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
    }

    private static void Quitar()
    {
        if (Instalado())
        {
            Parar();
            Sc("delete", CanalSensores.NombreServicio);
        }
        if (Directory.Exists(Destino))
        {
            Directory.Delete(Destino, recursive: true);
        }
        var padre = Path.GetDirectoryName(Destino)!;
        if (Directory.Exists(padre) && !Directory.EnumerateFileSystemEntries(padre).Any())
        {
            Directory.Delete(padre);
        }
    }

    private static void Parar()
    {
        using var servicio = new ServiceController(CanalSensores.NombreServicio);
        if (servicio.Status != ServiceControllerStatus.Stopped)
        {
            servicio.Stop();
            servicio.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
        }
    }

    private static void Sc(params string[] argumentos)
    {
        var inicio = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "sc.exe"))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        foreach (var a in argumentos)
        {
            inicio.ArgumentList.Add(a);
        }
        using var proceso = Process.Start(inicio)!;
        var salida = proceso.StandardOutput.ReadToEnd();
        proceso.WaitForExit(15_000);
        if (proceso.ExitCode != 0)
        {
            throw new InvalidOperationException($"sc {argumentos[0]} falló: {salida.Trim()}");
        }
    }
}
