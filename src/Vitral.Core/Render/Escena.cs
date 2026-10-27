using SkiaSharp;
using Vitral.Core.Medios;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.Core.Render;

public sealed record CajaCapa(string Id, float X, float Y, float Ancho, float Alto);

public sealed record EntradaEscena(Tema Tema, IFondoCargado Fondo, int Cuadro, LecturaSensores Lectura, Func<string, TipoSensor> TipoDe);

public static class Escena
{
    private static readonly SKColor ColorVelo = new(0x0d, 0x0f, 0x14, 184);
    private static readonly SKSamplingOptions Muestreo = new(SKFilterMode.Linear, SKMipmapMode.None);

    public static IReadOnlyList<CajaCapa> Dibujar(SKCanvas lienzo, EntradaEscena e)
    {
        ArgumentNullException.ThrowIfNull(lienzo);
        ArgumentNullException.ThrowIfNull(e);
        DibujarFondo(lienzo, e.Tema, e.Fondo, e.Cuadro);
        var cajas = new List<CajaCapa>(e.Tema.Capas.Count);
        foreach (var capa in e.Tema.Capas)
        {
            if (!capa.Visible)
            {
                continue;
            }
            var caja = capa switch
            {
                CapaSensor s => DibujarSensor(lienzo, s, e),
                CapaTexto t => DibujarTexto(lienzo, t),
                CapaMedidor m => DibujarMedidor(lienzo, m, e),
                _ => null,
            };
            if (caja is not null)
            {
                cajas.Add(caja);
            }
        }
        return cajas;
    }

    public static void DibujarFondo(SKCanvas lienzo, Tema tema, IFondoCargado fondo, int cuadro)
    {
        ArgumentNullException.ThrowIfNull(lienzo);
        ArgumentNullException.ThrowIfNull(tema);
        ArgumentNullException.ThrowIfNull(fondo);
        lienzo.Clear(SKColor.Parse(tema.Fondo.Color));
        var imagen = fondo.Imagen(cuadro);
        if (imagen is null)
        {
            return;
        }
        var destino = Ajustar(imagen.Width, imagen.Height, tema.Ancho, tema.Alto, tema.Fondo.Ajuste);
        lienzo.DrawImage(imagen, destino, Muestreo);
    }

    public static SKRect Ajustar(float ancho, float alto, float anchoLienzo, float altoLienzo, AjusteFondo ajuste)
    {
        if (ajuste == AjusteFondo.Estirar || ancho <= 0 || alto <= 0)
        {
            return new SKRect(0, 0, anchoLienzo, altoLienzo);
        }
        var escalaX = anchoLienzo / ancho;
        var escalaY = altoLienzo / alto;
        var escala = ajuste == AjusteFondo.Cubrir ? Math.Max(escalaX, escalaY) : Math.Min(escalaX, escalaY);
        var w = ancho * escala;
        var h = alto * escala;
        var x = (anchoLienzo - w) / 2;
        var y = (altoLienzo - h) / 2;
        return new SKRect(x, y, x + w, y + h);
    }

    public static CajaCapa? Medir(Capa capa, EntradaEscena e)
    {
        ArgumentNullException.ThrowIfNull(e);
        switch (capa)
        {
            case CapaSensor s:
                {
                    var d = DistribuirSensor(s, e);
                    d.Etiqueta?.Dispose();
                    d.Valor.Dispose();
                    return d.Caja;
                }
            case CapaTexto t when !string.IsNullOrEmpty(t.Texto):
                {
                    using var linea = new LineaTexto(t.Texto, Tipografia.Etiquetas, t.Tamano);
                    return CajaTexto(t, linea);
                }
            default:
                return null;
        }
    }

    private static CajaCapa DibujarSensor(SKCanvas lienzo, CapaSensor s, EntradaEscena e)
    {
        var (caja, etiqueta, valor, baseEtiqueta, baseValor) = DistribuirSensor(s, e);
        using var valorLiberable = valor;
        using var etiquetaLiberable = etiqueta;
        if (s.Velo)
        {
            DibujarVelo(lienzo, caja, s.Tamano);
        }
        using var pintura = new SKPaint { IsAntialias = true };
        if (etiqueta is not null)
        {
            pintura.Color = SKColor.Parse(s.ColorEtiqueta);
            etiqueta.Dibujar(lienzo, LineaTexto.Izquierda(s.X, etiqueta.Ancho, s.Alineacion), baseEtiqueta, pintura);
        }
        pintura.Color = SKColor.Parse(s.Color);
        valor.Dibujar(lienzo, LineaTexto.Izquierda(s.X, valor.Ancho, s.Alineacion), baseValor, pintura);
        return caja;
    }

    private static (CajaCapa Caja, LineaTexto? Etiqueta, LineaTexto Valor, float BaseEtiqueta, float BaseValor) DistribuirSensor(CapaSensor s, EntradaEscena e)
    {
        var texto = FormatoSensor.Texto(e.Lectura.Valor(s.SensorId), e.TipoDe(s.SensorId), s.Formato);
        var valor = new LineaTexto(texto, Tipografia.Cifras, s.Tamano);
        var etiqueta = string.IsNullOrEmpty(s.Etiqueta) ? null : new LineaTexto(s.Etiqueta, Tipografia.Etiquetas, s.TamanoEtiqueta);

        var baseEtiqueta = s.Y + (etiqueta?.AltoMayuscula ?? 0);
        var separacion = etiqueta is null ? 0 : s.TamanoEtiqueta * 0.5f;
        var baseValor = baseEtiqueta + separacion + valor.AltoMayuscula;
        var ancho = Math.Max(valor.Ancho, etiqueta?.Ancho ?? 0);
        var caja = new CajaCapa(s.Id, LineaTexto.Izquierda(s.X, ancho, s.Alineacion), s.Y, ancho, baseValor - s.Y);
        return (caja, etiqueta, valor, baseEtiqueta, baseValor);
    }

    private static CajaCapa DibujarTexto(SKCanvas lienzo, CapaTexto t)
    {
        if (string.IsNullOrEmpty(t.Texto))
        {
            return new CajaCapa(t.Id, t.X, t.Y, t.Tamano, t.Tamano);
        }
        using var linea = new LineaTexto(t.Texto, Tipografia.Etiquetas, t.Tamano);
        var caja = CajaTexto(t, linea);
        if (t.Velo)
        {
            DibujarVelo(lienzo, caja, t.Tamano);
        }
        using var pintura = new SKPaint { IsAntialias = true, Color = SKColor.Parse(t.Color) };
        linea.Dibujar(lienzo, caja.X, t.Y + linea.AltoMayuscula, pintura);
        return caja;
    }

    private static CajaCapa CajaTexto(CapaTexto t, LineaTexto linea) =>
        new(t.Id, LineaTexto.Izquierda(t.X, linea.Ancho, t.Alineacion), t.Y, linea.Ancho, linea.AltoMayuscula);

    private static CajaCapa DibujarMedidor(SKCanvas lienzo, CapaMedidor m, EntradaEscena e)
    {
        var dato = e.Lectura.Valor(m.SensorId);
        var fraccion = dato is { } v ? Math.Clamp((v - m.Minimo) / (m.Maximo - m.Minimo), 0, 1) : 0;
        using var pista = new SKPaint { IsAntialias = true, Color = SKColor.Parse(m.ColorPista), Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = m.Grosor };
        using var relleno = new SKPaint { IsAntialias = true, Color = SKColor.Parse(m.Color), Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = m.Grosor };
        var texto = FormatoSensor.Texto(dato, e.TipoDe(m.SensorId), m.Formato);

        if (m.Estilo == EstiloMedidor.Barra)
        {
            var y = m.Y + (m.Grosor / 2);
            lienzo.DrawLine(m.X + (m.Grosor / 2), y, m.X + m.Ancho - (m.Grosor / 2), y, pista);
            if (fraccion > 0)
            {
                var fin = m.X + (m.Grosor / 2) + ((m.Ancho - m.Grosor) * fraccion);
                lienzo.DrawLine(m.X + (m.Grosor / 2), y, fin, y, relleno);
            }
            var alto = m.Grosor;
            if (m.MostrarValor)
            {
                using var linea = new LineaTexto(texto, Tipografia.Cifras, Math.Max(14, m.Grosor * 1.6f));
                using var pintura = new SKPaint { IsAntialias = true, Color = SKColor.Parse(m.Color) };
                var baseY = m.Y + m.Grosor + (linea.Tamano * 0.25f) + linea.AltoMayuscula;
                linea.Dibujar(lienzo, m.X, baseY, pintura);
                alto = baseY - m.Y;
            }
            return new CajaCapa(m.Id, m.X, m.Y, m.Ancho, alto);
        }

        var radio = (m.Ancho - m.Grosor) / 2;
        var centro = new SKPoint(m.X + (m.Ancho / 2), m.Y + (m.Ancho / 2));
        var ovalo = new SKRect(centro.X - radio, centro.Y - radio, centro.X + radio, centro.Y + radio);
        lienzo.DrawArc(ovalo, 135, 270, false, pista);
        if (fraccion > 0)
        {
            lienzo.DrawArc(ovalo, 135, 270 * fraccion, false, relleno);
        }
        if (m.MostrarValor)
        {
            using var linea = new LineaTexto(texto, Tipografia.Cifras, m.Ancho * 0.24f);
            using var pintura = new SKPaint { IsAntialias = true, Color = SKColor.Parse(m.Color) };
            linea.Dibujar(lienzo, centro.X - (linea.Ancho / 2), centro.Y + (linea.AltoMayuscula / 2), pintura);
        }
        return new CajaCapa(m.Id, m.X, m.Y, m.Ancho, m.Ancho);
    }

    private static void DibujarVelo(SKCanvas lienzo, CajaCapa caja, float tamano)
    {
        var margen = Math.Max(6, tamano * 0.14f);
        var rect = new SKRect(caja.X - margen, caja.Y - margen, caja.X + caja.Ancho + margen, caja.Y + caja.Alto + margen);
        using var pintura = new SKPaint { IsAntialias = true, Color = ColorVelo };
        lienzo.DrawRoundRect(rect, margen * 0.8f, margen * 0.8f, pintura);
    }
}
