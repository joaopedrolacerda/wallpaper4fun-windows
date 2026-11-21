using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App.Sources;

public sealed class VideoSource(IBundledVideoLocator locator) : IWallpaperSource
{
    private const string VideoFilter = "Vídeos|*.mp4;*.m4v;*.mov;*.wmv;*.avi;*.mkv";

    public string Id => SourceIds.Video;
    public string Title => "Vídeo";

    public FrameworkElement CreateWallpaper(WallpaperManager manager) =>
        new VideoWallpaperView().BindTo(manager, view => view.Apply(manager.VideoPath, manager.Playback));

    public FrameworkElement CreateControls(WallpaperManager manager) =>
        new VideoGallery(manager, locator, () => ChooseVideo(manager));

    private static void ChooseVideo(WallpaperManager manager)
    {
        var dialog = new OpenFileDialog { Filter = VideoFilter, Title = "Escolha um vídeo para usar como wallpaper" };
        if (dialog.ShowDialog() == true) manager.ImportVideo(dialog.FileName);
    }
}

/// <summary>
/// Toca um vídeo mudo em loop, preenchendo a tela. O player do WPF pode congelar depois que um
/// jogo em tela cheia toma a placa de vídeo; por isso o vídeo é recarregado ao voltar de uma
/// pausa e sempre que o <see cref="StallDetector"/> percebe que ele parou sozinho.
/// </summary>
internal sealed class VideoWallpaperView : ContentControl
{
    private readonly MediaElement _player = new()
    {
        LoadedBehavior = MediaState.Manual,
        UnloadedBehavior = MediaState.Close,
        IsMuted = true,
        Stretch = Stretch.UniformToFill,
    };
    private readonly DispatcherTimer _watchdog = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly StallDetector _stall = new();
    private string? _path;
    private Playback _playback = new(Paused: true, Speed: 1);
    private TimeSpan _resumeAt;
    private bool _reportedGiveUp;

    public VideoWallpaperView()
    {
        Background = Brushes.Black;
        Content = _player;
        _player.MediaEnded += (_, _) => Restart();
        _player.MediaOpened += (_, _) => _player.Position = _resumeAt;
        _player.MediaFailed += (_, e) => StartupTrace.Step($"vídeo falhou: {e.ErrorException}");
        _watchdog.Tick += (_, _) => CheckForStall();
        Loaded += (_, _) => _watchdog.Start();
        Unloaded += (_, _) => _watchdog.Stop();
    }

    public void Apply(string? path, Playback playback)
    {
        var resuming = _playback.Paused && !playback.Paused && path == _path;
        _playback = playback;
        if (resuming) Reload();
        else Load(path);
        ApplyPlayback();
    }

    private void Load(string? path)
    {
        if (path == _path) return;
        _path = path;
        _resumeAt = TimeSpan.Zero;
        _player.Source = path is null ? null : new Uri(path);
    }

    /// <summary>Abre o vídeo de novo e continua de onde estava.</summary>
    private void Reload()
    {
        if (_path is null) return;
        _resumeAt = _player.Position;
        _player.Close();
        _player.Source = new Uri(_path);
    }

    private void ApplyPlayback()
    {
        if (_path is null) return;
        _player.SpeedRatio = _playback.Speed;
        if (_playback.Paused) _player.Pause();
        else _player.Play();
    }

    private void CheckForStall()
    {
        var shouldBePlaying = _path is not null && !_playback.Paused;
        var stalled = _stall.Check(_player.Position, DateTimeOffset.Now, shouldBePlaying);
        ReportGiveUpOnce();
        if (!stalled) return;
        StartupTrace.Step($"vídeo travado em {_player.Position:mm\\:ss}; recarregando {Path.GetFileName(_path)}");
        Reload();
        ApplyPlayback();
    }

    private void ReportGiveUpOnce()
    {
        if (_stall.GaveUp == _reportedGiveUp) return;
        _reportedGiveUp = _stall.GaveUp;
        if (_reportedGiveUp) StartupTrace.Step($"o vídeo {Path.GetFileName(_path)} não anda nem recarregando; parei de tentar");
    }

    private void Restart()
    {
        _player.Position = TimeSpan.Zero;
        _player.Play();
    }
}

/// <summary>Vídeos do app em miniatura, o vídeo importado e o botão para escolher outro arquivo.</summary>
internal sealed class VideoGallery : StackPanel
{
    private readonly WallpaperManager _manager;
    private readonly IBundledVideoLocator _locator;

    public VideoGallery(WallpaperManager manager, IBundledVideoLocator locator, Action onChooseFile)
    {
        _manager = manager;
        _locator = locator;
        Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = Tiles(),
        });
        Children.Add(ChooseFileButton(onChooseFile));
    }

    private StackPanel Tiles()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var video in VideoCatalog.All) row.Children.Add(BundledTile(video));
        row.Children.Add(ImportedTile());
        return row;
    }

    private GalleryTile BundledTile(BundledVideo video) =>
        new GalleryTile(video.Title, Thumbnail(video), () => _manager.SelectBundledVideo(video.Id))
            .BindTo(_manager, tile => tile.IsSelected = _manager.Settings.BundledVideoId == video.Id);

    /// <summary>Só aparece depois que o usuário importou um vídeo.</summary>
    private GalleryTile ImportedTile() =>
        new GalleryTile("", new TextBlock { Text = "🎞", FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                _manager.SelectImportedVideo)
            .BindTo(_manager, tile =>
            {
                var path = _manager.ImportedVideoPath;
                tile.Visibility = path is null ? Visibility.Collapsed : Visibility.Visible;
                tile.Title = path is null ? "" : Path.GetFileNameWithoutExtension(path);
                tile.IsSelected = _manager.Settings.BundledVideoId is null;
            });

    private UIElement Thumbnail(BundledVideo video) =>
        _locator.ThumbnailPath(video) is { } path
            ? new Image { Source = new BitmapImage(new Uri(path)), Stretch = Stretch.UniformToFill }
            : new Border { Background = Brushes.Black };

    private static Button ChooseFileButton(Action onChooseFile)
    {
        var button = new Button { Content = "Escolher outro vídeo…", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => onChooseFile();
        return button;
    }
}

/// <summary>Uma miniatura clicável com título e borda de seleção.</summary>
internal sealed class GalleryTile : Button
{
    private readonly Border _frame;
    private readonly TextBlock _title = new() { FontSize = 11, Width = 96, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };

    public GalleryTile(string title, UIElement preview, Action onClick)
    {
        _frame = new Border { Width = 96, Height = 54, CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(3), ClipToBounds = true, Child = preview };
        Title = title;
        Content = Stack(_frame, _title);
        Margin = new Thickness(0, 0, 10, 0);
        Background = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Click += (_, _) => onClick();
        IsSelected = false;
    }

    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    public bool IsSelected
    {
        set => _frame.BorderBrush = value ? SystemColors.HighlightBrush : Brushes.Transparent;
    }

    private static StackPanel Stack(params UIElement[] children)
    {
        var stack = new StackPanel();
        foreach (var child in children) stack.Children.Add(child);
        return stack;
    }
}
