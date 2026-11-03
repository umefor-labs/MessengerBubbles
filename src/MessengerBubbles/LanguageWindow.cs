using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MessengerBubbles.Core;

namespace MessengerBubbles;

// Asked once, on first run, in both languages.
class LanguageWindow : Window
{
    AppLanguage _choice = Texts.DefaultFor(CultureInfo.CurrentUICulture.Name);

    LanguageWindow()
    {
        Title = "Messenger Bubbles";
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        buttons.Children.Add(Choice("Tiếng Việt", AppLanguage.Vietnamese));
        buttons.Children.Add(Choice("English", AppLanguage.English));

        var root = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
        root.Children.Add(new TextBlock
        {
            Text = "Chọn ngôn ngữ / Choose language", FontSize = 15, Margin = new Thickness(0, 0, 0, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        root.Children.Add(buttons);
        Content = root;
    }

    Button Choice(string text, AppLanguage language)
    {
        var button = new Button
        {
            Content = text, Width = 110, Height = 32, Margin = new Thickness(6, 0, 6, 0),
            IsDefault = language == _choice,
        };
        button.Click += (_, _) =>
        {
            _choice = language;
            Close();
        };
        return button;
    }

    public static AppLanguage Ask()
    {
        var window = new LanguageWindow();
        window.ShowDialog();
        return window._choice;
    }
}
