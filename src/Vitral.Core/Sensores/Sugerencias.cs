namespace Vitral.Core.Sensores;

public static class Sugerencias
{
    public static string TemperaturaCpu(IReadOnlyList<SensorInfo> sensores) =>
        Elegir(sensores, s => s.Tipo == TipoSensor.Temperatura && EsCpu(s), ["Tctl", "Package", "Tdie", "Core"]);

    public static string TemperaturaGpu(IReadOnlyList<SensorInfo> sensores) =>
        Elegir(sensores, s => s.Tipo == TipoSensor.Temperatura && EsGpu(s), ["GPU Core", "Core"]);

    public static string CargaCpu(IReadOnlyList<SensorInfo> sensores) =>
        Elegir(sensores, s => s.Tipo == TipoSensor.Carga && EsCpu(s), ["Total"]);

    private static bool EsCpu(SensorInfo s) => s.Id.Contains("cpu/", StringComparison.OrdinalIgnoreCase) && !EsGpu(s);

    private static bool EsGpu(SensorInfo s) => s.Id.StartsWith("/gpu", StringComparison.OrdinalIgnoreCase);

    private static string Elegir(IReadOnlyList<SensorInfo> sensores, Func<SensorInfo, bool> filtro, string[] preferidos)
    {
        ArgumentNullException.ThrowIfNull(sensores);
        var candidatos = sensores.Where(filtro).ToList();
        foreach (var nombre in preferidos)
        {
            var hallado = candidatos.Find(s => s.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
            if (hallado is not null)
            {
                return hallado.Id;
            }
        }
        return candidatos.Count > 0 ? candidatos[0].Id : string.Empty;
    }
}
