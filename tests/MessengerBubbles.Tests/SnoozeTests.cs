using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class SnoozeTests
{
    [Fact]
    public void ForOneHour_IsAnHourLater() =>
        Assert.Equal(new DateTime(2026, 11, 12, 15, 30, 0), Snooze.ForOneHour(new DateTime(2026, 11, 12, 14, 30, 0)));

    [Theory]
    [InlineData(22, 13)]
    [InlineData(7, 13)]
    [InlineData(14, 13)]
    [InlineData(2, 12)]
    public void UntilMorning_IsNextSevenAm(int hour, int expectedDay) =>
        Assert.Equal(new DateTime(2026, 11, expectedDay, 7, 0, 0), Snooze.UntilMorning(new DateTime(2026, 11, 12, hour, 0, 0)));

    [Fact]
    public void IsSnoozed_OnlyBeforeTheEnd()
    {
        var now = new DateTime(2026, 11, 12, 14, 0, 0);
        Assert.False(new Settings().IsSnoozed(now));
        Assert.True(new Settings { SnoozedUntil = now.AddMinutes(1) }.IsSnoozed(now));
        Assert.False(new Settings { SnoozedUntil = now }.IsSnoozed(now));
    }
}
