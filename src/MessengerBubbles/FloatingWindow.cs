using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MessengerBubbles.Core;
using WinForms = System.Windows.Forms;

namespace MessengerBubbles;

// Borderless always-on-top window that never takes focus. Positions are physical pixels, for mixed-DPI screens.
class FloatingWindow : Window
{
    const int GwlExStyle = -20;
    const long WsExNoActivate = 0x08000000, WsExToolWindow = 0x00000080;
    const uint SwpNoMove = 0x0002;
    const uint SwpNoSize = 0x0001, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010;
    const double GlideMs = 180;

    [DllImport("user32.dll")] static extern long GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll")] static extern long SetWindowLongPtr(IntPtr hWnd, int index, long value);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out Rect32 rect);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point32 point);

    [StructLayout(LayoutKind.Sequential)] struct Rect32 { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] struct Point32 { public int X, Y; }

    // Windows setting "Animation effects"; off means no motion anywhere.
    protected static bool MotionOn => SystemParameters.ClientAreaAnimation;

    DispatcherTimer? _glide;

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
        Closed += (_, _) => StopGlide();
    }

    public Box PixelBox()
    {
        GetWindowRect(Handle, out var r);
        return new Box(r.L, r.T, r.R - r.L, r.B - r.T);
    }

    public void MoveTo(double x, double y)
    {
        StopGlide();
        SetPosition(x, y);
    }

    public void GlideTo(double x, double y)
    {
        StopGlide();
        if (!MotionOn)
        {
            SetPosition(x, y);
            return;
        }
        var from = PixelBox();
        var clock = Stopwatch.StartNew();
        _glide = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(15) };
        _glide.Tick += (_, _) =>
        {
            var t = Math.Min(1, clock.ElapsedMilliseconds / GlideMs);
            var eased = 1 - Math.Pow(1 - t, 3);
            SetPosition(from.X + (x - from.X) * eased, from.Y + (y - from.Y) * eased);
            if (t >= 1) StopGlide();
        };
        _glide.Start();
    }

    protected void StopGlide()
    {
        _glide?.Stop();
        _glide = null;
    }

    void SetPosition(double x, double y) =>
        SetWindowPos(Handle, IntPtr.Zero, (int)x, (int)y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);

    public void BringToFront() =>
        SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);

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
