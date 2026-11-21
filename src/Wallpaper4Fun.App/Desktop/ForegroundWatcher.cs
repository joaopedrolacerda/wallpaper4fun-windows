using System.Text;
using System.Windows.Threading;
using Wallpaper4Fun.Core;
using static Wallpaper4Fun.App.Desktop.NativeMethods;

namespace Wallpaper4Fun.App.Desktop;

/// <summary>
/// Olha a janela da frente de tempos em tempos e avisa o manager quando um app em tela cheia
/// (jogo, vídeo) está escondendo o wallpaper. A decisão em si fica no <see cref="FullscreenDetector"/>.
/// </summary>
internal sealed class ForegroundWatcher : IDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1.5);

    private readonly WallpaperManager _manager;
    private readonly DispatcherTimer _timer = new() { Interval = Interval };
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;

    public ForegroundWatcher(WallpaperManager manager)
    {
        _manager = manager;
        _timer.Tick += (_, _) => _manager.FullscreenAppChanged(IsFullscreenAppInFront());
        _timer.Start();
    }

    public void Dispose() => _timer.Stop();

    private bool IsFullscreenAppInFront()
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero || BelongsToThisApp(window)) return false;
        if (!GetWindowRect(window, out var rect) || !TryGetMonitor(window, out var monitor)) return false;
        return FullscreenDetector.IsFullscreen(ToPixelRect(rect), ToPixelRect(monitor), ClassName(window));
    }

    private bool BelongsToThisApp(IntPtr window)
    {
        GetWindowThreadProcessId(window, out var processId);
        return processId == _ownProcessId;
    }

    private static bool TryGetMonitor(IntPtr window, out Rect monitor)
    {
        var info = new MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfo>() };
        var ok = GetMonitorInfo(MonitorFromWindow(window, MonitorDefaultToNearest), ref info);
        monitor = info.Monitor;
        return ok;
    }

    private static string ClassName(IntPtr window)
    {
        var name = new StringBuilder(256);
        GetClassName(window, name, name.Capacity);
        return name.ToString();
    }

    private static PixelRect ToPixelRect(Rect r) => new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
}
