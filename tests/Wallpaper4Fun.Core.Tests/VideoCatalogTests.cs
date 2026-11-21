using System.Runtime.CompilerServices;

namespace Wallpaper4Fun.Core.Tests;

public sealed class VideoCatalogTests
{
    [Fact(DisplayName = "o catálogo tem os 5 vídeos padrão, nessa ordem")]
    public void StandardVideos() =>
        Assert.Equal(new[] { "cat-sunset", "motorcycle", "appa", "red-planet", "fractal" }, VideoCatalog.All.Select(v => v.Id));

    [Fact(DisplayName = "os ids dos vídeos não se repetem")]
    public void UniqueIds() => Assert.Equal(VideoCatalog.All.Count, VideoCatalog.All.Select(v => v.Id).Distinct().Count());

    [Fact(DisplayName = "todo vídeo tem um título")]
    public void TitlesNotEmpty() => Assert.All(VideoCatalog.All, v => Assert.False(string.IsNullOrWhiteSpace(v.Title)));

    [Fact(DisplayName = "encontra o vídeo pelo id")]
    public void FindsById() => Assert.Equal("Appa na lua", VideoCatalog.Video("appa")?.Title);

    [Fact(DisplayName = "id desconhecido não encontra vídeo")]
    public void UnknownIsNull() => Assert.Null(VideoCatalog.Video("nao-existe"));

    [Fact(DisplayName = "o vídeo padrão das configurações existe no catálogo")]
    public void DefaultSettingsVideoExists() => Assert.NotNull(VideoCatalog.Video(WallpaperSettings.Default.BundledVideoId!));

    [Fact(DisplayName = "todo vídeo do catálogo está na pasta Videos do app, com miniatura")]
    public void EveryVideoIsShippedWithApp()
    {
        var locator = new FolderVideoLocator(AppVideosFolder());
        Assert.All(VideoCatalog.All, v =>
        {
            Assert.True(locator.VideoPath(v) is not null, $"falta {v.Id}.mp4");
            Assert.True(locator.ThumbnailPath(v) is not null, $"falta {v.Id}.jpg");
        });
    }

    private static string AppVideosFolder([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "src", "Wallpaper4Fun.App", "Videos"));
}

public sealed class FolderVideoLocatorTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private static readonly BundledVideo Appa = new("appa", "Appa na lua");

    public void Dispose() => _folder.Dispose();

    [Fact(DisplayName = "encontra o vídeo e a miniatura pelo id")]
    public void FindsFiles()
    {
        var video = _folder.MakeFile("appa.mp4");
        var thumb = _folder.MakeFile("appa.jpg");
        var locator = new FolderVideoLocator(_folder.Path);

        Assert.Equal(video, locator.VideoPath(Appa));
        Assert.Equal(thumb, locator.ThumbnailPath(Appa));
    }

    [Fact(DisplayName = "arquivo que não existe dá nulo")]
    public void MissingFilesAreNull()
    {
        var locator = new FolderVideoLocator(_folder.Path);
        Assert.Null(locator.VideoPath(Appa));
        Assert.Null(locator.ThumbnailPath(Appa));
    }
}
