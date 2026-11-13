namespace MessengerBubbles.Core;

public enum AppLanguage { Vietnamese, English }

// Positional, so a language missing a text does not compile.
public record Texts(
    string OpenFacebook, string PersistentBubble, string GroupBubbles, string Preview, string Sound,
    string PanelSize, string Small, string Medium, string Large, string Autostart, string Language, string Exit,
    string NeedLogin, string PanelBroken, string ListBroken, string LoginTitle, string NoRuntime,
    string OtherChats, string ChatList, string CheckUpdates, string UpdateAvailable,
    string DisableGpu, string RestartToApply,
    string Snooze, string SnoozeHour, string SnoozeMorning, string SnoozeOff, string SnoozedUntil,
    string VersionLabel, string CheckNow, string UpToDate, string CheckFailed)
{
    public static readonly Texts Vietnamese = new(
        "Mở Facebook", "Bóng thường trực", "Gom bóng", "Xem trước nội dung", "Âm báo",
        "Kích thước khung chat", "Nhỏ", "Vừa", "Lớn", "Khởi động cùng Windows", "Ngôn ngữ", "Thoát",
        "Cần đăng nhập lại Facebook",
        "Không mở được khung chat. Facebook có thể đã đổi giao diện",
        "Không đọc được danh sách chat. Facebook có thể đã đổi giao diện",
        "Messenger Bubbles: đăng nhập Facebook, xong thì đóng cửa sổ này",
        "Máy chưa có WebView2 Runtime, app không chạy được.",
        "Các cuộc chat khác", "Danh sách chat", "Báo bản mới", "Có bản mới {0}. Bấm vào đây để tải",
        "Tắt GPU (giảm RAM)", "Thoát rồi mở lại app để áp dụng",
        "Tạm im lặng", "1 giờ", "Đến 7 giờ sáng", "Bật lại thông báo", "Tạm im lặng đến {0:HH:mm}",
        "Phiên bản {0}", "Kiểm tra bản mới", "Đang dùng bản mới nhất ({0})",
        "Không kiểm tra được bản mới. Kiểm tra mạng rồi thử lại");

    public static readonly Texts English = new(
        "Open Facebook", "Persistent bubble", "Group bubbles", "Preview messages", "Sound",
        "Chat panel size", "Small", "Medium", "Large", "Start with Windows", "Language", "Quit",
        "Please log in to Facebook again",
        "Could not open the chat panel. Facebook may have changed its page",
        "Could not read the chat list. Facebook may have changed its page",
        "Messenger Bubbles: log in to Facebook, then close this window",
        "WebView2 Runtime is not installed, so the app cannot run.",
        "Other chats", "Chat list", "Notify about updates", "Version {0} is available. Click here to download",
        "Disable GPU (less RAM)", "Quit and reopen the app to apply this",
        "Pause notifications", "For 1 hour", "Until 7 AM", "Resume notifications", "Paused until {0:HH:mm}",
        "Version {0}", "Check for updates", "You have the latest version ({0})",
        "Could not check for updates. Check the network and try again");

    public static Texts Current { get; set; } = Vietnamese;

    public static Texts For(AppLanguage language) => language == AppLanguage.English ? English : Vietnamese;

    // Used when the language window is closed without a choice.
    public static AppLanguage DefaultFor(string culture) =>
        culture.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ? AppLanguage.Vietnamese : AppLanguage.English;
}
