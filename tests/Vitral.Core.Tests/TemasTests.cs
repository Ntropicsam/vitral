using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public class TemasTests
{
    [Fact]
    public void Ida_y_vuelta_conserva_las_capas_y_su_tipo()
    {
        var tema = new Tema
        {
            Nombre = "Neón",
            Fondo = new Fondo { Tipo = TipoFondo.Animacion, Archivo = "fondo.cuadros" },
            Capas =
            [
                new CapaSensor { Id = "a", SensorId = "/amdcpu/0/temperature/2", Etiqueta = "CPU", X = 600, Y = 48, Velo = true },
                new CapaTexto { Id = "b", Texto = "Hola", Alineacion = Alineacion.Centro },
                new CapaMedidor { Id = "c", Estilo = EstiloMedidor.Barra, Maximo = 90 },
            ],
        };
        var leido = SerializadorTemas.Leer(SerializadorTemas.Escribir(tema));
        Assert.Equal("Neón", leido.Nombre);
        Assert.Equal(TipoFondo.Animacion, leido.Fondo.Tipo);
        Assert.IsType<CapaSensor>(leido.Capas[0]);
        Assert.True(((CapaSensor)leido.Capas[0]).Velo);
        Assert.Equal(Alineacion.Centro, Assert.IsType<CapaTexto>(leido.Capas[1]).Alineacion);
        Assert.Equal(90, Assert.IsType<CapaMedidor>(leido.Capas[2]).Maximo);
    }

    [Fact]
    public void El_json_usa_nombres_en_minuscula_y_enums_como_texto()
    {
        var json = SerializadorTemas.Escribir(new Tema { Capas = [new CapaTexto()] });
        Assert.Contains("\"tipo\": \"texto\"", json, StringComparison.Ordinal);
        Assert.Contains("\"alineacion\": \"Izquierda\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no es json")]
    [InlineData("null")]
    [InlineData("{\"capas\":[{\"tipo\":\"desconocida\"}]}")]
    public void Json_que_no_sirve_lanza_TemaInvalido(string json) =>
        Assert.Throws<TemaInvalidoException>(() => SerializadorTemas.Leer(json));

    [Fact]
    public void Tema_de_una_version_futura_lanza() =>
        Assert.Throws<TemaInvalidoException>(() => SerializadorTemas.Leer("{\"version\": 99}"));

    [Fact]
    public void Campos_que_faltan_toman_su_valor_por_defecto()
    {
        var tema = SerializadorTemas.Leer("{}");
        Assert.Equal((640, 480), (tema.Ancho, tema.Alto));
        Assert.Empty(tema.Capas);
        Assert.Equal(TipoFondo.Color, tema.Fondo.Tipo);
    }

    [Fact]
    public void El_tipo_de_la_capa_puede_llegar_en_cualquier_orden()
    {
        var tema = SerializadorTemas.Leer("{\"capas\":[{\"id\":\"a\",\"texto\":\"Hola\",\"tipo\":\"texto\"}]}");
        Assert.Equal("Hola", Assert.IsType<CapaTexto>(tema.Capas[0]).Texto);
    }

    [Fact]
    public void Capa_sin_visible_ni_tamano_queda_visible_y_con_su_tamano()
    {
        var tema = SerializadorTemas.Leer("{\"capas\":[{\"tipo\":\"sensor\",\"sensorId\":\"cpu\"}]}");
        var s = Assert.IsType<CapaSensor>(tema.Capas[0]);
        Assert.True(s.Visible);
        Assert.Equal(88, s.Tamano);
    }

    [Theory]
    [InlineData(@"..\..\Windows\win.ini")]
    [InlineData("C:\\x.png")]
    [InlineData("carpeta/fondo.png")]
    [InlineData(".oculto")]
    [InlineData("")]
    [InlineData(null)]
    public void Archivo_de_fondo_con_ruta_se_descarta(string? archivo)
    {
        var tema = ValidadorTemas.Normalizar(new Tema { Fondo = new Fondo { Tipo = TipoFondo.Imagen, Archivo = archivo } });
        Assert.Null(tema.Fondo.Archivo);
        Assert.Equal(TipoFondo.Color, tema.Fondo.Tipo);
    }

    [Fact]
    public void Colores_invalidos_toman_el_de_respaldo()
    {
        var tema = ValidadorTemas.Normalizar(new Tema
        {
            Fondo = new Fondo { Color = "rojo" },
            Capas = [new CapaSensor { Color = "#GGHHII", ColorEtiqueta = "#ABCDEF" }],
        });
        Assert.Equal("#0d0f14", tema.Fondo.Color);
        var s = (CapaSensor)tema.Capas[0];
        Assert.Equal("#f4efe6", s.Color);
        Assert.Equal("#abcdef", s.ColorEtiqueta);
    }

    [Fact]
    public void Ids_repetidos_o_vacios_se_reemplazan()
    {
        var tema = ValidadorTemas.Normalizar(new Tema
        {
            Capas = [new CapaTexto { Id = "x" }, new CapaTexto { Id = "x" }, new CapaTexto { Id = "" }],
        });
        Assert.Equal(3, tema.Capas.Select(c => c.Id).Distinct().Count());
        Assert.Equal("x", tema.Capas[0].Id);
    }

    [Fact]
    public void Numeros_fuera_de_rango_o_no_finitos_se_limitan()
    {
        var tema = ValidadorTemas.Normalizar(new Tema
        {
            Ancho = 10,
            Capas = [new CapaSensor { X = float.NaN, Tamano = 9999 }, new CapaMedidor { Minimo = 80, Maximo = 20 }],
        });
        Assert.Equal(64, tema.Ancho);
        var s = (CapaSensor)tema.Capas[0];
        Assert.True(float.IsFinite(s.X));
        Assert.Equal(400, s.Tamano);
        var m = (CapaMedidor)tema.Capas[1];
        Assert.Equal((20f, 80f), (m.Minimo, m.Maximo));
    }

    [Fact]
    public void Minimo_igual_al_maximo_abre_un_rango()
    {
        var m = (CapaMedidor)ValidadorTemas.Normalizar(new Tema { Capas = [new CapaMedidor { Minimo = 50, Maximo = 50 }] }).Capas[0];
        Assert.True(m.Maximo > m.Minimo);
    }

    [Fact]
    public void Nombres_vacios_y_largos()
    {
        var tema = ValidadorTemas.Normalizar(new Tema { Nombre = "  ", Capas = [new CapaTexto { Texto = new string('a', 500) }] });
        Assert.Equal("Tema sin nombre", tema.Nombre);
        Assert.Equal(ValidadorTemas.LargoMaximoTexto, ((CapaTexto)tema.Capas[0]).Texto.Length);
    }

    [Fact]
    public void Capas_de_mas_se_cortan()
    {
        var capas = Enumerable.Range(0, 100).Select(_ => (Capa)new CapaTexto()).ToList();
        Assert.Equal(ValidadorTemas.MaximoCapas, ValidadorTemas.Normalizar(new Tema { Capas = capas }).Capas.Count);
    }
}
