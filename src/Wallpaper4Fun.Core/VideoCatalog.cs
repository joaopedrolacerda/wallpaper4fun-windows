namespace Wallpaper4Fun.Core;

/// <summary>Um vídeo que vem com o app. Arquivos: <c>Videos/&lt;Id&gt;.mp4</c> e a miniatura <c>Videos/&lt;Id&gt;.jpg</c>.</summary>
public sealed record BundledVideo(string Id, string Title);

/// <summary>
/// Vídeos padrão do app. Para adicionar um: coloque <c>&lt;id&gt;.mp4</c> e <c>&lt;id&gt;.jpg</c> em
/// <c>src/Wallpaper4Fun.App/Videos/</c> e inclua uma entrada aqui; nada mais precisa mudar (open-closed).
/// </summary>
public static class VideoCatalog
{
    public const string DefaultId = "cat-sunset";

    public static IReadOnlyList<BundledVideo> All { get; } =
    [
        new(DefaultId, "Gato no pôr do sol"),
        new("motorcycle", "Moto na estrada"),
        new("appa", "Appa na lua"),
        new("red-planet", "Planeta vermelho"),
        new("fractal", "Fractal violeta"),
    ];

    public static BundledVideo? Video(string id) => All.FirstOrDefault(v => v.Id == id);
}

/// <summary>Onde ficam os arquivos dos vídeos do app.</summary>
public interface IBundledVideoLocator
{
    string? VideoPath(BundledVideo video);
    string? ThumbnailPath(BundledVideo video);
}

/// <summary>Procura os vídeos numa pasta. No app, é a pasta <c>Videos</c> ao lado do .exe.</summary>
public sealed class FolderVideoLocator(string folder) : IBundledVideoLocator
{
    public string? VideoPath(BundledVideo video) => Existing($"{video.Id}.mp4");
    public string? ThumbnailPath(BundledVideo video) => Existing($"{video.Id}.jpg");

    private string? Existing(string fileName)
    {
        var path = Path.Combine(folder, fileName);
        return File.Exists(path) ? path : null;
    }
}
