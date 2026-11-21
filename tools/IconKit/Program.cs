using SkiaSharp;
using Svg.Skia;

// IconKit: png <svg> <out.png> <size> [inset]   |   ico <out.ico> <png>...
switch (args[0])
{
    case "png": RenderPng(args[1], args[2], int.Parse(args[3]), args.Length > 4 ? float.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture) : 0f); break;
    case "ico": WriteIco(args[1], args[2..]); break;
}

static void RenderPng(string svgPath, string outPath, int size, float inset)
{
    using var svg = new SKSvg();
    var picture = svg.Load(svgPath) ?? throw new Exception("SVG inválido");
    var art = size * (1 - 2 * inset);
    using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.Transparent);
    canvas.Translate(size * inset, size * inset);
    canvas.Scale(art / picture.CullRect.Width, art / picture.CullRect.Height);
    canvas.DrawPicture(picture, new SKPaint { IsAntialias = true });
    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
    File.WriteAllBytes(outPath, data.ToArray());
}

// ICO com imagens PNG dentro (aceito do Windows Vista em diante).
static void WriteIco(string outPath, string[] pngs)
{
    var images = pngs.Select(File.ReadAllBytes).ToArray();
    using var w = new BinaryWriter(File.Create(outPath));
    w.Write((short)0); w.Write((short)1); w.Write((short)images.Length);
    var offset = 6 + 16 * images.Length;
    foreach (var png in images)
    {
        using var codec = SKCodec.Create(new MemoryStream(png));
        var s = codec.Info.Width;
        w.Write((byte)(s >= 256 ? 0 : s)); w.Write((byte)(s >= 256 ? 0 : s));
        w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
        w.Write(png.Length); w.Write(offset);
        offset += png.Length;
    }
    foreach (var png in images) w.Write(png);
}
