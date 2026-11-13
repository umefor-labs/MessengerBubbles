# Thay đổi

Các thay đổi đáng chú ý của từng bản phát hành. Số phiên bản theo [lộ trình](docs/ROADMAP.md).

## v0.9.1 (chưa phát hành)

- Menu khay hiện phiên bản đang chạy (`Phiên bản 0.9.1`), và di chuột lên icon khay cũng thấy số phiên bản.
- Thêm `Kiểm tra bản mới` trong menu khay: hỏi GitHub ngay lúc bấm và luôn trả lời bằng thông báo, gồm có bản mới (bấm để mở trang tải), đang dùng bản mới nhất, hoặc không kiểm tra được vì mất mạng. Dùng được cả khi đã tắt `Báo bản mới`.

## v0.9.0 (12/11/2026)

- Thêm `Tạm im lặng` trong menu khay: `1 giờ` hoặc `Đến 7 giờ sáng`. Trong lúc đó tin mới không bật bóng, không xem trước, không kêu; hết giờ tự báo lại, hoặc chọn `Bật lại thông báo`.
- Hai tin giống hệt nhau đến liên tiếp giờ bật bóng lại, nếu cách nhau đủ lâu để nhãn thời gian của cuộc chat trẻ lại (ví dụ `5 phút` về `Vừa xong`).
- Trang theo dõi tự tải lại khi đã mở quá 6 giờ và máy rảnh 5 phút, để không phình bộ nhớ khi chạy qua đêm. Tin đến trong lúc tải lại vẫn được báo.

## v0.8.1 (06/11/2026)

- Khi bật `Gom bóng`, kéo cả cụm xuống dấu X sẽ đóng toàn bộ cụm (kể cả bóng `+N` cuộc chat khác), không chỉ cuộc trò chuyện nằm trên cùng. Tin mới đến sau đó vẫn bật bóng lại như thường.

## v0.8.0 (06/11/2026)

- Thêm `Tắt GPU (giảm RAM)` trong menu khay, mặc định tắt. Bật thì WebView2 chạy với `--disable-gpu` từ lần mở app sau. README có lệnh PowerShell để đo bộ nhớ trước và sau.

## v0.7.0 (05/11/2026)

- Cuộc chat đã tắt thông báo trên Facebook không bật bóng và không kêu nữa. App nhận biết qua icon chuông gạch ngang trong danh sách chat, nên Facebook đổi icon là lại bật bóng. Tin của các cuộc chat này vẫn được tính vào số trên bóng thường trực.
- Mã nguồn dùng license [MIT](LICENSE).
- Thêm file thay đổi này. Lộ trình ghi đúng bản phát hành mới nhất và trỏ tới số đo bộ nhớ trong README.

## v0.6.1 (04/11/2026)

- Khi bật `Khởi động cùng Windows`, mỗi lần mở app ghi lại đường dẫn của chính nó, nên chuyển file `.exe` đi chỗ khác không còn để lại mục khởi động hỏng.

## v0.6.0 (04/11/2026)

- Báo khi có bản mới trên GitHub Releases, 1 phút sau khi mở và mỗi 24 giờ. Tắt được bằng `Báo bản mới` trong menu khay.
- Thêm video giới thiệu, hiện dạng GIF trong README.

## v0.5.0 (03/11/2026)

- Giao diện tiếng Anh, chọn ngôn ngữ ở lần chạy đầu và trong menu khay.
- README tiếng Anh, tài liệu cho repo công khai.
- Giảm bộ nhớ: file `.exe` không nén nữa, bớt khoảng 80 MB bộ nhớ riêng, và trang theo dõi chạy với `MemoryUsageTargetLevel = Low`.

## v0.4.0 (03/11/2026)

- Đóng gói thành một file `.exe` tự chứa .NET, có biểu tượng riêng.
- GitHub Actions chạy test mỗi lần đẩy code và tạo bản phát hành khi gắn tag.
- Đổi lên .NET 10, báo lỗi khi khung chat không mở được, bóng về màn hình còn lại khi rút màn hình.
- Hiệu ứng bóng, gom bóng, tắt âm báo, chọn cỡ khung chat, bóng đổi theo cuộc chat đang mở.

## v0.1.0 (02/11/2026)

- Bản đầu tiên: mỗi cuộc chat có tin mới bật một bóng riêng, có số tin, âm báo và xem trước nội dung. Bấm bóng mở thẳng cuộc chat.
