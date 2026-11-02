# MessengerBubbles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** App Windows 11 hiện mỗi cuộc chat Facebook có tin mới thành một bong bóng nổi, bấm vào đọc và trả lời được.

**Architecture:** Logic thuần nằm trong `MessengerBubbles.Core` (không phụ thuộc WPF, có unit test). `MessengerBubbles` là app WPF: một WebView2 ẩn chạy probe JS đọc danh sách chat, chuyển dữ liệu cho Core, rồi Core quyết định bóng nào hiện. Mọi WebView2 dùng chung một profile.

**Tech Stack:** C#, .NET 8, WPF, `Microsoft.Web.WebView2` 1.0.2903.40, xUnit. Khay hệ thống dùng `System.Windows.Forms.NotifyIcon` (`UseWindowsForms=true`).

**Spec:** `docs/2026-11-02-messenger-bubbles-design.md`

## Global Constraints

- Thư mục dữ liệu: `%LOCALAPPDATA%\MessengerBubbles\` (`profile\`, `settings.json`, log).
- Không ghi tên, nội dung tin, avatar xuống đĩa. Log chỉ ghi trạng thái và lỗi.
- Chỉ nhận tin nhắn từ trang có `https` và host `facebook.com` hoặc `*.facebook.com`.
- App không lưu, không nhập mật khẩu; không gửi tin tự động.
- Mọi chữ trên giao diện bằng tiếng Việt. Không dùng logo Messenger.
- Đổi lên .NET 10 chỉ được phải sửa `TargetFramework`.
- Thư mục làm việc chưa có git: bỏ các bước commit trừ khi người dùng bảo khởi tạo repo.

## Review Focus

1. Nhãn thời gian đổi ("1 phút" thành "2 phút") trên dòng đang đậm: không được bật bóng. Test ở Task 2.
2. Người chưa có trong danh sách đã tải nhắn đến, dòng mới xuất hiện và đậm: phải bật bóng. Test ở Task 2.
3. Tải lại trang hoặc đăng nhập lại khi đang có nhiều tin chưa đọc: không được bật bóng hàng loạt. Test ở Task 2.
4. Rút màn hình phụ, vị trí đã lưu nằm ngoài mọi màn hình: bóng phải về màn hình đang có. Test ở Task 3.
5. `settings.json` hỏng: app vẫn chạy với mặc định. Test ở Task 4.

---

### Task 1: Kiểm tra hai điểm chặn bằng bản thử

Hai kết quả này có thể đổi cách làm trình theo dõi. Task 2-4 làm song song được trong lúc chờ; Task 5 trở đi phải đợi kết quả.

**Files:**
- Modify: `BubbleSpike/Program.cs`

**Interfaces:**
- Produces: kết luận ghi vào `docs/superpowers/plans/2026-11-02-gate-results.md`: URL trình theo dõi, cách ẩn cửa sổ, và cấu trúc dòng chat.

- [ ] **Step 1: Thêm vào bản thử**

Đặt `AdditionalBrowserArguments = "--disable-background-timer-throttling --disable-renderer-backgrounding"`, đổi nút thành ẩn 8 giờ, và ghi log cấu trúc dòng: số dòng chữ và dòng cuối có khớp mẫu thời gian không, không ghi nội dung.

- [ ] **Step 2: Kiểm tra đánh dấu đã đọc (người dùng làm)**

Nhờ nhắn một tin khi app đang mở và không bấm gì trong app. Đạt: trên điện thoại tin vẫn là chưa đọc, và trong log `/messages` không tự mở cuộc chat có tin mới. Không đạt: thử URL khác và dừng để báo người dùng trước khi làm tiếp.

- [ ] **Step 3: Kiểm tra chạy nền 8 giờ (người dùng làm)**

Ẩn cửa sổ 8 giờ, rồi nhờ nhắn một tin và ghi lại giờ gửi. Đạt: log có `dom-row-changed` đậm trong vòng 10 giây. Không đạt: thử cửa sổ đặt ngoài màn hình thay vì ẩn, rồi đo lại.

- [ ] **Step 4: Ghi kết luận vào file gate-results**

### Task 2: Phát hiện tin mới

**Files:**
- Create: `MessengerBubbles.sln`, `src/MessengerBubbles.Core/MessengerBubbles.Core.csproj` (`net8.0`), `tests/MessengerBubbles.Tests/MessengerBubbles.Tests.csproj` (xUnit)
- Create: `src/MessengerBubbles.Core/RowSnapshot.cs`, `RowText.cs`, `NewMessageDetector.cs`
- Test: `tests/MessengerBubbles.Tests/NewMessageDetectorTests.cs`, `RowTextTests.cs`

**Interfaces:**
- Produces:
  - `record RowSnapshot(string ThreadHref, string Text, bool Bold, string AvatarUrl)`
  - `static string RowText.WithoutTime(string text)`; `static (string Name, string Preview) RowText.Parse(string text)`
  - `record ScanResult(IReadOnlyList<RowSnapshot> NewMessages, IReadOnlyList<string> NowRead, int UnreadThreads)`
  - `class NewMessageDetector { ScanResult Scan(IReadOnlyList<RowSnapshot> rows); void Reset(); }`

- [ ] **Step 1: Viết test thất bại**

```csharp
[Fact] void FirstScan_NeverReportsNew()            // lần đầu, dòng đậm: NewMessages rỗng, UnreadThreads == 1
[Fact] void BoldAndTextChanged_IsNew()
[Fact] void NotBoldToBold_IsNew()
[Fact] void TextChangedNotBold_IsNotNew()          // trường hợp E92B3C80 trong bản thử
[Fact] void BoldUnchanged_IsNotNew()
[Fact] void TimeLabelChangedOnBoldRow_IsNotNew()   // "An\nChào · 1 phút" thành "An\nChào · 2 phút"
[Fact] void UnseenBoldRowAfterFirstScan_IsNew()
[Fact] void UnseenNotBoldRow_IsNotNew()
[Fact] void BoldToNotBold_ReportedInNowRead()
[Fact] void AfterReset_NextScanIsBaselineAgain()
```
`RowTextTests`: bỏ được `1 phút`, `2 giờ`, `3 ngày`, `1 tuần`, `5m`, `2h`, `Vừa xong`, `Just now`; `Parse` tách dòng đầu làm tên, phần còn lại không có thời gian làm xem trước; chuỗi rỗng không ném lỗi. Chỉnh mẫu theo cấu trúc Task 1 nếu có kết quả.

- [ ] **Step 2: Chạy và xác nhận thất bại**

Run: `dotnet test` — Expected: lỗi biên dịch vì chưa có type.

- [ ] **Step 3: Viết code**

So sánh bằng `RowText.WithoutTime(Text)`, lưu trạng thái theo `ThreadHref`. Thêm comment: `shortcut: hai tin giống hệt liên tiếp trên dòng đang đậm không được đếm, sửa nếu tìm được tín hiệu theo từng tin`.

- [ ] **Step 4: Chạy và xác nhận đạt**

Run: `dotnet test` — Expected: tất cả đạt.

### Task 3: Danh sách bóng và tính vị trí

**Files:**
- Create: `src/MessengerBubbles.Core/BubbleSet.cs`, `Geometry.cs`
- Test: `tests/MessengerBubbles.Tests/BubbleSetTests.cs`, `GeometryTests.cs`

**Interfaces:**
- Consumes: `RowSnapshot`
- Produces:
  - `record BubbleState(string ThreadHref, string Name, string AvatarUrl, int Unread)`
  - `class BubbleSet { const int Max = 5; IReadOnlyList<BubbleState> Visible; int Overflow; void OnNewMessage(RowSnapshot r); void MarkRead(string href); void Dismiss(string href); }`
  - `record Box(double X, double Y, double W, double H)`
  - `static class Geometry { double SnapX(Box bubble, Box workArea); Box PlacePanel(Box bubble, double w, double h, Box workArea); Box RestoreOnScreen(Box saved, IReadOnlyList<Box> workAreas); bool InDismissZone(Box bubble, Box workArea); }`

- [ ] **Step 1: Viết test thất bại**

```csharp
[Fact] void NewThread_AddsBubbleWithUnreadOne()
[Fact] void SecondMessageSameThread_IncrementsOnly()
[Fact] void Full_ClosesOldestWithoutUnread()
[Fact] void FullAllUnread_GoesToOverflow()       // Visible.Count == 5, Overflow == 1
[Fact] void FreedSlot_PromotesOldestOverflow()
[Fact] void MarkRead_ClearsBadgeKeepsBubble()
[Fact] void Dismissed_ReappearsOnNextMessage()
[Fact] void SnapX_GoesToNearestEdge()
[Fact] void PlacePanel_StaysInsideWorkArea()     // bóng sát từng góc, khung 380x560 không tràn
[Fact] void RestoreOnScreen_OffScreenSaved_MovesToFirstWorkArea()
[Fact] void InDismissZone_OnlyNearBottomCentre()
```

- [ ] **Step 2: Chạy và xác nhận thất bại** — Run: `dotnet test` — Expected: lỗi biên dịch.
- [ ] **Step 3: Viết code**
- [ ] **Step 4: Chạy và xác nhận đạt** — Run: `dotnet test` — Expected: tất cả đạt.

### Task 4: Tùy chọn, khởi động cùng Windows và các bộ đếm phụ

**Files:**
- Create: `src/MessengerBubbles.Core/Settings.cs`, `Autostart.cs`, `SoundGate.cs`, `Backoff.cs`, `FacebookUrl.cs`, `RowsHealth.cs`
- Test: `tests/MessengerBubbles.Tests/SettingsTests.cs`, `AutostartTests.cs`, `HelpersTests.cs`

**Interfaces:**
- Produces:
  - `record Settings { bool PersistentBubble=false; bool Preview=false; bool Autostart=false; Dictionary<string,Box> Positions; static Settings Load(string path); void Save(string path); }`
  - `static class Autostart { void Set(bool on, string exePath, string subKey = @"Software\Microsoft\Windows\CurrentVersion\Run"); bool IsOn(string subKey = ...); }` — tên giá trị `MessengerBubbles`, đường dẫn có ngoặc kép
  - `class SoundGate { bool ShouldPlay(DateTime now); }` — 3 giây
  - `static class Backoff { TimeSpan Next(int attempt); }` — 5, 10, 20... giây, tối đa 300 giây
  - `static class FacebookUrl { bool IsTrusted(string uri); bool IsLoginPage(string uri); string? ToThreadUrl(string href); }`
  - `class RowsHealth { bool IsBroken(int rowCount, DateTime now); }` — 2 phút không có dòng

- [ ] **Step 1: Viết test thất bại**

```csharp
[Fact] void Load_Missing_ReturnsDefaultsAllOff()
[Fact] void Load_Corrupt_ReturnsDefaults()
[Fact] void SaveLoad_RoundTrips()
[Fact] void Autostart_SetAndClear()               // dùng subKey tạm dưới HKCU, xóa khi xong
[Fact] void SoundGate_SecondWithin3s_False()
[Fact] void Backoff_DoublesAndCapsAt300s()
[Fact] void IsTrusted_RejectsLookalikes()         // evilfacebook.com, http://, facebook.com.evil.com
[Fact] void ToThreadUrl_RelativeOk_ForeignNull()  // /messages/t/1, /messages/e2ee/t/1; https://evil.com/... ra null
[Fact] void IsLoginPage_DetectsLoginAndCheckpoint()
[Fact] void RowsHealth_BrokenOnlyAfter2Min_ResetsOnRows()
```

- [ ] **Step 2: Chạy và xác nhận thất bại** — Run: `dotnet test` — Expected: lỗi biên dịch.
- [ ] **Step 3: Viết code**
- [ ] **Step 4: Chạy và xác nhận đạt** — Run: `dotnet test` — Expected: tất cả đạt.

### Task 5: Trình theo dõi và khung app

Chờ kết quả Task 1.

**Files:**
- Create: `src/MessengerBubbles/MessengerBubbles.csproj` (`net8.0-windows`, `UseWPF`, `UseWindowsForms`), `Program.cs`, `AppPaths.cs`, `Watcher.cs`, `Assets/probe.js` (nhúng làm resource)

**Interfaces:**
- Consumes: `NewMessageDetector`, `FacebookUrl`, `RowsHealth`, `Backoff`
- Produces:
  - `class Watcher { Task StartAsync(CoreWebView2Environment env); event Action<ScanResult> Scanned; event Action NeedsLogin; event Action<bool> HealthChanged; void ShowWindow(); }`
  - `static Task<CoreWebView2Environment> AppPaths.CreateEnvironmentAsync()` — profile dùng chung và hai cờ chạy nền

- [ ] **Step 1: Viết `probe.js`**

Chỉ chạy ở frame chính. Dùng `MutationObserver` kèm quét định kỳ 5 giây làm lưới an toàn; gửi cả danh sách dòng khi có đổi; selector `a[href*="/messages/t/"],a[href*="/messages/e2ee/t/"]`; chữ đậm theo `fontWeight >= 600` như bản thử.

- [ ] **Step 2: Viết `Watcher`**

Dùng URL và cách ẩn đã chốt ở Task 1. Đặt `IsMuted = true`. Bỏ qua tin nhắn có `Source` không qua `FacebookUrl.IsTrusted`. Khi điều hướng xong hoặc tải lại thì gọi `detector.Reset()`. Vào trang đăng nhập thì phát `NeedsLogin`. Bắt `ProcessFailed` thì tạo lại. Lỗi tải trang thì thử lại theo `Backoff`.

- [ ] **Step 3: Viết `Program.cs`**

Một phiên chạy bằng named mutex `MessengerBubbles.SingleInstance`; phiên thứ hai thoát ngay. Tạm ghi số lượng `NewMessages` vào log, không ghi nội dung.

- [ ] **Step 4: Kiểm tra với tin thật**

Run: `dotnet run --project src/MessengerBubbles`. Expected: nhắn một tin, log có đúng `new=1`; tải lại trang khi còn tin chưa đọc, log có `new=0`.

### Task 6: Cửa sổ bóng

**Files:**
- Create: `src/MessengerBubbles/BubbleWindow.cs`, `BubbleController.cs`, `DismissZoneWindow.cs`

**Interfaces:**
- Consumes: `Watcher.Scanned`, `BubbleSet`, `Geometry`, `Settings.Positions`
- Produces:
  - `class BubbleController { void Apply(ScanResult r); event Action<string, Box> BubbleClicked; }`
  - `BubbleClicked` nhận `ThreadHref`, hoặc chuỗi rỗng cho bóng `+N` và bóng thường trực

- [ ] **Step 1: Viết `BubbleWindow`**

56 px, tròn, trong suốt, `Topmost`, `ShowInTaskbar=false`, thêm `WS_EX_NOACTIVATE` và `WS_EX_TOOLWINDOW` qua `SetWindowLong`, Per-Monitor V2 DPI trong manifest. Avatar nạp vào `BitmapImage` trong bộ nhớ ngay khi phát hiện. Chấm đỏ hiện `Unread`. Avatar lỗi thì hiện chữ cái đầu của tên.

- [ ] **Step 2: Viết kéo thả và đóng**

Phân biệt bấm và kéo bằng ngưỡng 4 px. Thả thì dùng `Geometry.SnapX`, rồi lưu vị trí. `DismissZoneWindow` chỉ hiện trong lúc kéo; thả trong vùng thì `BubbleSet.Dismiss`.

- [ ] **Step 3: Kiểm tra thủ công**

Expected: nhắn từ hai người ra hai bóng và đang gõ ở app khác không mất focus. Kéo hít cạnh được trên cả hai màn hình nếu có. Kéo xuống đáy thì đóng, tin tiếp theo bật lại. Đọc trên điện thoại thì mất chấm đỏ, bóng vẫn còn.

### Task 7: Khung chat

**Files:**
- Create: `src/MessengerBubbles/ChatPanel.cs`

**Interfaces:**
- Consumes: `BubbleController.BubbleClicked`, `Geometry.PlacePanel`, `FacebookUrl.ToThreadUrl`, `AppPaths.CreateEnvironmentAsync`
- Produces: `class ChatPanel { void Toggle(string threadHref, Box bubble); }` — chuỗi rỗng mở `https://www.facebook.com/messages`

- [ ] **Step 1: Viết `ChatPanel`**

Một cửa sổ 380 × 560 px dùng chung, `Topmost`, có một WebView2. Bấm bóng khác thì chuyển cuộc chat, bấm lại đúng bóng thì đóng, mất focus thì đóng. Mở link ngoài `facebook.com` bằng trình duyệt mặc định, không mở trong khung.

- [ ] **Step 2: Kiểm tra thủ công**

Expected: đọc và trả lời được ở khung nhỏ với chat thường, chat nhóm và chat mã hóa; gõ tiếng Việt Telex được; khung không tràn màn hình ở bốn góc; mở khung thì tin thành đã đọc. Trang Facebook không hiển thị tốt ở 380 px thì báo người dùng trước khi chỉnh CSS của trang.

### Task 8: Tùy chọn, âm thanh, khay hệ thống

**Files:**
- Create: `src/MessengerBubbles/TrayIcon.cs`, `PreviewWindow.cs`, `Assets/new-message.wav`, `Assets/tray.ico`, `Assets/tray-error.ico`
- Modify: `src/MessengerBubbles/BubbleController.cs`, `src/MessengerBubbles/Program.cs`

**Interfaces:**
- Consumes: `Settings`, `Autostart`, `SoundGate`, `RowText.Parse`, `Watcher.NeedsLogin`, `Watcher.HealthChanged`

- [ ] **Step 1: Viết `TrayIcon`**

Menu đúng chữ: `Mở Facebook`, `Bóng thường trực`, `Xem trước nội dung`, `Khởi động cùng Windows`, `Thoát`. Mỗi tùy chọn lưu và có hiệu lực ngay. Hết phiên hoặc lỗi giao diện thì đổi sang `tray-error.ico` và hiện thông báo: `Cần đăng nhập lại Facebook`, `Không đọc được danh sách chat. Facebook có thể đã đổi giao diện`.

- [ ] **Step 2: Thêm bóng thường trực vào `BubbleController`**

Hiện `UnreadThreads`, không tính vào `BubbleSet.Max`, không đóng được bằng kéo xuống đáy.

- [ ] **Step 3: Thêm xem trước và âm thanh**

`PreviewWindow` hiện tên và đoạn đầu cạnh bóng khoảng 5 giây khi `Settings.Preview` bật, không lấy focus. Phát WAV bằng `SoundPlayer` khi `SoundGate.ShouldPlay` trả về đúng.

- [ ] **Step 4: Kiểm tra thủ công**

Expected: bật tắt từng tùy chọn có hiệu lực ngay và còn sau khi mở lại app. Tùy chọn khởi động thêm và xóa đúng giá trị trong `Run`. Hai tin cách nhau dưới 3 giây chỉ kêu một lần, và không kêu trùng âm Facebook. Xem trước tắt thì không hiện nội dung.

### Task 9: Kiểm tra toàn bộ với tin thật

**Files:**
- Modify: `README.md` (tạo mới: cách chạy, thư mục dữ liệu, cách xóa dữ liệu và đăng xuất)

- [ ] **Step 1: Chạy test**

Run: `dotnet test` — Expected: tất cả đạt.

- [ ] **Step 2: Đóng gói bản chạy**

Run: `dotnet publish src/MessengerBubbles -c Release -o publish`. Expected: có `publish\MessengerBubbles.exe` chạy được, mở lần hai không tạo phiên thứ hai.

- [ ] **Step 3: Rà soát "coi là thất bại" trong spec với tin thật**

Expected: mở app không làm tin thành đã đọc, bóng không cướp focus, thư mục dữ liệu không có tên hay nội dung tin (tìm thử bằng một tên có thật trong `settings.json` và log), và đổi selector trong `probe.js` thành sai thì trong khoảng 2 phút khay báo lỗi.

- [ ] **Step 4: Chạy nền qua đêm**

Expected: sáng hôm sau, tin đến vẫn bật bóng trong vòng 10 giây.
