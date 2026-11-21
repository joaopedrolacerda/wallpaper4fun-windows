namespace Wallpaper4Fun.Core.Tests;

public sealed class WallpaperManagerTests : IDisposable
{
    private readonly TemporaryFolder _config = new();
    private readonly TemporaryFolder _videos = new();
    private readonly TemporaryFolder _source = new();
    private readonly TemporaryFolder _bundled = new();
    private readonly FakeDisplay _display = new();

    private SettingsStore Store => new(_config.File("settings.json"));
    private WallpaperManager MakeManager() =>
        new(Store, new VideoLibrary(_videos.Path), new FolderVideoLocator(_bundled.Path), _display);

    public void Dispose()
    {
        _config.Dispose();
        _videos.Dispose();
        _source.Dispose();
        _bundled.Dispose();
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
        SaveSettings(s => s with { VideoFileName = "sumiu.mp4", BundledVideoId = null });
        Assert.Null(MakeManager().VideoPath);
    }

    // Vídeos que vêm com o app

    [Fact(DisplayName = "por padrão, o vídeo escolhido é o padrão do catálogo")]
    public void DefaultsToBundledVideo()
    {
        var expected = _bundled.MakeFile($"{WallpaperSettings.Default.BundledVideoId}.mp4");
        Assert.Equal(expected, MakeManager().VideoPath);
    }

    [Fact(DisplayName = "escolher um vídeo do app passa a usá-lo como wallpaper")]
    public void SelectBundledVideo()
    {
        var appa = _bundled.MakeFile("appa.mp4");
        var manager = MakeManager();

        manager.SelectBundledVideo("appa");

        Assert.Equal(appa, manager.VideoPath);
        Assert.Equal("appa", manager.Settings.BundledVideoId);
        Assert.Equal(SourceIds.Video, manager.Settings.SourceId);
    }

    [Fact(DisplayName = "importar um vídeo deixa de usar o vídeo do app")]
    public void ImportReplacesBundledChoice()
    {
        _bundled.MakeFile("appa.mp4");
        var manager = MakeManager();
        manager.SelectBundledVideo("appa");

        manager.ImportVideo(_source.MakeFile("praia.mp4"));

        Assert.Null(manager.Settings.BundledVideoId);
        Assert.Equal(_videos.File("praia.mp4"), manager.VideoPath);
    }

    [Fact(DisplayName = "voltar para um vídeo do app mantém o importado guardado")]
    public void BundledChoiceKeepsImportedFile()
    {
        _bundled.MakeFile("appa.mp4");
        var manager = MakeManager();
        manager.ImportVideo(_source.MakeFile("praia.mp4"));

        manager.SelectBundledVideo("appa");

        Assert.Equal(_videos.File("praia.mp4"), manager.ImportedVideoPath);
    }

    [Fact(DisplayName = "dá pra voltar para o vídeo importado")]
    public void SelectImportedVideoAgain()
    {
        _bundled.MakeFile("appa.mp4");
        var manager = MakeManager();
        manager.ImportVideo(_source.MakeFile("praia.mp4"));
        manager.SelectBundledVideo("appa");

        manager.SelectImportedVideo();

        Assert.Null(manager.Settings.BundledVideoId);
        Assert.Equal(_videos.File("praia.mp4"), manager.VideoPath);
    }

    [Fact(DisplayName = "se o vídeo do app não existe, fica sem vídeo")]
    public void MissingBundledFile()
    {
        var manager = MakeManager();
        manager.SelectBundledVideo("appa");
        Assert.Null(manager.VideoPath);
    }
}
