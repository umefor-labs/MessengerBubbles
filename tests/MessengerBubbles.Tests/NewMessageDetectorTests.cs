using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

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

    [Fact]
    public void MutedRow_NewMessage_IsNotReported()
    {
        var d = new NewMessageDetector();
        var start = new DateTime(2026, 1, 1);
        d.Scan([new RowSnapshot("/t/1", "A\ncu", false, "", Muted: true)], start);
        var r = d.Scan([new RowSnapshot("/t/1", "A\nmoi", true, "", Muted: true)], start.AddMinutes(1));
        Assert.Empty(r.NewMessages);
    }

    [Fact]
    public void MutedRow_TurnedBackOn_DoesNotReportOldMessage()
    {
        var d = new NewMessageDetector();
        var start = new DateTime(2026, 1, 1);
        d.Scan([new RowSnapshot("/t/1", "A\ncu", false, "", Muted: true)], start);
        d.Scan([new RowSnapshot("/t/1", "A\nmoi", true, "", Muted: true)], start.AddMinutes(1));
        var r = d.Scan([new RowSnapshot("/t/1", "A\nmoi", true, "")], start.AddMinutes(2));
        Assert.Empty(r.NewMessages);
    }

    [Fact]
    public void MutedRow_TurnedBackOn_ReportsNextMessage()
    {
        var d = new NewMessageDetector();
        var start = new DateTime(2026, 1, 1);
        d.Scan([new RowSnapshot("/t/1", "A\ncu", false, "", Muted: true)], start);
        d.Scan([new RowSnapshot("/t/1", "A\ncu", false, "")], start.AddMinutes(1));
        var r = d.Scan([new RowSnapshot("/t/1", "A\nmoi", true, "")], start.AddMinutes(2));
        Assert.Single(r.NewMessages);
    }

    [Fact]
    public void SameTextAgainWithYoungerTime_IsNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào\n·\n5 phút", true));
        var r = d.Scan([Row("/t/1", "An\nChào\n·\nVừa xong", true)], Settled);
        Assert.Equal("/t/1", Assert.Single(r.NewMessages).ThreadHref);
    }

    [Fact]
    public void SameTextAgainAfterHourLabel_IsNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào · 2 giờ", true));
        Assert.Single(d.Scan([Row("/t/1", "An\nChào · 1 phút", true)], Settled).NewMessages);
    }

    [Fact]
    public void TimeLabelMovingToNextUnit_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào\n·\n59 phút", true));
        Assert.Empty(d.Scan([Row("/t/1", "An\nChào\n·\n1 giờ", true)], Settled).NewMessages);
    }

    [Fact]
    public void YoungerTimeWithoutPriorLabel_IsNotNew()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", true));
        Assert.Empty(d.Scan([Row("/t/1", "An\nChào\n·\nVừa xong", true)], Settled).NewMessages);
    }

    [Fact]
    public void Restart_ReportsMessageThatArrivedDuringReload()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", false));
        d.Scan([Row("/t/1", "An\nChào", false)], Settled);
        d.Restart();
        var later = Settled.AddHours(6);
        Assert.Empty(d.Scan([Row("/t/1", "An", true)], later).NewMessages);
        Assert.Empty(d.Scan([Row("/t/1", "An\nMới", true)], later.AddSeconds(5)).NewMessages);
        var r = d.Scan([Row("/t/1", "An\nMới", true)], later.AddSeconds(11));
        Assert.Equal("/t/1", Assert.Single(r.NewMessages).ThreadHref);
    }

    [Fact]
    public void Restart_DoesNotRepeatMessagesSeenBeforeReload()
    {
        var d = SettledOn(Row("/t/1", "An\nChào", true));
        d.Scan([Row("/t/1", "An\nChào", true)], Settled);
        d.Restart();
        var later = Settled.AddHours(6);
        d.Scan([Row("/t/1", "An\nChào", true)], later);
        Assert.Empty(d.Scan([Row("/t/1", "An\nChào", true)], later.AddSeconds(11)).NewMessages);
    }

    [Fact]
    public void Restart_OnEmptyDetector_ActsLikeReset()
    {
        var d = new NewMessageDetector();
        d.Restart();
        d.Scan([Row("/t/1", "An\nChào", true)], T0);
        Assert.Empty(d.Scan([Row("/t/1", "An\nChào", true)], Settled).NewMessages);
    }
}
