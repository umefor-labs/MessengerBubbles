using System.Drawing;
using MessengerBubbles.Core;
using WinForms = System.Windows.Forms;

namespace MessengerBubbles;

class TrayIcon : IDisposable
{
    readonly WinForms.NotifyIcon _icon = new() { Text = "Messenger Bubbles", Visible = true };
    readonly Icon _ok = Draw(Color.FromArgb(8, 102, 255));
    readonly Icon _error = Draw(Color.FromArgb(220, 40, 40));
    bool _login, _broken;

    public TrayIcon(Settings settings, Action openFacebook, Action settingsChanged, Action exit)
    {
        _icon.Icon = _ok;
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Mở Facebook", null, (_, _) => openFacebook());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(Toggle("Bóng thường trực", settings.PersistentBubble, on =>
        {
            settings.PersistentBubble = on;
        }));
        menu.Items.Add(Toggle("Gom bóng", settings.GroupBubbles, on =>
        {
            settings.GroupBubbles = on;
        }));
        menu.Items.Add(Toggle("Xem trước nội dung", settings.Preview, on =>
        {
            settings.Preview = on;
        }));
        menu.Items.Add(Toggle("Âm báo", settings.Sound, on =>
        {
            settings.Sound = on;
        }));
        menu.Items.Add(SizeMenu());
        menu.Items.Add(Toggle("Khởi động cùng Windows", Autostart.IsOn(), on =>
        {
            Autostart.Set(on, Environment.ProcessPath!);
            settings.Autostart = on;
        }));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Thoát", null, (_, _) => exit());
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => openFacebook();

        WinForms.ToolStripMenuItem SizeMenu()
        {
            var parent = new WinForms.ToolStripMenuItem("Kích thước khung chat");
            foreach (var (text, size) in new[] { ("Nhỏ", PanelSize.Small), ("Vừa", PanelSize.Medium), ("Lớn", PanelSize.Large) })
            {
                var item = new WinForms.ToolStripMenuItem(text) { Checked = settings.PanelSize == size };
                item.Click += (_, _) =>
                {
                    foreach (WinForms.ToolStripMenuItem other in parent.DropDownItems) other.Checked = other == item;
                    settings.PanelSize = size;
                    settings.Save(AppPaths.SettingsFile);
                };
                parent.DropDownItems.Add(item);
            }
            return parent;
        }

        WinForms.ToolStripMenuItem Toggle(string text, bool state, Action<bool> apply)
        {
            var item = new WinForms.ToolStripMenuItem(text) { CheckOnClick = true, Checked = state };
            item.CheckedChanged += (_, _) =>
            {
                apply(item.Checked);
                settings.Save(AppPaths.SettingsFile);
                settingsChanged();
            };
            return item;
        }
    }

    public void SetLogin(bool needed)
    {
        _login = needed;
        _icon.Icon = _login || _broken ? _error : _ok;
        if (needed)
            _icon.ShowBalloonTip(5000, "Messenger Bubbles", "Cần đăng nhập lại Facebook", WinForms.ToolTipIcon.Warning);
    }

    public void NotifyPanelBroken() =>
        _icon.ShowBalloonTip(5000, "Messenger Bubbles", "Không mở được khung chat. Facebook có thể đã đổi giao diện", WinForms.ToolTipIcon.Error);

    public void SetBroken(bool broken)
    {
        _broken = broken;
        _icon.Icon = _login || _broken ? _error : _ok;
        if (broken)
            _icon.ShowBalloonTip(5000, "Messenger Bubbles", "Không đọc được danh sách chat. Facebook có thể đã đổi giao diện", WinForms.ToolTipIcon.Error);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    static Icon Draw(Color color)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(color))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillEllipse(brush, 2, 2, 28, 28);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
