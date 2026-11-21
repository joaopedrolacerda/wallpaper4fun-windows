using SkiaSharp;

namespace Wallpaper4Fun.Core;

/// <summary>
/// Desenha um shader do <see cref="ShaderCatalog"/> num canvas do Skia. Renderiza numa
/// resolução menor e amplia, porque o shader roda na CPU e o fundo é suave.
/// </summary>
public sealed class ShaderRenderer
{
    public const float DefaultScale = 0.5f;

    private readonly Dictionary<string, SKRuntimeEffect> _effects = [];

    public void Render(SKCanvas canvas, SKImageInfo target, string shaderId, float time, float scale = DefaultScale)
    {
        using var frame = RenderFrame(RenderSize(target, scale), shaderId, time);
        canvas.DrawImage(frame, target.Rect, new SKSamplingOptions(SKFilterMode.Linear));
    }

    public static SKSizeI RenderSize(SKImageInfo target, float scale) =>
        new(Math.Max(1, (int)(target.Width * scale)), Math.Max(1, (int)(target.Height * scale)));

    private SKImage RenderFrame(SKSizeI size, string shaderId, float time)
    {
        using var surface = SKSurface.Create(new SKImageInfo(size.Width, size.Height));
        using var shader = MakeShader(shaderId, size, time);
        using var paint = new SKPaint { Shader = shader };
        surface.Canvas.DrawRect(SKRect.Create(size.Width, size.Height), paint);
        return surface.Snapshot();
    }

    private SKShader MakeShader(string shaderId, SKSizeI size, float time)
    {
        var effect = Effect(shaderId);
        var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            ["iResolution"] = new[] { (float)size.Width, size.Height },
            ["iTime"] = time,
        };
        return effect.ToShader(uniforms);
    }

    /// <summary>Compila cada shader uma vez só e reaproveita.</summary>
    private SKRuntimeEffect Effect(string shaderId)
    {
        if (_effects.TryGetValue(shaderId, out var cached)) return cached;
        var effect = Compile(ShaderCatalog.Style(shaderId));
        _effects[shaderId] = effect;
        return effect;
    }

    private static SKRuntimeEffect Compile(ShaderStyle style) =>
        SKRuntimeEffect.CreateShader(style.Source, out var errors)
        ?? throw new InvalidOperationException($"Shader '{style.Id}' não compilou: {errors}");
}
