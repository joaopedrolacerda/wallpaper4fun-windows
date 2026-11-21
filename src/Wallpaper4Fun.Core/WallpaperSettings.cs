namespace Wallpaper4Fun.Core;

/// <summary>Ids dos tipos de wallpaper que vêm com o app. Salvos nas configurações: não mude depois de publicados.</summary>
public static class SourceIds
{
    public const string Shader = "shader";
    public const string Video = "video";
}

/// <summary>Tudo que o usuário escolhe no painel. É imutável: mude com <c>with</c> via <see cref="WallpaperManager.Update"/>.</summary>
public sealed record WallpaperSettings
{
    public string SourceId { get; init; } = SourceIds.Shader;
    public string ShaderId { get; init; } = ShaderCatalog.DefaultId;
    public double Speed { get; init; } = 1;
    public bool IsPaused { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool HideToTrayWhenClosed { get; init; } = true;
    /// <summary>Vídeo importado pelo usuário (nome do arquivo na <see cref="VideoLibrary"/>).</summary>
    public string? VideoFileName { get; init; }

    /// <summary>Vídeo do app escolhido (<see cref="VideoCatalog"/>). <c>null</c> = usar o vídeo importado.</summary>
    public string? BundledVideoId { get; init; } = VideoCatalog.DefaultId;

    public static WallpaperSettings Default { get; } = new();
}
