using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace CodexPulse;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private bool _isShuttingDown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();
        _mainWindow.Closed += (_, _) => ShutdownApplication();

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Codex Pulse",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ToggleWindow();

        _mainWindow.Show();
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show Codex Pulse", null, (_, _) => ShowWindow());
        menu.Items.Add("Refresh limits", null, async (_, _) =>
        {
            if (_mainWindow is not null)
            {
                await _mainWindow.RefreshAsync();
            }
        });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ShutdownApplication());
        return menu;
    }

    private void ToggleWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void ShutdownApplication()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        _trayIcon?.Dispose();
        _trayIcon = null;
        _mainWindow?.DisposeClient();
        Shutdown();
    }
}
