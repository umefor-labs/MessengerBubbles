using System.Text.RegularExpressions;

namespace ChatBubbles.Core;

public static partial class RowText
{
    [GeneratedRegex(@"(?:·\s*)?(?:\d+\s*(?:phút|giờ|ngày|tuần|tháng|năm|mins?|hrs?|m|h|d|w|y)|vừa xong|just now)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TimeAtEnd();

    [GeneratedRegex(@"^(?:unread message|tin nhắn chưa đọc)\s*:?$", RegexOptions.IgnoreCase)]
    private static partial Regex UnreadLabel();

    [GeneratedRegex(@"^(?:đang hoạt động|active now|hoạt động .+ trước|active .+ ago)$", RegexOptions.IgnoreCase)]
    private static partial Regex PresenceLabel();

    public static string WithoutTime(string text) => string.Join('\n', ContentLines(text));

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
