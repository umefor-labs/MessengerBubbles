using MessengerBubbles.Core;
using Microsoft.Win32;

namespace MessengerBubbles.Tests;

public class AutostartTests : IDisposable
{
    const string SubKey = @"Software\MessengerBubblesTest";

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(SubKey, false);

    [Fact]
    public void Set_On_WritesQuotedPath()
    {
        Autostart.Set(true, @"C:\My Apps\MessengerBubbles.exe", SubKey);
        Assert.True(Autostart.IsOn(SubKey));
        using var key = Registry.CurrentUser.OpenSubKey(SubKey);
        Assert.Equal("\"C:\\My Apps\\MessengerBubbles.exe\"", key!.GetValue("MessengerBubbles"));
    }

    [Fact]
    public void Set_Off_RemovesValue()
    {
        Autostart.Set(true, @"C:\a.exe", SubKey);
        Autostart.Set(false, @"C:\a.exe", SubKey);
        Assert.False(Autostart.IsOn(SubKey));
    }

    [Fact]
    public void Refresh_On_WritesNewPath()
    {
        Autostart.Set(true, @"C:\old\a.exe", SubKey);
        Autostart.Refresh(@"C:\new\a.exe", SubKey);
        using var key = Registry.CurrentUser.OpenSubKey(SubKey);
        Assert.Equal("\"C:\\new\\a.exe\"", key!.GetValue("MessengerBubbles"));
    }

    [Fact]
    public void Refresh_Off_StaysOff()
    {
        Autostart.Set(false, @"C:\a.exe", SubKey);
        Autostart.Refresh(@"C:\new\a.exe", SubKey);
        Assert.False(Autostart.IsOn(SubKey));
    }

    [Fact]
    public void Set_OffWhenAlreadyOff_DoesNotThrow()
    {
        Autostart.Set(false, @"C:\a.exe", SubKey);
        Assert.False(Autostart.IsOn(SubKey));
    }
}
