using Velopack;
using Velopack.Sources;
using Vitral.Core.Ejecucion;

namespace Vitral.App;

internal sealed class Actualizador : IDisposable
{
    public const string Repositorio = "https://github.com/Ntropicsam/vitral";

    private static readonly TimeSpan PrimeraRevision = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(6);

    private readonly UpdateManager _gestor = new(new GithubSource(Repositorio, null, false));
    private readonly System.Threading.Timer _reloj;
    private UpdateInfo? _lista;
    private int _revisando;

    public Actualizador()
    {
        _reloj = new System.Threading.Timer(_ => _ = RevisarAsync(), null, PrimeraRevision, Intervalo);
    }

    public event Action<string>? Lista;

    public bool Instalado => _gestor.IsInstalled;

    public string? VersionLista => _lista?.TargetFullRelease.Version.ToString();

    public void ReiniciarYActualizar()
    {
        if (_lista is not null)
        {
            _gestor.ApplyUpdatesAndRestart(_lista.TargetFullRelease, ["--bandeja"]);
        }
    }

    public void AplicarAlSalir()
    {
        if (_lista is not null)
        {
            _gestor.WaitExitThenApplyUpdates(_lista.TargetFullRelease, silent: true, restart: false);
        }
    }

    public void Dispose() => _reloj.Dispose();

    private async Task RevisarAsync()
    {
        if (!_gestor.IsInstalled || _lista is not null || Interlocked.Exchange(ref _revisando, 1) == 1)
        {
            return;
        }
        try
        {
            var nueva = await _gestor.CheckForUpdatesAsync().ConfigureAwait(false);
            if (nueva is null)
            {
                return;
            }
            await _gestor.DownloadUpdatesAsync(nueva).ConfigureAwait(false);
            _lista = nueva;
            Registro.Escribir($"Actualización lista: {VersionLista}");
            Lista?.Invoke(VersionLista!);
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidOperationException or TaskCanceledException)
        {
            Registro.Escribir($"No se pudo revisar si hay versión nueva: {e.Message}");
        }
        finally
        {
            Volatile.Write(ref _revisando, 0);
        }
    }
}
