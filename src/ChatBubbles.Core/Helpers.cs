namespace ChatBubbles.Core;

public class SoundGate
{
    static readonly TimeSpan Quiet = TimeSpan.FromSeconds(3);
    DateTime? _last;

    public bool ShouldPlay(DateTime now)
    {
        if (_last != null && now - _last < Quiet) return false;
        _last = now;
        return true;
    }
}

public static class Backoff
{
    public static TimeSpan Next(int attempt) => TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, attempt)));
}

public static class FacebookUrl
{
    const string Origin = "https://www.facebook.com";

    public static bool IsTrusted(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var u) && u.Scheme == "https" &&
        (u.Host == "facebook.com" || u.Host.EndsWith(".facebook.com"));

    public static bool IsLoginPage(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) return false;
        var path = u.AbsolutePath;
        return path.StartsWith("/login") || path.StartsWith("/checkpoint") || path.StartsWith("/two_step_verification");
    }

    public static string? ToThreadUrl(string href)
    {
        if (href.StartsWith("/messages/")) return Origin + href;
        return IsTrusted(href) && new Uri(href).AbsolutePath.StartsWith("/messages/") ? href : null;
    }
}

public class RowsHealth
{
    static readonly TimeSpan Limit = TimeSpan.FromMinutes(2);
    DateTime? _emptySince;

    public bool IsBroken(int rowCount, DateTime now)
    {
        if (rowCount > 0)
        {
            _emptySince = null;
            return false;
        }
        _emptySince ??= now;
        return now - _emptySince >= Limit;
    }
}
