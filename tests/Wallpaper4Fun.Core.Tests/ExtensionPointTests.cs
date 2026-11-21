using SkiaSharp;

namespace Wallpaper4Fun.Core.Tests;

public sealed class SourceRegistryTests
{
    private sealed record FakeSource(string Id, string Title) : IWallpaperSourceInfo;

    private static readonly SourceRegistry<FakeSource> Registry = new([
        new FakeSource(SourceIds.Shader, "Shader"),
        new FakeSource(SourceIds.Video, "Vídeo"),
    ]);

    [Fact(DisplayName = "encontra o tipo pelo id")]
    public void FindsById() => Assert.Equal("Vídeo", Registry.Source(SourceIds.Video).Title);

    [Fact(DisplayName = "id desconhecido cai no primeiro tipo")]
    public void UnknownFallsBackToFirst() => Assert.Equal(SourceIds.Shader, Registry.Source("nao-existe").Id);

    [Fact(DisplayName = "um tipo novo funciona só sendo registrado")]
    public void ExtendsWithNewSource()
    {
        var extended = new SourceRegistry<FakeSource>([.. Registry.Sources, new FakeSource("web", "Página")]);
        Assert.Equal("Página", extended.Source("web").Title);
    }

    [Fact(DisplayName = "ids repetidos são recusados")]
    public void RejectsDuplicateIds() =>
        Assert.Throws<ArgumentException>(() => new SourceRegistry<FakeSource>([new("a", "A"), new("a", "B")]));

    [Fact(DisplayName = "registro vazio é recusado")]
    public void RejectsEmpty() => Assert.Throws<ArgumentException>(() => new SourceRegistry<FakeSource>([]));
}

public sealed class ShaderCatalogTests
{
    [Fact(DisplayName = "o catálogo tem Aurora, Ondas e Plasma")]
    public void StandardShaders() =>
        Assert.Equal(new[] { "aurora", "waves", "plasma" }, ShaderCatalog.All.Select(s => s.Id));

    [Fact(DisplayName = "os ids dos shaders não se repetem")]
    public void UniqueIds() => Assert.Equal(ShaderCatalog.All.Count, ShaderCatalog.All.Select(s => s.Id).Distinct().Count());

    [Fact(DisplayName = "todo shader tem um título")]
    public void TitlesNotEmpty() => Assert.All(ShaderCatalog.All, s => Assert.False(string.IsNullOrWhiteSpace(s.Title)));

    [Fact(DisplayName = "encontra o shader pelo id")]
    public void FindsById() => Assert.Equal("Plasma", ShaderCatalog.Style("plasma").Title);

    [Fact(DisplayName = "id desconhecido cai no primeiro shader")]
    public void UnknownFallsBackToFirst() => Assert.Equal(ShaderCatalog.All[0], ShaderCatalog.Style("nao-existe"));

    [Fact(DisplayName = "o shader padrão das configurações existe no catálogo")]
    public void DefaultSettingsShaderExists() =>
        Assert.Contains(ShaderCatalog.All, s => s.Id == WallpaperSettings.Default.ShaderId);

    public static TheoryData<string> ShaderIds => [.. ShaderCatalog.All.Select(s => s.Id)];

    [Theory(DisplayName = "todo shader do catálogo compila no Skia")]
    [MemberData(nameof(ShaderIds))]
    public void ShaderCompiles(string id)
    {
        using var effect = SKRuntimeEffect.CreateShader(ShaderCatalog.Style(id).Source, out var errors);
        Assert.True(effect is not null, errors);
        Assert.Equal(new[] { "iResolution", "iTime" }, effect!.Uniforms.Order());
    }
}
