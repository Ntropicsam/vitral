using System.Text.Json.Serialization;

namespace Vitral.Core.Temas;

public sealed record Tema
{
    public const int VersionActual = 1;

    public int Version { get; init; } = VersionActual;
    public string Nombre { get; init; } = "Tema nuevo";
    public int Ancho { get; init; } = 640;
    public int Alto { get; init; } = 480;
    public Fondo Fondo { get; init; } = new();
    public IReadOnlyList<Capa> Capas { get; init; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter<TipoFondo>))]
public enum TipoFondo
{
    Color,
    Imagen,
    Animacion,
}

[JsonConverter(typeof(JsonStringEnumConverter<AjusteFondo>))]
public enum AjusteFondo
{
    Cubrir,
    Contener,
    Estirar,
}

public sealed record Marco(float X, float Y, float Ancho, float Alto);

public sealed record Fondo
{
    public TipoFondo Tipo { get; init; } = TipoFondo.Color;
    public string Color { get; init; } = "#0d0f14";
    public string? Archivo { get; init; }
    public AjusteFondo Ajuste { get; init; } = AjusteFondo.Cubrir;
    public Marco? Marco { get; init; }
    public float RadioMarco { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<Alineacion>))]
public enum Alineacion
{
    Izquierda,
    Centro,
    Derecha,
}

[JsonConverter(typeof(JsonStringEnumConverter<FormatoValor>))]
public enum FormatoValor
{
    Corto,
    Detallado,
}

[JsonConverter(typeof(JsonStringEnumConverter<EstiloMedidor>))]
public enum EstiloMedidor
{
    Anillo,
    Barra,
}

[JsonConverter(typeof(JsonStringEnumConverter<PosicionEtiqueta>))]
public enum PosicionEtiqueta
{
    Encima,
    Debajo,
    Lado,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "tipo")]
[JsonDerivedType(typeof(CapaSensor), "sensor")]
[JsonDerivedType(typeof(CapaTexto), "texto")]
[JsonDerivedType(typeof(CapaMedidor), "medidor")]
public abstract record Capa
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Nombre { get; init; } = "Capa";
    public bool Visible { get; init; } = true;
    public float X { get; init; }
    public float Y { get; init; }
}

public sealed record CapaSensor : Capa
{
    public string SensorId { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;
    public FormatoValor Formato { get; init; } = FormatoValor.Corto;
    public float Tamano { get; init; } = 88;
    public string Color { get; init; } = "#f4efe6";
    public string Fuente { get; init; } = "barlow";
    public float TamanoEtiqueta { get; init; } = 22;
    public string ColorEtiqueta { get; init; } = "#9aa3b5";
    public string FuenteEtiqueta { get; init; } = "barlow";
    public PosicionEtiqueta PosicionEtiqueta { get; init; } = PosicionEtiqueta.Encima;
    public Alineacion Alineacion { get; init; } = Alineacion.Derecha;
    public bool Velo { get; init; }
}

public sealed record CapaTexto : Capa
{
    public string Texto { get; init; } = "Texto";
    public float Tamano { get; init; } = 40;
    public string Color { get; init; } = "#f4efe6";
    public string Fuente { get; init; } = "barlow";
    public Alineacion Alineacion { get; init; } = Alineacion.Izquierda;
    public bool Velo { get; init; }
}

public sealed record CapaMedidor : Capa
{
    public string SensorId { get; init; } = string.Empty;
    public EstiloMedidor Estilo { get; init; } = EstiloMedidor.Anillo;
    public float Minimo { get; init; }
    public float Maximo { get; init; } = 100;
    public float Ancho { get; init; } = 160;
    public float Grosor { get; init; } = 14;
    public string Color { get; init; } = "#ffb020";
    public string ColorPista { get; init; } = "#2a2f3a";
    public string Fuente { get; init; } = "barlow";
    public bool MostrarValor { get; init; } = true;
    public FormatoValor Formato { get; init; } = FormatoValor.Corto;
}
