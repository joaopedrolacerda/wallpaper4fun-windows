using System.IO;
using System.Windows;
using Wallpaper4Fun.App.Desktop;
using Wallpaper4Fun.App.Panel;
using Wallpaper4Fun.App.Sources;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App;

/// <summary>Monta as peças do app e liga a lógica (<see cref="WallpaperManager"/>) ao Windows.</summary>
public partial class App : Application
{
    private DesktopWindowPresenter? _presenter;
    private WallpaperManager? _manager;
    private SystemEventObserver? _systemEvents;
    private TrayIcon? _tray;
    private PanelWindow? _panel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _presenter = new DesktopWindowPresenter();
        var manager = CreateManager(_presenter);
        _manager = manager;
        _presenter.MakeContent = () => new WallpaperHost(manager, AppSources.Standard);
        _systemEvents = new SystemEventObserver(manager, Dispatcher);
        _tray = new TrayIcon(manager, ShowPanel, Quit);
        manager.Start();
        ShowPanel();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _systemEvents?.Dispose();
        _presenter?.Dispose();
        base.OnExit(e);
    }

    private static WallpaperManager CreateManager(IWallpaperDisplay display) =>
        new(new SettingsStore(AppPaths.SettingsFile), new VideoLibrary(AppPaths.VideosFolder), AppPaths.BundledVideos, display);

    private void ShowPanel()
    {
        _panel ??= new PanelWindow(_manager!, AppSources.Standard, Quit);
        _panel.Show();
        _panel.Activate();
    }

    private void Quit()
    {
        _panel?.AllowClose();
        Shutdown();
    }
}

/// <summary>Onde o app guarda seus dados: %APPDATA%\wallpaper4fun.</summary>
internal static class AppPaths
{
    private static readonly string Root =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wallpaper4fun");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string VideosFolder => Path.Combine(Root, "Wallpapers");

    /// <summary>Vídeos padrão, instalados na pasta Videos ao lado do .exe.</summary>
    public static FolderVideoLocator BundledVideos { get; } = new(Path.Combine(AppContext.BaseDirectory, "Videos"));
}
