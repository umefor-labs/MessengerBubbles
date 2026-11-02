using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class SettingsTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("chatbubbles").FullName;
    string Path_ => Path.Combine(_dir, "settings.json");

    public void Dispose() => Directory.Delete(_dir, true);

    static void AssertDefaults(Settings s)
    {
        Assert.False(s.PersistentBubble);
        Assert.True(s.Preview);
        Assert.True(s.Sound);
        Assert.Equal(PanelSize.Medium, s.PanelSize);
        Assert.False(s.Autostart);
        Assert.Empty(s.Positions);
    }

    [Fact]
    public void Load_Missing_ReturnsDefaultsAllOff() => AssertDefaults(Settings.Load(Path_));

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{\"Positions\":null}")]
    [InlineData("{\"Positions\":{\"anchor\":null}}")]
    public void Load_Corrupt_ReturnsDefaults(string content)
    {
        File.WriteAllText(Path_, content);
        AssertDefaults(Settings.Load(Path_));
    }

    [Fact]
    public void SaveLoad_RoundTrips()
    {
        var s = new Settings { Preview = false, Positions = { ["anchor"] = new Box(1, 2, 56, 56) } };
        s.Save(Path_);
        var loaded = Settings.Load(Path_);
        Assert.False(loaded.Preview);
        Assert.False(loaded.PersistentBubble);
        Assert.Equal(new Box(1, 2, 56, 56), loaded.Positions["anchor"]);
    }

    [Fact]
    public void Save_MissingFolder_CreatesIt()
    {
        var nested = Path.Combine(_dir, "a", "settings.json");
        new Settings().Save(nested);
        Assert.True(File.Exists(nested));
    }
}
