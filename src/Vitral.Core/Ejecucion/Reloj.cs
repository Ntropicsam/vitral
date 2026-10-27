using System.Diagnostics;

namespace Vitral.Core.Ejecucion;

public interface IReloj
{
    long Milisegundos { get; }
}

public sealed class RelojSistema : IReloj
{
    private readonly Stopwatch _cronometro = Stopwatch.StartNew();

    public long Milisegundos => _cronometro.ElapsedMilliseconds;
}

public static class Registro
{
    private const long TamanoMaximo = 1024 * 1024;
    private static readonly Lock Candado = new();
    private static string? _ruta;

    public static void Configurar(string ruta) => _ruta = ruta;

    public static void Escribir(string mensaje)
    {
        if (_ruta is null)
        {
            return;
        }
        lock (Candado)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                var info = new FileInfo(_ruta);
                if (info.Exists && info.Length > TamanoMaximo)
                {
                    File.Move(_ruta, _ruta + ".1", overwrite: true);
                }
                File.AppendAllText(_ruta, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {mensaje}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }
        }
    }
}
