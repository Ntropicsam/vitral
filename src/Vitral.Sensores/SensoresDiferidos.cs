using Vitral.Core.Ejecucion;
using Vitral.Core.Sensores;

namespace Vitral.Sensores;

public sealed class SensoresDiferidos : IFuenteSensores
{
    private readonly Task<IFuenteSensores> _carga;
    private IReadOnlySet<string>? _interes;

    public SensoresDiferidos(Func<IFuenteSensores> crear)
    {
        _carga = Task.Run(() =>
        {
            try
            {
                var fuente = crear();
                fuente.Interes(Volatile.Read(ref _interes));
                return fuente;
            }
            catch (Exception e)
            {
                Registro.Escribir($"Los sensores no arrancaron: {e}");
                return (IFuenteSensores)new SensoresVacios();
            }
        });
    }

    public Task<IFuenteSensores> Listos => _carga;

    public IReadOnlyList<SensorInfo> Sensores => _carga.IsCompletedSuccessfully ? _carga.Result.Sensores : [];

    public LecturaSensores Leer() => _carga.IsCompletedSuccessfully ? _carga.Result.Leer() : LecturaSensores.Vacia;

    public void Interes(IReadOnlySet<string>? ids)
    {
        Volatile.Write(ref _interes, ids);
        if (_carga.IsCompletedSuccessfully)
        {
            _carga.Result.Interes(ids);
        }
    }

    public void Dispose()
    {
        if (_carga.IsCompletedSuccessfully)
        {
            _carga.Result.Dispose();
        }
    }
}
