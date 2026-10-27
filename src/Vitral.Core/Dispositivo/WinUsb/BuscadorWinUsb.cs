using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Vitral.Core.Dispositivo.WinUsb;

public sealed class BuscadorWinUsb : IBuscadorUsb
{
    private static readonly string Marca = $"vid_{Protocolo.Vid:x4}&pid_{Protocolo.Pid:x4}";

    public ResultadoApertura Abrir()
    {
        var rutas = Rutas().ToList();
        if (rutas.Count == 0)
        {
            return ResultadoApertura.Fallo(MotivoSinPanel.NoConectado, "No hay ninguna pantalla 87AD:70DB conectada.");
        }

        Win32Exception? ultimo = null;
        foreach (var ruta in rutas)
        {
            try
            {
                return ResultadoApertura.Abierto(PuertoWinUsb.Abrir(ruta));
            }
            catch (Win32Exception e)
            {
                ultimo = e;
            }
        }

        return ultimo?.NativeErrorCode == Nativo.ErrorAccessDenied
            ? ResultadoApertura.Fallo(MotivoSinPanel.Ocupado, "Otro programa tiene abierta la pantalla (¿TRCC sigue abierto?).")
            : ResultadoApertura.Fallo(MotivoSinPanel.Error, $"No se pudo abrir la pantalla: {ultimo?.Message}");
    }

    private static IEnumerable<string> Rutas()
    {
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in GuidsDeclarados().Append(Nativo.InterfazUsbGenerica))
        {
            foreach (var ruta in RutasDeInterfaz(guid))
            {
                if (ruta.Contains(Marca, StringComparison.OrdinalIgnoreCase) && vistas.Add(ruta))
                {
                    yield return ruta;
                }
            }
        }
    }

    private static List<Guid> GuidsDeclarados()
    {
        var resultado = new List<Guid>();
        using var raiz = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\USB\VID_{Protocolo.Vid:X4}&PID_{Protocolo.Pid:X4}");
        if (raiz is null)
        {
            return resultado;
        }
        foreach (var instancia in raiz.GetSubKeyNames())
        {
            using var parametros = raiz.OpenSubKey($@"{instancia}\Device Parameters");
            var valores = parametros?.GetValue("DeviceInterfaceGUIDs") switch
            {
                string[] varios => varios,
                string uno => [uno],
                _ => parametros?.GetValue("DeviceInterfaceGUID") is string g ? [g] : [],
            };
            foreach (var valor in valores)
            {
                if (Guid.TryParse(valor, out var guid))
                {
                    resultado.Add(guid);
                }
            }
        }
        return resultado;
    }

    private static List<string> RutasDeInterfaz(Guid guid)
    {
        var rutas = new List<string>();
        var conjunto = Nativo.SetupDiGetClassDevs(guid, 0, 0, Nativo.DigcfPresent | Nativo.DigcfDeviceInterface);
        if (conjunto == -1)
        {
            return rutas;
        }
        try
        {
            var datos = new Nativo.SpDeviceInterfaceData { CbSize = Marshal.SizeOf<Nativo.SpDeviceInterfaceData>() };
            for (uint i = 0; Nativo.SetupDiEnumDeviceInterfaces(conjunto, 0, guid, i, ref datos); i++)
            {
                if (LeerRuta(conjunto, ref datos) is { } ruta)
                {
                    rutas.Add(ruta);
                }
            }
        }
        finally
        {
            Nativo.SetupDiDestroyDeviceInfoList(conjunto);
        }
        return rutas;
    }

    private static string? LeerRuta(nint conjunto, ref Nativo.SpDeviceInterfaceData datos)
    {
        Nativo.SetupDiGetDeviceInterfaceDetail(conjunto, ref datos, 0, 0, out var requerido, 0);
        if (requerido <= 0)
        {
            return null;
        }
        var bufer = Marshal.AllocHGlobal(requerido);
        try
        {
            Marshal.WriteInt32(bufer, IntPtr.Size == 8 ? 8 : 6);
            return Nativo.SetupDiGetDeviceInterfaceDetail(conjunto, ref datos, bufer, requerido, out _, 0)
                ? Marshal.PtrToStringUni(bufer + 4)
                : null;
        }
        finally
        {
            Marshal.FreeHGlobal(bufer);
        }
    }
}
