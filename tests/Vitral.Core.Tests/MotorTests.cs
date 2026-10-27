using SkiaSharp;
using Vitral.Core.Dispositivo;
using Vitral.Core.Ejecucion;
using Vitral.Core.Medios;
using Vitral.Core.Render;
using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public sealed class MotorTests : IDisposable
{
    private readonly RelojFalso _reloj = new();
    private readonly BuscadorFalso _buscador = new();
    private readonly SensoresFalsos _sensores = new();
    private readonly Motor _motor;

    public MotorTests()
    {
        _motor = new Motor(new Pantalla(_buscador), _sensores, _reloj);
        _motor.UsarTema(TemasIniciales.Basico("x", "cpu", "gpu"), Path.GetTempPath());
    }

    public void Dispose() => _motor.Dispose();

    [Fact]
    public void Primer_paso_conecta_dibuja_y_envia()
    {
        _motor.Paso();
        Assert.Equal(1, _buscador.Puerto.Cuadros);
        Assert.Equal(1, _sensores.Lecturas);
    }

    [Fact]
    public void Si_nada_cambia_reenvia_cada_segundo_y_no_antes()
    {
        _motor.Paso();
        _reloj.Milisegundos = 500;
        _motor.Paso();
        Assert.Equal(1, _buscador.Puerto.Cuadros);
        _reloj.Milisegundos = 1000;
        _motor.Paso();
        Assert.Equal(2, _buscador.Puerto.Cuadros);
    }

    [Fact]
    public void Un_valor_nuevo_del_sensor_se_dibuja_al_leerlo()
    {
        var cuadros = new List<CuadroRenderizado>();
        _motor.VistaPrevia = true;
        _motor.CuadroListo += cuadros.Add;
        _motor.Paso();
        _sensores.Valores["cpu"] = 70;
        _reloj.Milisegundos = 1000;
        _motor.Paso();
        Assert.Equal(2, cuadros.Count);
        Assert.NotEqual(cuadros[0].Vista, cuadros[1].Vista);
    }

    [Fact]
    public void Sin_vista_previa_no_avisa_cuadros()
    {
        var avisos = 0;
        _motor.CuadroListo += _ => avisos++;
        _motor.Paso();
        Assert.Equal(0, avisos);
    }

    [Fact]
    public void Sin_pantalla_reintenta_cada_dos_segundos_y_sigue_dibujando_la_vista()
    {
        _buscador.Falla = MotivoSinPanel.NoConectado;
        var cuadros = 0;
        _motor.VistaPrevia = true;
        _motor.CuadroListo += _ => cuadros++;
        _motor.Paso();
        _reloj.Milisegundos = 1500;
        _motor.Paso();
        Assert.Equal(1, _buscador.Aperturas);
        _reloj.Milisegundos = 2000;
        _buscador.Falla = null;
        _motor.Paso();
        Assert.Equal(2, _buscador.Aperturas);
        Assert.Equal(1, _buscador.Puerto.Cuadros);
        Assert.Equal(2, cuadros);
    }

    [Fact]
    public void Sin_pantalla_ni_editor_no_dibuja_ni_lee_sensores()
    {
        _buscador.Falla = MotivoSinPanel.NoConectado;
        var cuadros = 0;
        _motor.CuadroListo += _ => cuadros++;
        _motor.Paso();
        _reloj.Milisegundos = 1000;
        _motor.Paso();
        Assert.Equal(0, cuadros);
        Assert.Equal(0, _sensores.Lecturas);
    }

    [Fact]
    public void Solo_pide_los_sensores_del_tema_y_todos_con_el_editor_abierto()
    {
        _motor.Paso();
        Assert.Equal(["cpu", "gpu"], _sensores.UltimoInteres!.Order());
        _motor.VistaPrevia = true;
        _motor.Paso();
        Assert.Null(_sensores.UltimoInteres);
    }

    [Fact]
    public void Un_tema_sin_sensores_no_los_lee()
    {
        _motor.UsarTema(new Tema(), Path.GetTempPath());
        _motor.Paso();
        Assert.Equal(0, _sensores.Lecturas);
    }

    [Fact]
    public void Si_se_pierde_la_pantalla_se_reconecta()
    {
        _motor.Paso();
        _buscador.Puerto.FallarEscritura = true;
        _reloj.Milisegundos = 1000;
        _motor.Paso();
        _reloj.Milisegundos = 1600;
        _motor.Paso();
        Assert.Equal(2, _buscador.Aperturas);
        Assert.Equal(1, _buscador.Puerto.Cuadros);
    }

    [Fact]
    public void La_animacion_avanza_segun_la_duracion_de_cada_cuadro()
    {
        var carpeta = CarpetaConAnimacion([40, 60]);
        var vistos = new List<int>();
        _motor.VistaPrevia = true;
        _motor.CuadroListo += c => vistos.Add(c.Cuadro);
        _motor.UsarTema(new Tema { Fondo = new Fondo { Tipo = TipoFondo.Animacion, Archivo = "fondo.cuadros" } }, carpeta);
        _motor.Paso();
        _reloj.Milisegundos = 39;
        _motor.Paso();
        _reloj.Milisegundos = 40;
        _motor.Paso();
        _reloj.Milisegundos = 100;
        _motor.Paso();
        Assert.Equal([0, 1, 0], vistos);
        Directory.Delete(carpeta, recursive: true);
    }

    [Fact]
    public void En_pausa_la_animacion_no_avanza()
    {
        var carpeta = CarpetaConAnimacion([40, 40]);
        _motor.UsarTema(new Tema { Fondo = new Fondo { Tipo = TipoFondo.Animacion, Archivo = "fondo.cuadros" } }, carpeta);
        _motor.Pausar(true);
        _motor.Paso();
        _reloj.Milisegundos = 500;
        _motor.Paso();
        Assert.Equal(1, _buscador.Puerto.Cuadros);
        Directory.Delete(carpeta, recursive: true);
    }

    [Fact]
    public void Siguiente_paso_no_espera_mas_de_lo_necesario()
    {
        var siguiente = _motor.Paso();
        Assert.InRange(siguiente, 1, Motor.ReenvioMs);
    }

    [Fact]
    public void Mide_la_legibilidad_pedida()
    {
        ResultadoLegibilidad? resultado = null;
        _motor.LegibilidadLista += r => resultado = r;
        _motor.Paso();
        var tema = TemasIniciales.Basico("x", "cpu", "gpu");
        _motor.UsarTema(tema, Path.GetTempPath());
        _motor.MedirLegibilidad(tema.Capas[0].Id);
        _motor.Paso();
        Assert.NotNull(resultado);
        Assert.True(resultado!.Suficiente);
    }

    private static string CarpetaConAnimacion(int[] duraciones)
    {
        var carpeta = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vitral-motor-" + Guid.NewGuid().ToString("N"))).FullName;
        var cuadros = duraciones.Select((_, i) => MediosTests.Jpeg(i % 2 == 0 ? SKColors.Red : SKColors.Blue)).ToArray();
        using var archivo = File.Create(Path.Combine(carpeta, "fondo.cuadros"));
        ArchivoCuadros.Escribir(new Animacion(cuadros, duraciones), archivo);
        return carpeta;
    }
}
