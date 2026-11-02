using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ChatBubbles.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Geometry = ChatBubbles.Core.Geometry;

namespace ChatBubbles;

// Keeps one Facebook page loaded and suspended, so a bubble opens without a reload.
class ChatPanel : Window
{
    const double PanelW = 380, PanelH = 560;
    const uint SwpNoZOrder = 0x0004;
    const uint MonitorDefaultToNearest = 2;
    const int OpenTries = 40;
    static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    [StructLayout(LayoutKind.Sequential)] struct Point32 { public int X, Y; }

    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(Point32 point, uint flags);
    [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);

    WebView2 _web = new();
    Task? _init;
    Task? _suspending;
    string? _openHref;
    string? _loadedHref;
    Box? _bubble;

    public ChatPanel()
    {
        Width = PanelW;
        Height = PanelH;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        BorderBrush = Brushes.Gray;
        BorderThickness = new Thickness(1);
        Content = _web;
        Deactivated += (_, _) => HidePanel();
    }

    // Loads the page off-screen at startup; the control needs a shown window to initialize.
    public async void Preload()
    {
        Left = -32000;
        Top = -32000;
        Show();
        try
        {
            await (_init ??= InitAsync());
            for (var i = 0; i < OpenTries * 2 && _openHref == null; i++)
            {
                if (await RunAsync("document.querySelector('a[href*=\"/messages/t/\"],a[href*=\"/messages/e2ee/t/\"]') !== null") == "true") break;
                await Task.Delay(RetryDelay);
            }
        }
        catch (Exception e) when (e is not WebView2RuntimeNotFoundException)
        {
            AppPaths.Log($"panel preload failed: {e.GetType().Name}");
            _init = null;
        }
        if (_openHref != null) return;
        Hide();
        _suspending = SuspendAsync();
    }

    public async void Toggle(string threadHref, Box bubble)
    {
        if (IsVisible && _openHref == threadHref)
        {
            HidePanel();
            return;
        }
        var url = threadHref == "" ? AppPaths.MessagesUrl : FacebookUrl.ToThreadUrl(threadHref);
        if (url == null)
        {
            AppPaths.Log("rejected thread link");
            return;
        }

        _bubble = bubble;
        _openHref = threadHref;
        Show();
        PlaceBesideBubble();
        Activate();

        try
        {
            if (_suspending != null) await _suspending;
            await (_init ??= InitAsync());
            var core = _web.CoreWebView2;
            if (core.IsSuspended) core.Resume();
            // The user may have clicked away or chosen another bubble while waiting.
            if (!IsVisible || _openHref != threadHref) return;
            if (threadHref == "")
            {
                if (_loadedHref != null) core.Navigate(url);
                _loadedHref = null;
            }
            else
            {
                // Always click: the user may have gone back to the list inside the panel.
                await OpenThreadAsync(threadHref, url);
            }
            _web.Focus();
            await RunAsync("window.__cbFocus?.(); 0");
        }
        catch (Exception e) when (e is COMException or InvalidOperationException or JsonException)
        {
            AppPaths.Log($"panel open failed: {e.GetType().Name}");
            _init = null;
            HidePanel();
        }
    }

    public async Task<bool> IsShowingAsync(string href)
    {
        if (!IsVisible || _init?.IsCompletedSuccessfully != true) return false;
        try
        {
            return await RunAsync($"window.__cbShowing?.({JsonSerializer.Serialize(href)}) === true") == "true";
        }
        catch (Exception e) when (e is COMException or InvalidOperationException)
        {
            AppPaths.Log($"panel state failed: {e.GetType().Name}");
            return false;
        }
    }

    async Task OpenThreadAsync(string href, string url)
    {
        var arg = JsonSerializer.Serialize(href);
        var navigated = false;
        for (var i = 0; i < OpenTries && _openHref == href; i++)
        {
            var status = await RunAsync($"window.__cbOpen ? window.__cbOpen({arg}) : 'wait'");
            if (status is "\"clicked\"" or "\"pushed\"")
            {
                _loadedHref = href;
                return;
            }
            if (status == "\"off-page\"" && !navigated)
            {
                navigated = true;
                _web.CoreWebView2.Navigate(AppPaths.MessagesUrl);
            }
            await Task.Delay(RetryDelay);
        }
        if (_openHref != href) return;
        AppPaths.Log("thread row not found, opening by url");
        _web.CoreWebView2.Navigate(url);
        _loadedHref = href;
    }

    Task<string> RunAsync(string js) => _web.CoreWebView2.ExecuteScriptAsync(js);

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Dispatcher.InvokeAsync(PlaceBesideBubble);
    }

    void PlaceBesideBubble()
    {
        if (_bubble is not { } bubble) return;
        var monitor = MonitorFromPoint(new Point32 { X = (int)bubble.X, Y = (int)bubble.Y }, MonitorDefaultToNearest);
        GetDpiForMonitor(monitor, 0, out var dpi, out _);
        var scale = dpi / 96.0;
        var area = FloatingWindow.WorkAreaAt(bubble.X, bubble.Y);
        var box = Geometry.PlacePanel(bubble, PanelW * scale, PanelH * scale, area);
        SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero, (int)box.X, (int)box.Y, (int)box.W, (int)box.H, SwpNoZOrder);
    }

    async Task InitAsync()
    {
        await _web.EnsureCoreWebView2Async(await AppPaths.CreateEnvironmentAsync());
        var core = _web.CoreWebView2;
        AppPaths.Harden(core);
        core.ProcessFailed += OnProcessFailed;
        _web.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) HidePanel();
        };
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("panel.js")!;
        await core.AddScriptToExecuteOnDocumentCreatedAsync(new StreamReader(stream).ReadToEnd());
        core.Navigate(AppPaths.MessagesUrl);
    }

    void HidePanel()
    {
        if (!IsVisible) return;
        _openHref = null;
        Hide();
        _suspending = SuspendAsync();
    }

    // A suspended page runs nothing, so a thread left open cannot mark incoming messages as read.
    async Task SuspendAsync()
    {
        if (_init?.IsCompletedSuccessfully != true) return;
        await Task.Delay(300);
        var core = _web.CoreWebView2;
        if (IsVisible || core == null || core.IsSuspended) return;
        try
        {
            if (!await core.TrySuspendAsync()) AppPaths.Log("panel suspend refused");
        }
        catch (Exception e) when (e is COMException or InvalidOperationException)
        {
            AppPaths.Log($"panel suspend failed: {e.GetType().Name}");
        }
    }

    void OnProcessFailed(object? _, CoreWebView2ProcessFailedEventArgs e)
    {
        if (e.ProcessFailedKind != CoreWebView2ProcessFailedKind.BrowserProcessExited) return;
        AppPaths.Log("panel browser process exited");
        Dispatcher.InvokeAsync(() =>
        {
            Hide();
            _openHref = null;
            _loadedHref = null;
            _init = null;
            _suspending = null;
            _web.Dispose();
            _web = new WebView2();
            Content = _web;
        });
    }
}
