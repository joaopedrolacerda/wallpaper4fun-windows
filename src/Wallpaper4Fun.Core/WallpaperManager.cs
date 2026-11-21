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
    private readonly IBundledVideoLocator _bundledVideos;
    private readonly IWallpaperDisplay _display;
    private bool _isStarted;

    public WallpaperManager(SettingsStore store, VideoLibrary library, IBundledVideoLocator bundledVideos, IWallpaperDisplay display)
    {
        _store = store;
        _library = library;
        _bundledVideos = bundledVideos;
        _display = display;
        Settings = store.Load();
        ImportedVideoPath = Settings.VideoFileName is { } name ? library.Video(name) : null;
    }

    /// <summary>Disparado depois de qualquer mudança de estado, para a interface se atualizar.</summary>
    public event EventHandler? Changed;

    public WallpaperSettings Settings { get; private set; }
    /// <summary>Vídeo importado pelo usuário, guardado mesmo quando um vídeo do app está escolhido.</summary>
    public string? ImportedVideoPath { get; private set; }

    /// <summary>O vídeo que deve tocar: o do app escolhido ou, se nenhum, o importado.</summary>
    public string? VideoPath => Settings.BundledVideoId is { } id ? BundledVideoPath(id) : ImportedVideoPath;
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
            UseImportedVideo(_library.ImportVideo(sourcePath, replacing: ImportedVideoPath));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LastError = $"Não foi possível importar o vídeo: {e.Message}";
            NotifyChanged();
        }
    }

    public void SelectBundledVideo(string id) => ChooseVideo(bundledId: id);

    public void SelectImportedVideo() => ChooseVideo(bundledId: null);

    private void UseImportedVideo(string path)
    {
        ImportedVideoPath = path;
        LastError = null;
        Update(s => s with { SourceId = SourceIds.Video, BundledVideoId = null, VideoFileName = Path.GetFileName(path) });
    }

    private void ChooseVideo(string? bundledId) =>
        Update(s => s with { SourceId = SourceIds.Video, BundledVideoId = bundledId });

    private string? BundledVideoPath(string id) =>
        VideoCatalog.Video(id) is { } video ? _bundledVideos.VideoPath(video) : null;

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
