namespace MessengerBubbles.Core;

public record RowSnapshot(string ThreadHref, string Text, bool Bold, string AvatarUrl, bool Muted = false);

public record ScanResult(IReadOnlyList<RowSnapshot> NewMessages, IReadOnlyList<string> NowRead, int UnreadThreads);
