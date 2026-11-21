namespace Wallpaper4Fun.Core;

/// <summary>Acumula o tempo da animação, assim mudar a velocidade ou pausar não faz a imagem "pular".</summary>
public sealed class ShaderClock
{
    /// <summary>Maior passo aceito entre dois quadros; intervalos maiores (pausa, suspensão) contam só isso.</summary>
    public static readonly TimeSpan MaxStep = TimeSpan.FromSeconds(0.1);

    private TimeSpan? _last;
    private double _elapsedSeconds;

    public float Tick(TimeSpan now, double speed)
    {
        if (_last is { } last) _elapsedSeconds += Step(last, now).TotalSeconds * speed;
        _last = now;
        return (float)_elapsedSeconds;
    }

    private static TimeSpan Step(TimeSpan last, TimeSpan now)
    {
        var step = now - last;
        return step > MaxStep ? MaxStep : step;
    }
}

public enum PanelCloseAction { HideToTray, Quit }

public enum PanelMinimizeAction { HideToTray, StayInTaskbar }

/// <summary>
/// Decide o que acontece quando o usuário fecha ou minimiza o painel. Uma opção só
/// ("ficar só na bandeja") vale para os dois.
/// </summary>
public static class ClosePolicy
{
    public static PanelCloseAction Decide(bool hideToTrayWhenClosed) =>
        hideToTrayWhenClosed ? PanelCloseAction.HideToTray : PanelCloseAction.Quit;

    public static PanelMinimizeAction DecideMinimize(bool hideToTrayWhenClosed) =>
        hideToTrayWhenClosed ? PanelMinimizeAction.HideToTray : PanelMinimizeAction.StayInTaskbar;
}

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>
/// A janela do desktop do Windows (WorkerW) cobre todos os monitores e começa no canto
/// superior esquerdo da área virtual. Converte a posição de um monitor para dentro dela.
/// </summary>
public static class DesktopLayout
{
    public static PixelRect ChildBounds(PixelRect screen, int virtualOriginX, int virtualOriginY) =>
        screen with { X = screen.X - virtualOriginX, Y = screen.Y - virtualOriginY };
}
