namespace Wallpaper4Fun.Core;

/// <summary>
/// Guarda os vídeos escolhidos numa pasta do app. Copiar o arquivo evita que o
/// wallpaper quebre se o usuário mover ou apagar o original.
/// </summary>
public sealed class VideoLibrary(string folder)
{
    public string ImportVideo(string sourcePath, string? replacing = null)
    {
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, Path.GetFileName(sourcePath));
        EnsureExists(sourcePath);
        DeleteIfPresent(replacing);
        File.Copy(sourcePath, destination, overwrite: true);
        return destination;
    }

    public string? Video(string fileName)
    {
        var path = Path.Combine(folder, fileName);
        return File.Exists(path) ? path : null;
    }

    private static void EnsureExists(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Vídeo não encontrado.", path);
    }

    private static void DeleteIfPresent(string? path)
    {
        if (path is not null && File.Exists(path)) File.Delete(path);
    }
}
