namespace Vitral.Core.Medios;

public static class LectorZt
{
    private const byte Cabecera = 0xDC;

    public static bool Parece(ReadOnlySpan<byte> datos) => datos.Length > 5 && datos[0] == Cabecera;

    public static Animacion Leer(ReadOnlySpan<byte> datos)
    {
        if (!Parece(datos))
        {
            throw new FormatoDeMedioException("No es un Theme.zt de TRCC.");
        }
        var lector = new LectorBinario(datos, 1);
        var cantidad = lector.Int32();
        return ArchivoCuadros.LeerCuerpo(ref lector, cantidad, duracionesAcumuladas: true);
    }
}
