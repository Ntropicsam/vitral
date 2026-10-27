using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Vitral.App;

internal static partial class IconoVitral
{
    private static readonly Color Plomo = Color.FromArgb(0x0d, 0x0f, 0x14);
    private static readonly Color[] Panos =
    [
        Color.FromArgb(0xff, 0xb0, 0x20),
        Color.FromArgb(0x7a, 0xd3, 0xff),
        Color.FromArgb(0xff, 0x6b, 0x5a),
        Color.FromArgb(0xf4, 0xef, 0xe6),
    ];

    public static Bitmap Dibujar(int lado, bool apagado = false)
    {
        var mapa = new Bitmap(lado, lado);
        using var g = Graphics.FromImage(mapa);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        var radio = lado * 0.22f;
        using (var fondo = Redondeado(new RectangleF(0, 0, lado, lado), radio))
        using (var plomo = new SolidBrush(Plomo))
        {
            g.FillPath(plomo, fondo);
        }

        var margen = Math.Max(1.5f, lado * 0.12f);
        var junta = Math.Max(1f, lado * 0.07f);
        var celda = (lado - (margen * 2) - junta) / 2;
        for (var i = 0; i < 4; i++)
        {
            var x = margen + ((i % 2) * (celda + junta));
            var y = margen + ((i / 2) * (celda + junta));
            var color = apagado ? Gris(Panos[i]) : Panos[i];
            using var pano = Redondeado(new RectangleF(x, y, celda, celda), celda * 0.18f);
            using var pincel = new SolidBrush(color);
            g.FillPath(pincel, pano);
        }
        return mapa;
    }

    public static Icon Icono(int lado, bool apagado = false)
    {
        using var mapa = Dibujar(lado, apagado);
        var manejador = mapa.GetHicon();
        try
        {
            using var temporal = Icon.FromHandle(manejador);
            return (Icon)temporal.Clone();
        }
        finally
        {
            DestroyIcon(manejador);
        }
    }

    private static Color Gris(Color c)
    {
        var l = (int)((c.R * 0.3) + (c.G * 0.59) + (c.B * 0.11));
        return Color.FromArgb(l / 2 + 40, l / 2 + 40, l / 2 + 44);
    }

    private static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var d = radio * 2;
        var camino = new GraphicsPath();
        camino.AddArc(r.X, r.Y, d, d, 180, 90);
        camino.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        camino.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        camino.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        camino.CloseFigure();
        return camino;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(nint icono);
}
