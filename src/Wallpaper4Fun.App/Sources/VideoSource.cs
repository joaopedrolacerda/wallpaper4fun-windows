using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App.Sources;

public sealed class VideoSource : IWallpaperSource
{
    private const string VideoFilter = "Vídeos|*.mp4;*.m4v;*.mov;*.wmv;*.avi;*.mkv";

    public string Id => SourceIds.Video;
    public string Title => "Vídeo";

    public FrameworkElement CreateWallpaper(WallpaperManager manager) =>
        new VideoWallpaperView().BindTo(manager, view => view.Apply(manager.VideoPath, manager.Playback));

    public FrameworkElement CreateControls(WallpaperManager manager) =>
        new VideoControls(() => ChooseVideo(manager)).BindTo(manager, row => row.ShowVideo(manager.VideoPath));

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

/// <summary>Nome do vídeo atual e o botão para escolher outro.</summary>
internal sealed class VideoControls : DockPanel
{
    private readonly TextBlock _name = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };

    public VideoControls(Action onChoose)
    {
        var button = new Button { Content = "Escolher vídeo…", Padding = new Thickness(12, 4, 12, 4) };
        button.Click += (_, _) => onChoose();
        SetDock(button, Dock.Right);
        Children.Add(button);
        Children.Add(_name);
    }

    public void ShowVideo(string? path)
    {
        _name.Text = path is null ? "🎞  Nenhum vídeo escolhido" : $"🎞  {Path.GetFileName(path)}";
        _name.Foreground = path is null ? SystemColors.GrayTextBrush : SystemColors.ControlTextBrush;
    }
}
