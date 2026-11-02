using ChatBubbles.Core;
using Microsoft.Win32;

namespace ChatBubbles.Tests;

public class AutostartTests : IDisposable
{
    const string SubKey = @"Software\ChatBubblesTest";

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(SubKey, false);

    [Fact]
    public void Set_On_WritesQuotedPath()
    {
        Autostart.Set(true, @"C:\My Apps\ChatBubbles.exe", SubKey);
        Assert.True(Autostart.IsOn(SubKey));
        using var key = Registry.CurrentUser.OpenSubKey(SubKey);
        Assert.Equal("\"C:\\My Apps\\ChatBubbles.exe\"", key!.GetValue("ChatBubbles"));
    }

    [Fact]
    public void Set_Off_RemovesValue()
    {
        Autostart.Set(true, @"C:\a.exe", SubKey);
        Autostart.Set(false, @"C:\a.exe", SubKey);
        Assert.False(Autostart.IsOn(SubKey));
    }

    [Fact]
    public void Set_OffWhenAlreadyOff_DoesNotThrow()
    {
        Autostart.Set(false, @"C:\a.exe", SubKey);
        Assert.False(Autostart.IsOn(SubKey));
    }
}
