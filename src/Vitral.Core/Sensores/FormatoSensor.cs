using System.Globalization;
using Vitral.Core.Temas;

namespace Vitral.Core.Sensores;

public static class FormatoSensor
{
    public const string SinDato = "–";

    private static readonly CultureInfo Cultura = CrearCultura();

    public static string Texto(float? valor, TipoSensor tipo, FormatoValor formato)
    {
        if (valor is not { } v || !float.IsFinite(v))
        {
            return SinDato;
        }

        var detallado = formato == FormatoValor.Detallado;
        return tipo switch
        {
            TipoSensor.Temperatura => detallado ? $"{Numero(v, 1)} °C" : $"{Numero(v, 0)}°",
            TipoSensor.Carga => $"{Numero(v, detallado ? 1 : 0)} %",
            TipoSensor.Reloj => v >= 1000 ? $"{Numero(v / 1000, detallado ? 2 : 1)} GHz" : $"{Numero(v, 0)} MHz",
            TipoSensor.Ventilador => $"{Numero(v, 0)} RPM",
            TipoSensor.Potencia => $"{Numero(v, detallado ? 1 : 0)} W",
            TipoSensor.Memoria => $"{Numero(v, 1)} GB",
            TipoSensor.Datos => $"{Numero(v, 1)} GB",
            TipoSensor.Voltaje => $"{Numero(v, detallado ? 3 : 2)} V",
            _ => Numero(v, detallado ? 1 : 0),
        };
    }

    private static string Numero(float valor, int decimales)
    {
        var redondeado = Math.Round(valor, decimales, MidpointRounding.AwayFromZero);
        if (redondeado == 0)
        {
            redondeado = 0;
        }
        return redondeado.ToString("N" + decimales, Cultura);
    }

    private static CultureInfo CrearCultura()
    {
        var cultura = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        cultura.NumberFormat.NumberDecimalSeparator = ",";
        cultura.NumberFormat.NumberGroupSeparator = ".";
        cultura.NumberFormat.NegativeSign = "-";
        return CultureInfo.ReadOnly(cultura);
    }
}
