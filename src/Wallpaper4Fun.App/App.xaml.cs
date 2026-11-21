using System.IO;
using System.Windows;
using System.Windows.Threading;
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
    private ForegroundWatcher? _foreground;
    private TrayIcon? _tray;
    private PanelWindow? _panel;

    protected override void OnStartup(StartupEventArgs e)
    {
        StartupTrace.Step("OnStartup");
        base.OnStartup(e);
        ErrorReporter.Install(this);
        try
        {
            StartApp();
        }
        catch (Exception error)
        {
            ErrorReporter.ReportFatal(error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// O painel abre primeiro: se algo depois falhar, a pessoa ao menos vê o app e a mensagem
    /// de erro. Bandeja e wallpaper vêm depois, cada um sem derrubar o resto.
    /// </summary>
    private void StartApp()
    {
        StartupTrace.Step("criando o manager");
        _presenter = new DesktopWindowPresenter();
        var manager = CreateManager(_presenter);
        _manager = manager;
        _presenter.MakeContent = () => new WallpaperHost(manager, AppSources.Standard);
        StartupTrace.Step("abrindo o painel");
        ShowPanel();
        StartupTrace.Step("painel aberto");
        ErrorReporter.Try("criar o ícone da bandeja", () => _tray = new TrayIcon(manager, ShowPanel, Quit));
        ErrorReporter.Try("acompanhar a suspensão do sistema", () => _systemEvents = new SystemEventObserver(manager, Dispatcher));
        ErrorReporter.Try("acompanhar apps em tela cheia", () => _foreground = new ForegroundWatcher(manager));
        ErrorReporter.Try("mostrar o wallpaper atrás dos ícones", manager.Start);
        StartupTrace.Step("inicialização concluída");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _systemEvents?.Dispose();
        _foreground?.Dispose();
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

/// <summary>
/// Mostra e grava erros inesperados em vez de o app sumir em silêncio. O log fica em
/// <c>%APPDATA%\wallpaper4fun\crash.log</c>.
/// </summary>
internal static class ErrorReporter
{
    private static readonly CrashLog Log = new(AppPaths.CrashLogFile);

    public static void Install(Application app)
    {
        app.DispatcherUnhandledException += OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Record(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => Record(e.Exception);
    }

    /// <summary>Roda um passo da inicialização; se falhar, avisa e o app continua.</summary>
    public static void Try(string step, Action action)
    {
        try
        {
            StartupTrace.Step($"{step}…");
            action();
            StartupTrace.Step($"{step}: ok");
        }
        catch (Exception error)
        {
            Record(error);
            Show($"Não foi possível {step}. O resto do app continua funcionando.", error, MessageBoxImage.Warning);
        }
    }

    public static void ReportFatal(Exception error)
    {
        Record(error);
        Show("O wallpaper4fun não conseguiu abrir.", error, MessageBoxImage.Error);
    }

    // Erros na thread da interface depois de aberto: avisa e mantém o app vivo.
    private static void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Record(e.Exception);
        Show("Aconteceu um erro inesperado.", e.Exception, MessageBoxImage.Warning);
        e.Handled = true;
    }

    private static void Record(Exception? error)
    {
        if (error is null) return;
        Log.Write(error, DateTimeOffset.Now);
        StartupTrace.Step($"ERRO: {error}");
    }

    private static void Show(string summary, Exception error, MessageBoxImage icon) =>
        MessageBox.Show($"{summary}\n\n{error.GetType().Name}: {error.Message}\n\nDetalhes em:\n{Log.FilePath}",
            "wallpaper4fun", MessageBoxButton.OK, icon);
}

/// <summary>Onde o app guarda seus dados: %APPDATA%\wallpaper4fun.</summary>
internal static class AppPaths
{
    private static readonly string Root =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wallpaper4fun");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string VideosFolder => Path.Combine(Root, "Wallpapers");
    public static string CrashLogFile => Path.Combine(Root, "crash.log");
    public static string StartupLogFile => Path.Combine(Root, "startup.log");

    /// <summary>Vídeos padrão, instalados na pasta Videos ao lado do .exe.</summary>
    public static FolderVideoLocator BundledVideos { get; } = new(Path.Combine(AppContext.BaseDirectory, "Videos"));
}
