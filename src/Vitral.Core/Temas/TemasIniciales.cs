using Vitral.Core.Sensores;

namespace Vitral.Core.Temas;

public static class TemasIniciales
{
    public const int AnchoBase = 640;
    public const int AltoBase = 480;

    public static Tema Basico(string nombre, string sensorCpu, string sensorGpu) => new()
    {
        Nombre = nombre,
        Capas =
        [
            new CapaSensor { Nombre = "CPU", Etiqueta = "CPU", SensorId = sensorCpu, X = 600, Y = 48, Alineacion = Alineacion.Derecha },
            new CapaSensor { Nombre = "GPU", Etiqueta = "GPU", SensorId = sensorGpu, X = 600, Y = 250, Alineacion = Alineacion.Derecha },
        ],
    };

    public static Tema Plantilla(IReadOnlyList<SensorInfo> sensores, int ancho = AnchoBase, int alto = AltoBase)
    {
        ArgumentNullException.ThrowIfNull(sensores);
        var tema = new Tema
        {
            Nombre = "Plantilla de prueba",
            Fondo = new Fondo { Color = "#101319" },
            Capas =
            [
                new CapaMedidor
                {
                    Nombre = "Carga de la CPU",
                    SensorId = Sugerencias.CargaCpu(sensores),
                    X = 40,
                    Y = 110,
                    Ancho = 240,
                    Grosor = 18,
                    Color = "#ffb020",
                    ColorPista = "#262b35",
                },
                new CapaSensor { Nombre = "CPU", Etiqueta = "CPU", SensorId = Sugerencias.TemperaturaCpu(sensores), X = 600, Y = 70, Tamano = 96, Alineacion = Alineacion.Derecha },
                new CapaSensor { Nombre = "GPU", Etiqueta = "GPU", SensorId = Sugerencias.TemperaturaGpu(sensores), X = 600, Y = 260, Tamano = 96, Color = "#7ad3ff", Alineacion = Alineacion.Derecha },
                new CapaTexto { Nombre = "Firma", Texto = "Vitral", X = 40, Y = 416, Tamano = 26, Color = "#8a919e" },
            ],
        };
        return Escalar(tema, ancho, alto);
    }

    public static Tema Escalar(Tema tema, int ancho, int alto)
    {
        ArgumentNullException.ThrowIfNull(tema);
        if (ancho <= 0 || alto <= 0 || (ancho == tema.Ancho && alto == tema.Alto))
        {
            return tema;
        }
        var ex = (float)ancho / tema.Ancho;
        var ey = (float)alto / tema.Alto;
        var e = Math.Min(ex, ey);
        var capas = tema.Capas.Select(c => (c with { X = c.X * ex, Y = c.Y * ey }) switch
        {
            CapaSensor s => s with { Tamano = s.Tamano * e, TamanoEtiqueta = s.TamanoEtiqueta * e },
            CapaTexto t => t with { Tamano = t.Tamano * e },
            CapaMedidor m => m with { Ancho = m.Ancho * e, Grosor = m.Grosor * e },
            var otra => otra,
        }).ToList();
        var marco = tema.Fondo.Marco is { } m0 ? new Marco(m0.X * ex, m0.Y * ey, m0.Ancho * ex, m0.Alto * ey) : null;
        var fondo = tema.Fondo with { Marco = marco, RadioMarco = tema.Fondo.RadioMarco * e };
        return tema with { Ancho = ancho, Alto = alto, Capas = capas, Fondo = fondo };
    }
}
