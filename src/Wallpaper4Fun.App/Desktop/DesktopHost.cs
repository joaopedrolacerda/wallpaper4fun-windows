using System.Text;
using Wallpaper4Fun.Core;
using static Wallpaper4Fun.App.Desktop.NativeMethods;

namespace Wallpaper4Fun.App.Desktop;

/// <summary>
/// Onde pendurar a janela do wallpaper. <paramref name="InsertAfter"/> é a janela dos ícones
/// quando o wallpaper precisa ficar logo abaixo dela (layout do Windows 11 24H2 em diante).
/// </summary>
internal readonly record struct DesktopParent(IntPtr Handle, IntPtr InsertAfter)
{
    public bool IsModernLayout => InsertAfter != IntPtr.Zero;
}

/// <summary>
/// Coloca janelas atrás dos ícones do desktop. Pede ao Explorer (Progman) para criar a
/// camada "WorkerW" entre o papel de parede e os ícones, e pendura a janela nela.
/// </summary>
internal static class DesktopHost
{
    public static void Attach(IntPtr window, PixelRect bounds)
    {
        var parent = FindParent();
        PrepareStyles(window, parent.IsModernLayout);
        SetParent(window, parent.Handle);
        if (parent.IsModernLayout) PlaceBelowIcons(window, parent.InsertAfter);
        MoveWindow(window, bounds.X, bounds.Y, bounds.Width, bounds.Height, repaint: true);
    }

    /// <summary>Repinta o papel de parede original depois que nossas janelas saem.</summary>
    public static void RestoreSystemWallpaper()
    {
        var path = new StringBuilder(260);
        if (!SystemParametersInfo(SpiGetDeskWallpaper, (uint)path.Capacity, path, 0)) return;
        SystemParametersInfoSet(SpiSetDeskWallpaper, 0, path.ToString(), SpifUpdateIniFile);
    }

    private static DesktopParent FindParent()
    {
        var progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, SpawnWorkerW, IntPtr.Zero, IntPtr.Zero, SmtoNormal, 1000, out _);
        return ModernParent(progman) ?? ClassicParent() ?? new DesktopParent(progman, IntPtr.Zero);
    }

    /// <summary>Windows 11 24H2+: a WorkerW e os ícones viram filhos do próprio Progman.</summary>
    private static DesktopParent? ModernParent(IntPtr progman)
    {
        if (FindWindowEx(progman, IntPtr.Zero, "WorkerW", null) == IntPtr.Zero) return null;
        var icons = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        return new DesktopParent(progman, icons);
    }

    /// <summary>Windows 10 e 11 até 23H2: a WorkerW certa é a irmã logo depois da janela que contém os ícones.</summary>
    private static DesktopParent? ClassicParent()
    {
        var workerW = IntPtr.Zero;
        EnumWindows((top, _) =>
        {
            if (FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                workerW = FindWindowEx(IntPtr.Zero, top, "WorkerW", null);
            return true;
        }, IntPtr.Zero);
        return workerW == IntPtr.Zero ? null : new DesktopParent(workerW, IntPtr.Zero);
    }

    /// <summary>Fora do Alt+Tab, sem roubar foco; no layout novo, precisa ser "layered" para aparecer.</summary>
    private static void PrepareStyles(IntPtr window, bool modernLayout)
    {
        var style = (long)GetWindowLongPtr(window, GwlExStyle) | WsExToolWindow | WsExNoActivate;
        if (modernLayout) style |= WsExLayered;
        SetWindowLongPtr(window, GwlExStyle, (IntPtr)style);
        if (modernLayout) SetLayeredWindowAttributes(window, 0, 255, LwaAlpha);
    }

    private static void PlaceBelowIcons(IntPtr window, IntPtr icons) =>
        SetWindowPos(window, icons, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
}
