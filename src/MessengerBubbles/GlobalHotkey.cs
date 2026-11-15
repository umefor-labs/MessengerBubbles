using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MessengerBubbles;

// A system-wide key combination, heard through a hidden message-only window.
sealed class GlobalHotkey : IDisposable
{
    const int WmHotkey = 0x0312, Id = 1;
    public const uint ModAlt = 0x1, ModControl = 0x2, ModNoRepeat = 0x4000;
    const uint VkM = 0x4D;

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    readonly HwndSource _source;

    public event Action? Pressed;

    // False when another app already owns the combination.
    public bool Registered { get; }

    // Ctrl+Alt+M.
    public GlobalHotkey()
    {
        _source = new HwndSource(new HwndSourceParameters("MessengerBubblesHotkey") { ParentWindow = new IntPtr(-3) });
        _source.AddHook(Hook);
        Registered = RegisterHotKey(_source.Handle, Id, ModControl | ModAlt | ModNoRepeat, VkM);
    }

    IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == Id)
        {
            Pressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (Registered) UnregisterHotKey(_source.Handle, Id);
        _source.Dispose();
    }
}
