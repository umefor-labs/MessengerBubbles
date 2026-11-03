using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class UpdateCheckTests
{
    static string Latest(string tag) => $"{{\"tag_name\":\"{tag}\"}}";

    [Theory]
    [InlineData("v0.5.1", "0.5.0.0", "0.5.1")]
    [InlineData("v0.6.0", "0.5.0.0", "0.6.0")]
    [InlineData("v1.0.0", "0.5.0", "1.0.0")]
    [InlineData("0.10.0", "0.9.0.0", "0.10.0")]
    public void Newer_IsReturned(string tag, string current, string expected) =>
        Assert.Equal(Version.Parse(expected), UpdateCheck.NewerVersion(Latest(tag), Version.Parse(current)));

    [Theory]
    [InlineData("v0.5.0", "0.5.0.0")]
    [InlineData("v0.5.0", "0.5.0")]
    [InlineData("v0.4.0", "0.5.0.0")]
    [InlineData("v0.5", "0.5.0.0")]
    public void SameOrOlder_IsNull(string tag, string current) =>
        Assert.Null(UpdateCheck.NewerVersion(Latest(tag), Version.Parse(current)));

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"tag_name\":null}")]
    [InlineData("{\"tag_name\":3}")]
    [InlineData("{\"tag_name\":\"latest\"}")]
    [InlineData("{\"tag_name\":\"v1.0.0-beta\"}")]
    public void Unreadable_IsNull(string json) =>
        Assert.Null(UpdateCheck.NewerVersion(json, new Version(0, 5, 0, 0)));
}
