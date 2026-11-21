using System.Windows.Threading;
using Microsoft.Win32;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App.Desktop;

/// <summary>Avisa o manager quando o PC suspende/bloqueia e quando volta, para pausar a animação.</summary>
internal sealed class SystemEventObserver : IDisposable
{
    private readonly WallpaperManager _manager;
    private readonly Dispatcher _dispatcher;

    public SystemEventObserver(WallpaperManager manager, Dispatcher dispatcher)
    {
        _manager = manager;
        _dispatcher = dispatcher;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) Post(_manager.SystemDidSuspend);
        if (e.Mode == PowerModes.Resume) Post(_manager.SystemDidResume);
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock) Post(_manager.SystemDidSuspend);
        if (e.Reason == SessionSwitchReason.SessionUnlock) Post(_manager.SystemDidResume);
    }

    // Os eventos chegam numa thread do sistema; o manager vive na thread da interface.
    private void Post(Action action) => _dispatcher.BeginInvoke(action);
}
