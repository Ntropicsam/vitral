using Vitral.Core.Dispositivo;

namespace Vitral.Core.Tests;

public class PantallaTests
{
    [Fact]
    public void Conecta_y_toma_la_resolucion_del_handshake()
    {
        var buscador = new BuscadorFalso();
        using var p = new Pantalla(buscador);
        Assert.True(p.Conectar());
        Assert.Equal(EstadoConexion.Conectada, p.Estado.Conexion);
        Assert.Equal(640, p.Estado.Info!.Ancho);
    }

    [Theory]
    [InlineData(MotivoSinPanel.NoConectado, EstadoConexion.NoConectada)]
    [InlineData(MotivoSinPanel.Ocupado, EstadoConexion.Ocupada)]
    [InlineData(MotivoSinPanel.Error, EstadoConexion.Error)]
    public void Sin_panel_queda_el_motivo(MotivoSinPanel motivo, EstadoConexion esperado)
    {
        using var p = new Pantalla(new BuscadorFalso { Falla = motivo });
        Assert.False(p.Conectar());
        Assert.Equal(esperado, p.Estado.Conexion);
        Assert.False(p.Conectada);
    }

    [Fact]
    public void Reintenta_el_handshake_si_la_primera_respuesta_llega_vacia()
    {
        using var p = new Pantalla(new BuscadorConRespuestasMalas(2));
        Assert.True(p.Conectar());
    }

    [Fact]
    public void Handshake_fallido_libera_el_puerto()
    {
        var buscador = new BuscadorConRespuestasMalas(5);
        using var p = new Pantalla(buscador);
        Assert.False(p.Conectar());
        Assert.True(buscador.Ultimo!.Liberado);
        Assert.Equal(EstadoConexion.Error, p.Estado.Conexion);
    }

    [Fact]
    public void Una_pantalla_RGB565_queda_como_no_soportada_y_suelta_el_puerto()
    {
        var buscador = new BuscadorFalso();
        var respuesta = Datos.RespuestaMjolnir.ToArray();
        respuesta[24] = 32;
        buscador.Puerto.Respuesta = respuesta;
        using var p = new Pantalla(buscador);
        Assert.False(p.Conectar());
        Assert.Equal(EstadoConexion.NoSoportada, p.Estado.Conexion);
        Assert.True(buscador.Puerto.Liberado);
    }

    [Fact]
    public void Enviar_sin_conectar_lanza()
    {
        using var p = new Pantalla(new BuscadorFalso());
        Assert.Throws<InvalidOperationException>(() => p.Enviar([1, 2, 3]));
    }

    [Fact]
    public void Escritura_corta_desconecta_y_lanza()
    {
        var buscador = new BuscadorFalso();
        using var p = new Pantalla(buscador);
        p.Conectar();
        buscador.Puerto.EscrituraCorta = true;
        Assert.Throws<IOException>(() => p.Enviar(new byte[100]));
        Assert.False(p.Conectada);
        Assert.True(buscador.Puerto.Liberado);
    }

    [Fact]
    public void Envia_cabecera_mas_imagen_y_ZLP_cuando_toca()
    {
        var buscador = new BuscadorFalso();
        using var p = new Pantalla(buscador);
        p.Conectar();
        p.Enviar(new byte[1024 - 64]);
        var escrituras = buscador.Puerto.Escrituras.Skip(1).ToList();
        Assert.Equal([1024, 0], escrituras.Select(e => e.Length));
    }

    private sealed class BuscadorConRespuestasMalas(int malas) : IBuscadorUsb
    {
        public PuertoFalso? Ultimo { get; private set; }

        public ResultadoApertura Abrir()
        {
            Ultimo = new PuertoFalso { RespuestasMalas = malas };
            return ResultadoApertura.Abierto(Ultimo);
        }
    }
}
