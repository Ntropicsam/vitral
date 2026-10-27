using System.Text.Json;
using System.Text.Json.Nodes;
using Vitral.Core.Dispositivo;
using Vitral.Core.Ejecucion;
using Vitral.Core.Render;
using Vitral.Core.Sensores;
using Vitral.Core.Temas;

namespace Vitral.App.Editor;

internal static class Mensajes
{
    public static readonly JsonSerializerOptions Opciones = SerializadorTemas.Opciones;

    public static string Estado(EstadoMotor e) => Armar("estado", new
    {
        conexion = e.Pantalla.Conexion.ToString(),
        modelo = e.Pantalla.Info?.Modelo,
        detectado = e.Pantalla.Info?.ModeloDetectado,
        forzado = e.Pantalla.Info?.Forzado ?? false,
        ancho = e.Pantalla.Info?.Ancho,
        alto = e.Pantalla.Info?.Alto,
        detalle = e.Pantalla.Detalle,
        fps = Math.Round(e.Fps),
        cuadro = e.Cuadro,
        cuadros = e.Cuadros,
        pausado = e.Pausado,
    });

    public static string Temas(IEnumerable<TemaGuardado> temas, string? activo) => Armar("temas", new
    {
        activo,
        lista = temas.Select(t => new
        {
            id = t.Id,
            nombre = t.Tema.Nombre,
            miniatura = File.Exists(Path.Combine(t.Carpeta, AlmacenTemas.ArchivoMiniatura))
                ? $"https://{VentanaEditor.HostTemas}/{t.Id}/{AlmacenTemas.ArchivoMiniatura}?v={File.GetLastWriteTimeUtc(Path.Combine(t.Carpeta, AlmacenTemas.ArchivoMiniatura)).Ticks}"
                : null,
        }),
    });

    public static string Tema(string id, Tema tema) => Armar("tema", new { id, tema });

    public static string Sensores(IReadOnlyList<SensorInfo> sensores, bool leeCpu) => Armar("sensores", new
    {
        leeCpu,
        lista = sensores.Select(s => new { id = s.Id, nombre = s.Nombre, hardware = s.Hardware, tipo = s.Tipo.ToString(), agregado = s.Agregado }),
    });

    public static string Valores(LecturaSensores lectura) => Armar("valores", new { valores = lectura.Valores });

    public static string Vista(CuadroRenderizado c) => Armar("vista", new
    {
        jpeg = Convert.ToBase64String(c.Vista),
        cuadro = c.Cuadro,
        cajas = c.Cajas.Select(k => new { id = k.Id, x = k.X, y = k.Y, ancho = k.Ancho, alto = k.Alto }),
    });

    public static string Legibilidad(ResultadoLegibilidad? r) => Armar("legibilidad", r is null
        ? new { capaId = (string?)null, contraste = 0.0, suficiente = true }
        : new { capaId = (string?)r.CapaId, contraste = r.Contraste, suficiente = r.Suficiente });

    public static string Guardado(string id) => Armar("guardado", new { id });

    public static string Ajustes(object datos) => Armar("ajustes", datos);

    public static string Modelos(IEnumerable<(string Clave, ModeloPanel Modelo)> modelos) => Armar("modelos", new
    {
        lista = modelos.Select(m => new { clave = m.Clave, nombre = m.Modelo.Nombre, ancho = m.Modelo.Ancho, alto = m.Modelo.Alto }),
    });

    public static string Error(string mensaje) => Armar("error", new { mensaje });

    private static string Armar(string tipo, object datos)
    {
        var nodo = JsonSerializer.SerializeToNode(datos, Opciones)!.AsObject();
        nodo["tipo"] = tipo;
        return nodo.ToJsonString(Opciones);
    }

    public static string? Texto(JsonObject m, string campo) =>
        m.TryGetPropertyValue(campo, out var v) && v is JsonValue j && j.TryGetValue<string>(out var s) ? s : null;

    public static int? Entero(JsonObject m, string campo) =>
        m.TryGetPropertyValue(campo, out var v) && v is JsonValue j && j.TryGetValue<int>(out var n) ? n : null;

    public static bool? Logico(JsonObject m, string campo) =>
        m.TryGetPropertyValue(campo, out var v) && v is JsonValue j && j.TryGetValue<bool>(out var b) ? b : null;
}
