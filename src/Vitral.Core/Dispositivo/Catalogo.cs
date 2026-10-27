namespace Vitral.Core.Dispositivo;

public sealed record ModeloPanel(string Nombre, int Ancho, int Alto, FormatoCuadro Formato, int GiroDeFabrica = 0)
{
    public bool Soportado => Formato == FormatoCuadro.Jpeg;
}

public static class Catalogo
{
    private static readonly ModeloPanel PorDefecto = new("Pantalla 480 × 480", 480, 480, FormatoCuadro.Jpeg);

    public static ModeloPanel Buscar(byte pm, byte sub) => (pm, sub) switch
    {
        (1, 48) => Jpeg(1600, 720),
        (1, 49) => Jpeg(1920, 462),
        (1, _) => PorDefecto with { Nombre = "Grand Vision" },
        (3, _) => PorDefecto with { Nombre = "Core Vision" },
        (4, 1) => PorDefecto with { Nombre = "Hyper Vision" },
        (5, _) => new("Mjolnir Vision", 320, 240, FormatoCuadro.Jpeg),
        (6, 1) => PorDefecto with { Nombre = "Frozen Warframe Ultra", GiroDeFabrica = 180 },
        (6, 2) => PorDefecto with { Nombre = "Frozen Vision V2", GiroDeFabrica = 180 },
        (6, _) => PorDefecto with { GiroDeFabrica = 180 },
        (7, 1) => new("Stream Vision", 640, 480, FormatoCuadro.Jpeg),
        (7, 2) => new("Mjolnir Vision Pro", 640, 480, FormatoCuadro.Jpeg),
        (7 or 14, _) => Jpeg(640, 480),
        (9 or 11 or 20, _) => Jpeg(854, 480),
        (10 or 16, _) => Jpeg(960, 540),
        (12, _) => Jpeg(800, 480),
        (13 or 17 or 18, _) => Jpeg(960, 320),
        (15, _) => new("Pantalla 640 × 172", 640, 172, FormatoCuadro.Rgb565),
        (32, _) => new("Pantalla 320 × 320", 320, 320, FormatoCuadro.Rgb565),
        (50, _) => new("Pantalla 320 × 240", 320, 240, FormatoCuadro.Rgb565),
        (63 or 64, _) => Jpeg(1600, 720),
        (65 or 66, _) => Jpeg(1920, 462),
        (68, _) => Jpeg(1280, 480),
        (69, _) => Jpeg(1920, 440),
        _ => PorDefecto,
    };

    public static readonly IReadOnlyList<string> Elegibles =
    [
        "7-2", "7-1", "5-0", "1-0", "3-0", "4-1", "6-1", "6-2",
        "1-48", "1-49", "68-0", "69-0", "16-0", "13-0", "11-0", "12-0",
    ];

    public static string Clave(byte pm, byte sub) => $"{pm}-{sub}";

    public static ModeloPanel? PorClave(string? clave)
    {
        if (clave is null || !Elegibles.Contains(clave))
        {
            return null;
        }
        var partes = clave.Split('-');
        return Buscar(byte.Parse(partes[0], System.Globalization.CultureInfo.InvariantCulture), byte.Parse(partes[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    private static ModeloPanel Jpeg(int ancho, int alto) => new($"Pantalla {ancho} × {alto}", ancho, alto, FormatoCuadro.Jpeg);
}
