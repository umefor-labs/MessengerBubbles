# Messenger Bubbles

App desktop cho Windows 11 hiển thị chat Facebook dạng bong bóng nổi, mỗi cuộc chat một bóng, tương tự chat head trên Android. Viết bằng WPF và WebView2, bọc trang `https://www.facebook.com/messages`. Đây là dự án cá nhân, không liên kết với Meta.

Phiên bản mới nhất: `v0.4.0`. Trạng thái từng tiêu chí nằm trong [lộ trình](docs/ROADMAP.md).

## Rủi ro

App đọc giao diện web của Facebook do không có API công khai:

- Facebook đổi cấu trúc trang là phần phát hiện tin hoặc khung chat có thể hỏng. Cách xử lý ở [danh sách kiểm tra](docs/FACEBOOK-CHECKLIST.md).
- [Điều khoản của Facebook](https://www.facebook.com/terms.php) cấm truy cập bằng phương tiện tự động nếu chưa được phép. App tự đọc danh sách chat ít nhất 5 giây một lần, nên tài khoản có thể bị hạn chế. Ghi chú đầy đủ ở [PUBLISHING.md](docs/PUBLISHING.md).

## Yêu cầu

| Thành phần | Yêu cầu |
|---|---|
| Hệ điều hành | Windows |
| WebView2 Runtime | Bắt buộc, Windows 11 có sẵn. Thiếu thì app báo lỗi rồi thoát |
| .NET | Không cần với file phát hành. Build từ mã nguồn cần SDK .NET 10 |

## Cài và chạy

Tải `MessengerBubbles-<tag>-win-x64.exe` ở GitHub Releases rồi chạy. File không có chữ ký số, nên SmartScreen có thể cảnh báo. 

Lần đầu, cửa sổ đăng nhập hiện ra. Đăng nhập Facebook, nhập mã PIN tin nhắn mã hóa nếu được hỏi, rồi đóng cửa sổ. Đóng cửa sổ chỉ ẩn nó đi. Muốn tắt app thì chọn `Thoát` trong menu khay.

## Menu khay

| Mục | Mặc định | Tác dụng |
|---|---|---|
| `Mở Facebook` | | Hiện cửa sổ trang theo dõi. Bấm đúp icon cũng vậy |
| `Bóng thường trực` | Tắt | Một bóng cố định mở danh sách chat, có số cuộc chat chưa đọc |
| `Gom bóng` | Tắt | Từ 2 bóng trở lên thì xếp chồng, bấm thì xòe, 5 giây sau gom lại |
| `Xem trước nội dung` | Bật | Hiện thẻ tên và nội dung tin bên cạnh bóng trong 5 giây |
| `Âm báo` | Bật | Âm riêng của app, cách nhau ít nhất 3 giây |
| `Kích thước khung chat` | Vừa | Nhỏ 340×480, Vừa 380×560, Lớn 460×720 (DIP). Áp dụng từ lần mở sau |
| `Khởi động cùng Windows` | Tắt | Ghi giá trị `MessengerBubbles` vào `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| `Thoát` | | Tắt app |

Icon khay chuyển đỏ khi cần đăng nhập lại hoặc khi đọc danh sách chat thất bại.

## Kiến trúc

```text
                     facebook.com/messages
                      |                 |
          Watcher (WebView2 ẩn)    ChatPanel (WebView2, treo khi ẩn)
          inject probe.js           inject panel.js
                      |                 ^
              danh sách dòng chat       | mở cuộc chat
                      v                 |
          NewMessageDetector            |
                      |                 |
                 ScanResult             |
                      v                 |
          BubbleController ------- bấm bóng
             |        |       |
         BubbleSet  BubbleWindow  PreviewWindow, DismissZoneWindow
```

Solution có ba project:

| Project | Nội dung |
|---|---|
| `src/MessengerBubbles.Core` | Logic không phụ thuộc giao diện: phát hiện tin, tập bóng, hình học, cài đặt, autostart |
| `src/MessengerBubbles` | WPF, WinForms `NotifyIcon`, WebView2, tài nguyên nhúng |
| `tests/MessengerBubbles.Tests` | xUnit, chỉ test project Core |

| File | Vai trò |
|---|---|
| `Program.cs` | Điểm vào, mutex `MessengerBubbles.SingleInstance`, nối các thành phần |
| `Watcher.cs` | WebView2 ẩn luôn ở trang danh sách, nhận dữ liệu từ `probe.js`, theo dõi đăng nhập và sức khỏe, đọc avatar |
| `ChatPanel.cs` | Khung chat, nạp sẵn và treo khi ẩn |
| `BubbleController.cs` | Áp `ScanResult` vào cửa sổ bóng, âm báo, xem trước, gom bóng |
| `BubbleWindow.cs` | Một bóng: avatar, số tin, kéo thả, hiệu ứng |
| `FloatingWindow.cs` | Lớp nền của cửa sổ nổi: vị trí theo pixel vật lý, không nhận focus |
| `OverlayWindows.cs` | Vùng đóng bóng và thẻ xem trước |
| `TrayIcon.cs` | Icon khay, menu, thông báo lỗi |
| `AppPaths.cs` | Đường dẫn, môi trường WebView2, ghi log, các hạn chế bảo mật |
| `Assets/probe.js` | Đọc dòng chat trong trang theo dõi |
| `Assets/panel.js` | CSS và hàm điều khiển trong trang khung chat |

Hai WebView2 dùng chung một thư mục hồ sơ và cùng tùy chọn, nên chung một tiến trình trình duyệt và một phiên đăng nhập.

## Phát hiện tin mới

`probe.js` tìm mọi `a[href*="/messages/t/"]` và `a[href*="/messages/e2ee/t/"]`, rồi gửi về app qua `chrome.webview.postMessage`. Mỗi dòng gồm:

| Trường | Nguồn |
|---|---|
| `href` | Thuộc tính `href` của thẻ `a` |
| `text` | `innerText` của dòng |
| `bold` | Có `span` với `font-weight` từ 600 trở lên |
| `avatar` | `src` của `img` hoặc `xlink:href` của `image` |

Lệnh gửi chạy sau mỗi thay đổi DOM, gộp trong 500 ms, và tự chạy mỗi 5 giây. App bỏ qua dữ liệu đến từ trang không phải `facebook.com` qua HTTPS.

`NewMessageDetector.Scan` so mỗi dòng với lần quét trước, sau khi `RowText` đã bỏ thời gian và nhãn trạng thái:

| Kết quả | Điều kiện |
|---|---|
| Tin mới | Đã qua thời gian chờ, dòng in đậm, và (dòng chưa từng thấy, hoặc trước đó không in đậm, hoặc nội dung đổi) |
| Đã đọc | Trước đó in đậm, giờ không in đậm |

Thời gian chờ là 10 giây tính từ lần đầu thấy dòng chat, vì Facebook dựng danh sách dần sau khi tải. Mỗi lần trang theo dõi điều hướng thì trạng thái được đặt lại. Nhờ đó nhãn "Đang hoạt động" đổi hay thời gian nhảy không tính là tin mới.

Tin của cuộc chat đang hiện trong khung chat bị lọc ở `BubbleController.Apply`, qua `window.__cbShowing`.

## Khung chat

Trang trong khung được nạp sẵn ngoài màn hình lúc khởi động, chờ có dòng chat tối đa 40 giây, rồi treo bằng `CoreWebView2.TrySuspendAsync`. Bấm bóng thì trang chạy lại ngay, không tải lại. Trang bị treo không chạy gì, nên cuộc chat để mở không làm tin đến thành đã đọc. Đây là lý do mở app không ảnh hưởng trạng thái chưa đọc.

Việc mở cuộc chat đi qua các hàm mà `panel.js` gắn vào `window`:

| Hàm | Tác dụng |
|---|---|
| `__cbOpen(href)` | Bấm dòng chat trong trang. Trả `clicked`, `pushed`, `wait` hoặc `off-page` |
| `__cbCurrent()` | Đường dẫn cuộc chat đang thật sự hiện, hoặc `null` |
| `__cbShowing(href)` | Cuộc chat `href` có đang hiện không |
| `__cbHasRows()` | Đã có dòng chat chưa |
| `__cbFocus()` | Đưa con trỏ vào ô soạn tin |

Bấm dòng chat là để điều hướng trong trang, vì mở thẳng URL ở bề ngang hẹp sẽ rơi về danh sách. `__cbOpen` được thử tối đa 40 lần, cách nhau 500 ms. Không tìm thấy dòng chat thì mới mở bằng URL. Sau đó app chờ tối đa 20 giây để cuộc chat hiện, quá thời gian thì báo lỗi ở khay.

Nút quay lại của Facebook chỉ ẩn cuộc chat và giữ nó trong DOM, nên đường dẫn không đủ để biết cuộc chat có đang hiện. `__cbCurrent` kiểm tra thêm `[role=log]` và `visibility` của `[role=main]`.

CSS trong `panel.js` ẩn `[role=banner]`, cho `[role=main]` chứa `[role=log]` phủ kín khung, đặt danh sách phủ kín ngay bên dưới nó, và đổi thanh cuộn thành 6 px không mũi tên.

Khung ẩn khi mất focus hoặc khi nhấn `Esc`. Nếu lúc ẩn đang mở một cuộc chat khác với cuộc chat của bóng đã bấm, `BubbleSet.SwitchTo` đổi bóng sang cuộc chat đó và giữ vị trí cũ. Có ngoại lệ:

| Trường hợp | Xử lý |
|---|---|
| Bóng cũ có tin chưa đọc đến sau | Giữ bóng cũ, thêm bóng mới |
| Cuộc chat mới đã có bóng | Đóng bóng cũ, giữ bóng kia ở chỗ của nó |
| Mở từ bóng thường trực hoặc `+N` | Thêm bóng mới |
| Đã đủ 5 bóng đều chưa đọc | Không thêm |
| Cuộc chat không có trong danh sách của trang theo dõi | Không đổi, vì không có tên và avatar |

## Bong bóng

Tối đa 5 bóng hiện cùng lúc. Bóng thứ 6 trở đi thay bóng cũ nhất không còn tin chưa đọc. Nếu cả 5 đều chưa đọc thì vào bóng `+N`, và được đưa lên khi có chỗ trống.

Bóng mới đặt phía trên chồng bóng của cột, hết chỗ thì xuống dưới vị trí neo, đầy cột thì sang cột phía trong. Vị trí neo là chỗ thả bóng gần nhất. Chưa có thì là mép phải màn hình chính, ở 1/4 chiều cao.

Thả bóng thì nó trượt về mép gần nhất. Thả vào vùng 240×140 px giữa đáy màn hình thì bóng bị đóng, và hiện lại nếu có tin mới. Bấm bóng là xóa số tin ngay, không chờ Facebook báo đã đọc, nên số đếm sau đó chỉ gồm tin đến sau lúc bấm.

Thẻ xem trước đặt cạnh bóng. Nếu đè lên khung chat đang mở thì chuyển lên trên khung, không đủ chỗ thì xuống dưới. Không còn chỗ cả hai phía thì vẫn đè.

Khi cắm hoặc rút màn hình, `SystemEvents.DisplaySettingsChanged` đưa bóng nằm ngoài màn hình về màn hình chính.

| Hằng số | Giá trị | Vị trí |
|---|---|---|
| Kích thước bóng | 56 DIP | `BubbleWindow` |
| Khoảng cách giữa bóng | 8 | `BubbleController.Spacing` |
| Ngưỡng tính là kéo | 4 px | `BubbleWindow.DragThreshold` |
| Thời gian trượt | 180 ms | `FloatingWindow.GlideMs` |
| Thời gian bật ra, hút vào | 180 ms | `BubbleWindow.PopTime` |
| Độ lộ giữa các bóng trong chồng | 4 | `BubbleController.StackPeek` |
| Thời gian tự gom | 5 giây | `BubbleController._collapse` |

Mọi hiệu ứng tắt khi `SystemParameters.ClientAreaAnimation` là `false`, tức khi tắt "Animation effects" của Windows.

## Cửa sổ và DPI

App khai báo `PerMonitorV2` trong `app.manifest`. Vị trí cửa sổ nổi đọc và ghi bằng pixel vật lý qua `GetWindowRect` và `SetWindowPos`, để đúng trên nhiều màn hình khác DPI. Kích thước khung chat đổi từ DIP sang pixel theo DPI của màn hình chứa bóng, và bị giới hạn trong vùng làm việc.

Cửa sổ nổi dùng `WS_EX_NOACTIVATE` để không lấy focus và `WS_EX_TOOLWINDOW` để không hiện trên thanh tác vụ. Khung chat bo góc bằng `DwmSetWindowAttribute` với `DWMWA_WINDOW_CORNER_PREFERENCE`.

## Tự phục hồi

| Tình huống | Xử lý |
|---|---|
| Trang theo dõi không gửi gì trong 60 giây | Tải lại, chờ theo `5 × 2^n` giây, tối đa 300 giây |
| Điều hướng thất bại | Như trên, trừ khi bị hủy (`OperationCanceled`) |
| Tiến trình render chết hoặc treo | Tải lại |
| Tiến trình trình duyệt chết | Tạo lại WebView2 |
| Không có dòng chat nào trong 2 phút | Icon khay đỏ, báo lỗi |
| Trang chuyển về `/login`, `/checkpoint` hoặc `/two_step_verification` | Icon khay đỏ, hiện cửa sổ đăng nhập |
| Trang theo dõi bị đưa về `/` hoặc `/home.php` | Quay lại `/messages` |
| Ngoại lệ không bắt được trên luồng giao diện | Ghi log tên ngoại lệ, app chạy tiếp |

Kiểm tra sức khỏe chạy mỗi 30 giây và sau mỗi lần nhận dữ liệu.

## Dữ liệu và bảo mật

Mọi thứ nằm trong `%LOCALAPPDATA%\MessengerBubbles\`:

| Đường dẫn | Nội dung |
|---|---|
| `profile\` | Hồ sơ WebView2: phiên đăng nhập và dữ liệu Facebook lưu lại, có thể có cả tin nhắn và ảnh. Đừng chia sẻ thư mục này |
| `settings.json` | Tùy chọn và vị trí bóng |
| `app.log` | Trạng thái và lỗi, không có tên hay nội dung tin |

`settings.json` được ghi ra file tạm rồi đổi tên, nên không bị hỏng giữa chừng. File lỗi thì app dùng giá trị mặc định.

| Khóa | Kiểu | Mặc định |
|---|---|---|
| `PersistentBubble` | bool | `false` |
| `Preview` | bool | `true` |
| `Autostart` | bool | `false` |
| `Sound` | bool | `true` |
| `GroupBubbles` | bool | `false` |
| `PanelSize` | 0 nhỏ, 1 vừa, 2 lớn | `1` |
| `Positions` | Từ khóa `anchor`, `persistent` sang `{X, Y, W, H}` | rỗng |

Các hạn chế trong `AppPaths.Harden` và `FacebookUrl`:

- Từ chối quyền thông báo của trang.
- Chỉ cho điều hướng trong `https://*.facebook.com` và `about:blank`. Link khác do người dùng bấm thì mở bằng trình duyệt mặc định, chỉ với `http` và `https`.
- Không mở cửa sổ mới trong app.
- Từ chối link cuộc chat không nằm dưới `/messages/`.

App không tự gửi dữ liệu đi đâu ngoài các request của chính trang Facebook. Avatar đọc từ ảnh trang đã tải qua CDP `Page.getResourceContent`, giữ trong bộ nhớ, tối đa 2 MB mỗi ảnh. WebView2 chạy với `--disk-cache-size=1`, cùng `--disable-background-timer-throttling` và `--disable-renderer-backgrounding` để trang ẩn vẫn chạy đúng giờ. Trang theo dõi bị tắt tiếng.

Gỡ app: đăng xuất Facebook trong `Mở Facebook`, tắt `Khởi động cùng Windows`, chọn `Thoát`, rồi xóa thư mục trên và file `.exe`.

## Đọc log

| Dòng log | Ý nghĩa |
|---|---|
| `new=N` | Phát hiện N tin mới |
| `health broken=True` | Không đọc được dòng chat trong 2 phút |
| `login needed=True` | Facebook yêu cầu đăng nhập lại |
| `panel did not show the thread` | Cuộc chat không hiện sau 20 giây, thường do `panel.js` đã lỗi thời |
| `thread row not found, opening by url` | Không có dòng chat trong khung, phải mở bằng URL |
| `panel suspend refused` | WebView2 từ chối treo trang |
| `rejected thread link` | Link cuộc chat không hợp lệ |
| `bad message from page` | Dữ liệu từ `probe.js` sai định dạng |
| `navigation failed: <status>` | Tải trang lỗi, sẽ thử lại |
| `process failed: <kind>` | Tiến trình WebView2 lỗi |
| `avatar not available: <type>` | Không đọc được avatar, bóng hiện chữ cái đầu |
| `unhandled: <type>` | Ngoại lệ không bắt được |

## Build

```bash
git clone https://github.com/umefor-labs/MessengerBubbles.git
```

```bash
dotnet run --project src/MessengerBubbles
```

```bash
dotnet test
```

Gồm 111 test cho phát hiện tin, tách nội dung dòng chat, tập bóng, hình học, cài đặt, autostart và URL. Phần WPF và WebView2 không có test tự động, phải kiểm tra tay theo [danh sách](docs/FACEBOOK-CHECKLIST.md).

Đóng gói một file tự chứa .NET vào `publish\MessengerBubbles.exe`, khoảng 166 MB. File không nén, vì `EnableCompressionInSingleFile` giảm còn 72 MB nhưng làm app tốn thêm gần 80 MB bộ nhớ riêng:

```bash
dotnet publish src/MessengerBubbles -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Phụ thuộc chính: `Microsoft.Web.WebView2` 1.0.2903.40, xUnit 2.5.3. Số phiên bản khi build tại máy nằm trong `src/MessengerBubbles/MessengerBubbles.csproj`.

## CI và phát hành

| Workflow | Khi nào chạy | Làm gì |
|---|---|---|
| `.github/workflows/test.yml` | Đẩy lên `main`, pull request | `dotnet test` |
| `.github/workflows/release.yml` | Đẩy tag `v*` | Test, build một file, tạo GitHub Release kèm `.exe` |

Số phiên bản của bản phát hành lấy từ tag. Tạo bản mới:

```bash
git tag -a v0.5.0 -m "v0.5.0"
```

```bash
git push origin v0.5.0
```

## Tài nguyên

Đo một lần trên một máy, 3 phút sau khi mở, với hai trang WebView2 đang tải:

| Tiến trình | Bộ nhớ riêng | Working set |
|---|---|---|
| App | 61 MB | 142 MB |
| WebView2, 9 tiến trình | 495 MB | 403 MB |

Working set cộng dồn cả bộ nhớ dùng chung giữa các tiến trình. Trang theo dõi chạy với `MemoryUsageTargetLevel = Low`.

Bản trước khi hạ mức bộ nhớ dùng 658 MB WebView2 sau 6 phút và 700 MB sau 8 giờ. Chưa đo bản hiện tại khi chạy lâu, nên chưa kết luận được có rò rỉ hay không.

## Giới hạn đã biết

- Hai tin giống hệt nhau đến liên tiếp khi dòng vẫn in đậm chỉ tính một, vì danh sách chat không có tín hiệu riêng cho từng tin.
- Cuộc trò chuyện đã tắt thông báo trên Facebook vẫn bật bóng và kêu.
- Đổi cỡ khung chat chỉ qua menu, không điều chỉnh trực tiếp từ khung chat, vì WebView2 che vùng kéo nếu không chừa viền.
- Khung chat không có bóng đổ. Thẻ xem trước luôn màu trắng, không theo chế độ tối.
- Kéo chồng bóng vào vùng đóng chỉ đóng bóng trên cùng.
- Chưa báo khi có bản mới, vì repo riêng tư thì app không đọc được Releases nếu không nhúng token.
- File phát hành chưa được thử trên máy sạch.

## Tài liệu khác

- [Lộ trình](docs/ROADMAP.md)
- [Thiết kế ban đầu](docs/2026-11-02-messenger-bubbles-design.md)
- [Danh sách kiểm tra khi Facebook đổi giao diện](docs/FACEBOOK-CHECKLIST.md)
- [Ghi chú trước khi công khai repo](docs/PUBLISHING.md)

## License

Chưa có, nên hiện chưa ai được phép dùng lại mã nguồn.
