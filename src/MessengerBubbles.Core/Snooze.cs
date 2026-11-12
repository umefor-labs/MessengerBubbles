namespace MessengerBubbles.Core;

public static class Snooze
{
    public const int MorningHour = 7;

    public static DateTime ForOneHour(DateTime now) => now.AddHours(1);

    // The next 7 AM: later today when it is still before 7, otherwise tomorrow.
    public static DateTime UntilMorning(DateTime now)
    {
        var today = now.Date.AddHours(MorningHour);
        return now < today ? today : today.AddDays(1);
    }
}
