using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
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
    const double CardW = 280, CardH = 56, ShadowRoom = 10;

    readonly Ellipse _avatar = new() { Width = 40, Height = 40 };
    readonly TextBlock _initial = new()
    {
        Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
    };
    readonly TextBlock _name = new()
    {
        FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis,
        Foreground = new SolidColorBrush(Color.FromRgb(0x05, 0x05, 0x05)),
    };
    readonly TextBlock _body = new()
    {
        FontSize = 12, Margin = new Thickness(0, 1, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis,
        Foreground = new SolidColorBrush(Color.FromRgb(0x65, 0x67, 0x6B)),
    };
    readonly Border _card;
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

    public PreviewWindow() : base(CardW + 2 * ShadowRoom, CardH + 2 * ShadowRoom)
    {
        var face = new Grid { Margin = new Thickness(8, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        face.Children.Add(_avatar);
        face.Children.Add(_initial);

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
        text.Children.Add(_name);
        text.Children.Add(_body);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(text, 1);
        row.Children.Add(face);
        row.Children.Add(text);

        _card = new Border
        {
            Margin = new Thickness(ShadowRoom),
            CornerRadius = new CornerRadius(CardH / 2),
            Background = Brushes.White,
            Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Direction = 270, Opacity = 0.3 },
            Child = row,
        };
        Content = _card;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Hide();
        };
    }

    public void ShowBeside(Box bubble, string name, string preview, ImageSource? avatar, Box? panel)
    {
        _name.Text = name;
        _body.Text = preview;
        _initial.Text = BubbleController.Initial(name);
        _avatar.Fill = new SolidColorBrush(Color.FromRgb(0x08, 0x66, 0xFF));
        if (avatar != null) SetAvatar(avatar);
        Show();
        // The placed box is the card; the window around it only holds the shadow.
        var size = PixelBox();
        var scale = size.W / (CardW + 2 * ShadowRoom);
        var box = Geometry.PlacePreview(bubble, CardW * scale, CardH * scale, WorkAreaAt(bubble.X, bubble.Y), panel);
        MoveTo(box.X - ShadowRoom * scale, box.Y - ShadowRoom * scale);
        if (MotionOn) _card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
        _timer.Stop();
        _timer.Start();
    }

    public void SetAvatar(ImageSource image)
    {
        _avatar.Fill = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
        _initial.Text = "";
    }
}
