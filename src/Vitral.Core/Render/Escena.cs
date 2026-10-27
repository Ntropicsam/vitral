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
        var cajas = new List<CajaCapa>(e.Tema.Capas.Count + 1);
        if (e.Tema.Fondo.Marco is { } marco)
        {
            cajas.Add(new CajaCapa(IdFondo, marco.X, marco.Y, marco.Ancho, marco.Alto));
        }
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
        var marco = tema.Fondo.Marco ?? new Marco(0, 0, tema.Ancho, tema.Alto);
        var destino = Ajustar(imagen.Width, imagen.Height, marco.Ancho, marco.Alto, tema.Fondo.Ajuste);
        destino.Offset(marco.X, marco.Y);
        lienzo.Save();
        var area = new SKRect(marco.X, marco.Y, marco.X + marco.Ancho, marco.Y + marco.Alto);
        if (tema.Fondo.RadioMarco > 0)
        {
            using var redondeado = new SKRoundRect(area, tema.Fondo.RadioMarco);
            lienzo.ClipRoundRect(redondeado, antialias: true);
        }
        else
        {
            lienzo.ClipRect(area);
        }
        lienzo.DrawImage(imagen, destino, Muestreo);
        lienzo.Restore();
    }

    public const string IdFondo = "__fondo";

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
                    using var linea = new LineaTexto(t.Texto, Tipografia.Etiquetas(t.Fuente), t.Tamano);
                    return CajaTexto(t, linea);
                }
            default:
                return null;
        }
    }

    private static CajaCapa DibujarSensor(SKCanvas lienzo, CapaSensor s, EntradaEscena e)
    {
        var d = DistribuirSensor(s, e);
        using var valorLiberable = d.Valor;
        using var etiquetaLiberable = d.Etiqueta;
        if (s.Velo)
        {
            DibujarVelo(lienzo, d.Caja, s.Tamano);
        }
        using var pintura = new SKPaint { IsAntialias = true };
        if (d.Etiqueta is not null)
        {
            pintura.Color = SKColor.Parse(s.ColorEtiqueta);
            d.Etiqueta.Dibujar(lienzo, d.XEtiqueta, d.BaseEtiqueta, pintura);
        }
        pintura.Color = SKColor.Parse(s.Color);
        d.Valor.Dibujar(lienzo, d.XValor, d.BaseValor, pintura);
        return d.Caja;
    }

    private sealed record DistribucionSensor(CajaCapa Caja, LineaTexto? Etiqueta, LineaTexto Valor, float XEtiqueta, float BaseEtiqueta, float XValor, float BaseValor);

    private static DistribucionSensor DistribuirSensor(CapaSensor s, EntradaEscena e)
    {
        var texto = FormatoSensor.Texto(e.Lectura.Valor(s.SensorId), e.TipoDe(s.SensorId), s.Formato);
        var valor = new LineaTexto(texto, Tipografia.Cifras(s.Fuente), s.Tamano);
        var etiqueta = string.IsNullOrEmpty(s.Etiqueta) ? null : new LineaTexto(s.Etiqueta, Tipografia.Etiquetas(s.FuenteEtiqueta), s.TamanoEtiqueta);
        if (etiqueta is null)
        {
            var solo = LineaTexto.Izquierda(s.X, valor.Ancho, s.Alineacion);
            return new(new CajaCapa(s.Id, solo, s.Y, valor.Ancho, valor.AltoMayuscula), null, valor, 0, 0, solo, s.Y + valor.AltoMayuscula);
        }

        var separacion = s.TamanoEtiqueta * 0.5f;
        if (s.PosicionEtiqueta == PosicionEtiqueta.Lado)
        {
            var hueco = s.TamanoEtiqueta * 0.6f;
            var total = etiqueta.Ancho + hueco + valor.Ancho;
            var izquierda = LineaTexto.Izquierda(s.X, total, s.Alineacion);
            var baseLinea = s.Y + valor.AltoMayuscula;
            var caja = new CajaCapa(s.Id, izquierda, s.Y, total, valor.AltoMayuscula);
            return new(caja, etiqueta, valor, izquierda, baseLinea, izquierda + etiqueta.Ancho + hueco, baseLinea);
        }

        var ancho = Math.Max(valor.Ancho, etiqueta.Ancho);
        var xValor = LineaTexto.Izquierda(s.X, valor.Ancho, s.Alineacion);
        var xEtiqueta = LineaTexto.Izquierda(s.X, etiqueta.Ancho, s.Alineacion);
        float baseEtiqueta, baseValor, fondo;
        if (s.PosicionEtiqueta == PosicionEtiqueta.Debajo)
        {
            baseValor = s.Y + valor.AltoMayuscula;
            baseEtiqueta = baseValor + separacion + etiqueta.AltoMayuscula;
            fondo = baseEtiqueta;
        }
        else
        {
            baseEtiqueta = s.Y + etiqueta.AltoMayuscula;
            baseValor = baseEtiqueta + separacion + valor.AltoMayuscula;
            fondo = baseValor;
        }
        var cajaColumna = new CajaCapa(s.Id, LineaTexto.Izquierda(s.X, ancho, s.Alineacion), s.Y, ancho, fondo - s.Y);
        return new(cajaColumna, etiqueta, valor, xEtiqueta, baseEtiqueta, xValor, baseValor);
    }

    private static CajaCapa DibujarTexto(SKCanvas lienzo, CapaTexto t)
    {
        if (string.IsNullOrEmpty(t.Texto))
        {
            return new CajaCapa(t.Id, t.X, t.Y, t.Tamano, t.Tamano);
        }
        using var linea = new LineaTexto(t.Texto, Tipografia.Etiquetas(t.Fuente), t.Tamano);
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
                using var linea = new LineaTexto(texto, Tipografia.Cifras(m.Fuente), Math.Max(14, m.Grosor * 1.6f));
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
            using var linea = new LineaTexto(texto, Tipografia.Cifras(m.Fuente), m.Ancho * 0.24f);
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
