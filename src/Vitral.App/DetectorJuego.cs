using System.Runtime.InteropServices;

namespace Vitral.App;

internal static partial class DetectorJuego
{
    private const int Ocupado = 2;
    private const int Direct3DPantallaCompleta = 3;
    private const int Presentacion = 4;

    public static bool HayJuego() =>
        SHQueryUserNotificationState(out var estado) == 0 && estado is Ocupado or Direct3DPantallaCompleta or Presentacion;

    [LibraryImport("shell32.dll")]
    private static partial int SHQueryUserNotificationState(out int estado);
}
