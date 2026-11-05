using System.Diagnostics;
using System.IO;
using MessengerBubbles.Core;
using Microsoft.Web.WebView2.Core;

namespace MessengerBubbles;

static class AppPaths
{
    public const string MessagesUrl = "https://www.facebook.com/messages";

    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MessengerBubbles");

    public static string SettingsFile => Path.Combine(Dir, "settings.json");

    // Read once, because a running browser process keeps the options it started with.
    static readonly Lazy<string> BrowserArgs = new(() => WebViewArgs.For(Settings.Load(SettingsFile).DisableGpu));

    // Same folder and options on every call, so all WebView2s share one browser process and login.
    public static Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        Directory.CreateDirectory(Dir);
        var options = new CoreWebView2EnvironmentOptions(BrowserArgs.Value);
        return CoreWebView2Environment.CreateAsync(null, Path.Combine(Dir, "profile"), options);
    }

    // Status and errors only: never names or message text.
    public static void Log(string line)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.AppendAllText(Path.Combine(Dir, "app.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}\n");
        }
        catch (IOException)
        {
        }
    }

    public static void Harden(CoreWebView2 core)
    {
        core.PermissionRequested += (_, e) =>
        {
            if (e.PermissionKind == CoreWebView2PermissionKind.Notifications)
                e.State = CoreWebView2PermissionState.Deny;
        };
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (e.IsUserInitiated) OpenExternal(e.Uri);
        };
        core.NavigationStarting += (_, e) =>
        {
            if (e.Uri == "about:blank" || FacebookUrl.IsTrusted(e.Uri)) return;
            e.Cancel = true;
            // A page redirect must not open browser tabs on every retry.
            if (e.IsUserInitiated) OpenExternal(e.Uri);
        };
    }

    static void OpenExternal(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var u) && u.Scheme is "http" or "https")
            Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
    }
}
