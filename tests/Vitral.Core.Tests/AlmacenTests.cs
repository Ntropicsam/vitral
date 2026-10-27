using SkiaSharp;
using Vitral.Core.Medios;
using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public sealed class AlmacenTests : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), "vitral-pruebas-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    [Fact]
    public void Crear_listar_guardar_y_leer()
    {
        var almacen = new AlmacenTemas(_raiz);
        Assert.Empty(almacen.Listar());
        var creado = almacen.Crear(new Tema { Nombre = "Uno" });
        almacen.Guardar(creado.Id, creado.Tema with { Nombre = "Uno bis" });
        var lista = almacen.Listar();
        Assert.Single(lista);
        Assert.Equal("Uno bis", lista[0].Tema.Nombre);
        Assert.False(File.Exists(Path.Combine(creado.Carpeta, AlmacenTemas.ArchivoTema + ".tmp")));
    }

    [Fact]
    public void Un_tema_danado_no_tumba_la_lista()
    {
        var almacen = new AlmacenTemas(_raiz);
        almacen.Crear(new Tema { Nombre = "Bueno" });
        var malo = Directory.CreateDirectory(Path.Combine(_raiz, "malo"));
        File.WriteAllText(Path.Combine(malo.FullName, AlmacenTemas.ArchivoTema), "{roto");
        Assert.Single(almacen.Listar());
    }

    [Theory]
    [InlineData("..")]
    [InlineData(@"..\otro")]
    [InlineData("a/b")]
    [InlineData("")]
    [InlineData(null)]
    public void Ids_con_rutas_se_rechazan(string? id)
    {
        var almacen = new AlmacenTemas(_raiz);
        Assert.False(AlmacenTemas.EsIdValido(id));
        Assert.Null(almacen.Intentar(id!));
    }

    [Fact]
    public void Guardar_un_tema_que_no_existe_lanza()
    {
        var almacen = new AlmacenTemas(_raiz);
        Assert.Throws<KeyNotFoundException>(() => almacen.Guardar("noexiste", new Tema()));
    }

    [Fact]
    public void Duplicar_copia_tambien_el_fondo()
    {
        var almacen = new AlmacenTemas(_raiz);
        var creado = almacen.Crear(new Tema { Nombre = "Base" });
        var medio = Importador.GuardarAnimacion(new Animacion([MediosTests.Jpeg(SKColors.Red)], [100]));
        var conFondo = almacen.Guardar(creado.Id, almacen.ConFondo(creado.Id, creado.Tema, medio));
        var copia = almacen.Duplicar(conFondo.Id);
        Assert.Equal("Base (copia)", copia.Tema.Nombre);
        Assert.True(File.Exists(Path.Combine(copia.Carpeta, "fondo.cuadros")));
    }

    [Fact]
    public void Cambiar_de_animacion_a_imagen_borra_el_fondo_viejo()
    {
        var almacen = new AlmacenTemas(_raiz);
        var creado = almacen.Crear(new Tema());
        var animacion = Importador.GuardarAnimacion(new Animacion([MediosTests.Jpeg(SKColors.Red)], [100]));
        var tema = almacen.ConFondo(creado.Id, creado.Tema, animacion);
        tema = almacen.ConFondo(creado.Id, tema, new MedioImportado(TipoMedio.Imagen, "fondo.png", [1, 2, 3]));
        Assert.Equal(TipoFondo.Imagen, tema.Fondo.Tipo);
        Assert.False(File.Exists(Path.Combine(creado.Carpeta, "fondo.cuadros")));
    }

    [Fact]
    public void Importar_un_theme_zt_de_TRCC_deja_una_animacion()
    {
        var jpeg = MediosTests.Jpeg(SKColors.Red);
        using var memoria = new MemoryStream();
        using (var w = new BinaryWriter(memoria, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            w.Write((byte)0xDC);
            w.Write(1);
            w.Write(41);
            w.Write(jpeg.Length);
            w.Write(jpeg);
        }
        var almacen = new AlmacenTemas(_raiz);
        var creado = almacen.Crear(new Tema());
        var tema = almacen.ImportarFondo(creado.Id, creado.Tema, "Theme.zt", memoria.ToArray());
        Assert.Equal(TipoFondo.Animacion, tema.Fondo.Tipo);
    }

    [Fact]
    public void Ajustes_danados_vuelven_a_los_de_fabrica()
    {
        Directory.CreateDirectory(_raiz);
        var ruta = Path.Combine(_raiz, "ajustes.json");
        File.WriteAllText(ruta, "no es json");
        var almacen = new AlmacenAjustes(ruta);
        Assert.Equal(100, almacen.Leer().Brillo);
        almacen.Guardar(new Ajustes { Brillo = 150, Rotacion = 450, TemaActivo = "../x" });
        var leidos = almacen.Leer();
        Assert.Equal((100, 90), (leidos.Brillo, leidos.Rotacion));
        Assert.Null(leidos.TemaActivo);
    }
}
