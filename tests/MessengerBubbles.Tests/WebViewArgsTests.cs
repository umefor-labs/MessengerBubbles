using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class WebViewArgsTests
{
    [Fact]
    public void Default_KeepsGpu() => Assert.DoesNotContain("--disable-gpu", WebViewArgs.For(false));

    [Fact]
    public void DisableGpu_AddsFlagAndKeepsTheRest()
    {
        var args = WebViewArgs.For(true);
        Assert.Contains("--disable-gpu", args);
        Assert.StartsWith(WebViewArgs.For(false), args);
    }
}
