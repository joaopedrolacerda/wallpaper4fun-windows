using Wallpaper4Fun.Core;
using Forms = System.Windows.Forms;

namespace Wallpaper4Fun.App;

/// <summary>Ícone na bandeja do sistema (perto do relógio) com os atalhos do app.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly WallpaperManager _manager;
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _enabledItem;
    private readonly Forms.ToolStripMenuItem _pausedItem;

    public TrayIcon(WallpaperManager manager, Action openPanel, Action quit)
    {
        _manager = manager;
        _enabledItem = MenuItem("Wallpaper ativo", () => Toggle(s => s with { IsEnabled = !s.IsEnabled }));
        _pausedItem = MenuItem("Pausar", () => Toggle(s => s with { IsPaused = !s.IsPaused }));
        _icon = CreateIcon(BuildMenu(openPanel, quit), openPanel);
        _manager.Changed += OnManagerChanged;
        Refresh();
    }

    public void Dispose()
    {
        _manager.Changed -= OnManagerChanged;
        _icon.Visible = false;
        _icon.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu(Action openPanel, Action quit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.AddRange([
            _enabledItem,
            _pausedItem,
            new Forms.ToolStripSeparator(),
            MenuItem("Abrir painel…", openPanel),
            new Forms.ToolStripSeparator(),
            MenuItem("Sair", quit),
        ]);
        return menu;
    }

    private static Forms.NotifyIcon CreateIcon(Forms.ContextMenuStrip menu, Action openPanel)
    {
        var icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "wallpaper4fun",
            ContextMenuStrip = menu,
            Visible = true,
        };
        icon.DoubleClick += (_, _) => openPanel();
        return icon;
    }

    private static Forms.ToolStripMenuItem MenuItem(string text, Action onClick) =>
        new(text, image: null, onClick: (_, _) => onClick());

    private void Toggle(Func<WallpaperSettings, WallpaperSettings> change) => _manager.Update(change);

    private void OnManagerChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        _enabledItem.Checked = _manager.Settings.IsEnabled;
        _pausedItem.Checked = _manager.Settings.IsPaused;
        _pausedItem.Enabled = _manager.Settings.IsEnabled;
    }
}
