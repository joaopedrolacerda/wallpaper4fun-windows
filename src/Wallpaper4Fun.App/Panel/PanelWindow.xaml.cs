using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Wallpaper4Fun.App.Sources;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App.Panel;

/// <summary>Painel de controle. Só monta a interface; as regras ficam no <see cref="WallpaperManager"/>.</summary>
public partial class PanelWindow : Window
{
    private readonly WallpaperManager _manager;
    private readonly SourceRegistry<IWallpaperSource> _registry;
    private readonly Action _quit;
    private string? _shownControlsId;
    private bool _allowClose;

    public PanelWindow(WallpaperManager manager, SourceRegistry<IWallpaperSource> registry, Action quit)
    {
        _manager = manager;
        _registry = registry;
        _quit = quit;
        InitializeComponent();
        AddSourceButtons();
        AddHideShortcut();
        _manager.Changed += (_, _) => Refresh();
        IsVisibleChanged += (_, _) => OnVisibilityChanged();
        Refresh();
        // Só depois de carregar os valores: o slider dispara ValueChanged ao aplicar o mínimo,
        // o que sobrescreveria a velocidade salva.
        SpeedSlider.ValueChanged += OnSpeedChanged;
    }

    /// <summary>Deixa a janela fechar de verdade (usado ao sair do app).</summary>
    public void AllowClose() => _allowClose = true;

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (_allowClose) return;
        e.Cancel = true;
        ApplyClosePolicy();
    }

    private void ApplyClosePolicy()
    {
        if (ClosePolicy.Decide(_manager.Settings.HideToTrayWhenClosed) == PanelCloseAction.HideToTray) Hide();
        else _quit();
    }

    /// <summary>
    /// Escondido na bandeja, o painel solta a prévia e as miniaturas: um vídeo ou shader
    /// tocando onde ninguém vê só disputa CPU e placa de vídeo com o wallpaper de verdade.
    /// </summary>
    private void OnVisibilityChanged()
    {
        PreviewFrame.Child = IsVisible ? new WallpaperHost(_manager, _registry) : null;
        _shownControlsId = null;
        if (IsVisible) RefreshSourceControls();
        else SourceControls.Content = null;
    }

    // Montagem

    private void AddSourceButtons()
    {
        foreach (var source in _registry.Sources) SourceButtons.Children.Add(SourceButton(source));
    }

    private ToggleButton SourceButton(IWallpaperSource source)
    {
        var button = new ToggleButton { Content = source.Title, Tag = source.Id, Padding = new Thickness(16, 4, 16, 4), Margin = new Thickness(0, 0, 8, 0) };
        button.Click += (_, _) => _manager.Update(s => s with { SourceId = source.Id });
        return button;
    }

    private void AddHideShortcut()
    {
        var hide = new RoutedCommand();
        CommandBindings.Add(new CommandBinding(hide, (_, _) => Hide()));
        InputBindings.Add(new KeyBinding(hide, Key.W, ModifierKeys.Control));
    }

    // Estado → interface

    private void Refresh()
    {
        var settings = _manager.Settings;
        RefreshSourceButtons(settings.SourceId);
        RefreshSourceControls();
        EnabledBox.IsChecked = settings.IsEnabled;
        PausedBox.IsChecked = settings.IsPaused;
        TrayBox.IsChecked = settings.HideToTrayWhenClosed;
        SpeedSlider.Value = settings.Speed;
        DisabledBadge.Visibility = settings.IsEnabled ? Visibility.Collapsed : Visibility.Visible;
        ErrorText.Text = _manager.LastError ?? "";
    }

    private void RefreshSourceButtons(string selectedId)
    {
        foreach (var button in SourceButtons.Children.OfType<ToggleButton>())
            button.IsChecked = (string)button.Tag == _registry.Source(selectedId).Id;
    }

    /// <summary>Cada tipo de wallpaper traz os próprios controles (open-closed).</summary>
    private void RefreshSourceControls()
    {
        var source = _registry.Source(_manager.Settings.SourceId);
        if (source.Id == _shownControlsId) return;
        _shownControlsId = source.Id;
        SourceControls.Content = source.CreateControls(_manager);
    }

    // Interface → estado

    private void OnEnabledClick(object sender, RoutedEventArgs e) => Update(s => s with { IsEnabled = EnabledBox.IsChecked == true });
    private void OnPausedClick(object sender, RoutedEventArgs e) => Update(s => s with { IsPaused = PausedBox.IsChecked == true });
    private void OnTrayClick(object sender, RoutedEventArgs e) => Update(s => s with { HideToTrayWhenClosed = TrayBox.IsChecked == true });
    private void OnHideClick(object sender, RoutedEventArgs e) => Hide();

    private void OnSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (e.NewValue == _manager.Settings.Speed) return;
        Update(s => s with { Speed = e.NewValue });
    }

    private void Update(Func<WallpaperSettings, WallpaperSettings> change) => _manager.Update(change);
}
