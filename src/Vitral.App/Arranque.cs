using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace Vitral.App;

internal static class Arranque
{
    private const string ClaveInicio = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Nombre = "Vitral";
    private const string TareaTrcc = "TRCCAppStartup";

    public static bool Activo()
    {
        using var clave = Registry.CurrentUser.OpenSubKey(ClaveInicio);
        return clave?.GetValue(Nombre) is string;
    }

    public static void Activar()
    {
        using var clave = Registry.CurrentUser.CreateSubKey(ClaveInicio);
        clave.SetValue(Nombre, $"\"{Ejecutable()}\" --bandeja");
    }

    public static void Desactivar()
    {
        using var clave = Registry.CurrentUser.OpenSubKey(ClaveInicio, writable: true);
        clave?.DeleteValue(Nombre, throwOnMissingValue: false);
    }

    public static bool TrccArrancaSolo()
    {
        var (codigo, salida) = Schtasks("/Query", "/TN", TareaTrcc, "/FO", "CSV", "/NH");
        return codigo == 0
            && !salida.Contains("Deshabilitado", StringComparison.OrdinalIgnoreCase)
            && !salida.Contains("Disabled", StringComparison.OrdinalIgnoreCase);
    }

    public static bool DesactivarTrcc()
    {
        var inicio = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), $"/Change /TN \"{TareaTrcc}\" /DISABLE")
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        try
        {
            using var proceso = Process.Start(inicio);
            proceso?.WaitForExit(15_000);
            return proceso is { HasExited: true, ExitCode: 0 };
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static string Ejecutable()
    {
        var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Vitral.exe"));
        return File.Exists(raiz) && File.Exists(Path.Combine(Path.GetDirectoryName(raiz)!, "Update.exe"))
            ? raiz
            : Environment.ProcessPath!;
    }

    private static (int Codigo, string Salida) Schtasks(params string[] argumentos)
    {
        var inicio = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in argumentos)
        {
            inicio.ArgumentList.Add(a);
        }
        using var proceso = Process.Start(inicio)!;
        var salida = proceso.StandardOutput.ReadToEnd();
        proceso.WaitForExit(10_000);
        return (proceso.ExitCode, salida);
    }
}
