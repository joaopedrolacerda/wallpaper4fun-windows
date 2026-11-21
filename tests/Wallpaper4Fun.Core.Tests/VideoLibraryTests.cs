namespace Wallpaper4Fun.Core.Tests;

public sealed class VideoLibraryTests : IDisposable
{
    private readonly TemporaryFolder _source = new();
    private readonly TemporaryFolder _storage = new();
    private VideoLibrary Library => new(_storage.Path);

    public void Dispose()
    {
        _source.Dispose();
        _storage.Dispose();
    }

    [Fact(DisplayName = "importar copia o vídeo para a pasta da biblioteca")]
    public void ImportCopiesIntoFolder()
    {
        var video = _source.MakeFile("praia.mp4");

        var imported = Library.ImportVideo(video);

        Assert.Equal(_storage.File("praia.mp4"), imported);
        Assert.True(File.Exists(imported));
        Assert.True(File.Exists(video), "o original continua onde estava");
    }

    [Fact(DisplayName = "importar cria a pasta da biblioteca se ela não existir")]
    public void ImportCreatesFolder()
    {
        var library = new VideoLibrary(_storage.File("nova"));

        var imported = library.ImportVideo(_source.MakeFile("praia.mp4"));

        Assert.True(File.Exists(imported));
    }

    [Fact(DisplayName = "importar um vídeo novo apaga o anterior")]
    public void ImportReplacesPrevious()
    {
        var first = Library.ImportVideo(_source.MakeFile("a.mp4"));

        Library.ImportVideo(_source.MakeFile("b.mp4"), replacing: first);

        Assert.False(File.Exists(first));
    }

    [Fact(DisplayName = "importar um vídeo com o mesmo nome substitui o conteúdo")]
    public void ImportOverwritesSameName()
    {
        var first = Library.ImportVideo(_source.MakeFile("a.mp4", "velho"));
        var newer = _source.MakeFile("a.mp4", "novo");

        var imported = Library.ImportVideo(newer, replacing: first);

        Assert.Equal("novo", File.ReadAllText(imported));
    }

    [Fact(DisplayName = "importar um arquivo que não existe lança erro")]
    public void ImportMissingFileThrows() =>
        Assert.ThrowsAny<IOException>(() => Library.ImportVideo(_source.File("nao-existe.mp4")));

    [Fact(DisplayName = "encontra um vídeo salvo pelo nome")]
    public void FindsSavedVideo()
    {
        var imported = Library.ImportVideo(_source.MakeFile("praia.mp4"));
        Assert.Equal(imported, Library.Video("praia.mp4"));
    }

    [Fact(DisplayName = "não encontra vídeo que não está na pasta")]
    public void MissingVideoIsNull() => Assert.Null(Library.Video("sumiu.mp4"));
}
