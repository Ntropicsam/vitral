using SkiaSharp;
using Vitral.Core.Medios;
using Vitral.Core.Render;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public class RenderTests
{
    private static readonly LecturaSensores Lectura = new(new Dictionary<string, float> { ["cpu"] = 52, ["gpu"] = 61 });

    private static EntradaEscena Entrada(Tema tema, IFondoCargado? fondo = null) =>
        new(tema, fondo ?? new FondoColor(), 0, Lectura, _ => TipoSensor.Temperatura);

    private static SKBitmap Decodificar(byte[] jpeg) => SKBitmap.Decode(jpeg);

    [Fact]
    public void Compone_un_jpeg_del_tamano_del_panel_con_una_caja_por_capa_visible()
    {
        var tema = TemasIniciales.Basico("x", "cpu", "gpu") with
        {
            Capas = [.. TemasIniciales.Basico("x", "cpu", "gpu").Capas, new CapaTexto { Visible = false }],
        };
        using var c = new Compositor();
        var r = c.Componer(Entrada(tema), 640, 480, AjustesSalida.PorDefecto);
        using var mapa = Decodificar(r.Pantalla);
        Assert.Equal((640, 480), (mapa.Width, mapa.Height));
        Assert.Equal(2, r.Cajas.Count);
        Assert.Same(r.Pantalla, r.Vista);
    }

    [Fact]
    public void La_cifra_alineada_a_la_derecha_termina_en_su_X()
    {
        var tema = TemasIniciales.Basico("x", "cpu", "gpu");
        using var c = new Compositor();
        var caja = c.Componer(Entrada(tema), 640, 480, AjustesSalida.PorDefecto).Cajas[0];
        Assert.Equal(600, caja.X + caja.Ancho, 1);
        Assert.Equal(48, caja.Y);
        Assert.True(caja.Alto > 60);
    }

    [Fact]
    public void Las_cifras_tienen_ancho_fijo()
    {
        var unos = new Tema { Capas = [new CapaTexto { Id = "a", Texto = "11" }] };
        var ochos = new Tema { Capas = [new CapaTexto { Id = "a", Texto = "88" }] };
        using var c = new Compositor();
        var a = c.Componer(Entrada(unos), 640, 480, AjustesSalida.PorDefecto).Cajas[0];
        var b = c.Componer(Entrada(ochos), 640, 480, AjustesSalida.PorDefecto).Cajas[0];
        Assert.Equal(a.Ancho, b.Ancho, 2);
    }

    [Fact]
    public void Brillo_cero_deja_todo_negro()
    {
        var tema = new Tema { Fondo = new Fondo { Color = "#ffffff" } };
        using var c = new Compositor();
        var r = c.Componer(Entrada(tema), 640, 480, new AjustesSalida(0, 0));
        using var mapa = Decodificar(r.Pantalla);
        var p = mapa.GetPixel(320, 240);
        Assert.True(p.Red < 8 && p.Green < 8 && p.Blue < 8);
    }

    [Fact]
    public void Rotar_180_lleva_la_esquina_arriba_izquierda_abajo_derecha()
    {
        var tema = new Tema { Fondo = new Fondo { Color = "#000000" }, Capas = [new CapaMedidor { X = 0, Y = 0, Ancho = 120, Grosor = 40, Estilo = EstiloMedidor.Barra, SensorId = "cpu", Minimo = 0, Maximo = 1, Color = "#ffffff", MostrarValor = false }] };
        using var c = new Compositor();
        var r = c.Componer(Entrada(tema), 640, 480, new AjustesSalida(100, 180));
        Assert.NotSame(r.Pantalla, r.Vista);
        using var panel = Decodificar(r.Pantalla);
        Assert.True(panel.GetPixel(580, 460).Red > 200);
        Assert.True(panel.GetPixel(40, 20).Red < 40);
    }

    [Fact]
    public void Rotar_90_cabe_en_el_panel()
    {
        using var c = new Compositor();
        var r = c.Componer(Entrada(new Tema()), 640, 480, new AjustesSalida(100, 90));
        using var panel = Decodificar(r.Pantalla);
        using var vista = Decodificar(r.Vista);
        Assert.Equal((640, 480), (panel.Width, panel.Height));
        Assert.Equal((640, 480), (vista.Width, vista.Height));
    }

    [Theory]
    [InlineData(-90, 270)]
    [InlineData(450, 90)]
    [InlineData(45, 0)]
    public void Rotacion_se_normaliza(int entrada, int esperada) =>
        Assert.Equal(esperada, AjustesSalida.NormalizarRotacion(entrada));

    [Theory]
    [InlineData(AjusteFondo.Cubrir, -64, 0, 704, 480)]
    [InlineData(AjusteFondo.Contener, 0, 40, 640, 440)]
    [InlineData(AjusteFondo.Estirar, 0, 0, 640, 480)]
    public void Ajuste_del_fondo(AjusteFondo ajuste, float x, float y, float derecha, float abajo)
    {
        var r = Escena.Ajustar(1920, 1200, 640, 480, ajuste);
        Assert.Equal((x, y), (r.Left, r.Top));
        Assert.Equal((derecha, abajo), (r.Right, r.Bottom));
    }

    [Fact]
    public void Cifra_clara_sobre_fondo_negro_es_legible()
    {
        var tema = TemasIniciales.Basico("x", "cpu", "gpu") with { Fondo = new Fondo { Color = "#000000" } };
        var r = Legibilidad.Medir(Entrada(tema), tema.Capas[0].Id)!;
        Assert.True(r.Suficiente);
        Assert.True(r.Contraste > 15);
    }

    [Fact]
    public void Cifra_clara_sobre_fondo_claro_no_alcanza_y_el_velo_la_salva()
    {
        var claro = TemasIniciales.Basico("x", "cpu", "gpu") with { Fondo = new Fondo { Color = "#d8d0c0" } };
        var sinVelo = Legibilidad.Medir(Entrada(claro), claro.Capas[0].Id)!;
        Assert.False(sinVelo.Suficiente);

        var conVelo = claro with { Capas = [((CapaSensor)claro.Capas[0]) with { Velo = true }] };
        var r = Legibilidad.Medir(Entrada(conVelo), conVelo.Capas[0].Id)!;
        Assert.True(r.Contraste > sinVelo.Contraste);
        Assert.True(r.Suficiente);
    }

    [Fact]
    public void Legibilidad_de_capa_inexistente_o_medidor_es_nula()
    {
        var tema = new Tema { Capas = [new CapaMedidor { Id = "m" }] };
        Assert.Null(Legibilidad.Medir(Entrada(tema), "m"));
        Assert.Null(Legibilidad.Medir(Entrada(tema), "no-existe"));
    }

    [Fact]
    public void Contraste_de_WCAG()
    {
        Assert.Equal(21, Legibilidad.Contraste(1, 0), 1);
        Assert.Equal(1, Legibilidad.Contraste(0.5, 0.5), 3);
    }

    [Fact]
    public void Sensor_sin_dato_se_dibuja_con_raya()
    {
        var tema = new Tema { Capas = [new CapaSensor { Id = "s", SensorId = "no-existe" }] };
        using var c = new Compositor();
        var r = c.Componer(Entrada(tema), 640, 480, AjustesSalida.PorDefecto);
        Assert.Single(r.Cajas);
    }
}
