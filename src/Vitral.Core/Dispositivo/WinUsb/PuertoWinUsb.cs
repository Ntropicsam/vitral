using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Vitral.Core.Dispositivo.WinUsb;

internal sealed class PuertoWinUsb : IPuertoUsb
{
    private const uint TiempoEscrituraMs = 5000;
    private const uint TiempoLecturaMs = 1000;

    private readonly SafeFileHandle _archivo;
    private nint _interfaz;

    private PuertoWinUsb(SafeFileHandle archivo, nint interfaz)
    {
        _archivo = archivo;
        _interfaz = interfaz;
    }

    public static PuertoWinUsb Abrir(string ruta)
    {
        var archivo = Nativo.CreateFile(
            ruta,
            Nativo.GenericRead | Nativo.GenericWrite,
            Nativo.FileShareRead | Nativo.FileShareWrite,
            0,
            Nativo.OpenExisting,
            Nativo.FileAttributeNormal | Nativo.FileFlagOverlapped,
            0);
        if (archivo.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            archivo.Dispose();
            throw new Win32Exception(error);
        }

        if (!Nativo.WinUsb_Initialize(archivo, out var interfaz))
        {
            var error = Marshal.GetLastPInvokeError();
            archivo.Dispose();
            throw new Win32Exception(error);
        }

        var puerto = new PuertoWinUsb(archivo, interfaz);
        puerto.Politica(Protocolo.EndpointSalida, TiempoEscrituraMs);
        puerto.Politica(Protocolo.EndpointEntrada, TiempoLecturaMs);
        return puerto;
    }

    public unsafe int Escribir(ReadOnlySpan<byte> datos)
    {
        ObjectDisposedException.ThrowIf(_interfaz == 0, this);
        fixed (byte* p = datos)
        {
            if (!Nativo.WinUsb_WritePipe(_interfaz, Protocolo.EndpointSalida, p, (uint)datos.Length, out var salieron, 0))
            {
                throw ErrorDeEs("escribir en la pantalla");
            }
            return (int)salieron;
        }
    }

    public unsafe int Leer(Span<byte> destino)
    {
        ObjectDisposedException.ThrowIf(_interfaz == 0, this);
        fixed (byte* p = destino)
        {
            if (!Nativo.WinUsb_ReadPipe(_interfaz, Protocolo.EndpointEntrada, p, (uint)destino.Length, out var leidos, 0))
            {
                var error = Marshal.GetLastPInvokeError();
                if (error == Nativo.ErrorSemTimeout)
                {
                    return 0;
                }
                throw new IOException($"No se pudo leer de la pantalla: {new Win32Exception(error).Message}", error);
            }
            return (int)leidos;
        }
    }

    public void Dispose()
    {
        if (_interfaz != 0)
        {
            Nativo.WinUsb_Free(_interfaz);
            _interfaz = 0;
        }
        _archivo.Dispose();
    }

    private void Politica(byte endpoint, uint milisegundos)
    {
        if (!Nativo.WinUsb_SetPipePolicy(_interfaz, endpoint, Nativo.PipeTransferTimeout, sizeof(uint), in milisegundos))
        {
            throw ErrorDeEs("configurar el tiempo de espera del USB");
        }
    }

    private static IOException ErrorDeEs(string accion)
    {
        var error = Marshal.GetLastPInvokeError();
        return new IOException($"No se pudo {accion}: {new Win32Exception(error).Message}", error);
    }
}
