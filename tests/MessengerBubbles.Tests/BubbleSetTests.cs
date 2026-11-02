using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class BubbleSetTests
{
    static RowSnapshot Row(int i) => new($"/t/{i}", $"Người {i}\nChào", true, $"a{i}");

    static BubbleSet FullOfUnread()
    {
        var s = new BubbleSet();
        for (var i = 1; i <= 5; i++) s.OnNewMessage(Row(i));
        return s;
    }

    [Fact]
    public void NewThread_AddsBubbleWithUnreadOne()
    {
        var s = new BubbleSet();
        s.OnNewMessage(Row(1));
        Assert.Equal(new BubbleState("/t/1", "Người 1", "a1", 1), Assert.Single(s.Visible));
    }

    [Fact]
    public void SecondMessageSameThread_IncrementsOnly()
    {
        var s = new BubbleSet();
        s.OnNewMessage(Row(1));
        s.OnNewMessage(Row(1));
        Assert.Equal(2, Assert.Single(s.Visible).Unread);
    }

    [Fact]
    public void Full_ClosesOldestWithoutUnread()
    {
        var s = FullOfUnread();
        s.MarkRead("/t/3");
        s.OnNewMessage(Row(6));
        Assert.Equal(["/t/1", "/t/2", "/t/4", "/t/5", "/t/6"], s.Visible.Select(b => b.ThreadHref));
        Assert.Equal(0, s.Overflow);
    }

    [Fact]
    public void FullAllUnread_GoesToOverflow()
    {
        var s = FullOfUnread();
        s.OnNewMessage(Row(6));
        Assert.Equal(5, s.Visible.Count);
        Assert.Equal(1, s.Overflow);
    }

    [Fact]
    public void SecondMessageInOverflow_DoesNotCountTwice()
    {
        var s = FullOfUnread();
        s.OnNewMessage(Row(6));
        s.OnNewMessage(Row(6));
        Assert.Equal(1, s.Overflow);
    }

    [Fact]
    public void FreedSlot_PromotesOldestOverflow()
    {
        var s = FullOfUnread();
        s.OnNewMessage(Row(6));
        s.OnNewMessage(Row(7));
        s.Dismiss("/t/1");
        Assert.Contains(s.Visible, b => b.ThreadHref == "/t/6");
        Assert.Equal(1, s.Overflow);
    }

    [Fact]
    public void MarkRead_ClearsBadgeKeepsBubble()
    {
        var s = new BubbleSet();
        s.OnNewMessage(Row(1));
        s.MarkRead("/t/1");
        Assert.Equal(0, Assert.Single(s.Visible).Unread);
    }

    [Fact]
    public void MarkRead_DropsThreadFromOverflow()
    {
        var s = FullOfUnread();
        s.OnNewMessage(Row(6));
        s.MarkRead("/t/6");
        Assert.Equal(0, s.Overflow);
    }

    [Fact]
    public void Dismissed_ReappearsOnNextMessage()
    {
        var s = new BubbleSet();
        s.OnNewMessage(Row(1));
        s.Dismiss("/t/1");
        Assert.Empty(s.Visible);
        s.OnNewMessage(Row(1));
        Assert.Equal(1, Assert.Single(s.Visible).Unread);
    }
}
