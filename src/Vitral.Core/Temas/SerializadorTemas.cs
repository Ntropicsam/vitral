using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitral.Core.Temas;

public static class SerializadorTemas
{
    public static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowOutOfOrderMetadataProperties = true,
    };

    public static string Escribir(Tema tema) =>
        JsonSerializer.Serialize(ValidadorTemas.Normalizar(tema), Opciones);

    public static Tema Leer(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new TemaInvalidoException("El tema está vacío.");
        }
        try
        {
            var tema = JsonSerializer.Deserialize<Tema>(json, Opciones)
                ?? throw new TemaInvalidoException("El tema está vacío.");
            if (tema.Version > Tema.VersionActual)
            {
                throw new TemaInvalidoException($"El tema es de una versión más nueva de Vitral ({tema.Version}).");
            }
            return ValidadorTemas.Normalizar(tema);
        }
        catch (JsonException e)
        {
            throw new TemaInvalidoException($"El tema no se pudo leer: {e.Message}");
        }
    }
}

public sealed class TemaInvalidoException(string mensaje) : Exception(mensaje);
