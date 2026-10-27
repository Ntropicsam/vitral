using Vitral.Core.Sensores;

namespace Vitral.Sensores;

public sealed class SensoresIntercambiables(IFuenteSensores inicial) : IFuenteSensores
{
    private readonly Lock _candado = new();
    private IFuenteSensores _actual = inicial;
    private IReadOnlySet<string>? _interes;

    public IFuenteSensores Actual
    {
        get
        {
            lock (_candado)
            {
                return _actual;
            }
        }
    }

    public IReadOnlyList<SensorInfo> Sensores => Actual.Sensores;

    public LecturaSensores Leer() => Actual.Leer();

    public void Interes(IReadOnlySet<string>? ids)
    {
        lock (_candado)
        {
            _interes = ids;
            _actual.Interes(ids);
        }
    }

    public void Cambiar(IFuenteSensores nueva)
    {
        ArgumentNullException.ThrowIfNull(nueva);
        IFuenteSensores vieja;
        lock (_candado)
        {
            vieja = _actual;
            nueva.Interes(_interes);
            _actual = nueva;
        }
        vieja.Dispose();
    }

    public void Dispose() => Actual.Dispose();
}
