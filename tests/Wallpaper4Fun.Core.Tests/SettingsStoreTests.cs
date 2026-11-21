namespace Wallpaper4Fun.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private SettingsStore Store => new(_folder.File("settings.json"));

    public void Dispose() => _folder.Dispose();

    [Fact(DisplayName = "sem nada salvo, carrega as configurações padrão")]
    public void LoadsDefaultsWhenEmpty() => Assert.Equal(WallpaperSettings.Default, Store.Load());

    [Fact(DisplayName = "o padrão é shader Aurora, ativo, sem pausa, velocidade 1 e indo pra bandeja ao fechar")]
    public void DefaultValues()
    {
        var settings = WallpaperSettings.Default;
        Assert.Equal(SourceIds.Shader, settings.SourceId);
        Assert.Equal("aurora", settings.ShaderId);
        Assert.True(settings.IsEnabled);
        Assert.False(settings.IsPaused);
        Assert.Equal(1, settings.Speed);
        Assert.True(settings.HideToTrayWhenClosed);
        Assert.Null(settings.VideoFileName);
    }

    [Fact(DisplayName = "salvar e carregar devolve as mesmas configurações")]
    public void RoundTrip()
    {
        var settings = WallpaperSettings.Default with
        {
            SourceId = SourceIds.Video,
            ShaderId = "plasma",
            Speed = 2.5,
            IsPaused = true,
            VideoFileName = "praia.mp4",
        };

        Store.Save(settings);

        Assert.Equal(settings, Store.Load());
    }

    [Fact(DisplayName = "salvar cria a pasta se ela não existir")]
    public void SaveCreatesFolder()
    {
        var store = new SettingsStore(_folder.File(Path.Combine("nova", "settings.json")));
        store.Save(WallpaperSettings.Default);
        Assert.Equal(WallpaperSettings.Default, store.Load());
    }

    [Fact(DisplayName = "arquivo corrompido volta para o padrão")]
    public void CorruptedFileFallsBackToDefault()
    {
        File.WriteAllText(_folder.File("settings.json"), "lixo{");
        Assert.Equal(WallpaperSettings.Default, Store.Load());
    }
}
