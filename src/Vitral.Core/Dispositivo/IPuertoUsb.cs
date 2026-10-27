namespace Vitral.Core.Dispositivo;

public interface IPuertoUsb : IDisposable
{
    int Escribir(ReadOnlySpan<byte> datos);

    int Leer(Span<byte> destino);
}

public interface IBuscadorUsb
{
    ResultadoApertura Abrir();
}

public enum MotivoSinPanel
{
    NoConectado,
    Ocupado,
    Error,
}

public sealed record ResultadoApertura(IPuertoUsb? Puerto, MotivoSinPanel Motivo, string Detalle)
{
    public static ResultadoApertura Abierto(IPuertoUsb puerto) => new(puerto, MotivoSinPanel.Error, string.Empty);

    public static ResultadoApertura Fallo(MotivoSinPanel motivo, string detalle) => new(null, motivo, detalle);
}
