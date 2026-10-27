using System.Globalization;

namespace Vitral.App;

internal static class Textos
{
    private static readonly Dictionary<string, (string Es, string En)> Todos = new(StringComparer.Ordinal)
    {
        ["abrir"] = ("Abrir Vitral", "Open Vitral"),
        ["salir"] = ("Salir", "Quit"),
        ["reiniciarVersion"] = ("Reiniciar con la versión {0}", "Restart with version {0}"),
        ["nuevaVersion.titulo"] = ("Hay una versión nueva de Vitral", "A new version of Vitral is ready"),
        ["nuevaVersion.texto"] = ("La {0} ya está descargada. Se instala la próxima vez que abras Vitral, o ya mismo desde Ajustes.", "Version {0} is downloaded. It installs the next time Vitral starts, or right away from Settings."),
        ["ocupada.titulo"] = ("La pantalla está ocupada", "The screen is busy"),
        ["ocupada.texto"] = ("Otro programa la tiene abierta, casi siempre TRCC. Ciérralo desde su ícono de la bandeja.", "Another program has it open, usually TRCC. Close it from its tray icon."),
        ["cpu.titulo"] = ("Falta la temperatura de la CPU", "CPU temperature is missing"),
        ["cpu.texto"] = ("Windows solo la deja leer con permisos. Haz clic aquí para activarla.", "Windows only allows it with permission. Click here to enable it."),
        ["cpu.listo"] = ("Vitral ya lee la temperatura de la CPU.", "Vitral can now read the CPU temperature."),
        ["listo"] = ("Listo", "Done"),
        ["estado.conectada"] = ("Conectada", "Connected"),
        ["estado.noConectada"] = ("No encuentro la pantalla", "Screen not found"),
        ["estado.ocupada"] = ("Otro programa tiene la pantalla", "Another program has the screen"),
        ["estado.noSoportada"] = ("{0}: todavía no es compatible", "{0}: not supported yet"),
        ["estado.buscando"] = ("Buscando la pantalla…", "Looking for the screen…"),
        ["estado.error"] = ("La pantalla no responde", "The screen is not responding"),
        ["lector.error"] = ("No se pudo {0} el lector de la CPU: {1}", "Could not {0} the CPU reader: {1}"),
        ["lector.instalar"] = ("instalar", "install"),
        ["lector.quitar"] = ("quitar", "remove"),
        ["temas.error"] = ("Vitral no pudo leer sus temas: {0}", "Vitral could not read its themes: {0}"),
        ["error.medio"] = ("Ese archivo no sirve como fondo: {0}", "That file can't be used as a background: {0}"),
        ["error.tema"] = ("Ese tema está dañado: {0}", "That theme is damaged: {0}"),
        ["error.disco"] = ("No se pudo guardar en el disco: {0}", "Could not save to disk: {0}"),
        ["error.otro"] = ("Algo salió mal: {0}", "Something went wrong: {0}"),
        ["temaNuevo"] = ("Tema nuevo", "New theme"),
        ["webview"] = ("Para el editor hace falta WebView2 de Microsoft, que viene con Windows 11. Instálalo desde go.microsoft.com/fwlink/p/?LinkId=2124703.", "The editor needs Microsoft WebView2, included with Windows 11. Get it from go.microsoft.com/fwlink/p/?LinkId=2124703."),
    };

    public static string Idioma { get; private set; } = "es";

    public static void Usar(string preferido) =>
        Idioma = preferido is "es" or "en"
            ? preferido
            : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es" ? "es" : "en";

    public static string T(string clave, params object[] datos)
    {
        var plantilla = Todos.TryGetValue(clave, out var t) ? (Idioma == "es" ? t.Es : t.En) : clave;
        return datos.Length == 0 ? plantilla : string.Format(CultureInfo.CurrentCulture, plantilla, datos);
    }
}
