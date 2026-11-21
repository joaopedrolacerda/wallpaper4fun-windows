namespace Wallpaper4Fun.Core.Tests;

public sealed class ShaderClockTests
{
    private static readonly TimeSpan Start = TimeSpan.Zero;
    private static TimeSpan At(double seconds) => Start + TimeSpan.FromSeconds(seconds);

    [Fact(DisplayName = "o primeiro tique começa do zero")]
    public void FirstTickIsZero() => Assert.Equal(0f, new ShaderClock().Tick(Start, speed: 1));

    [Fact(DisplayName = "acumula o tempo passado entre os tiques")]
    public void Accumulates()
    {
        var clock = new ShaderClock();
        clock.Tick(Start, 1);
        clock.Tick(At(0.05), 1);
        Assert.Equal(0.1f, clock.Tick(At(0.1), 1), precision: 5);
    }

    [Fact(DisplayName = "a velocidade multiplica o tempo")]
    public void SpeedMultiplies()
    {
        var clock = new ShaderClock();
        clock.Tick(Start, 2);
        Assert.Equal(0.1f, clock.Tick(At(0.05), 2), precision: 5);
    }

    [Fact(DisplayName = "um intervalo grande (pausa) não faz a animação pular")]
    public void ClampsLongGaps()
    {
        var clock = new ShaderClock();
        clock.Tick(Start, 1);
        Assert.Equal((float)ShaderClock.MaxStep.TotalSeconds, clock.Tick(At(60), 1), precision: 5);
    }
}

public sealed class ClosePolicyTests
{
    [Fact(DisplayName = "com a opção ligada, fechar o painel só esconde e o app fica na bandeja")]
    public void HidesToTray() => Assert.Equal(PanelCloseAction.HideToTray, ClosePolicy.Decide(hideToTrayWhenClosed: true));

    [Fact(DisplayName = "com a opção desligada, fechar o painel encerra o app")]
    public void Quits() => Assert.Equal(PanelCloseAction.Quit, ClosePolicy.Decide(hideToTrayWhenClosed: false));
}

public sealed class DesktopLayoutTests
{
    [Fact(DisplayName = "o monitor principal fica na origem da área do desktop")]
    public void PrimaryScreenAtOrigin()
    {
        var bounds = DesktopLayout.ChildBounds(new PixelRect(0, 0, 1920, 1080), virtualOriginX: 0, virtualOriginY: 0);
        Assert.Equal(new PixelRect(0, 0, 1920, 1080), bounds);
    }

    [Fact(DisplayName = "um monitor à esquerda do principal é deslocado para coordenadas positivas")]
    public void ScreenLeftOfPrimary()
    {
        var bounds = DesktopLayout.ChildBounds(new PixelRect(-1280, 0, 1280, 1024), virtualOriginX: -1280, virtualOriginY: 0);
        Assert.Equal(new PixelRect(0, 0, 1280, 1024), bounds);
    }

    [Fact(DisplayName = "com um monitor à esquerda, o principal fica depois dele")]
    public void PrimaryAfterLeftScreen()
    {
        var bounds = DesktopLayout.ChildBounds(new PixelRect(0, 0, 1920, 1080), virtualOriginX: -1280, virtualOriginY: -200);
        Assert.Equal(new PixelRect(1280, 200, 1920, 1080), bounds);
    }
}
