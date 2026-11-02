using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Media.Imaging;
using MessengerBubbles.Core;

namespace MessengerBubbles;

class BubbleController
{
    const double Spacing = 8;
    const int MaxAvatarBytes = 2_000_000;
    const string AnchorKey = "anchor", PersistentKey = "persistent";

    readonly Func<string, Task<byte[]?>> _loadResource;
    readonly Func<string, Task<bool>> _isShowing;
    readonly Settings _settings;
    readonly BubbleSet _set = new();
    readonly Dictionary<string, BubbleWindow> _windows = new();
    readonly DismissZoneWindow _zone = new();
    readonly PreviewWindow _preview = new();
    readonly SoundGate _sound = new();
    readonly SoundPlayer _player;
    BubbleWindow? _overflow, _persistent;
    int _unreadThreads;

    public event Action<string, Box>? BubbleClicked;

    public BubbleController(Settings settings, Func<string, Task<byte[]?>> loadResource, Func<string, Task<bool>> isShowing)
    {
        _settings = settings;
        _loadResource = loadResource;
        _isShowing = isShowing;
        _player = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("new-message.wav"));
        _player.Load();
    }

    public async void Apply(ScanResult r)
    {
        // The user is already reading the conversation open in the panel.
        var fresh = new List<RowSnapshot>();
        foreach (var row in r.NewMessages)
            if (!await _isShowing(row.ThreadHref))
                fresh.Add(row);

        foreach (var row in fresh) _set.OnNewMessage(row);
        foreach (var href in r.NowRead) _set.MarkRead(href);
        _unreadThreads = r.UnreadThreads;
        Refresh();

        foreach (var row in fresh)
            if (_windows.TryGetValue(row.ThreadHref, out var w))
                _ = LoadAvatarAsync(w, row.AvatarUrl);

        if (fresh.Count == 0) return;
        if (_sound.ShouldPlay(DateTime.Now)) _player.Play();
        var last = fresh[^1];
        if (_settings.Preview && _windows.TryGetValue(last.ThreadHref, out var bubble))
        {
            var (name, text) = RowText.Parse(last.Text);
            _preview.ShowBeside(bubble.PixelBox(), name, text);
        }
    }

    // Called after a scan and whenever a setting changes.
    public void Refresh()
    {
        foreach (var href in _windows.Keys.Except(_set.Visible.Select(b => b.ThreadHref)).ToList())
        {
            _windows[href].Close();
            _windows.Remove(href);
        }
        foreach (var b in _set.Visible)
        {
            if (!_windows.TryGetValue(b.ThreadHref, out var w))
            {
                w = NewBubble(b.Name, Initial(b.Name), () => b.ThreadHref, canDismiss: true);
                _windows[b.ThreadHref] = w;
                PlaceInFreeSlot(w);
            }
            w.SetBadge(b.Unread);
        }
        RefreshOverflow();
        RefreshPersistent();
    }

    void RefreshOverflow()
    {
        if (_set.Overflow == 0)
        {
            _overflow?.Close();
            _overflow = null;
            return;
        }
        if (_overflow == null)
        {
            _overflow = NewBubble("Các cuộc chat khác", "", () => "", canDismiss: false);
            PlaceInFreeSlot(_overflow);
        }
        _overflow.SetLabel($"+{_set.Overflow}");
    }

    void RefreshPersistent()
    {
        if (!_settings.PersistentBubble)
        {
            _persistent?.Close();
            _persistent = null;
            return;
        }
        if (_persistent == null)
        {
            _persistent = NewBubble("Danh sách chat", "💬", () => "", canDismiss: false, PersistentKey);
            _persistent.Show();
            if (_settings.Positions.TryGetValue(PersistentKey, out var saved))
            {
                var box = Geometry.RestoreOnScreen(saved, FloatingWindow.AllWorkAreas());
                _persistent.MoveTo(box.X, box.Y);
            }
            else
            {
                PlaceInFreeSlot(_persistent);
            }
        }
        _persistent.SetBadge(_unreadThreads);
    }

    BubbleWindow NewBubble(string name, string label, Func<string> href, bool canDismiss, string positionKey = AnchorKey)
    {
        var w = new BubbleWindow(name, label);
        w.Clicked += () => BubbleClicked?.Invoke(href(), w.PixelBox());
        w.DragStarted += () =>
        {
            if (!canDismiss) return;
            var c = FloatingWindow.CursorPixel();
            _zone.ShowOn(FloatingWindow.WorkAreaAt(c.X, c.Y));
        };
        w.DragMoved += () =>
        {
            if (!canDismiss) return;
            var box = w.PixelBox();
            _zone.SetHot(Geometry.InDismissZone(box, FloatingWindow.WorkAreaAt(box.X, box.Y)));
        };
        w.DragEnded += () =>
        {
            var box = w.PixelBox();
            var area = FloatingWindow.WorkAreaAt(box.X, box.Y);
            _zone.Hide();
            if (canDismiss && Geometry.InDismissZone(box, area))
            {
                _set.Dismiss(href());
                Refresh();
                return;
            }
            var snapped = box with { X = Geometry.SnapX(box, area) };
            w.MoveTo(snapped.X, snapped.Y);
            _settings.Positions[positionKey] = snapped;
            _settings.Save(AppPaths.SettingsFile);
        };
        return w;
    }

    void PlaceInFreeSlot(BubbleWindow w)
    {
        w.Show();
        var size = w.PixelBox();
        var areas = FloatingWindow.AllWorkAreas();
        var slot = _settings.Positions.TryGetValue(AnchorKey, out var saved)
            ? Geometry.RestoreOnScreen(saved, areas)
            : new Box(areas[0].X + areas[0].W - size.W, areas[0].Y + areas[0].H / 4, size.W, size.H);
        var area = FloatingWindow.WorkAreaAt(slot.X, slot.Y);
        var others = _windows.Values.Append(_overflow).Append(_persistent)
            .Where(o => o != null && o != w && o.IsVisible).Select(o => o!.PixelBox()).ToList();

        var free = Geometry.FindFreeSlot(slot with { W = size.W, H = size.H }, others, area, Spacing);
        w.MoveTo(free.X, free.Y);
    }

    static string Initial(string name) => name.Length > 0 ? name[..1].ToUpper() : "?";

    async Task LoadAvatarAsync(BubbleWindow w, string url)
    {
        var bytes = await _loadResource(url);
        if (bytes == null || bytes.Length > MaxAvatarBytes) return;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(bytes);
            image.DecodePixelWidth = 112;
            image.EndInit();
            image.Freeze();
            w.SetAvatar(image);
        }
        catch (Exception e) when (e is NotSupportedException or IOException or InvalidOperationException)
        {
            AppPaths.Log($"avatar failed: {e.GetType().Name}");
        }
    }
}
