using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ChatBubbles.Core;
using WinForms = System.Windows.Forms;

namespace ChatBubbles;

// Borderless always-on-top window that never takes focus. Positions are physical pixels, for mixed-DPI screens.
class FloatingWindow : Window
{
    const int GwlExStyle = -20;
    const long WsExNoActivate = 0x08000000, WsExToolWindow = 0x00000080;
    const uint SwpNoSize = 0x0001, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010;

    [DllImport("user32.dll")] static extern long GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll")] static extern long SetWindowLongPtr(IntPtr hWnd, int index, long value);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out Rect32 rect);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point32 point);

    [StructLayout(LayoutKind.Sequential)] struct Rect32 { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] struct Point32 { public int X, Y; }

    protected IntPtr Handle { get; }

    protected FloatingWindow(double sizeW, double sizeH)
    {
        Width = sizeW;
        Height = sizeH;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Handle = new WindowInteropHelper(this).EnsureHandle();
        SetWindowLongPtr(Handle, GwlExStyle, GetWindowLongPtr(Handle, GwlExStyle) | WsExNoActivate | WsExToolWindow);
    }

    public Box PixelBox()
    {
        GetWindowRect(Handle, out var r);
        return new Box(r.L, r.T, r.R - r.L, r.B - r.T);
    }

    public void MoveTo(double x, double y) =>
        SetWindowPos(Handle, IntPtr.Zero, (int)x, (int)y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);

    public void Place(Box box) =>
        SetWindowPos(Handle, IntPtr.Zero, (int)box.X, (int)box.Y, (int)box.W, (int)box.H, SwpNoZOrder | SwpNoActivate);

    public static (int X, int Y) CursorPixel()
    {
        GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    public static Box WorkAreaAt(double x, double y) =>
        ToBox(WinForms.Screen.FromPoint(new System.Drawing.Point((int)x, (int)y)).WorkingArea);

    public static IReadOnlyList<Box> AllWorkAreas()
    {
        // Primary first, so a bubble from an unplugged screen lands there.
        return WinForms.Screen.AllScreens.OrderByDescending(s => s.Primary).Select(s => ToBox(s.WorkingArea)).ToList();
    }

    static Box ToBox(System.Drawing.Rectangle r) => new(r.X, r.Y, r.Width, r.Height);
}
