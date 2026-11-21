using System.Text.Json;

namespace Wallpaper4Fun.Core;

/// <summary>Lê e grava <see cref="WallpaperSettings"/> num arquivo JSON.</summary>
public sealed class SettingsStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public WallpaperSettings Load()
    {
        try
        {
            return Deserialize(File.ReadAllText(filePath));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return WallpaperSettings.Default;
        }
    }

    public void Save(WallpaperSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static WallpaperSettings Deserialize(string json) =>
        JsonSerializer.Deserialize<WallpaperSettings>(json, JsonOptions) ?? WallpaperSettings.Default;
}
