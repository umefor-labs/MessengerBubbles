namespace MessengerBubbles.Core;

public record BubbleState(string ThreadHref, string Name, string AvatarUrl, int Unread);

public class BubbleSet
{
    public const int Max = 5;

    readonly List<BubbleState> _visible = new();
    readonly List<BubbleState> _overflow = new();

    public IReadOnlyList<BubbleState> Visible => _visible;
    public int Overflow => _overflow.Count;

    public void OnNewMessage(RowSnapshot r)
    {
        if (Bump(_visible, r) || Bump(_overflow, r)) return;

        var bubble = new BubbleState(r.ThreadHref, RowText.Parse(r.Text).Name, r.AvatarUrl, 1);
        if (_visible.Count >= Max)
        {
            var idle = _visible.FindIndex(b => b.Unread == 0);
            if (idle < 0)
            {
                _overflow.Add(bubble);
                return;
            }
            _visible.RemoveAt(idle);
        }
        _visible.Add(bubble);
    }

    // The user moved on from one conversation to another inside the panel.
    public void SwitchTo(string from, RowSnapshot to)
    {
        _overflow.RemoveAll(b => b.ThreadHref == to.ThreadHref);
        var i = _visible.FindIndex(b => b.ThreadHref == from);
        // A message that came after the user left must keep its bubble.
        var replaceable = i >= 0 && _visible[i].Unread == 0;
        if (_visible.Any(b => b.ThreadHref == to.ThreadHref))
        {
            if (replaceable) Dismiss(from);
            return;
        }
        var bubble = new BubbleState(to.ThreadHref, RowText.Parse(to.Text).Name, to.AvatarUrl, 0);
        if (replaceable)
        {
            _visible[i] = bubble;
            return;
        }
        if (_visible.Count >= Max)
        {
            var idle = _visible.FindIndex(b => b.Unread == 0);
            // All full of unread: those matter more than a conversation already read.
            if (idle < 0) return;
            _visible.RemoveAt(idle);
        }
        _visible.Add(bubble);
    }

    public void MarkRead(string href)
    {
        _overflow.RemoveAll(b => b.ThreadHref == href);
        var i = _visible.FindIndex(b => b.ThreadHref == href);
        if (i >= 0) _visible[i] = _visible[i] with { Unread = 0 };
    }

    public void Dismiss(string href)
    {
        _visible.RemoveAll(b => b.ThreadHref == href);
        if (_overflow.Count > 0 && _visible.Count < Max)
        {
            _visible.Add(_overflow[0]);
            _overflow.RemoveAt(0);
        }
    }

    // The whole group went to the dismiss zone, including the chats waiting in overflow.
    public void DismissAll()
    {
        _visible.Clear();
        _overflow.Clear();
    }

    static bool Bump(List<BubbleState> list, RowSnapshot r)
    {
        var i = list.FindIndex(b => b.ThreadHref == r.ThreadHref);
        if (i < 0) return false;
        list[i] = list[i] with { Unread = list[i].Unread + 1, AvatarUrl = r.AvatarUrl };
        return true;
    }
}
