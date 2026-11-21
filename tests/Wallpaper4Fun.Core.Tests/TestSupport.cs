namespace Wallpaper4Fun.Core.Tests;

/// <summary>Pasta temporária própria de cada teste, apagada no fim. Nunca use pastas reais do usuário.</summary>
public sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"w4f-tests-{Guid.NewGuid():N}");

    public TemporaryFolder() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public string MakeFile(string name, string contents = "video")
    {
        var path = File(name);
        System.IO.File.WriteAllText(path, contents);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}

/// <summary>Dublê das janelas do desktop: só registra o que foi pedido.</summary>
public sealed class FakeDisplay : IWallpaperDisplay
{
    public bool IsShowing { get; private set; }
    public int ShowCount { get; private set; }

    public void Show()
    {
        IsShowing = true;
        ShowCount++;
    }

    public void Hide() => IsShowing = false;
}
