using ChatBubbles.Core;

namespace ChatBubbles.Tests;

public class HelpersTests
{
    static readonly DateTime T0 = new(2026, 11, 2, 12, 0, 0);

    [Fact]
    public void SoundGate_FirstPlays_SecondWithin3sDoesNot_LaterPlays()
    {
        var g = new SoundGate();
        Assert.True(g.ShouldPlay(T0));
        Assert.False(g.ShouldPlay(T0.AddSeconds(2.9)));
        Assert.True(g.ShouldPlay(T0.AddSeconds(3)));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(6, 300)]
    [InlineData(5000, 300)]
    public void Backoff_DoublesAndCapsAt300s(int attempt, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), Backoff.Next(attempt));

    [Theory]
    [InlineData("https://www.facebook.com/messages", true)]
    [InlineData("https://facebook.com/", true)]
    [InlineData("http://www.facebook.com/", false)]
    [InlineData("https://evilfacebook.com/", false)]
    [InlineData("https://facebook.com.evil.com/", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    public void IsTrusted_RejectsLookalikes(string uri, bool expected) =>
        Assert.Equal(expected, FacebookUrl.IsTrusted(uri));

    [Theory]
    [InlineData("/messages/t/1", "https://www.facebook.com/messages/t/1")]
    [InlineData("/messages/e2ee/t/1", "https://www.facebook.com/messages/e2ee/t/1")]
    [InlineData("https://www.facebook.com/messages/t/1", "https://www.facebook.com/messages/t/1")]
    [InlineData("https://evil.com/messages/t/1", null)]
    [InlineData("//evil.com/messages/t/1", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("", null)]
    public void ToThreadUrl_RelativeOk_ForeignNull(string href, string? expected) =>
        Assert.Equal(expected, FacebookUrl.ToThreadUrl(href));

    [Theory]
    [InlineData("https://www.facebook.com/login/?next=x", true)]
    [InlineData("https://www.facebook.com/login.php", true)]
    [InlineData("https://www.facebook.com/checkpoint/1", true)]
    [InlineData("https://www.facebook.com/two_step_verification/two_factor/", true)]
    [InlineData("https://www.facebook.com/messages/t/login", false)]
    public void IsLoginPage_DetectsLoginAndCheckpoint(string uri, bool expected) =>
        Assert.Equal(expected, FacebookUrl.IsLoginPage(uri));

    [Fact]
    public void RowsHealth_BrokenOnlyAfter2Min_ResetsOnRows()
    {
        var h = new RowsHealth();
        Assert.False(h.IsBroken(0, T0));
        Assert.False(h.IsBroken(0, T0.AddSeconds(119)));
        Assert.True(h.IsBroken(0, T0.AddSeconds(120)));
        Assert.False(h.IsBroken(3, T0.AddSeconds(121)));
        Assert.False(h.IsBroken(0, T0.AddSeconds(122)));
        Assert.True(h.IsBroken(0, T0.AddSeconds(242)));
    }
}
