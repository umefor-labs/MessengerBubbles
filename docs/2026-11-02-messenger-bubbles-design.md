# Thiết kế: App bong bóng chat Messenger cho Windows 11

Ngày: 2026-11-02. Trạng thái: chờ duyệt.

## Mục đích

Người dùng muốn nhận và trả lời tin Messenger trên Windows 11 bằng bong bóng nổi, giống chat head trên Android. Mục tiêu là thấy tin mới và trả lời ngay mà không phải mở tab Facebook.

Đây là app cá nhân, chạy trên một máy, với một tài khoản Facebook.

## Mục tiêu

- Mỗi cuộc chat có tin mới bật một bong bóng avatar riêng.
- Bấm vào bóng mở khung chat nhỏ của đúng cuộc chat đó, đọc và trả lời được.
- App chạy nền nhiều giờ mà vẫn phát hiện được tin mới.

## Không làm

- Không gửi tin tự động, không đọc hàng loạt lịch sử chat, không dùng API không chính thức của Meta.
- Không hỗ trợ nhiều tài khoản, không có bộ cài, không phát hành cho người khác.
- Không dùng logo Messenger cho app. Tên app là Messenger Bubbles, theo quyết định của người dùng.
- Bản đầu không có gọi thoại/video trong khung chat nhỏ.

## Coi là thất bại nếu

- Mở app làm tin bị đánh dấu đã đọc khi người dùng chưa bấm vào bóng.
- Bóng cướp focus khi người dùng đang gõ ở app khác.
- App ngừng phát hiện tin mà không báo gì.
- Code của app tự ghi tên, nội dung tin hoặc avatar xuống đĩa. Dữ liệu trang do WebView2 lưu trong `profile\` là ngoại lệ, xem mục Quyền riêng tư.

## Ràng buộc

- Messenger cá nhân không có API công khai. Meta thông báo messenger.com chuyển sang `facebook.com/messages` từ tháng 4/2026, và bản thử xác nhận trang này chạy trong WebView2.
- Máy có .NET SDK 8.0.425 và WebView2 Runtime 156. .NET 8 hết hỗ trợ ngày 10/11/2026, nên viết sao cho đổi lên .NET 10 chỉ cần sửa `TargetFramework`.
- Chưa kiểm tra việc này có phù hợp điều khoản của Meta không. App chỉ hiển thị trang và đọc trang trong phiên của chính người dùng, nhưng rủi ro với tài khoản không phải bằng không.

## Các cách đã cân nhắc

| Cách | Được | Mất |
|---|---|---|
| **WPF + WebView2 (đã chọn)** | Nhẹ, dùng runtime có sẵn, cửa sổ Windows thật | Viết nhiều code hơn, hiệu ứng tốn công |
| Electron | Hiệu ứng dễ, viết bằng HTML/CSS | Kèm Chromium, từ 100 MB trở lên |
| Tiện ích trình duyệt | Không có | Bóng không nổi ra ngoài trình duyệt, nên đã loại |

Người dùng chọn WPF + WebView2 theo khuyến nghị, vì app chạy nền cả ngày nên nhẹ quan trọng hơn hiệu ứng.

## Kết quả bản thử (`BubbleSpike`)

- Thông báo web (`NotificationReceived`, `Notification`, `showNotification`): 0 lần phát. Không dùng.
- Tiêu đề trang: nhấp nháy khi có tin chưa đọc, không cho biết của ai. Không đủ để dùng.
- Dòng cuộc chat `a[href*="/messages/t/"]`, `a[href*="/messages/e2ee/t/"]`: có mã cuộc chat, avatar, chữ, và chữ đậm khi chưa đọc. Vẫn hoạt động khi cửa sổ ẩn, với thời gian đã quan sát khoảng một phút.
- Chữ đổi không đồng nghĩa với tin mới: một dòng không đậm vẫn đổi chữ.
- Một dòng đang đậm từ trước khi mở app, nên không thể coi mọi dòng đậm là tin mới.

Bản thử là code bỏ đi. App thật là project mới, chỉ mang theo những gì đã học được.

## Người dùng đã quyết định

- Mỗi người chat một bong bóng riêng.
- WPF + WebView2.
- Hướng hành vi đã đề xuất và được đồng ý viết thành thiết kế: Facebook chạy ẩn, bóng avatar nổi trên cùng, kéo được, hít cạnh màn hình, bấm mở khung chat nhỏ cạnh bóng, kéo xuống đáy để đóng, có icon khay hệ thống để thoát.
- Có tùy chọn bật bóng thường trực.
- Có tùy chọn hiện hoặc không hiện đoạn xem trước nội dung tin.
- Có âm thanh riêng.
- Có tùy chọn khởi động cùng Windows, mặc định tắt.
- Giao diện bằng tiếng Việt.

## Thiết kế

### Các phần

1. **Trình theo dõi:** một WebView2 ẩn mở `facebook.com/messages` và đọc danh sách chat.
2. **Quản lý bóng:** nhận sự kiện tin mới, tạo, cập nhật và xóa bóng.
3. **Cửa sổ bóng:** mỗi bóng là một cửa sổ WPF trong suốt, tròn, 56 px.
4. **Khung chat:** một WebView2 dùng chung, mở cuộc chat được chọn.
5. **Khay hệ thống:** mở lại trang Facebook, các tùy chọn trong mục "Tùy chọn", thoát.

Mọi WebView2 dùng chung một thư mục profile `%LOCALAPPDATA%\MessengerBubbles\profile`, nên chỉ đăng nhập một lần.

### Phát hiện tin mới

Một cuộc chat có tin mới khi dòng của nó **đang đậm** và thỏa một trong hai điều kiện: chữ vừa đổi, hoặc vừa chuyển từ không đậm sang đậm.

Lần quét đầu sau khi tải trang chỉ ghi nhận trạng thái, không bật bóng. Tin chưa đọc từ trước vì vậy không bật bóng hàng loạt, kể cả sau khi đăng nhập hoặc tải lại trang.

Dữ liệu chuyển từ trang sang app chỉ nhận từ nguồn `https` thuộc `facebook.com`, và phải kiểm tra `Source` của mọi tin nhắn từ trang.

Trang Facebook có thể tự mở một cuộc chat khi vào `/messages`. Trình theo dõi không được làm tin bị đánh dấu đã đọc. Bước đầu của kế hoạch phải kiểm tra việc này, và nếu có thì phải tìm URL hoặc cách hiển thị khác trước khi làm tiếp.

### Hành vi bóng

- Bóng mới xếp dọc ở cạnh phải màn hình chính. Tối đa 5 bóng; quá số đó thì bóng cũ nhất không có tin chưa đọc bị đóng, nếu không có thì gộp vào bóng `+N`.
- Tin mới vào cuộc chat đã có bóng: tăng số trên chấm đỏ, không bật bóng thứ hai.
- Kéo thả tự do; thả ra thì hít vào cạnh trái hoặc phải gần nhất của màn hình đang đứng.
- Kéo xuống vùng đóng ở đáy màn hình thì đóng bóng. Vùng này chỉ hiện trong lúc kéo.
- Cuộc chat đã đóng bóng sẽ bật lại khi có tin mới.
- Đọc tin ở nơi khác (điện thoại chẳng hạn) thì mất chấm đỏ nhưng bóng vẫn còn, giống Android.
- Bóng dùng `WS_EX_NOACTIVATE` và `WS_EX_TOOLWINDOW`: không cướp focus, không hiện trên taskbar.
- Vị trí đóng trên nhiều màn hình và DPI khác nhau phải đúng (Per-Monitor V2).

### Khung chat

- Bấm vào bóng mở khung khoảng 380 × 560 px cạnh bóng, nằm trọn trong màn hình và vào thẳng cuộc chat đó.
- Khung chỉ hiện cuộc trò chuyện, không có header và các nút của Facebook.
- Mở khung không tải lại trang: trang được tải sẵn khi app khởi động và tạm dừng (`TrySuspendAsync`) khi ẩn, còn việc chuyển cuộc chat dùng thao tác bấm dòng chat ngay trong trang.
- Chỉ mở một khung mỗi lúc. Bấm bóng khác thì chuyển cuộc chat, bấm lại bóng đó hoặc bấm ra ngoài thì đóng.
- Mở khung là đọc tin thật, nên Facebook đánh dấu đã đọc. Đây là hành vi đúng.
- Cuộc chat nhóm và chat mã hóa dùng cùng cách này, cần kiểm tra thật vì bản thử chưa đụng đến.

### Tùy chọn

Bật/tắt trong menu khay, tiếng Việt, có hiệu lực ngay. Lưu ở `%LOCALAPPDATA%\MessengerBubbles\settings.json`, chỉ gồm tùy chọn và vị trí bóng.

| Tùy chọn | Mặc định | Hành vi |
|---|---|---|
| Bóng thường trực | Tắt | Một bóng chung luôn nổi, có tổng số cuộc chat chưa đọc. Bấm vào mở danh sách chat trong khung chat. Không tính vào giới hạn 5 bóng và không đóng được bằng cách kéo xuống đáy; muốn ẩn thì tắt tùy chọn này. |
| Xem trước nội dung | Bật | Có tin mới thì hiện tên và đoạn đầu tin cạnh bóng khoảng 5 giây. Tắt thì chỉ có bóng và chấm đỏ. |
| Khởi động cùng Windows | Tắt | Bật thì thêm app vào `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, tắt thì xóa. |

### Âm thanh

Có tin mới thì phát một âm báo riêng, là file WAV ngắn kèm trong app. Nhiều tin đến dồn dập chỉ phát một lần trong 3 giây. Tắt tiếng trình theo dõi bằng `CoreWebView2.IsMuted` để không kêu trùng với âm của Facebook.

### Khi có sự cố

- **Hết phiên đăng nhập:** trình theo dõi bị chuyển sang trang đăng nhập thì báo ở khay và mở cửa sổ để người dùng tự đăng nhập. App không bao giờ lưu hay nhập mật khẩu.
- **Facebook đổi giao diện:** đã đăng nhập mà không thấy dòng chat nào trong 2 phút thì đổi icon khay sang trạng thái lỗi và báo. Đây là lỗi dễ xảy ra nhất, nên không được im lặng.
- **Mất mạng hoặc trang treo:** tải lại trang theo chu kỳ tăng dần. Sau khi tải lại, quét lại từ đầu như lần đầu.
- **WebView2 lỗi tiến trình:** bắt `ProcessFailed`, tạo lại trình theo dõi.

### Chạy nền lâu

Dùng `MutationObserver` để phát hiện thay đổi, và giữ một lần quét định kỳ làm lưới an toàn. Bật `--disable-background-timer-throttling` và `--disable-renderer-backgrounding` qua `AdditionalBrowserArguments`.

Tiêu chí đạt: cửa sổ ẩn 8 giờ, tin gửi đến vẫn bật bóng trong vòng 10 giây. Nếu không đạt, thử cửa sổ đặt ngoài màn hình thay vì ẩn. Phải kiểm tra việc này trước khi hoàn thiện giao diện.

### Quyền riêng tư

- Không ghi tên, nội dung, avatar xuống đĩa. Tên, đoạn xem trước và avatar chỉ giữ trong bộ nhớ, và link avatar có thể hết hạn nên phải tải về ngay khi phát hiện.
- Xem trước nội dung có thể lộ tin khi chia sẻ màn hình. Người dùng muốn thấy nó nên mặc định bật; tắt được ở menu khay.
- Log chỉ ghi trạng thái và lỗi, không ghi nội dung.
- Thư mục profile chứa phiên đăng nhập Facebook; phiên được bảo vệ ở mức tài khoản Windows.
- Profile hoạt động như hồ sơ trình duyệt, nên Facebook có thể lưu tin nhắn và ảnh trong đó, ví dụ ở IndexedDB. Không tránh được nếu muốn giữ đăng nhập. App đặt `--disk-cache-size=1` để giảm bộ nhớ đệm HTTP.

## Quyết định của người viết, cần người dùng xem lại

Những mục này người dùng chưa từng được hỏi:

- Bóng thường trực là một bóng chung có tổng số cuộc chat chưa đọc, bấm vào mở danh sách chat, mặc định tắt.
- Xem trước nội dung hiện khoảng 5 giây.
- Âm thanh luôn bật, chưa có tùy chọn tắt riêng, và chống phát dồn trong 3 giây.
- Tối đa 5 bóng, kèm bóng `+N`.
- Cuộc chat đã tắt thông báo trên Facebook vẫn có thể bật bóng và kêu nếu dòng của nó đậm, vì bản thử chưa cho biết cách nhận ra.

## Để người làm tự quyết

Cấu trúc code, cách vẽ icon khay, thông số hiệu ứng kéo và hít cạnh, và cách kiểm thử (tối thiểu có test cho logic phát hiện tin mới và logic chọn bóng bị đóng).
