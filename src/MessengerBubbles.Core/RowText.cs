using System.Text.RegularExpressions;

namespace MessengerBubbles.Core;

public static partial class RowText
{
    [GeneratedRegex(@"(?:·\s*)?(?:\d+\s*(?:phút|giờ|ngày|tuần|tháng|năm|mins?|hrs?|m|h|d|w|y)|vừa xong|just now)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TimeAtEnd();

    [GeneratedRegex(@"^(?:unread message|tin nhắn chưa đọc)\s*:?$", RegexOptions.IgnoreCase)]
    private static partial Regex UnreadLabel();

    [GeneratedRegex(@"^(?:đang hoạt động|active now|hoạt động .+ trước|active .+ ago)$", RegexOptions.IgnoreCase)]
    private static partial Regex PresenceLabel();

    [GeneratedRegex(@"(?:^|·|\s)(?:(\d+)\s*(phút|giờ|ngày|tuần|tháng|năm|mins?|hrs?|m|h|d|w|y)|vừa xong|just now)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AgeAtEnd();

    public static string WithoutTime(string text) => string.Join('\n', ContentLines(text));

    // How old the row's last message is by its time label, or null if the row shows none.
    public static TimeSpan? Age(string text)
    {
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var dot = lines.IndexOf("·");
        var candidates = dot > 0 ? lines.Skip(dot + 1).Take(1) : lines.Skip(1).TakeLast(1);
        foreach (var line in candidates)
        {
            var m = AgeAtEnd().Match(line);
            if (!m.Success) continue;
            if (!m.Groups[1].Success) return TimeSpan.Zero;
            var n = int.Parse(m.Groups[1].Value);
            return m.Groups[2].Value.ToLowerInvariant() switch
            {
                "phút" or "m" or "min" or "mins" => TimeSpan.FromMinutes(n),
                "giờ" or "h" or "hr" or "hrs" => TimeSpan.FromHours(n),
                "ngày" or "d" => TimeSpan.FromDays(n),
                "tuần" or "w" => TimeSpan.FromDays(7 * n),
                "tháng" => TimeSpan.FromDays(30 * n),
                _ => TimeSpan.FromDays(365 * n),
            };
        }
        return null;
    }

    public static (string Name, string Preview) Parse(string text)
    {
        var lines = ContentLines(text);
        return lines.Count == 0 ? ("", "") : (lines[0], string.Join(' ', lines.Skip(1)));
    }

    static List<string> ContentLines(string text)
    {
        var lines = text.Split('\n').Select(l => l.Trim())
            .Where(l => l.Length > 0 && !UnreadLabel().IsMatch(l))
            .ToList();
        // Facebook puts the online status before the name.
        if (lines.Count > 1 && PresenceLabel().IsMatch(lines[0])) lines.RemoveAt(0);
        // The first lone dot ends the message: time and status labels follow it.
        var dot = lines.IndexOf("·");
        if (dot > 0) lines.RemoveRange(dot, lines.Count - dot);
        else if (lines.Count > 1)
            lines[^1] = TimeAtEnd().Replace(lines[^1], "").Trim();
        lines.RemoveAll(l => l is "" or "·");
        return lines;
    }
}
