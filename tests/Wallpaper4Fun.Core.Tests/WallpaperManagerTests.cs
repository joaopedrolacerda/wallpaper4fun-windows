namespace Wallpaper4Fun.Core.Tests;

public sealed class WallpaperManagerTests : IDisposable
{
    private readonly TemporaryFolder _config = new();
    private readonly TemporaryFolder _videos = new();
    private readonly TemporaryFolder _source = new();
    private readonly FakeDisplay _display = new();

    private SettingsStore Store => new(_config.File("settings.json"));
    private WallpaperManager MakeManager() => new(Store, new VideoLibrary(_videos.Path), _display);

    public void Dispose()
    {
        _config.Dispose();
        _videos.Dispose();
        _source.Dispose();
    }

    private void SaveSettings(Func<WallpaperSettings, WallpaperSettings> change) =>
        Store.Save(change(WallpaperSettings.Default));

    // Configurações

    [Fact(DisplayName = "começa com as configurações salvas")]
    public void LoadsSavedSettings()
    {
        SaveSettings(s => s with { ShaderId = "waves" });
        Assert.Equal("waves", MakeManager().Settings.ShaderId);
    }

    [Fact(DisplayName = "mudar uma configuração salva na hora")]
    public void PersistsChanges()
    {
        MakeManager().Update(s => s with { Speed = 2 });
        Assert.Equal(2, Store.Load().Speed);
    }

    [Fact(DisplayName = "avisa quando algo muda")]
    public void RaisesChanged()
    {
        var manager = MakeManager();
        var raised = 0;
        manager.Changed += (_, _) => raised++;

        manager.Update(s => s with { Speed = 2 });

        Assert.Equal(1, raised);
    }

    // Ligar e desligar

    [Fact(DisplayName = "ao iniciar ativo, mostra o wallpaper")]
    public void StartShowsWhenEnabled()
    {
        MakeManager().Start();
        Assert.True(_display.IsShowing);
    }

    [Fact(DisplayName = "ao iniciar desligado, não mostra o wallpaper")]
    public void StartDoesNotShowWhenDisabled()
    {
        SaveSettings(s => s with { IsEnabled = false });
        MakeManager().Start();
        Assert.False(_display.IsShowing);
    }

    [Fact(DisplayName = "antes de iniciar, mudar configurações não mexe nas janelas")]
    public void NothingShownBeforeStart()
    {
        MakeManager().Update(s => s with { IsEnabled = true });
        Assert.Equal(0, _display.ShowCount);
    }

    [Fact(DisplayName = "desligar esconde e religar mostra de novo")]
    public void TogglingEnabled()
    {
        var manager = MakeManager();
        manager.Start();

        manager.Update(s => s with { IsEnabled = false });
        Assert.False(_display.IsShowing);

        manager.Update(s => s with { IsEnabled = true });
        Assert.True(_display.IsShowing);
    }

    [Fact(DisplayName = "mudar outras configurações não recria as janelas")]
    public void OtherChangesDoNotReshow()
    {
        var manager = MakeManager();
        manager.Start();

        manager.Update(s => s with { ShaderId = "plasma", Speed = 3 });

        Assert.Equal(1, _display.ShowCount);
    }

    // Pausa

    [Fact(DisplayName = "fica pausado quando o usuário pausa")]
    public void PausedByUser()
    {
        var manager = MakeManager();
        manager.Update(s => s with { IsPaused = true });
        Assert.True(manager.EffectivePaused);
    }

    [Fact(DisplayName = "pausa quando o sistema suspende ou bloqueia e volta depois")]
    public void PausedWhileSystemSuspended()
    {
        var manager = MakeManager();
        Assert.False(manager.EffectivePaused);

        manager.SystemDidSuspend();
        Assert.True(manager.EffectivePaused);

        manager.SystemDidResume();
        Assert.False(manager.EffectivePaused);
    }

    [Fact(DisplayName = "o playback junta a pausa efetiva e a velocidade")]
    public void PlaybackCombinesPauseAndSpeed()
    {
        var manager = MakeManager();
        manager.Update(s => s with { Speed = 2 });
        manager.SystemDidSuspend();

        Assert.Equal(new Playback(Paused: true, Speed: 2), manager.Playback);
    }

    // Vídeo

    [Fact(DisplayName = "importar um vídeo passa a usá-lo como wallpaper")]
    public void ImportVideoSelectsIt()
    {
        var manager = MakeManager();

        manager.ImportVideo(_source.MakeFile("praia.mp4"));

        Assert.Equal(_videos.File("praia.mp4"), manager.VideoPath);
        Assert.Equal(SourceIds.Video, manager.Settings.SourceId);
        Assert.Equal("praia.mp4", manager.Settings.VideoFileName);
        Assert.Null(manager.LastError);
    }

    [Fact(DisplayName = "falha ao importar mostra um erro e não troca o vídeo")]
    public void ImportFailureSetsError()
    {
        var manager = MakeManager();

        manager.ImportVideo(_source.File("nao-existe.mp4"));

        Assert.NotNull(manager.LastError);
        Assert.Null(manager.VideoPath);
        Assert.Equal(SourceIds.Shader, manager.Settings.SourceId);
    }

    [Fact(DisplayName = "um import que dá certo limpa o erro anterior")]
    public void SuccessClearsError()
    {
        var manager = MakeManager();
        manager.ImportVideo(_source.File("nao-existe.mp4"));

        manager.ImportVideo(_source.MakeFile("praia.mp4"));

        Assert.Null(manager.LastError);
    }

    [Fact(DisplayName = "ao abrir, recupera o vídeo salvo")]
    public void RestoresSavedVideo()
    {
        MakeManager().ImportVideo(_source.MakeFile("praia.mp4"));
        Assert.Equal(_videos.File("praia.mp4"), MakeManager().VideoPath);
    }

    [Fact(DisplayName = "se o vídeo salvo sumiu, fica sem vídeo")]
    public void MissingSavedVideo()
    {
        SaveSettings(s => s with { VideoFileName = "sumiu.mp4" });
        Assert.Null(MakeManager().VideoPath);
    }
}
