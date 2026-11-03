using System.Text.Json;

namespace MessengerBubbles.Core;

public static class UpdateCheck
{
    public const string ApiUrl = "https://api.github.com/repos/umefor-labs/MessengerBubbles/releases/latest";
    public const string PageUrl = "https://github.com/umefor-labs/MessengerBubbles/releases/latest";

    // The newer version in a releases/latest response, or null if there is none or it cannot be read.
    public static Version? NewerVersion(string json, Version current)
    {
        string? tag;
        try
        {
            using var doc = JsonDocument.Parse(json);
            tag = doc.RootElement.GetProperty("tag_name").GetString();
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
        if (tag == null || !Version.TryParse(tag.TrimStart('v'), out var latest)) return null;
        return Comparable(latest) > Comparable(current) ? latest : null;
    }

    // A missing part reads as -1, which would make 0.5.0 older than 0.5.0.0.
    static Version Comparable(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));
}
