using System.Drawing.Imaging;
using Vitral.App;

int[] lados = [16, 20, 24, 32, 40, 48, 64, 128, 256];
var imagenes = lados.Select(l =>
{
    using var mapa = IconoVitral.Dibujar(l);
    using var memoria = new MemoryStream();
    mapa.Save(memoria, ImageFormat.Png);
    return memoria.ToArray();
}).ToList();

if (args.Length > 1)
{
    File.WriteAllBytes(args[1], imagenes[^1]);
}

using var archivo = File.Create(args[0]);
using var w = new BinaryWriter(archivo);
w.Write((short)0);
w.Write((short)1);
w.Write((short)lados.Length);
var desplazamiento = 6 + (16 * lados.Length);
for (var i = 0; i < lados.Length; i++)
{
    w.Write((byte)(lados[i] >= 256 ? 0 : lados[i]));
    w.Write((byte)(lados[i] >= 256 ? 0 : lados[i]));
    w.Write((byte)0);
    w.Write((byte)0);
    w.Write((short)1);
    w.Write((short)32);
    w.Write(imagenes[i].Length);
    w.Write(desplazamiento);
    desplazamiento += imagenes[i].Length;
}
foreach (var imagen in imagenes)
{
    w.Write(imagen);
}
Console.WriteLine($"Ícono escrito en {args[0]}");
