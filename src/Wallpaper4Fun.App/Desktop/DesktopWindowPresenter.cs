using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Wallpaper4Fun.Core;
using Forms = System.Windows.Forms;

namespace Wallpaper4Fun.App.Desktop;

/// <summary>
/// Mostra o wallpaper numa janela por monitor, atrás dos ícones, e recria as janelas
/// quando os monitores mudam. Camada fina: as decisões ficam no <see cref="WallpaperManager"/>.
/// </summary>
internal sealed class DesktopWindowPresenter : IWallpaperDisplay, IDisposable
{
    private readonly List<Window> _windows = [];
    private bool _isShowing;

    public DesktopWindowPresenter() => SystemEvents.DisplaySettingsChanged += OnDisplaysChanged;

    public Func<FrameworkElement> MakeContent { get; set; } = () => new System.Windows.Controls.Grid();

    public void Show()
    {
        _isShowing = true;
        RebuildWindows();
    }

    public void Hide()
    {
        _isShowing = false;
        CloseWindows();
        DesktopHost.RestoreSystemWallpaper();
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaysChanged;
        Hide();
    }

    // O evento chega numa thread do sistema; as janelas só podem ser mexidas na thread da interface.
    private void OnDisplaysChanged(object? sender, EventArgs e) =>
        Application.Current.Dispatcher.BeginInvoke(RefreshIfShowing);

    private void RefreshIfShowing()
    {
        if (_isShowing) RebuildWindows();
    }

    private void RebuildWindows()
    {
        CloseWindows();
        var origin = Forms.SystemInformation.VirtualScreen.Location;
        foreach (var screen in Forms.Screen.AllScreens)
            _windows.Add(OpenWindow(DesktopLayout.ChildBounds(ToPixelRect(screen.Bounds), origin.X, origin.Y)));
    }

    /// <summary>Cria a janela sem mostrar, pendura atrás dos ícones e só então mostra (sem piscar na frente).</summary>
    private Window OpenWindow(PixelRect bounds)
    {
        var window = CreateWindow();
        var handle = new WindowInteropHelper(window).EnsureHandle();
        DesktopHost.Attach(handle, bounds);
        window.Show();
        return window;
    }

    private Window CreateWindow() => new()
    {
        WindowStyle = WindowStyle.None,
        ResizeMode = ResizeMode.NoResize,
        ShowInTaskbar = false,
        ShowActivated = false,
        Focusable = false,
        Background = Brushes.Black,
        Content = MakeContent(),
    };

    private void CloseWindows()
    {
        _windows.ForEach(w => w.Close());
        _windows.Clear();
    }

    private static PixelRect ToPixelRect(System.Drawing.Rectangle r) => new(r.X, r.Y, r.Width, r.Height);
}
