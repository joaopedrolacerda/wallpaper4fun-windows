namespace Wallpaper4Fun.Core.Tests;

public sealed class FullscreenDetectorTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);

    [Fact(DisplayName = "uma janela que cobre o monitor inteiro está em tela cheia")]
    public void CoversMonitor() =>
        Assert.True(FullscreenDetector.IsFullscreen(new PixelRect(0, 0, 1920, 1080), Monitor, "UnrealWindow"));

    [Fact(DisplayName = "uma janela maior que o monitor (bordas para fora) também conta")]
    public void LargerThanMonitor() =>
        Assert.True(FullscreenDetector.IsFullscreen(new PixelRect(-8, -8, 1936, 1096), Monitor, "UnrealWindow"));

    [Fact(DisplayName = "uma janela maximizada comum (sem cobrir a barra de tarefas) não conta")]
    public void MaximizedWindowIsNotFullscreen() =>
        Assert.False(FullscreenDetector.IsFullscreen(new PixelRect(0, 0, 1920, 1040), Monitor, "Chrome_WidgetWin_1"));

    [Fact(DisplayName = "a janela fica em tela cheia no monitor dela, mesmo fora do principal")]
    public void OnSecondMonitor() =>
        Assert.True(FullscreenDetector.IsFullscreen(new PixelRect(1920, 0, 2560, 1440), new PixelRect(1920, 0, 2560, 1440), "UnrealWindow"));

    [Theory(DisplayName = "o próprio desktop do Windows nunca conta como app em tela cheia")]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("Shell_TrayWnd")]
    public void DesktopIsNotFullscreen(string className) =>
        Assert.False(FullscreenDetector.IsFullscreen(Monitor, Monitor, className));
}

public sealed class StallDetectorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 21, 20, 0, 0, TimeSpan.Zero);
    private static DateTimeOffset At(double seconds) => T0.AddSeconds(seconds);
    private static TimeSpan Pos(double seconds) => TimeSpan.FromSeconds(seconds);

    [Fact(DisplayName = "vídeo andando normalmente não está travado")]
    public void AdvancingIsHealthy()
    {
        var detector = new StallDetector();
        for (var s = 0; s <= 10; s++) Assert.False(detector.Check(Pos(s), At(s), shouldBePlaying: true));
    }

    [Fact(DisplayName = "vídeo parado no mesmo ponto por tempo demais está travado")]
    public void StuckIsStalled()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: true);
        Assert.False(detector.Check(Pos(5), At(2), shouldBePlaying: true));
        Assert.True(detector.Check(Pos(5), At(StallDetector.Limit.TotalSeconds + 0.1), shouldBePlaying: true));
    }

    [Fact(DisplayName = "vídeo pausado de propósito não conta como travado")]
    public void PausedIsNotStalled()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: false);
        Assert.False(detector.Check(Pos(5), At(60), shouldBePlaying: false));
    }

    [Fact(DisplayName = "ao voltar da pausa, o tempo parado durante a pausa não conta")]
    public void ResumeStartsFresh()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: false);
        Assert.False(detector.Check(Pos(5), At(60), shouldBePlaying: true));
        Assert.False(detector.Check(Pos(5), At(61), shouldBePlaying: true));
    }

    [Fact(DisplayName = "voltar ao início no loop não é travamento")]
    public void LoopIsNotStall()
    {
        var detector = new StallDetector();
        detector.Check(Pos(9.9), At(0), shouldBePlaying: true);
        Assert.False(detector.Check(Pos(0.1), At(1), shouldBePlaying: true));
    }

    [Fact(DisplayName = "depois de avisar um travamento, recomeça a contagem")]
    public void ResetsAfterReporting()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: true);
        Assert.True(detector.Check(Pos(5), At(10), shouldBePlaying: true));
        Assert.False(detector.Check(Pos(5), At(11), shouldBePlaying: true));
    }

    [Fact(DisplayName = "depois de recargas seguidas sem o vídeo andar, desiste de recarregar")]
    public void GivesUpAfterRepeatedStalls()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: true);
        for (var i = 1; i <= StallDetector.MaxReloadsWithoutProgress; i++)
            Assert.True(detector.Check(Pos(5), At(10 * i), shouldBePlaying: true));

        Assert.False(detector.Check(Pos(5), At(100), shouldBePlaying: true));
        Assert.True(detector.GaveUp);
    }

    [Fact(DisplayName = "se o vídeo voltar a andar, volta a vigiar")]
    public void ResumesWatchingAfterProgress()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: true);
        for (var i = 1; i <= StallDetector.MaxReloadsWithoutProgress; i++) detector.Check(Pos(5), At(10 * i), shouldBePlaying: true);

        detector.Check(Pos(6), At(100), shouldBePlaying: true);

        Assert.False(detector.GaveUp);
        Assert.True(detector.Check(Pos(6), At(110), shouldBePlaying: true));
    }

    [Fact(DisplayName = "uma pausa de verdade também zera a desistência")]
    public void PauseResetsGiveUp()
    {
        var detector = new StallDetector();
        detector.Check(Pos(5), At(0), shouldBePlaying: true);
        for (var i = 1; i <= StallDetector.MaxReloadsWithoutProgress; i++) detector.Check(Pos(5), At(10 * i), shouldBePlaying: true);

        detector.Check(Pos(5), At(100), shouldBePlaying: false);

        Assert.False(detector.GaveUp);
    }
}
