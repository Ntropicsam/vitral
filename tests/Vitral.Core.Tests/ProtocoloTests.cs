using System.Buffers.Binary;
using Vitral.Core.Dispositivo;

namespace Vitral.Core.Tests;

public class ProtocoloTests
{
    [Fact]
    public void Handshake_trae_magia_y_comando_1_en_el_byte_56()
    {
        var p = Protocolo.PaqueteHandshake();
        Assert.Equal(64, p.Length);
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, p[..4]);
        Assert.Equal(1, p[56]);
        Assert.Equal(59, p.Count(b => b == 0));
    }

    [Fact]
    public void Lee_la_respuesta_real_de_la_Mjolnir()
    {
        var info = Protocolo.LeerRespuesta(Datos.RespuestaMjolnir);
        Assert.Equal(7, info.Pm);
        Assert.Equal(2, info.Sub);
        Assert.Equal("32240AE2", info.Identificador);
        Assert.Equal((640, 480), (info.Ancho, info.Alto));
        Assert.Equal(FormatoCuadro.Jpeg, info.Formato);
        Assert.Equal("Mjolnir Vision Pro", info.Modelo);
    }

    [Fact]
    public void Respuesta_corta_o_vacia_lanza()
    {
        Assert.Throws<ErrorDeProtocolo>(() => Protocolo.LeerRespuesta([]));
        Assert.Throws<ErrorDeProtocolo>(() => Protocolo.LeerRespuesta(Datos.RespuestaMjolnir.AsSpan(0, 56)));
    }

    [Fact]
    public void Respuesta_sin_magia_o_con_pm_cero_lanza()
    {
        var sinMagia = Datos.RespuestaMjolnir.ToArray();
        sinMagia[0] = 0;
        Assert.Throws<ErrorDeProtocolo>(() => Protocolo.LeerRespuesta(sinMagia));

        var pmCero = Datos.RespuestaMjolnir.ToArray();
        pmCero[24] = 0;
        Assert.Throws<ErrorDeProtocolo>(() => Protocolo.LeerRespuesta(pmCero));
    }

    [Theory]
    [InlineData(99, 480, 480, FormatoCuadro.Jpeg)]
    [InlineData(5, 320, 240, FormatoCuadro.Jpeg)]
    [InlineData(32, 320, 320, FormatoCuadro.Rgb565)]
    public void Pm_define_resolucion_y_formato(byte pm, int ancho, int alto, FormatoCuadro formato)
    {
        var r = Datos.RespuestaMjolnir.ToArray();
        r[24] = pm;
        var info = Protocolo.LeerRespuesta(r);
        Assert.Equal((ancho, alto, formato), (info.Ancho, info.Alto, info.Formato));
    }

    [Fact]
    public void Sin_marca_0x81_no_hay_identificador()
    {
        var r = Datos.RespuestaMjolnir.ToArray();
        r[56] = 0;
        Assert.Equal(string.Empty, Protocolo.LeerRespuesta(r).Identificador);
    }

    [Fact]
    public void Cabecera_del_cuadro_lleva_tipo_tamano_modo_y_largo()
    {
        var imagen = new byte[] { 0xFF, 0xD8, 1, 2, 3 };
        var c = Protocolo.ArmarCuadro(imagen, 640, 480, FormatoCuadro.Jpeg).AsSpan();
        Assert.Equal(69, c.Length);
        Assert.True(c[..4].SequenceEqual(new byte[] { 0x12, 0x34, 0x56, 0x78 }));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32LittleEndian(c[4..]));
        Assert.Equal(640u, BinaryPrimitives.ReadUInt32LittleEndian(c[8..]));
        Assert.Equal(480u, BinaryPrimitives.ReadUInt32LittleEndian(c[12..]));
        Assert.True(c[16..56].ToArray().All(b => b == 0));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32LittleEndian(c[56..]));
        Assert.Equal(5u, BinaryPrimitives.ReadUInt32LittleEndian(c[60..]));
        Assert.True(c[64..].SequenceEqual(imagen));
    }

    [Fact]
    public void Cuadro_vacio_o_de_tamano_cero_lanza()
    {
        Assert.Throws<ArgumentException>(() => Protocolo.ArmarCuadro([], 640, 480, FormatoCuadro.Jpeg));
        Assert.Throws<ArgumentOutOfRangeException>(() => Protocolo.ArmarCuadro([1], 0, 480, FormatoCuadro.Jpeg));
    }

    [Fact]
    public void Trozos_de_16_KiB_con_ZLP_si_el_total_es_multiplo_de_512()
    {
        var exacto = Protocolo.Trozos(new byte[32 * 1024]).ToList();
        Assert.Equal([16384, 16384, 0], exacto.Select(t => t.Length));

        var suelto = Protocolo.Trozos(new byte[20000]).ToList();
        Assert.Equal([16384, 3616], suelto.Select(t => t.Length));

        var chico = Protocolo.Trozos(new byte[1024]).ToList();
        Assert.Equal([1024, 0], chico.Select(t => t.Length));
    }
}
