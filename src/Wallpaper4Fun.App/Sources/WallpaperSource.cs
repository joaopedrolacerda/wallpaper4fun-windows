using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App.Sources;

/// <summary>
/// Um tipo de wallpaper com a sua parte visual. Open-closed: para criar um tipo novo,
/// escreva uma classe que implementa esta interface e registre em <see cref="AppSources.Standard"/>.
/// Nenhuma janela, painel ou <c>switch</c> existente precisa mudar.
/// </summary>
public interface IWallpaperSource : IWallpaperSourceInfo
{
    /// <summary>O que aparece na tela (e na prévia do painel). Deve se atualizar sozinho com o manager.</summary>
    FrameworkElement CreateWallpaper(WallpaperManager manager);

    /// <summary>Controles específicos deste tipo, mostrados no painel.</summary>
    FrameworkElement CreateControls(WallpaperManager manager);
}

/// <summary>Único ponto de registro dos tipos de wallpaper.</summary>
public static class AppSources
{
    public static SourceRegistry<IWallpaperSource> Standard { get; } = new([
        new ShaderSource(),
        new VideoSource(AppPaths.BundledVideos),
    ]);
}

/// <summary>Mostra o wallpaper do tipo escolhido e troca quando o tipo muda.</summary>
public sealed class WallpaperHost : ContentControl
{
    private readonly WallpaperManager _manager;
    private readonly SourceRegistry<IWallpaperSource> _registry;
    private string? _shownSourceId;

    public WallpaperHost(WallpaperManager manager, SourceRegistry<IWallpaperSource> registry)
    {
        _manager = manager;
        _registry = registry;
        Background = Brushes.Black;
        this.BindTo(manager, _ => ShowCurrentSource());
    }

    private void ShowCurrentSource()
    {
        var source = _registry.Source(_manager.Settings.SourceId);
        if (source.Id == _shownSourceId) return;
        _shownSourceId = source.Id;
        Content = source.CreateWallpaper(_manager);
    }
}

public static class ManagerBinding
{
    /// <summary>
    /// Aplica o estado do manager no elemento agora e a cada mudança, enquanto ele estiver
    /// na tela. Um lugar só para esse padrão (DRY), sem vazar inscrições de eventos.
    /// </summary>
    public static T BindTo<T>(this T element, WallpaperManager manager, Action<T> apply) where T : FrameworkElement
    {
        void OnChanged(object? sender, EventArgs e) => apply(element);
        element.Loaded += (_, _) => Subscribe(manager, OnChanged, () => apply(element));
        element.Unloaded += (_, _) => manager.Changed -= OnChanged;
        apply(element);
        return element;
    }

    private static void Subscribe(WallpaperManager manager, EventHandler handler, Action applyNow)
    {
        manager.Changed -= handler;
        manager.Changed += handler;
        applyNow();
    }
}
