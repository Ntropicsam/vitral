using Vitral.Core.Dispositivo;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.Core.Tests;

public class CatalogoTests
{
    [Theory]
    [InlineData(7, 2, "Mjolnir Vision Pro", 640, 480)]
    [InlineData(7, 1, "Stream Vision", 640, 480)]
    [InlineData(5, 0, "Mjolnir Vision", 320, 240)]
    [InlineData(1, 48, "Pantalla 1600 × 720", 1600, 720)]
    [InlineData(1, 49, "Pantalla 1920 × 462", 1920, 462)]
    [InlineData(1, 0, "Grand Vision", 480, 480)]
    [InlineData(11, 0, "Pantalla 854 × 480", 854, 480)]
    [InlineData(16, 0, "Pantalla 960 × 540", 960, 540)]
    [InlineData(18, 0, "Pantalla 960 × 320", 960, 320)]
    [InlineData(68, 0, "Pantalla 1280 × 480", 1280, 480)]
    [InlineData(69, 0, "Pantalla 1920 × 440", 1920, 440)]
    [InlineData(250, 0, "Pantalla 480 × 480", 480, 480)]
    public void Modelos_conocidos(byte pm, byte sub, string nombre, int ancho, int alto)
    {
        var m = Catalogo.Buscar(pm, sub);
        Assert.Equal((nombre, ancho, alto), (m.Nombre, m.Ancho, m.Alto));
        Assert.True(m.Soportado);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(32)]
    [InlineData(50)]
    public void Las_de_RGB565_no_estan_soportadas(byte pm) => Assert.False(Catalogo.Buscar(pm, 0).Soportado);

    [Fact]
    public void La_Frozen_Warframe_Ultra_viene_girada_de_fabrica() =>
        Assert.Equal(180, Catalogo.Buscar(6, 1).GiroDeFabrica);

    [Fact]
    public void Sugerencias_de_sensores()
    {
        SensorInfo[] sensores =
        [
            new("/amdcpu/0/load/0", "CPU Total", "AMD Ryzen 5 5600T", TipoSensor.Carga),
            new("/amdcpu/0/temperature/2", "Core (Tctl/Tdie)", "AMD Ryzen 5 5600T", TipoSensor.Temperatura),
            new("/gpu-amd/0/temperature/7", "GPU Hot Spot", "AMD Radeon RX 9060 XT", TipoSensor.Temperatura),
            new("/gpu-amd/0/temperature/0", "GPU Core", "AMD Radeon RX 9060 XT", TipoSensor.Temperatura),
        ];
        Assert.Equal("/amdcpu/0/temperature/2", Sugerencias.TemperaturaCpu(sensores));
        Assert.Equal("/gpu-amd/0/temperature/0", Sugerencias.TemperaturaGpu(sensores));
        Assert.Equal("/amdcpu/0/load/0", Sugerencias.CargaCpu(sensores));
        Assert.Equal(string.Empty, Sugerencias.TemperaturaCpu([]));
    }

    [Fact]
    public void La_plantilla_se_ajusta_al_tamano_de_la_pantalla()
    {
        var base640 = TemasIniciales.Plantilla([]);
        var ancha = TemasIniciales.Plantilla([], 1920, 462);
        Assert.Equal((640, 480), (base640.Ancho, base640.Alto));
        Assert.Equal((1920, 462), (ancha.Ancho, ancha.Alto));
        Assert.Equal(base640.Capas.Count, ancha.Capas.Count);
        Assert.Equal(base640.Capas[1].X * 3, ancha.Capas[1].X, 1);
        Assert.True(((CapaSensor)ancha.Capas[1]).Tamano < ((CapaSensor)base640.Capas[1]).Tamano);
    }
}
