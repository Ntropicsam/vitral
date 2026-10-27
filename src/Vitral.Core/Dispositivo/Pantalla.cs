namespace Vitral.Core.Dispositivo;

public enum EstadoConexion
{
    Buscando,
    Conectada,
    NoConectada,
    Ocupada,
    NoSoportada,
    Error,
}

public sealed record EstadoPantalla(EstadoConexion Conexion, InfoPanel? Info, string Detalle);

public sealed class Pantalla(IBuscadorUsb buscador) : IDisposable
{
    private const int IntentosHandshake = 3;

    private IPuertoUsb? _puerto;

    public EstadoPantalla Estado { get; private set; } = new(EstadoConexion.Buscando, null, string.Empty);

    public bool Conectada => _puerto is not null && Estado.Info is not null;

    public bool Conectar()
    {
        if (Conectada)
        {
            return true;
        }

        var apertura = buscador.Abrir();
        if (apertura.Puerto is null)
        {
            var conexion = apertura.Motivo switch
            {
                MotivoSinPanel.NoConectado => EstadoConexion.NoConectada,
                MotivoSinPanel.Ocupado => EstadoConexion.Ocupada,
                _ => EstadoConexion.Error,
            };
            Estado = new EstadoPantalla(conexion, null, apertura.Detalle);
            return false;
        }

        try
        {
            var info = Handshake(apertura.Puerto);
            if (info.Formato != FormatoCuadro.Jpeg)
            {
                apertura.Puerto.Dispose();
                Estado = new EstadoPantalla(EstadoConexion.NoSoportada, info, "Esta pantalla recibe imágenes en RGB565, que Vitral todavía no maneja.");
                return false;
            }
            _puerto = apertura.Puerto;
            Estado = new EstadoPantalla(EstadoConexion.Conectada, info, string.Empty);
            return true;
        }
        catch (Exception e) when (e is IOException or ErrorDeProtocolo)
        {
            apertura.Puerto.Dispose();
            Estado = new EstadoPantalla(EstadoConexion.Error, null, e.Message);
            return false;
        }
    }

    public void Enviar(ReadOnlySpan<byte> imagen)
    {
        if (_puerto is null || Estado.Info is not { } info)
        {
            throw new InvalidOperationException("La pantalla no está conectada.");
        }

        var cuadro = Protocolo.ArmarCuadro(imagen, info.Ancho, info.Alto, info.Formato);
        try
        {
            foreach (var trozo in Protocolo.Trozos(cuadro))
            {
                var salieron = _puerto.Escribir(trozo.Span);
                if (salieron < trozo.Length)
                {
                    throw new IOException($"Escritura corta: salieron {salieron} de {trozo.Length} bytes.");
                }
            }
        }
        catch (IOException e)
        {
            Desconectar(EstadoConexion.Error, e.Message);
            throw;
        }
    }

    public void Desconectar(EstadoConexion conexion = EstadoConexion.NoConectada, string detalle = "")
    {
        _puerto?.Dispose();
        _puerto = null;
        Estado = new EstadoPantalla(conexion, null, detalle);
    }

    public void Dispose() => Desconectar();

    private static InfoPanel Handshake(IPuertoUsb puerto)
    {
        var paquete = Protocolo.PaqueteHandshake();
        var respuesta = new byte[Protocolo.TamanoRespuesta];
        ErrorDeProtocolo? ultimo = null;
        for (var intento = 0; intento < IntentosHandshake; intento++)
        {
            puerto.Escribir(paquete);
            var leidos = puerto.Leer(respuesta);
            try
            {
                return Protocolo.LeerRespuesta(respuesta.AsSpan(0, leidos));
            }
            catch (ErrorDeProtocolo e)
            {
                ultimo = e;
            }
        }
        throw ultimo!;
    }
}
