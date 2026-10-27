using Vitral.Core.Medios;

namespace Vitral.Core.Temas;

public sealed record TemaGuardado(string Id, Tema Tema, string Carpeta);

public sealed class AlmacenTemas
{
    public const string ArchivoTema = "tema.json";
    public const string ArchivoMiniatura = "miniatura.jpg";

    private readonly string _raiz;

    public AlmacenTemas(string raiz)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raiz);
        _raiz = Path.GetFullPath(raiz);
        Directory.CreateDirectory(_raiz);
    }

    public string Raiz => _raiz;

    public IReadOnlyList<TemaGuardado> Listar()
    {
        var temas = new List<TemaGuardado>();
        foreach (var carpeta in Directory.EnumerateDirectories(_raiz))
        {
            var id = Path.GetFileName(carpeta);
            if (EsIdValido(id) && Intentar(id) is { } tema)
            {
                temas.Add(tema);
            }
        }
        return temas.OrderBy(t => t.Tema.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public TemaGuardado? Primero()
    {
        var temas = Listar();
        return temas.Count > 0 ? temas[0] : null;
    }

    public TemaGuardado? Intentar(string id)
    {
        if (!EsIdValido(id))
        {
            return null;
        }
        var ruta = Path.Combine(Carpeta(id), ArchivoTema);
        if (!File.Exists(ruta))
        {
            return null;
        }
        try
        {
            return new TemaGuardado(id, SerializadorTemas.Leer(File.ReadAllText(ruta)), Carpeta(id));
        }
        catch (Exception e) when (e is TemaInvalidoException or IOException)
        {
            return null;
        }
    }

    public TemaGuardado Crear(Tema tema)
    {
        ArgumentNullException.ThrowIfNull(tema);
        var id = NuevoId();
        Directory.CreateDirectory(Carpeta(id));
        return Guardar(id, tema);
    }

    public TemaGuardado Guardar(string id, Tema tema)
    {
        var carpeta = CarpetaExistente(id);
        var normal = ValidadorTemas.Normalizar(tema);
        EscribirAtomico(Path.Combine(carpeta, ArchivoTema), System.Text.Encoding.UTF8.GetBytes(SerializadorTemas.Escribir(normal)));
        return new TemaGuardado(id, normal, carpeta);
    }

    public TemaGuardado Duplicar(string id)
    {
        var original = Intentar(id) ?? throw new KeyNotFoundException($"No existe el tema {id}.");
        var copia = Crear(original.Tema with { Nombre = $"{original.Tema.Nombre} (copia)" });
        foreach (var archivo in Directory.EnumerateFiles(original.Carpeta))
        {
            var nombre = Path.GetFileName(archivo);
            if (nombre != ArchivoTema)
            {
                File.Copy(archivo, Path.Combine(copia.Carpeta, nombre), overwrite: true);
            }
        }
        return copia;
    }

    public void Borrar(string id)
    {
        var carpeta = CarpetaExistente(id);
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
            carpeta,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
    }

    public Tema ImportarFondo(string id, Tema tema, string nombreOriginal, byte[] datos)
    {
        ArgumentNullException.ThrowIfNull(tema);
        var medio = Importador.Importar(nombreOriginal, datos);
        return ConFondo(id, tema, medio);
    }

    public Tema ConFondo(string id, Tema tema, MedioImportado medio)
    {
        ArgumentNullException.ThrowIfNull(tema);
        ArgumentNullException.ThrowIfNull(medio);
        var carpeta = CarpetaExistente(id);
        foreach (var viejo in new[] { "fondo.png", "fondo" + ArchivoCuadros.Extension })
        {
            if (viejo != medio.NombreArchivo)
            {
                File.Delete(Path.Combine(carpeta, viejo));
            }
        }
        EscribirAtomico(Path.Combine(carpeta, medio.NombreArchivo), medio.Contenido);
        var tipo = medio.Tipo == TipoMedio.Animacion ? TipoFondo.Animacion : TipoFondo.Imagen;
        return tema with { Fondo = tema.Fondo with { Tipo = tipo, Archivo = medio.NombreArchivo } };
    }

    public void GuardarMiniatura(string id, byte[] jpeg)
    {
        ArgumentNullException.ThrowIfNull(jpeg);
        EscribirAtomico(Path.Combine(CarpetaExistente(id), ArchivoMiniatura), jpeg);
    }

    public static bool EsIdValido(string? id) =>
        id is { Length: > 0 and <= 40 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    private string Carpeta(string id)
    {
        if (!EsIdValido(id))
        {
            throw new ArgumentException($"Id de tema inválido: {id}", nameof(id));
        }
        return Path.Combine(_raiz, id);
    }

    private string CarpetaExistente(string id)
    {
        var carpeta = Carpeta(id);
        if (!Directory.Exists(carpeta))
        {
            throw new KeyNotFoundException($"No existe el tema {id}.");
        }
        return carpeta;
    }

    private string NuevoId()
    {
        string id;
        do
        {
            id = Guid.NewGuid().ToString("N")[..10];
        }
        while (Directory.Exists(Path.Combine(_raiz, id)));
        return id;
    }

    private static void EscribirAtomico(string ruta, byte[] contenido)
    {
        var temporal = ruta + ".tmp";
        File.WriteAllBytes(temporal, contenido);
        File.Move(temporal, ruta, overwrite: true);
    }
}
