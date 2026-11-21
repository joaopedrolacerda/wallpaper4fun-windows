namespace Wallpaper4Fun.Core;

/// <summary>
/// Decide se a janela da frente é um app em tela cheia (jogo, vídeo), para pausar o wallpaper
/// enquanto ninguém consegue vê-lo.
/// </summary>
public static class FullscreenDetector
{
    // O próprio desktop e a barra de tarefas cobrem a tela, mas não são "apps em tela cheia".
    private static readonly HashSet<string> ShellClasses = ["Progman", "WorkerW", "Shell_TrayWnd"];

    public static bool IsFullscreen(PixelRect window, PixelRect monitor, string className) =>
        !ShellClasses.Contains(className) && Covers(window, monitor);

    private static bool Covers(PixelRect window, PixelRect monitor) =>
        window.X <= monitor.X && window.Y <= monitor.Y &&
        window.X + window.Width >= monitor.X + monitor.Width &&
        window.Y + window.Height >= monitor.Y + monitor.Height;
}

/// <summary>
/// Percebe quando um vídeo deveria estar tocando mas parou no mesmo ponto (o player do WPF
/// congela depois que um jogo em tela cheia toma a placa de vídeo). Avisa uma vez por travamento
/// e desiste depois de algumas recargas seguidas sem o vídeo andar, para não ficar recarregando
/// para sempre um vídeo que o PC não consegue tocar.
/// </summary>
public sealed class StallDetector
{
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);
    public const int MaxReloadsWithoutProgress = 2;

    private TimeSpan? _lastPosition;
    private DateTimeOffset _unchangedSince;
    private bool _wasPlaying;
    private int _reloadsWithoutProgress;

    public bool GaveUp => _reloadsWithoutProgress >= MaxReloadsWithoutProgress;

    public bool Check(TimeSpan position, DateTimeOffset now, bool shouldBePlaying)
    {
        if (!shouldBePlaying || !_wasPlaying || position != _lastPosition)
        {
            _reloadsWithoutProgress = 0;
            Restart(position, now, shouldBePlaying);
            return false;
        }
        if (GaveUp || now - _unchangedSince < Limit) return false;
        _reloadsWithoutProgress++;
        Restart(position, now, shouldBePlaying);
        return true;
    }

    private void Restart(TimeSpan position, DateTimeOffset now, bool playing)
    {
        _lastPosition = position;
        _unchangedSince = now;
        _wasPlaying = playing;
    }
}
