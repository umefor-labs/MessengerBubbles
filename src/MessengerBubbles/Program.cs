using System.Windows;
using MessengerBubbles.Core;
using Microsoft.Web.WebView2.Core;

namespace MessengerBubbles;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "MessengerBubbles.SingleInstance", out var first);
        if (!first) return;

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.DispatcherUnhandledException += (_, e) =>
        {
            AppPaths.Log($"unhandled: {e.Exception.GetType().Name}");
            e.Handled = true;
        };
        app.Startup += async (_, _) => await StartAsync(app);
        app.Run();
    }

    static async Task StartAsync(Application app)
    {
        var settings = Settings.Load(AppPaths.SettingsFile);
        var watcher = new Watcher();
        var panel = new ChatPanel(settings);
        var bubbles = new BubbleController(settings, watcher.GetLoadedResourceAsync, panel.IsShowingAsync, () => panel.ScreenBox);
        TrayIcon? tray = null;
        tray = new TrayIcon(settings, watcher.ShowWindow, bubbles.Refresh, () =>
        {
            tray?.Dispose();
            watcher.Exit();
            app.Shutdown();
        });

        watcher.Scanned += bubbles.Apply;
        watcher.NeedsLogin += tray.SetLogin;
        watcher.HealthChanged += tray.SetBroken;
        bubbles.BubbleClicked += panel.Toggle;
        panel.LayoutBroken += tray.NotifyPanelBroken;
        panel.LeftOnOtherThread += (from, path) =>
        {
            if (watcher.FindRow(path) is { } row) bubbles.SwitchTo(from, row);
        };
        bubbles.Refresh();

        try
        {
            await watcher.StartAsync(await AppPaths.CreateEnvironmentAsync());
            panel.Preload();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show("Máy chưa có WebView2 Runtime, app không chạy được.", "Messenger Bubbles");
            tray.Dispose();
            app.Shutdown();
        }
    }
}
