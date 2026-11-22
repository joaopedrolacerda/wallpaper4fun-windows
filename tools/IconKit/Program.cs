using SkiaSharp;
using Svg.Skia;

// IconKit: png <svg> <out.png> <size | LxA> [inset]   |   ico <out.ico> <png>...
switch (args[0])
{
    case "png": RenderPng(args[1], args[2], ParseSize(args[3]), args.Length > 4 ? float.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture) : 0f); break;
    case "ico": WriteIco(args[1], args[2..]); break;
}

// "256" vira 256x256; "720x1080" vira 720 de largura por 1080 de altura (artes da loja).
static (int Width, int Height) ParseSize(string text)
{
    var parts = text.Split('x').Select(int.Parse).ToArray();
    return (parts[0], parts.Length > 1 ? parts[1] : parts[0]);
}

static void RenderPng(string svgPath, string outPath, (int Width, int Height) size, float inset)
{
    using var svg = new SKSvg();
    var picture = svg.Load(svgPath) ?? throw new Exception("SVG inválido");
    var (width, height) = size;
    using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.Transparent);
    canvas.Translate(width * inset, height * inset);
    canvas.Scale(width * (1 - 2 * inset) / picture.CullRect.Width, height * (1 - 2 * inset) / picture.CullRect.Height);
    canvas.Translate(-picture.CullRect.Left, -picture.CullRect.Top);
    canvas.DrawPicture(picture, new SKPaint { IsAntialias = true });
    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
    File.WriteAllBytes(outPath, data.ToArray());
}

// ICO no formato que o Windows espera: BMP de 32 bits nos tamanhos pequenos e PNG só no de 256 px.
// (Algumas APIs, como System.Drawing.Icon, não lidam bem com PNG nos tamanhos pequenos.)
static void WriteIco(string outPath, string[] pngs)
{
    var entries = pngs.Select(File.ReadAllBytes).Select(IcoEntry).ToArray();
    using var w = new BinaryWriter(File.Create(outPath));
    w.Write((short)0); w.Write((short)1); w.Write((short)entries.Length);
    var offset = 6 + 16 * entries.Length;
    foreach (var (size, data) in entries)
    {
        w.Write((byte)(size >= 256 ? 0 : size)); w.Write((byte)(size >= 256 ? 0 : size));
        w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
        w.Write(data.Length); w.Write(offset);
        offset += data.Length;
    }
    foreach (var (_, data) in entries) w.Write(data);
}

static (int Size, byte[] Data) IcoEntry(byte[] png)
{
    using var bitmap = SKBitmap.Decode(png);
    return (bitmap.Width, bitmap.Width >= 256 ? png : Dib(bitmap));
}

// Bitmap de 32 bits (BGRA, linhas de baixo para cima) + máscara de 1 bit (1 = transparente).
static byte[] Dib(SKBitmap source)
{
    using var bgra = new SKBitmap(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
    source.CopyTo(bgra, SKColorType.Bgra8888);
    int width = bgra.Width, height = bgra.Height, maskStride = (width + 31) / 32 * 4;
    using var ms = new MemoryStream();
    using var w = new BinaryWriter(ms);
    w.Write(40); w.Write(width); w.Write(height * 2); w.Write((short)1); w.Write((short)32);
    w.Write(0); w.Write(width * height * 4 + maskStride * height); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
    for (var y = height - 1; y >= 0; y--)
        for (var x = 0; x < width; x++)
        {
            var c = bgra.GetPixel(x, y);
            w.Write(c.Blue); w.Write(c.Green); w.Write(c.Red); w.Write(c.Alpha);
        }
    for (var y = height - 1; y >= 0; y--)
    {
        var row = new byte[maskStride];
        for (var x = 0; x < width; x++)
            if (bgra.GetPixel(x, y).Alpha == 0) row[x / 8] |= (byte)(0x80 >> (x % 8));
        w.Write(row);
    }
    return ms.ToArray();
}
