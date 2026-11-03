using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessengerBubbles.Core;

namespace MessengerBubbles;

class BubbleWindow : FloatingWindow
{
    const int DragThreshold = 4;
    static readonly Duration PopTime = TimeSpan.FromMilliseconds(180);
    static readonly Brush Blue = new SolidColorBrush(Color.FromRgb(0x08, 0x66, 0xFF));

    readonly Ellipse _face = new() { Fill = Blue, Stroke = Brushes.White, StrokeThickness = 2 };
    readonly TextBlock _label = new()
    {
        Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
    };
    readonly TextBlock _badgeText = new() { Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(5, 0, 5, 1) };
    readonly Border _badge;
    readonly ScaleTransform _scale = new();

    (int X, int Y) _pressCursor;
    Box _pressBox = new(0, 0, 0, 0);
    bool _pressed, _dragging;

    public event Action? Clicked;
    public event Action? DragStarted;
    public event Action? DragMoved;
    public event Action? DragEnded;

    public BubbleWindow(string name, string label) : base(56, 56)
    {
        SetName(name);
        _label.Text = label;
        _badge = new Border
        {
            Background = Brushes.Red, CornerRadius = new CornerRadius(9), MinWidth = 18, Height = 18,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            Child = _badgeText, Visibility = Visibility.Collapsed,
        };
        // shortcut: no spring overshoot, the 56px window would clip it; needs a larger transparent window.
        var root = new Grid { Cursor = Cursors.Hand, RenderTransform = _scale, RenderTransformOrigin = new Point(0.5, 0.5) };
        root.Children.Add(_face);
        root.Children.Add(_label);
        root.Children.Add(_badge);
        Content = root;

        root.MouseLeftButtonDown += (_, e) =>
        {
            StopGlide();
            _pressed = true;
            _dragging = false;
            _pressCursor = CursorPixel();
            _pressBox = PixelBox();
            root.CaptureMouse();
            e.Handled = true;
        };
        root.MouseMove += (_, _) =>
        {
            if (!_pressed) return;
            var c = CursorPixel();
            if (!_dragging && Math.Abs(c.X - _pressCursor.X) + Math.Abs(c.Y - _pressCursor.Y) < DragThreshold) return;
            if (!_dragging)
            {
                _dragging = true;
                DragStarted?.Invoke();
            }
            MoveTo(_pressBox.X + c.X - _pressCursor.X, _pressBox.Y + c.Y - _pressCursor.Y);
            DragMoved?.Invoke();
        };
        root.MouseLeftButtonUp += (_, _) => Release(fire: true);
        root.LostMouseCapture += (_, _) => Release(fire: false);

        void Release(bool fire)
        {
            if (!_pressed) return;
            _pressed = false;
            var wasDragging = _dragging;
            _dragging = false;
            root.ReleaseMouseCapture();
            if (wasDragging) DragEnded?.Invoke();
            else if (fire) Clicked?.Invoke();
        }
    }

    public void SetName(string name)
    {
        AutomationProperties.SetName(this, name);
        ToolTip = name;
    }

    public void PopIn()
    {
        if (MotionOn) Animate(0.4, 1, EasingMode.EaseOut);
    }

    // Slides into the dismiss mark while shrinking, then calls done.
    public void SuckInto(double x, double y, Action done)
    {
        if (!MotionOn)
        {
            done();
            return;
        }
        GlideTo(x, y);
        Animate(1, 0.2, EasingMode.EaseIn).Completed += (_, _) => done();
    }

    AnimationClock Animate(double from, double to, EasingMode mode)
    {
        var animation = new DoubleAnimation(from, to, PopTime) { EasingFunction = new CubicEase { EasingMode = mode } };
        var clock = animation.CreateClock();
        _scale.ApplyAnimationClock(ScaleTransform.ScaleXProperty, clock);
        _scale.ApplyAnimationClock(ScaleTransform.ScaleYProperty, clock);
        return clock;
    }

    public void SetLabel(string label) => _label.Text = label;

    public void SetBadge(int count)
    {
        _badgeText.Text = count > 99 ? "99+" : count.ToString();
        _badge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public BitmapSource? Avatar { get; private set; }

    public void SetAvatar(BitmapSource image)
    {
        Avatar = image;
        _face.Fill = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
        _label.Text = "";
    }
}
