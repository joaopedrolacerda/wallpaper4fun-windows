namespace Wallpaper4Fun.Core;

/// <summary>
/// Quem mostra o wallpaper na tela. Em produção são as janelas atrás dos ícones
/// (<c>DesktopWindowPresenter</c> no app); nos testes, um dublê.
/// </summary>
public interface IWallpaperDisplay
{
    void Show();
    void Hide();
}

/// <summary>Pausa e velocidade, usadas por todos os tipos de wallpaper (DRY).</summary>
public readonly record struct Playback(bool Paused, double Speed);

/// <summary>
/// Estado do app e regras de quando mostrar, pausar e trocar o wallpaper.
/// Não conhece Windows nem WPF: tudo que fala com o sistema chega por injeção.
/// </summary>
public sealed class WallpaperManager
{
    private readonly SettingsStore _store;
    private readonly VideoLibrary _library;
    private readonly IWallpaperDisplay _display;
    private bool _isStarted;

    public WallpaperManager(SettingsStore store, VideoLibrary library, IWallpaperDisplay display)
    {
        _store = store;
        _library = library;
        _display = display;
        Settings = store.Load();
        VideoPath = Settings.VideoFileName is { } name ? library.Video(name) : null;
    }

    /// <summary>Disparado depois de qualquer mudança de estado, para a interface se atualizar.</summary>
    public event EventHandler? Changed;

    public WallpaperSettings Settings { get; private set; }
    public string? VideoPath { get; private set; }
    public string? LastError { get; private set; }
    public bool SystemSuspended { get; private set; }

    public bool EffectivePaused => Settings.IsPaused || SystemSuspended;

    /// <summary>Como a animação deve tocar agora. Use isto, não <c>Settings.IsPaused</c> direto.</summary>
    public Playback Playback => new(EffectivePaused, Settings.Speed);

    public void Start()
    {
        _isStarted = true;
        ApplyEnabledState();
    }

    public void Update(Func<WallpaperSettings, WallpaperSettings> change)
    {
        var old = Settings;
        Settings = change(old);
        _store.Save(Settings);
        if (old.IsEnabled != Settings.IsEnabled) ApplyEnabledState();
        NotifyChanged();
    }

    public void SystemDidSuspend() => SetSystemSuspended(true);
    public void SystemDidResume() => SetSystemSuspended(false);

    public void ImportVideo(string sourcePath)
    {
        try
        {
            SelectVideo(_library.ImportVideo(sourcePath, replacing: VideoPath));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LastError = $"Não foi possível importar o vídeo: {e.Message}";
            NotifyChanged();
        }
    }

    private void SelectVideo(string path)
    {
        VideoPath = path;
        LastError = null;
        Update(s => s with { SourceId = SourceIds.Video, VideoFileName = Path.GetFileName(path) });
    }

    private void SetSystemSuspended(bool suspended)
    {
        SystemSuspended = suspended;
        NotifyChanged();
    }

    private void ApplyEnabledState()
    {
        if (!_isStarted) return;
        if (Settings.IsEnabled) _display.Show();
        else _display.Hide();
    }

    private void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
