using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App.Sources;

public sealed class ShaderSource : IWallpaperSource
{
    public string Id => SourceIds.Shader;
    public string Title => "Shader";

    public FrameworkElement CreateWallpaper(WallpaperManager manager) =>
        new ShaderWallpaperView().BindTo(manager, view =>
        {
            view.ShaderId = manager.Settings.ShaderId;
            view.Playback = manager.Playback;
        });

    public FrameworkElement CreateControls(WallpaperManager manager) => new ShaderPicker(manager);
}

/// <summary>Desenha um shader do catálogo, animado a 30 fps.</summary>
public sealed class ShaderWallpaperView : SKElement
{
    /// <summary>30 fps é suave o bastante para um fundo e gasta bem menos CPU/bateria.</summary>
    public const double FramesPerSecond = 30;

    // Compartilhado: cada shader é compilado uma vez só. Usado só na thread da interface.
    private static readonly ShaderRenderer Renderer = new();

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1 / FramesPerSecond) };
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly ShaderClock _clock = new();
    private Playback _playback = new(Paused: false, Speed: 1);
    private string _shaderId = ShaderCatalog.DefaultId;
    private float _time;
    private bool _isOnScreen;

    public ShaderWallpaperView()
    {
        _timer.Tick += (_, _) => AdvanceFrame();
        Loaded += (_, _) => SetOnScreen(true);
        Unloaded += (_, _) => SetOnScreen(false);
    }

    public string ShaderId
    {
        get => _shaderId;
        set { _shaderId = value; InvalidateVisual(); }
    }

    public Playback Playback
    {
        get => _playback;
        set { _playback = value; UpdateTimer(); }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        Renderer.Render(e.Surface.Canvas, e.Info, _shaderId, _time);
    }

    private void AdvanceFrame()
    {
        _time = _clock.Tick(_stopwatch.Elapsed, _playback.Speed);
        InvalidateVisual();
    }

    private void SetOnScreen(bool onScreen)
    {
        _isOnScreen = onScreen;
        UpdateTimer();
    }

    private void UpdateTimer() => _timer.IsEnabled = _isOnScreen && !_playback.Paused;
}

/// <summary>Miniaturas animadas dos shaders; clicar escolhe o shader.</summary>
internal sealed class ShaderPicker : StackPanel
{
    private readonly WallpaperManager _manager;

    public ShaderPicker(WallpaperManager manager)
    {
        _manager = manager;
        Orientation = Orientation.Horizontal;
        foreach (var style in ShaderCatalog.All) Children.Add(Thumbnail(style));
    }

    private Button Thumbnail(ShaderStyle style)
    {
        var frame = new Border
        {
            Width = 140,
            Height = 80,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(8),
            Child = new ShaderWallpaperView { ShaderId = style.Id },
        };
        frame.BindTo(_manager, f => f.BorderBrush = IsSelected(style) ? SystemColors.HighlightBrush : Brushes.Transparent);
        return ThumbnailButton(style, frame);
    }

    private Button ThumbnailButton(ShaderStyle style, Border frame)
    {
        var content = new StackPanel();
        content.Children.Add(frame);
        content.Children.Add(new TextBlock { Text = style.Title, HorizontalAlignment = HorizontalAlignment.Center });
        var button = new Button { Content = content, Margin = new Thickness(0, 0, 12, 0), Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
        button.Click += (_, _) => _manager.Update(s => s with { ShaderId = style.Id });
        return button;
    }

    private bool IsSelected(ShaderStyle style) => style.Id == _manager.Settings.ShaderId;
}
