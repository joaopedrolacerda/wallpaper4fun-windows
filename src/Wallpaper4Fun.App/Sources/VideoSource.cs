using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

/// <summary>Toca um vídeo mudo em loop, preenchendo a tela.</summary>
internal sealed class VideoWallpaperView : ContentControl
{
    private readonly MediaElement _player = new()
    {
        LoadedBehavior = MediaState.Manual,
        UnloadedBehavior = MediaState.Close,
        IsMuted = true,
        Stretch = Stretch.UniformToFill,
    };
    private string? _path;

    public VideoWallpaperView()
    {
        Background = Brushes.Black;
        Content = _player;
        _player.MediaEnded += (_, _) => Restart();
    }

    public void Apply(string? path, Playback playback)
    {
        Load(path);
        ApplyPlayback(playback);
    }

    private void Load(string? path)
    {
        if (path == _path) return;
        _path = path;
        _player.Source = path is null ? null : new Uri(path);
    }

    private void ApplyPlayback(Playback playback)
    {
        if (_path is null) return;
        _player.SpeedRatio = playback.Speed;
        if (playback.Paused) _player.Pause();
        else _player.Play();
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
