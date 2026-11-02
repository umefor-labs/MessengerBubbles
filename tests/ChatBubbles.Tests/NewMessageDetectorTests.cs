using ChatBubbles.Core;

namespace ChatBubbles.Tests;

public class NewMessageDetectorTests
{
    static readonly DateTime T0 = new(2026, 11, 2, 12, 0, 0);
    static readonly DateTime Settled = T0.AddSeconds(11);

    static RowSnapshot Row(string href, string text, bool bold) => new(href, text, bold, "avatar");

    static NewMessageDetector SettledOn(params RowSnapshot[] rows)
    {
        var d = new NewMessageDetector();
        d.Scan(rows, T0);
        return d;
    }

    [Fact]
    public void FirstScan_NeverReportsNew()
    {
        var r = new NewMessageDetector().Scan([Row("/t/1", "An\nChào", true)], T0);
        Assert.Empty(r.NewMessages);
        Assert.Equal(1, r.UnreadThreads);
    }

    [Fact]
    public void ChangeDuringSettleWindow_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", true));
        var r = d.Scan([Row("/t/1", "An\nChào\nthêm dòng", true)], T0.AddSeconds(2));
        Assert.Empty(r.NewMessages);
    }

    [Fact]
    public void EmptyScans_DoNotStartSettleClock()
    {
        var d = new NewMessageDetector();
        d.Scan([], T0);
        var r = d.Scan([Row("/t/1", "An\nChào", true)], T0.AddSeconds(60));
        Assert.Empty(r.NewMessages);
    }

    [Fact]
    public void DuplicateHrefWithDifferentText_IsNotNewEveryScan()
    {
        RowSnapshot[] rows = [Row("/t/1", "An\nChào", true), Row("/t/1", "An", true)];
        var d = SettledOn(rows);
        var r = d.Scan(rows, Settled);
        Assert.Empty(r.NewMessages);
        Assert.Equal(1, r.UnreadThreads);
    }

    [Fact]
    public void BoldAndTextChanged_IsNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", true));
        var r = d.Scan([Row("/t/1", "An\nĂn chưa", true)], Settled);
        Assert.Equal("/t/1", Assert.Single(r.NewMessages).ThreadHref);
    }

    [Fact]
    public void NotBoldToBold_IsNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", false));
        var r = d.Scan([Row("/t/1", "An\nChào", true)], Settled);
        Assert.Single(r.NewMessages);
    }

    [Fact]
    public void TextChangedNotBold_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", false));
        Assert.Empty(d.Scan([Row("/t/1", "An\nBạn: ok", false)], Settled).NewMessages);
    }

    [Fact]
    public void BoldUnchanged_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", true));
        Assert.Empty(d.Scan([Row("/t/1", "An\nChào", true)], Settled).NewMessages);
    }

    [Fact]
    public void TimeLabelChangedOnBoldRow_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào\n·\n1 phút", true));
        Assert.Empty(d.Scan([Row("/t/1", "An\nChào\n·\n2 phút", true)], Settled).NewMessages);
    }

    [Fact]
    public void UnseenBoldRowAfterSettle_IsNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", false));
        var r = d.Scan([Row("/t/2", "Bình\nAlo", true), Row("/t/1", "An\nChào", false)], Settled);
        Assert.Equal("/t/2", Assert.Single(r.NewMessages).ThreadHref);
    }

    [Fact]
    public void UnseenNotBoldRow_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", false));
        Assert.Empty(d.Scan([Row("/t/2", "Bình\nAlo", false)], Settled).NewMessages);
    }

    [Fact]
    public void BoldToNotBold_ReportedInNowRead()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", true));
        var r = d.Scan([Row("/t/1", "An\nChào", false)], Settled);
        Assert.Equal("/t/1", Assert.Single(r.NowRead));
        Assert.Equal(0, r.UnreadThreads);
    }

    [Fact]
    public void AfterReset_NextScanIsBaselineAgain()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", false));
        d.Reset();
        var later = Settled.AddMinutes(5);
        Assert.Empty(d.Scan([Row("/t/1", "An\nMới", true), Row("/t/2", "Bình\nAlo", true)], later).NewMessages);
    }
}
