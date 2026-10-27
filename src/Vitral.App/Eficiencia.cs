using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Vitral.App;

internal static partial class Eficiencia
{
    private const int ProcessPowerThrottling = 4;
    private const uint VelocidadDeEjecucion = 0x1;

    public static void Reposo()
    {
        Ahorro(true);
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        using var proceso = Process.GetCurrentProcess();
        EmptyWorkingSet(proceso.Handle);
    }

    public static void Activo() => Ahorro(false);

    private static void Ahorro(bool prendido)
    {
        var estado = new EstadoEnergia
        {
            Version = 1,
            Mascara = VelocidadDeEjecucion,
            Estado = prendido ? VelocidadDeEjecucion : 0,
        };
        using var proceso = Process.GetCurrentProcess();
        SetProcessInformation(proceso.Handle, ProcessPowerThrottling, ref estado, (uint)Marshal.SizeOf<EstadoEnergia>());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EstadoEnergia
    {
        public uint Version;
        public uint Mascara;
        public uint Estado;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessInformation(nint proceso, int clase, ref EstadoEnergia informacion, uint tamano);

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyWorkingSet(nint proceso);
}
