namespace MessengerBubbles.Core;

public class NewMessageDetector
{
    // Facebook renders rows progressively after load, so early text changes are not messages.
    static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(10);

    readonly Dictionary<string, (string Text, bool Bold)> _seen = new();
    DateTime? _settledAt;

    public void Reset()
    {
        _seen.Clear();
        _settledAt = null;
    }

    public ScanResult Scan(IReadOnlyList<RowSnapshot> rows, DateTime now)
    {
        var unique = rows.DistinctBy(r => r.ThreadHref).ToList();
        // The clock starts at the first rendered row: a slow load must not count as settled.
        if (unique.Count > 0) _settledAt ??= now + SettleTime;
        var settled = now >= _settledAt;
        var fresh = new List<RowSnapshot>();
        var read = new List<string>();

        foreach (var row in unique)
        {
            var text = RowText.WithoutTime(row.Text);
            var known = _seen.TryGetValue(row.ThreadHref, out var prior);
            // shortcut: two identical messages in a row on a bold row count once, fix if a per-message signal turns up
            if (settled && row.Bold && !row.Muted && (!known || !prior.Bold || prior.Text != text))
                fresh.Add(row);
            if (known && prior.Bold && !row.Bold)
                read.Add(row.ThreadHref);
            _seen[row.ThreadHref] = (text, row.Bold);
        }

        return new ScanResult(fresh, read, unique.Count(r => r.Bold));
    }
}
