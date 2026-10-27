namespace Vitral.App;

internal static class Rutas
{
    public static readonly string Datos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vitral");
    public static readonly string Local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VitralDatos");
    public static readonly string Temas = Path.Combine(Datos, "Temas");
    public static readonly string Ajustes = Path.Combine(Datos, "ajustes.json");
    public static readonly string Registro = Path.Combine(Local, "vitral.log");
    public static readonly string WebView = Path.Combine(Local, "WebView2");
    public static readonly string Editor = Path.Combine(AppContext.BaseDirectory, "wwwroot");
}
