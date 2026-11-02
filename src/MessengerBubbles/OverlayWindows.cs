using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MessengerBubbles.Core;
using Geometry = MessengerBubbles.Core.Geometry;

namespace MessengerBubbles;

class DismissZoneWindow : FloatingWindow
{
    const double ZoneW = 240, ZoneH = 140;

    readonly Border _mark = new()
    {
        Width = 56, Height = 56, CornerRadius = new CornerRadius(28),
        Background = new SolidColorBrush(Color.FromArgb(200, 40, 40, 40)),
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock
        {
            Text = "✕", Foreground = Brushes.White, FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
        },
    };

    public DismissZoneWindow() : base(ZoneW, ZoneH)
    {
        Content = _mark;
    }

    public void ShowOn(Box workArea)
    {
        Show();
        Place(new Box(workArea.X + (workArea.W - ZoneW) / 2, workArea.Y + workArea.H - ZoneH, ZoneW, ZoneH));
    }

    public void SetHot(bool hot) =>
        _mark.Background = new SolidColorBrush(hot ? Color.FromArgb(230, 220, 40, 40) : Color.FromArgb(200, 40, 40, 40));
}

class PreviewWindow : FloatingWindow
{
    const double W = 260, H = 64;

    readonly TextBlock _name = new() { FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly TextBlock _body = new() { Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

    public PreviewWindow() : base(W, H)
    {
        var stack = new StackPanel { Margin = new Thickness(12, 8, 12, 8), VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(_name);
        stack.Children.Add(_body);
        Content = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(235, 30, 30, 30)),
            Child = stack,
        };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Hide();
        };
    }

    public void ShowBeside(Box bubble, string name, string preview)
    {
        _name.Text = name;
        _body.Text = preview;
        Show();
        var size = PixelBox();
        var area = WorkAreaAt(bubble.X, bubble.Y);
        var box = Geometry.PlacePanel(bubble, size.W, size.H, area);
        MoveTo(box.X, box.Y);
        _timer.Stop();
        _timer.Start();
    }
}
