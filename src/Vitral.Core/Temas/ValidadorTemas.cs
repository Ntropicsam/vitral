using System.Globalization;

namespace Vitral.Core.Temas;

public static class ValidadorTemas
{
    public const int MaximoCapas = 64;
    public const int LargoMaximoTexto = 200;
    public const int LargoMaximoNombre = 60;

    public static Tema Normalizar(Tema tema)
    {
        ArgumentNullException.ThrowIfNull(tema);
        var ancho = Math.Clamp(tema.Ancho, 64, 4096);
        var alto = Math.Clamp(tema.Alto, 64, 4096);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var capas = (tema.Capas ?? [])
            .Where(c => c is not null)
            .Take(MaximoCapas)
            .Select(c => NormalizarCapa(c, ancho, alto, ids))
            .ToList();

        return tema with
        {
            Version = Tema.VersionActual,
            Nombre = Recortar(tema.Nombre, LargoMaximoNombre, "Tema sin nombre"),
            Ancho = ancho,
            Alto = alto,
            Fondo = NormalizarFondo(tema.Fondo ?? new Fondo()),
            Capas = capas,
        };
    }

    public static string? ArchivoSeguro(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 120)
        {
            return null;
        }
        var limpio = nombre.Trim();
        if (limpio != Path.GetFileName(limpio)
            || limpio.StartsWith('.')
            || limpio.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }
        return limpio;
    }

    public static string Color(string? valor, string respaldo)
    {
        if (valor is { Length: 7 or 9 } && valor[0] == '#'
            && uint.TryParse(valor.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return valor.ToLowerInvariant();
        }
        return respaldo;
    }

    private static Fondo NormalizarFondo(Fondo fondo)
    {
        var archivo = ArchivoSeguro(fondo.Archivo);
        var tipo = fondo.Tipo != TipoFondo.Color && archivo is null ? TipoFondo.Color : fondo.Tipo;
        return fondo with
        {
            Tipo = tipo,
            Color = Color(fondo.Color, "#0d0f14"),
            Archivo = tipo == TipoFondo.Color ? null : archivo,
            Ajuste = Enum.IsDefined(fondo.Ajuste) ? fondo.Ajuste : AjusteFondo.Cubrir,
        };
    }

    private static Capa NormalizarCapa(Capa capa, int ancho, int alto, HashSet<string> ids)
    {
        var id = string.IsNullOrWhiteSpace(capa.Id) || capa.Id.Length > 32 || !ids.Add(capa.Id)
            ? NuevoId(ids)
            : capa.Id;
        var baseNormal = capa with
        {
            Id = id,
            Nombre = Recortar(capa.Nombre, LargoMaximoNombre, "Capa"),
            X = Limitar(capa.X, -ancho, ancho * 2),
            Y = Limitar(capa.Y, -alto, alto * 2),
        };

        return baseNormal switch
        {
            CapaSensor s => s with
            {
                SensorId = Recortar(s.SensorId, 200, string.Empty),
                Etiqueta = Recortar(s.Etiqueta, LargoMaximoNombre, string.Empty),
                Tamano = Limitar(s.Tamano, 8, 400),
                TamanoEtiqueta = Limitar(s.TamanoEtiqueta, 8, 200),
                Color = Color(s.Color, "#f4efe6"),
                ColorEtiqueta = Color(s.ColorEtiqueta, "#9aa3b5"),
                Formato = Enum.IsDefined(s.Formato) ? s.Formato : FormatoValor.Corto,
                Alineacion = Enum.IsDefined(s.Alineacion) ? s.Alineacion : Alineacion.Izquierda,
            },
            CapaTexto t => t with
            {
                Texto = Recortar(t.Texto, LargoMaximoTexto, string.Empty),
                Tamano = Limitar(t.Tamano, 8, 400),
                Color = Color(t.Color, "#f4efe6"),
                Alineacion = Enum.IsDefined(t.Alineacion) ? t.Alineacion : Alineacion.Izquierda,
            },
            CapaMedidor m => NormalizarMedidor(m),
            _ => baseNormal,
        };
    }

    private static CapaMedidor NormalizarMedidor(CapaMedidor m)
    {
        var minimo = Limitar(m.Minimo, -100_000, 100_000);
        var maximo = Limitar(m.Maximo, -100_000, 100_000);
        if (maximo <= minimo)
        {
            (minimo, maximo) = maximo < minimo ? (maximo, minimo) : (minimo, minimo + 100);
        }
        return m with
        {
            SensorId = Recortar(m.SensorId, 200, string.Empty),
            Estilo = Enum.IsDefined(m.Estilo) ? m.Estilo : EstiloMedidor.Anillo,
            Minimo = minimo,
            Maximo = maximo,
            Ancho = Limitar(m.Ancho, 16, 2000),
            Grosor = Limitar(m.Grosor, 1, 200),
            Color = Color(m.Color, "#ffb020"),
            ColorPista = Color(m.ColorPista, "#2a2f3a"),
            Formato = Enum.IsDefined(m.Formato) ? m.Formato : FormatoValor.Corto,
        };
    }

    private static string NuevoId(HashSet<string> ids)
    {
        string id;
        do
        {
            id = Guid.NewGuid().ToString("N")[..8];
        }
        while (!ids.Add(id));
        return id;
    }

    private static float Limitar(float valor, float minimo, float maximo) =>
        float.IsFinite(valor) ? Math.Clamp(valor, minimo, maximo) : minimo;

    private static string Recortar(string? texto, int largo, string respaldo)
    {
        var limpio = texto?.Trim() ?? string.Empty;
        if (limpio.Length == 0)
        {
            return respaldo;
        }
        return limpio.Length <= largo ? limpio : limpio[..largo];
    }
}
