using SkiaSharp;
using Vitral.Core.Dispositivo;
using Vitral.Core.Medios;
using Vitral.Core.Render;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public class NovedadesTests
{
    private static readonly SensorInfo[] Nucleos =
    [
        new("/amdcpu/0/temperature/2", "Core (Tctl/Tdie)", "AMD Ryzen 5 5600T", TipoSensor.Temperatura),
        new("/amdcpu/0/clock/1", "Core #1", "AMD Ryzen 5 5600T", TipoSensor.Reloj),
        new("/amdcpu/0/clock/2", "Core #2", "AMD Ryzen 5 5600T", TipoSensor.Reloj),
        new("/amdcpu/0/clock/3", "Core #3", "AMD Ryzen 5 5600T", TipoSensor.Reloj),
        new("/amdcpu/0/load/0", "CPU Total", "AMD Ryzen 5 5600T", TipoSensor.Carga),
        new("/gpu-amd/0/temperature/0", "GPU Core", "AMD Radeon RX 9060 XT", TipoSensor.Temperatura),
    ];

    [Theory]
    [InlineData("Core #1", "Core")]
    [InlineData("CPU Core #12 Distance to TjMax", "CPU Core Distance to TjMax")]
    [InlineData("GPU Core", null)]
    [InlineData("#3", null)]
    [InlineData("", null)]
    public void Base_de_serie(string nombre, string? esperado) => Assert.Equal(esperado, Agregados.BaseDeSerie(nombre));

    [Fact]
    public void Una_serie_suma_promedio_y_maximo_al_final_de_su_grupo()
    {
        var a = Agregados.Agrupar(Nucleos);
        var relojes = a.Sensores.Where(s => s.Tipo == TipoSensor.Reloj).ToList();
        Assert.Equal(["Core #1", "Core #2", "Core #3", "Core", "Core"], relojes.Select(s => s.Nombre));
        Assert.Equal([null, null, null, Agregados.Promedio, Agregados.Maximo], relojes.Select(s => s.Agregado));
        Assert.Equal(Nucleos.Length + 2, a.Sensores.Count);
        Assert.Equal("AMD Ryzen 5 5600T", a.Sensores[0].Hardware);
    }

    [Fact]
    public void Calcula_promedio_y_maximo_con_los_valores_que_haya()
    {
        var a = Agregados.Agrupar(Nucleos);
        var promedio = a.Sensores.First(s => s.Agregado == Agregados.Promedio).Id;
        var maximo = a.Sensores.First(s => s.Agregado == Agregados.Maximo).Id;
        var valores = new Dictionary<string, float> { ["/amdcpu/0/clock/1"] = 4000, ["/amdcpu/0/clock/2"] = 4400 };
        Agregados.Completar(valores, a, [promedio, maximo]);
        Assert.Equal(4200, valores[promedio]);
        Assert.Equal(4400, valores[maximo]);
    }

    [Fact]
    public void Pedir_un_agregado_trae_a_sus_miembros()
    {
        var a = Agregados.Agrupar(Nucleos);
        var promedio = a.Sensores.First(s => s.Agregado == Agregados.Promedio).Id;
        Assert.Equal(4, a.Expandir([promedio]).Count());
    }

    [Fact]
    public void Un_marco_se_limita_al_lienzo_y_el_completo_se_quita()
    {
        var t = ValidadorTemas.Normalizar(new Tema { Fondo = new Fondo { Marco = new Marco(-50, 10, 9000, 80), RadioMarco = 999 } });
        Assert.Equal(new Marco(0, 10, 640, 80), t.Fondo.Marco);
        Assert.Equal(40, t.Fondo.RadioMarco);
        var completo = ValidadorTemas.Normalizar(new Tema { Fondo = new Fondo { Marco = new Marco(0, 0, 640, 480) } });
        Assert.Null(completo.Fondo.Marco);
    }

    [Fact]
    public void Fuentes_desconocidas_vuelven_a_la_de_siempre()
    {
        var t = ValidadorTemas.Normalizar(new Tema { Capas = [new CapaSensor { Fuente = "comic", FuenteEtiqueta = "rajdhani" }] });
        var s = (CapaSensor)t.Capas[0];
        Assert.Equal(Fuentes.PorDefecto, s.Fuente);
        Assert.Equal("rajdhani", s.FuenteEtiqueta);
    }

    [Fact]
    public void Todas_las_fuentes_cargan_y_dibujan()
    {
        foreach (var f in Fuentes.Todas)
        {
            var tema = new Tema { Capas = [new CapaSensor { Id = "s", Etiqueta = "CPU", Fuente = f.Id, FuenteEtiqueta = f.Id }] };
            using var c = new Compositor();
            var r = c.Componer(Entrada(tema), 640, 480, AjustesSalida.PorDefecto);
            Assert.True(r.Cajas[0].Ancho > 0, f.Nombre);
        }
    }

    [Theory]
    [InlineData(PosicionEtiqueta.Encima)]
    [InlineData(PosicionEtiqueta.Debajo)]
    [InlineData(PosicionEtiqueta.Lado)]
    public void La_etiqueta_cambia_la_forma_de_la_caja(PosicionEtiqueta posicion)
    {
        var capa = new CapaSensor { Id = "s", Etiqueta = "CPU", SensorId = "cpu", X = 300, Y = 100, Alineacion = Alineacion.Izquierda, PosicionEtiqueta = posicion };
        using var c = new Compositor();
        var caja = c.Componer(Entrada(new Tema { Capas = [capa] }), 640, 480, AjustesSalida.PorDefecto).Cajas[0];
        var soloCifra = c.Componer(Entrada(new Tema { Capas = [capa with { Etiqueta = string.Empty }] }), 640, 480, AjustesSalida.PorDefecto).Cajas[0];
        if (posicion == PosicionEtiqueta.Lado)
        {
            Assert.True(caja.Ancho > soloCifra.Ancho);
            Assert.Equal(soloCifra.Alto, caja.Alto, 1);
        }
        else
        {
            Assert.True(caja.Alto > soloCifra.Alto);
        }
    }

    [Fact]
    public void Con_marco_el_fondo_queda_dentro_y_afuera_el_color()
    {
        var carpeta = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vitral-marco-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(carpeta, "fondo.png"), Png(SKColors.Red));
            var tema = new Tema { Fondo = new Fondo { Tipo = TipoFondo.Imagen, Archivo = "fondo.png", Color = "#000000", Marco = new Marco(0, 0, 320, 480) } };
            using var fondo = Fondos.Cargar(tema.Fondo, carpeta);
            using var c = new Compositor();
            var r = c.Componer(new EntradaEscena(tema, fondo, 0, LecturaSensores.Vacia, _ => TipoSensor.Otro), 640, 480, AjustesSalida.PorDefecto);
            using var mapa = SKBitmap.Decode(r.Pantalla);
            Assert.True(mapa.GetPixel(100, 240).Red > 200);
            Assert.True(mapa.GetPixel(500, 240).Red < 30);
            Assert.Equal(Escena.IdFondo, r.Cajas[0].Id);
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    [Theory]
    [InlineData(Distribucion.Completa)]
    [InlineData(Distribucion.MarcoIzquierda)]
    [InlineData(Distribucion.MarcoArriba)]
    [InlineData(Distribucion.MarcoCentrado)]
    [InlineData(Distribucion.SoloDatos)]
    public void Cada_distribucion_crea_un_tema_valido_del_tamano_de_la_pantalla(Distribucion d)
    {
        var tema = ValidadorTemas.Normalizar(Distribuciones.Crear(d, "Nuevo", Nucleos, 1920, 462));
        Assert.Equal((1920, 462), (tema.Ancho, tema.Alto));
        Assert.Equal("Nuevo", tema.Nombre);
        Assert.NotEmpty(tema.Capas);
        Assert.Equal(d is Distribucion.MarcoIzquierda or Distribucion.MarcoArriba or Distribucion.MarcoCentrado, tema.Fondo.Marco is not null);
    }

    [Fact]
    public void Forzar_un_modelo_cambia_resolucion_y_conserva_el_detectado()
    {
        var buscador = new BuscadorFalso();
        using var p = new Pantalla(buscador) { ModeloForzado = "1-49" };
        Assert.True(p.Conectar());
        var info = p.Estado.Info!;
        Assert.Equal((1920, 462), (info.Ancho, info.Alto));
        Assert.True(info.Forzado);
        Assert.Equal("Mjolnir Vision Pro", info.ModeloDetectado);
    }

    [Fact]
    public void Una_clave_que_no_es_elegible_no_fuerza_nada()
    {
        Assert.Null(Catalogo.PorClave("32-0"));
        Assert.Null(Catalogo.PorClave("x"));
        Assert.Null(Catalogo.PorClave(null));
        Assert.All(Catalogo.Elegibles, c => Assert.NotNull(Catalogo.PorClave(c)));
    }

    private static EntradaEscena Entrada(Tema tema) =>
        new(tema, new FondoColor(), 0, new LecturaSensores(new Dictionary<string, float> { ["cpu"] = 52 }), _ => TipoSensor.Temperatura);

    private static byte[] Png(SKColor color)
    {
        using var mapa = new SKBitmap(64, 64);
        mapa.Erase(color);
        using var imagen = SKImage.FromBitmap(mapa);
        using var datos = imagen.Encode(SKEncodedImageFormat.Png, 100);
        return datos.ToArray();
    }
}
