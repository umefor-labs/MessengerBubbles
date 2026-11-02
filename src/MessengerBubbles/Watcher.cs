using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using MessengerBubbles.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MessengerBubbles;

class Watcher : Window
{
    static readonly TimeSpan SilenceLimit = TimeSpan.FromSeconds(60);

    readonly NewMessageDetector _detector = new();
    readonly RowsHealth _health = new();
    readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromSeconds(30) };
    readonly DispatcherTimer _reload = new();
    WebView2 _web = new();
    CoreWebView2Environment? _env;
    DateTime _lastPost = DateTime.Now;
    int _lastRowCount;
    List<RowSnapshot> _lastRows = [];
    int _attempt;
    bool _broken, _needsLogin, _exiting;

    public event Action<ScanResult>? Scanned;
    public event Action<bool>? NeedsLogin;
    public event Action<bool>? HealthChanged;

    public Watcher()
    {
        Title = "Messenger Bubbles: đăng nhập Facebook, xong thì đóng cửa sổ này";
        Width = 1100;
        Height = 800;
        ShowInTaskbar = false;
        ShowActivated = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Content = _web;
        _tick.Tick += (_, _) => CheckHealth();
        _reload.Tick += async (_, _) =>
        {
            _reload.Stop();
            if (_web.CoreWebView2 == null) await AttachAsync();
            else _web.CoreWebView2.Navigate(AppPaths.MessagesUrl);
        };
    }

    public async Task StartAsync(CoreWebView2Environment env)
    {
        _env = env;
        _tick.Start();
        await AttachAsync();
    }

    public void ShowWindow()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
        ShowInTaskbar = true;
        Show();
        Activate();
    }

    public void Exit()
    {
        _exiting = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_exiting) return;
        // Closing only hides: the page keeps watching. Tray "Thoát" ends the app.
        e.Cancel = true;
        ShowInTaskbar = false;
        Hide();
        // A thread the user left open would mark incoming messages as read.
        var core = _web.CoreWebView2;
        if (core != null && new Uri(core.Source).AbsolutePath.TrimEnd('/') != "/messages")
            core.Navigate(AppPaths.MessagesUrl);
    }

    // The control needs a shown window to initialize, so a hidden watcher appears off-screen for the duration.
    async Task AttachAsync()
    {
        var wasHidden = !IsVisible;
        if (wasHidden)
        {
            Left = -32000;
            Top = -32000;
            Show();
        }
        try
        {
            _env ??= await AppPaths.CreateEnvironmentAsync();
            await _web.EnsureCoreWebView2Async(_env);
            var core = _web.CoreWebView2;
            core.IsMuted = true;
            AppPaths.Harden(core);
            core.WebMessageReceived += OnMessage;
            core.NavigationStarting += (_, _) => _detector.Reset();
            core.NavigationCompleted += OnNavigationCompleted;
            core.ProcessFailed += OnProcessFailed;
            await core.AddScriptToExecuteOnDocumentCreatedAsync(ReadProbe());
            core.Navigate(AppPaths.MessagesUrl);
        }
        catch (Exception e) when (e is not WebView2RuntimeNotFoundException)
        {
            AppPaths.Log($"attach failed: {e.GetType().Name}");
            ReplaceWebView();
            ScheduleReload();
        }
        if (wasHidden) Hide();
    }

    // Takes the image the page already loaded, so nothing is fetched again or cached on disk.
    public async Task<byte[]?> GetLoadedResourceAsync(string url)
    {
        var core = _web.CoreWebView2;
        if (core == null || url == "") return null;
        try
        {
            await core.CallDevToolsProtocolMethodAsync("Page.enable", "{}");
            using var tree = JsonDocument.Parse(await core.CallDevToolsProtocolMethodAsync("Page.getResourceTree", "{}"));
            var frameId = tree.RootElement.GetProperty("frameTree").GetProperty("frame").GetProperty("id").GetString();
            var request = JsonSerializer.Serialize(new { frameId, url });
            using var doc = JsonDocument.Parse(await core.CallDevToolsProtocolMethodAsync("Page.getResourceContent", request));
            var content = doc.RootElement.GetProperty("content").GetString() ?? "";
            return doc.RootElement.GetProperty("base64Encoded").GetBoolean() ? Convert.FromBase64String(content) : null;
        }
        catch (Exception e) when (e is COMException or JsonException or KeyNotFoundException or FormatException or InvalidOperationException)
        {
            AppPaths.Log($"avatar not available: {e.GetType().Name}");
            return null;
        }
    }

    // shortcut: a thread not in the watcher's loaded list gets no bubble, it has no name or avatar to show.
    public RowSnapshot? FindRow(string path) =>
        _lastRows.Find(r => r.ThreadHref.TrimEnd('/') == path.TrimEnd('/'));

    void ReplaceWebView()
    {
        _web.Dispose();
        _web = new WebView2();
        Content = _web;
        _env = null;
    }

    static string ReadProbe()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("probe.js")!;
        return new StreamReader(stream).ReadToEnd();
    }

    void OnMessage(object? _, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!FacebookUrl.IsTrusted(e.Source)) return;
        List<RowSnapshot> rows;
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            rows = doc.RootElement.GetProperty("rows").EnumerateArray().Select(r => new RowSnapshot(
                r.GetProperty("href").GetString() ?? "",
                r.GetProperty("text").GetString() ?? "",
                r.GetProperty("bold").GetBoolean(),
                r.GetProperty("avatar").GetString() ?? "")).Where(r => r.ThreadHref != "").ToList();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            AppPaths.Log("bad message from page");
            return;
        }

        _lastPost = DateTime.Now;
        _lastRowCount = rows.Count;
        _lastRows = rows;
        var result = _detector.Scan(rows, DateTime.Now);
        if (result.NewMessages.Count > 0) AppPaths.Log($"new={result.NewMessages.Count}");
        Scanned?.Invoke(result);
        CheckHealth();
    }

    void OnNavigationCompleted(object? _, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            // Cancelled means superseded or blocked on purpose, not a failed load.
            if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled) return;
            AppPaths.Log($"navigation failed: {e.WebErrorStatus}");
            ScheduleReload();
            return;
        }
        _attempt = 0;
        _lastPost = DateTime.Now;
        if (new Uri(_web.CoreWebView2.Source).AbsolutePath is "/" or "/home.php")
            _web.CoreWebView2.Navigate(AppPaths.MessagesUrl);
        CheckHealth();
    }

    void OnProcessFailed(object? _, CoreWebView2ProcessFailedEventArgs e)
    {
        AppPaths.Log($"process failed: {e.ProcessFailedKind}");
        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
            Dispatcher.InvokeAsync(ReplaceWebView);
        else if (e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessExited &&
                 e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
            return;
        ScheduleReload();
    }

    void ScheduleReload()
    {
        if (_reload.IsEnabled) return;
        _reload.Interval = Backoff.Next(_attempt++);
        _reload.Start();
    }

    void CheckHealth()
    {
        var now = DateTime.Now;
        if (now - _lastPost > SilenceLimit)
        {
            _lastRowCount = 0;
            ScheduleReload();
        }

        // Read on every tick because Facebook can route to login without a navigation event.
        var core = _web.CoreWebView2;
        var login = core != null && FacebookUrl.IsLoginPage(core.Source);
        if (login != _needsLogin)
        {
            _needsLogin = login;
            AppPaths.Log($"login needed={login}");
            NeedsLogin?.Invoke(login);
            if (login) ShowWindow();
        }

        var broken = _health.IsBroken(login ? 1 : _lastRowCount, now);
        if (broken == _broken) return;
        _broken = broken;
        AppPaths.Log($"health broken={broken}");
        HealthChanged?.Invoke(broken);
    }
}
