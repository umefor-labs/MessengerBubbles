using System.Diagnostics;
using System.Drawing;
using MessengerBubbles.Core;
using WinForms = System.Windows.Forms;

namespace MessengerBubbles;

class TrayIcon : IDisposable
{
    readonly WinForms.NotifyIcon _icon = new() { Text = $"Messenger Bubbles {UpdateCheck.Display(UpdateChecker.Current)}", Visible = true };
    readonly Icon _ok = Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
    readonly Icon _error = DrawError();
    readonly Settings _settings;
    readonly Action _openFacebook, _settingsChanged, _exit;
    readonly Func<Task<(CheckResult Result, Version? Newer)>> _checkNow;
    bool _login, _broken, _updateBalloon;

    public TrayIcon(Settings settings, Action openFacebook, Action settingsChanged, Action exit,
        Func<Task<(CheckResult Result, Version? Newer)>> checkNow)
    {
        _settings = settings;
        _checkNow = checkNow;
        _openFacebook = openFacebook;
        _settingsChanged = settingsChanged;
        _exit = exit;
        _icon.Icon = _ok;
        _icon.ContextMenuStrip = BuildMenu();
        _icon.DoubleClick += (_, _) => openFacebook();
        _icon.BalloonTipClicked += (_, _) =>
        {
            if (_updateBalloon) Process.Start(new ProcessStartInfo(UpdateCheck.PageUrl) { UseShellExecute = true });
        };
    }

    WinForms.ContextMenuStrip BuildMenu()
    {
        var texts = Texts.Current;
        var settings = _settings;
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add(texts.OpenFacebook, null, (_, _) => _openFacebook());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(Toggle(texts.PersistentBubble, settings.PersistentBubble, on =>
        {
            settings.PersistentBubble = on;
        }));
        menu.Items.Add(Toggle(texts.GroupBubbles, settings.GroupBubbles, on =>
        {
            settings.GroupBubbles = on;
        }));
        menu.Items.Add(Toggle(texts.Preview, settings.Preview, on =>
        {
            settings.Preview = on;
        }));
        menu.Items.Add(Toggle(texts.Sound, settings.Sound, on =>
        {
            settings.Sound = on;
        }));
        menu.Items.Add(SnoozeMenu());
        menu.Items.Add(SizeMenu());
        menu.Items.Add(LanguageMenu());
        menu.Items.Add(Toggle(texts.Autostart, Autostart.IsOn(), on =>
        {
            Autostart.Set(on, Environment.ProcessPath!);
            settings.Autostart = on;
        }));
        menu.Items.Add(Toggle(texts.CheckUpdates, settings.CheckUpdates, on =>
        {
            settings.CheckUpdates = on;
        }));
        menu.Items.Add(Toggle(texts.DisableGpu, settings.DisableGpu, on =>
        {
            settings.DisableGpu = on;
            Balloon(texts.RestartToApply, WinForms.ToolTipIcon.Info);
        }));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(new WinForms.ToolStripMenuItem(string.Format(texts.VersionLabel, UpdateCheck.Display(UpdateChecker.Current))) { Enabled = false });
        menu.Items.Add(CheckNowItem());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(texts.Exit, null, (_, _) => _exit());
        return menu;

        WinForms.ToolStripMenuItem CheckNowItem()
        {
            var item = new WinForms.ToolStripMenuItem(texts.CheckNow);
            item.Click += async (_, _) =>
            {
                item.Enabled = false;
                try
                {
                    var (result, newer) = await _checkNow();
                    switch (result)
                    {
                        case CheckResult.Newer: NotifyUpdate(newer!); break;
                        case CheckResult.UpToDate:
                            Balloon(string.Format(Texts.Current.UpToDate, UpdateCheck.Display(UpdateChecker.Current)), WinForms.ToolTipIcon.Info);
                            break;
                        default: Balloon(Texts.Current.CheckFailed, WinForms.ToolTipIcon.Warning); break;
                    }
                }
                finally { item.Enabled = true; }
            };
            return item;
        }

        WinForms.ToolStripMenuItem SizeMenu()
        {
            var parent = new WinForms.ToolStripMenuItem(texts.PanelSize);
            foreach (var (text, size) in new[] { (texts.Small, PanelSize.Small), (texts.Medium, PanelSize.Medium), (texts.Large, PanelSize.Large) })
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

        WinForms.ToolStripMenuItem SnoozeMenu()
        {
            var parent = new WinForms.ToolStripMenuItem(texts.Snooze);
            var off = new WinForms.ToolStripMenuItem(texts.SnoozeOff, null, (_, _) => Set(null));
            parent.DropDownItems.Add(texts.SnoozeHour, null, (_, _) => Set(Snooze.ForOneHour(DateTime.Now)));
            parent.DropDownItems.Add(texts.SnoozeMorning, null, (_, _) => Set(Snooze.UntilMorning(DateTime.Now)));
            parent.DropDownItems.Add(new WinForms.ToolStripSeparator());
            parent.DropDownItems.Add(off);
            // The pause ends by itself, so the label is worked out each time the menu opens.
            menu.Opening += (_, _) =>
            {
                var on = settings.IsSnoozed(DateTime.Now);
                parent.Text = on ? string.Format(texts.SnoozedUntil, settings.SnoozedUntil) : texts.Snooze;
                parent.Checked = on;
                off.Enabled = on;
            };
            return parent;

            void Set(DateTime? until)
            {
                settings.SnoozedUntil = until;
                settings.Save(AppPaths.SettingsFile);
                _settingsChanged();
            }
        }

        WinForms.ToolStripMenuItem LanguageMenu()
        {
            var parent = new WinForms.ToolStripMenuItem(texts.Language);
            foreach (var (text, language) in new[] { ("Tiếng Việt", AppLanguage.Vietnamese), ("English", AppLanguage.English) })
            {
                var item = new WinForms.ToolStripMenuItem(text) { Checked = settings.Language == language };
                item.Click += (_, _) =>
                {
                    settings.Language = language;
                    settings.Save(AppPaths.SettingsFile);
                    Texts.Current = Texts.For(language);
                    _icon.ContextMenuStrip = BuildMenu();
                    _settingsChanged();
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
                _settingsChanged();
            };
            return item;
        }
    }

    // A click only opens the release page after a balloon that said so.
    void Balloon(string text, WinForms.ToolTipIcon kind, bool update = false)
    {
        _updateBalloon = update;
        _icon.ShowBalloonTip(5000, "Messenger Bubbles", text, kind);
    }

    public void NotifyUpdate(Version version) =>
        Balloon(string.Format(Texts.Current.UpdateAvailable, version), WinForms.ToolTipIcon.Info, update: true);

    public void SetLogin(bool needed)
    {
        _login = needed;
        _icon.Icon = _login || _broken ? _error : _ok;
        if (needed) Balloon(Texts.Current.NeedLogin, WinForms.ToolTipIcon.Warning);
    }

    public void NotifyPanelBroken() => Balloon(Texts.Current.PanelBroken, WinForms.ToolTipIcon.Error);

    public void SetBroken(bool broken)
    {
        _broken = broken;
        _icon.Icon = _login || _broken ? _error : _ok;
        if (broken) Balloon(Texts.Current.ListBroken, WinForms.ToolTipIcon.Error);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    // The app icon's shape in red.
    static Icon DrawError()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(Color.FromArgb(220, 40, 40)))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillEllipse(brush, 1, 1, 30, 30);
            g.FillRectangle(Brushes.White, 8, 9, 16, 11);
            g.FillPolygon(Brushes.White, [new Point(11, 20), new Point(16, 20), new Point(11, 25)]);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
