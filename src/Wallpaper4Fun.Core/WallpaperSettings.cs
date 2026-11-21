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
    public string? VideoFileName { get; init; }

    public static WallpaperSettings Default { get; } = new();
}
