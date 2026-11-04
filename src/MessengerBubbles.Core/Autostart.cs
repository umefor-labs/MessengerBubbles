using Microsoft.Win32;

namespace MessengerBubbles.Core;

public static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "MessengerBubbles";

    public static void Set(bool on, string exePath, string subKey = RunKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey);
        if (on)
            key.SetValue(ValueName, $"\"{exePath}\"");
        else
            key.DeleteValue(ValueName, false);
    }

    // The exe may have moved since autostart was turned on.
    public static void Refresh(string exePath, string subKey = RunKey)
    {
        if (IsOn(subKey)) Set(true, exePath, subKey);
    }

    public static bool IsOn(string subKey = RunKey)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey);
        return key?.GetValue(ValueName) != null;
    }
}
