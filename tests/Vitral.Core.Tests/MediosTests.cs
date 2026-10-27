using SkiaSharp;
using Vitral.Core.Medios;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public class MediosTests
{
    internal static byte[] Jpeg(SKColor color, int ancho = 64, int alto = 48)
    {
        using var mapa = new SKBitmap(ancho, alto);
        mapa.Erase(color);
        using var imagen = SKImage.FromBitmap(mapa);
        using var datos = imagen.Encode(SKEncodedImageFormat.Jpeg, 90);
        return datos.ToArray();
    }

    [Fact]
    public void Archivo_de_cuadros_ida_y_vuelta()
    {
        var animacion = new Animacion([Jpeg(SKColors.Red), Jpeg(SKColors.Blue)], [41, 42]);
        using var memoria = new MemoryStream();
        ArchivoCuadros.Escribir(animacion, memoria);
        var leida = ArchivoCuadros.Leer(memoria.ToArray());
        Assert.Equal(2, leida.Cantidad);
        Assert.Equal([41, 42], leida.DuracionesMs);
        Assert.Equal(animacion.Cuadros[1], leida.Cuadros[1]);
    }

    [Fact]
    public void Archivo_cortado_o_ajeno_lanza()
    {
        var animacion = new Animacion([Jpeg(SKColors.Red)], [100]);
        using var memoria = new MemoryStream();
        ArchivoCuadros.Escribir(animacion, memoria);
        var bytes = memoria.ToArray();
        Assert.Throws<FormatoDeMedioException>(() => ArchivoCuadros.Leer(bytes.AsSpan(0, bytes.Length - 10)));
        Assert.Throws<FormatoDeMedioException>(() => ArchivoCuadros.Leer("hola"u8));
        Assert.Throws<FormatoDeMedioException>(() => ArchivoCuadros.Leer([]));
    }

    [Fact]
    public void Cantidad_de_cuadros_absurda_lanza_sin_reservar_memoria()
    {
        var falso = new byte[16];
        "VTRC"u8.CopyTo(falso);
        BitConverter.GetBytes(1).CopyTo(falso, 4);
        BitConverter.GetBytes(int.MaxValue).CopyTo(falso, 8);
        Assert.Throws<FormatoDeMedioException>(() => ArchivoCuadros.Leer(falso));
    }

    [Fact]
    public void Theme_zt_de_TRCC_pasa_marcas_acumuladas_a_duraciones()
    {
        var a = Jpeg(SKColors.Red);
        var b = Jpeg(SKColors.Blue);
        using var memoria = new MemoryStream();
        using (var w = new BinaryWriter(memoria, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            w.Write((byte)0xDC);
            w.Write(2);
            w.Write(41);
            w.Write(83);
            w.Write(a.Length);
            w.Write(a);
            w.Write(b.Length);
            w.Write(b);
        }
        var animacion = LectorZt.Leer(memoria.ToArray());
        Assert.Equal([41, 42], animacion.DuracionesMs);
        Assert.Equal(b, animacion.Cuadros[1]);
    }

    [Theory]
    [InlineData(0, Animacion.DuracionPorDefectoMs)]
    [InlineData(10, Animacion.DuracionPorDefectoMs)]
    [InlineData(-5, Animacion.DuracionPorDefectoMs)]
    [InlineData(40, 40)]
    [InlineData(999_999, 60_000)]
    public void Duraciones_raras_se_normalizan(int entrada, int esperada) =>
        Assert.Equal(esperada, Animacion.NormalizarDuracion(entrada));

    [Fact]
    public void Animacion_vacia_o_descuadrada_lanza()
    {
        Assert.Throws<ArgumentException>(() => new Animacion([], []));
        Assert.Throws<ArgumentException>(() => new Animacion([Jpeg(SKColors.Red)], [1, 2]));
    }

    [Fact]
    public void Importa_un_png_como_imagen()
    {
        using var mapa = new SKBitmap(100, 50);
        mapa.Erase(SKColors.Green);
        using var imagen = SKImage.FromBitmap(mapa);
        using var png = imagen.Encode(SKEncodedImageFormat.Png, 100);
        var medio = Importador.Importar("foto.png", png.ToArray());
        Assert.Equal(TipoMedio.Imagen, medio.Tipo);
        Assert.Equal("fondo.png", medio.NombreArchivo);
    }

    [Fact]
    public void Imagen_de_lado_enorme_se_rechaza_antes_de_decodificar()
    {
        using var mapa = new SKBitmap(Medidas.LadoMaximoEntrada + 1, 4);
        mapa.Erase(SKColors.Green);
        using var imagen = SKImage.FromBitmap(mapa);
        using var png = imagen.Encode(SKEncodedImageFormat.Png, 100);
        Assert.Throws<FormatoDeMedioException>(() => Importador.Importar("tira.png", png.ToArray()));
    }

    [Fact]
    public void Animacion_con_cuadros_enormes_se_rechaza()
    {
        var enorme = Jpeg(SKColors.Red, 2100, 2100);
        Assert.Throws<FormatoDeMedioException>(() => Importador.GuardarAnimacion(new Animacion([enorme], [100])));
    }

    [Fact]
    public void Animacion_con_cuadro_que_no_es_imagen_se_rechaza() =>
        Assert.Throws<FormatoDeMedioException>(() => Importador.GuardarAnimacion(new Animacion([[1, 2, 3]], [100])));

    [Fact]
    public void Importar_vacio_o_basura_lanza()
    {
        Assert.Throws<FormatoDeMedioException>(() => Importador.Importar("x.png", []));
        Assert.Throws<FormatoDeMedioException>(() => Importador.Importar("x.png", "no es imagen"u8.ToArray()));
    }

    [Fact]
    public void Fondo_sin_archivo_queda_de_color()
    {
        using var fondo = Fondos.Cargar(new Fondo { Tipo = TipoFondo.Imagen, Archivo = "no-existe.png" }, Path.GetTempPath());
        Assert.IsType<FondoColor>(fondo);
        Assert.Null(fondo.Imagen(0));
    }

    [Fact]
    public void Fondo_animado_da_vueltas()
    {
        using var fondo = new FondoAnimacion(new Animacion([Jpeg(SKColors.Red), Jpeg(SKColors.Blue)], [50, 60]));
        Assert.Equal(60, fondo.DuracionMs(3));
        Assert.NotNull(fondo.Imagen(-1));
    }

    [Theory]
    [InlineData(52.4f, TipoSensor.Temperatura, FormatoValor.Corto, "52°")]
    [InlineData(52.46f, TipoSensor.Temperatura, FormatoValor.Detallado, "52,5 °C")]
    [InlineData(-0.2f, TipoSensor.Temperatura, FormatoValor.Corto, "0°")]
    [InlineData(18.4f, TipoSensor.Carga, FormatoValor.Corto, "18 %")]
    [InlineData(4250f, TipoSensor.Reloj, FormatoValor.Corto, "4,3 GHz")]
    [InlineData(850f, TipoSensor.Reloj, FormatoValor.Corto, "850 MHz")]
    [InlineData(1850f, TipoSensor.Ventilador, FormatoValor.Corto, "1.850 RPM")]
    [InlineData(11.24f, TipoSensor.Memoria, FormatoValor.Corto, "11,2 GB")]
    [InlineData(65.4f, TipoSensor.Potencia, FormatoValor.Corto, "65 W")]
    [InlineData(1.2f, TipoSensor.Voltaje, FormatoValor.Corto, "1,20 V")]
    public void Formato_de_sensores_en_espanol(float valor, TipoSensor tipo, FormatoValor formato, string esperado) =>
        Assert.Equal(esperado, FormatoSensor.Texto(valor, tipo, formato));

    [Theory]
    [InlineData(null)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Sin_dato_se_ve_raya(float? valor) =>
        Assert.Equal(FormatoSensor.SinDato, FormatoSensor.Texto(valor, TipoSensor.Temperatura, FormatoValor.Corto));
}
