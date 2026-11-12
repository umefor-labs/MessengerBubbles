namespace MessengerBubbles.Core;

public class NewMessageDetector
{
    // Facebook renders rows progressively after load, so early text changes are not messages.
    static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(10);

    readonly Dictionary<string, (string Text, bool Bold, TimeSpan? Age)> _seen = new();
    DateTime? _settledAt;
    // After a planned reload the rows seen before it stay the baseline, so a message that arrives while the page loads is still reported.
    bool _keepBaseline;

    public void Reset()
    {
        _seen.Clear();
        _settledAt = null;
        _keepBaseline = false;
    }

    // Like Reset, but compares the reloaded page against the rows seen before the reload.
    public void Restart()
    {
        _settledAt = null;
        _keepBaseline = _seen.Count > 0;
    }

    public ScanResult Scan(IReadOnlyList<RowSnapshot> rows, DateTime now)
    {
        var unique = rows.DistinctBy(r => r.ThreadHref).ToList();
        // The clock starts at the first rendered row: a slow load must not count as settled.
        if (unique.Count > 0) _settledAt ??= now + SettleTime;
        var settled = now >= _settledAt;
        // While the reloaded page renders, its rows are half drawn and must not overwrite the baseline.
        if (!settled && _keepBaseline) return new ScanResult([], [], unique.Count(r => r.Bold));
        _keepBaseline = false;
        var fresh = new List<RowSnapshot>();
        var read = new List<string>();

        foreach (var row in unique)
        {
            var text = RowText.WithoutTime(row.Text);
            var age = RowText.Age(row.Text);
            var known = _seen.TryGetValue(row.ThreadHref, out var prior);
            // The same text again shows up only as a younger time label ("5 phút" back to "Vừa xong").
            // shortcut: a repeat within the same minute keeps the same label and still counts once
            var repeated = known && prior.Text == text && age < prior.Age;
            if (settled && row.Bold && !row.Muted && (!known || !prior.Bold || prior.Text != text || repeated))
                fresh.Add(row);
            if (known && prior.Bold && !row.Bold)
                read.Add(row.ThreadHref);
            _seen[row.ThreadHref] = (text, row.Bold, age);
        }

        return new ScanResult(fresh, read, unique.Count(r => r.Bold));
    }
}
