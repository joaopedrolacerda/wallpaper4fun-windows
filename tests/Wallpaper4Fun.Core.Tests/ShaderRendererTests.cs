using SkiaSharp;

namespace Wallpaper4Fun.Core.Tests;

public sealed class ShaderRendererTests
{
    private static readonly SKImageInfo Target = new(64, 40);

    private static SKBitmap Render(string shaderId, float time)
    {
        var bitmap = new SKBitmap(Target);
        using var canvas = new SKCanvas(bitmap);
        new ShaderRenderer().Render(canvas, Target, shaderId, time);
        return bitmap;
    }

    public static TheoryData<string> ShaderIds => [.. ShaderCatalog.All.Select(s => s.Id)];

    [Theory(DisplayName = "todo shader pinta a tela inteira, sem buracos transparentes")]
    [MemberData(nameof(ShaderIds))]
    public void PaintsWholeTarget(string id)
    {
        using var bitmap = Render(id, time: 1);
        Assert.All(bitmap.Pixels, pixel => Assert.Equal(255, pixel.Alpha));
    }

    [Theory(DisplayName = "a imagem muda com o tempo (anima)")]
    [MemberData(nameof(ShaderIds))]
    public void ChangesOverTime(string id)
    {
        using var early = Render(id, time: 0);
        using var later = Render(id, time: 5);
        Assert.NotEqual(early.Pixels, later.Pixels);
    }

    [Fact(DisplayName = "renderiza em resolução menor para economizar CPU")]
    public void RendersAtReducedScale() =>
        Assert.Equal(new SKSizeI(32, 20), ShaderRenderer.RenderSize(Target, scale: 0.5f));

    [Fact(DisplayName = "a resolução reduzida nunca chega a zero")]
    public void RenderSizeNeverZero() =>
        Assert.Equal(new SKSizeI(1, 1), ShaderRenderer.RenderSize(new SKImageInfo(1, 1), scale: 0.5f));
}
