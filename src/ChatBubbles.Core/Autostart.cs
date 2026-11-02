using Microsoft.Win32;

namespace ChatBubbles.Core;

public static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "ChatBubbles";

    public static void Set(bool on, string exePath, string subKey = RunKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey);
        if (on)
            key.SetValue(ValueName, $"\"{exePath}\"");
        else
            key.DeleteValue(ValueName, false);
    }

    public static bool IsOn(string subKey = RunKey)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey);
        return key?.GetValue(ValueName) != null;
    }
}
