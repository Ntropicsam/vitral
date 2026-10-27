using System.Text.Json.Serialization;
using Vitral.Core.Sensores;

namespace Vitral.Core.Temas;

[JsonConverter(typeof(JsonStringEnumConverter<Distribucion>))]
public enum Distribucion
{
    Completa,
    MarcoIzquierda,
    MarcoArriba,
    MarcoCentrado,
    SoloDatos,
}

public static class Distribuciones
{
    public static Tema Crear(Distribucion distribucion, string nombre, IReadOnlyList<SensorInfo> sensores, int ancho, int alto)
    {
        ArgumentNullException.ThrowIfNull(sensores);
        var cpu = Sugerencias.TemperaturaCpu(sensores);
        var gpu = Sugerencias.TemperaturaGpu(sensores);
        var baseTema = distribucion switch
        {
            Distribucion.MarcoIzquierda => new Tema
            {
                Fondo = new Fondo { Color = "#0d0f14", Marco = new Marco(0, 0, 400, 480) },
                Capas = [Cifra("CPU", cpu, 520, 90, Alineacion.Centro, 84), Cifra("GPU", gpu, 520, 270, Alineacion.Centro, 84, "#7ad3ff")],
            },
            Distribucion.MarcoArriba => new Tema
            {
                Fondo = new Fondo { Color = "#0d0f14", Marco = new Marco(0, 0, 640, 320) },
                Capas = [Cifra("CPU", cpu, 170, 344, Alineacion.Centro, 76, posicion: PosicionEtiqueta.Lado), Cifra("GPU", gpu, 470, 344, Alineacion.Centro, 76, "#7ad3ff", PosicionEtiqueta.Lado)],
            },
            Distribucion.MarcoCentrado => new Tema
            {
                Fondo = new Fondo { Color = "#0d0f14", Marco = new Marco(60, 40, 520, 300), RadioMarco = 18 },
                Capas = [Cifra("CPU", cpu, 190, 370, Alineacion.Centro, 64, posicion: PosicionEtiqueta.Lado), Cifra("GPU", gpu, 450, 370, Alineacion.Centro, 64, "#7ad3ff", PosicionEtiqueta.Lado)],
            },
            Distribucion.SoloDatos => TemasIniciales.Plantilla(sensores),
            _ => TemasIniciales.Basico(nombre, cpu, gpu),
        };
        return TemasIniciales.Escalar(baseTema with { Nombre = nombre }, ancho, alto);
    }

    private static CapaSensor Cifra(string etiqueta, string sensor, float x, float y, Alineacion alineacion, float tamano, string color = "#f4efe6", PosicionEtiqueta posicion = PosicionEtiqueta.Encima) => new()
    {
        Nombre = etiqueta,
        Etiqueta = etiqueta,
        SensorId = sensor,
        X = x,
        Y = y,
        Alineacion = alineacion,
        Tamano = tamano,
        Color = color,
        PosicionEtiqueta = posicion,
    };
}
