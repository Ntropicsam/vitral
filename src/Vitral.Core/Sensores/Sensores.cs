namespace Vitral.Core.Sensores;

public enum TipoSensor
{
    Temperatura,
    Carga,
    Reloj,
    Ventilador,
    Potencia,
    Memoria,
    Datos,
    Voltaje,
    Otro,
}

public sealed record SensorInfo(string Id, string Nombre, string Hardware, TipoSensor Tipo);

public sealed class LecturaSensores(IReadOnlyDictionary<string, float> valores)
{
    public static readonly LecturaSensores Vacia = new(new Dictionary<string, float>());

    public IReadOnlyDictionary<string, float> Valores { get; } = valores;

    public float? Valor(string id) => Valores.TryGetValue(id, out var v) && float.IsFinite(v) ? v : null;
}

public interface IFuenteSensores : IDisposable
{
    IReadOnlyList<SensorInfo> Sensores { get; }

    LecturaSensores Leer();

    void Interes(IReadOnlySet<string>? ids);
}

public sealed class SensoresVacios : IFuenteSensores
{
    public IReadOnlyList<SensorInfo> Sensores { get; } = [];

    public LecturaSensores Leer() => LecturaSensores.Vacia;

    public void Interes(IReadOnlySet<string>? ids)
    {
    }

    public void Dispose()
    {
    }
}
