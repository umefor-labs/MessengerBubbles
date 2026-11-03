# Messenger Bubbles

Muốn có chat head như trên Android cho Windows 11 nên làm cái này. Ai nhắn thì bật một bóng của người đó, bấm vào là chat luôn. App tự làm, không dính gì tới Meta.

Có tin thì bóng hiện avatar, số tin và kêu một tiếng. Bấm vào là vào thẳng cuộc chat, không tải lại trang, và chỉ thấy khung chat chứ không thấy phần còn lại của Facebook. Mở app không làm tin thành đã đọc, phải mở cuộc chat đó mới tính. Kéo bóng đi đâu cũng được, kéo xuống đáy màn hình để đóng.

Chuột phải vào icon ở khay để chỉnh: bóng thường trực, gom bóng, xem trước nội dung, âm báo, cỡ khung chat, khởi động cùng Windows.

## Đọc trước khi dùng

App tự đọc trang Facebook, mà [điều khoản của Facebook](https://www.facebook.com/terms.php) cấm truy cập bằng phương tiện tự động nếu chưa được phép. Tài khoản có thể bị hạn chế, nên tự cân nhắc.

Facebook không có API cho việc này, nên họ đổi giao diện là app có thể không bắt được tin nữa. Lúc đó icon khay chuyển đỏ và báo. Cách sửa ở [đây](docs/FACEBOOK-CHECKLIST.md).

Ngoài Facebook ra, app không gửi dữ liệu đi đâu.

## Cài

Tải file `.exe` ở GitHub Releases rồi chạy, không cần cài .NET. Máy cần WebView2 Runtime, Windows 11 có sẵn.

Lần đầu sẽ có cửa sổ đăng nhập Facebook. Đăng nhập xong (có hỏi mã PIN tin nhắn mã hóa thì nhập luôn) thì đóng nó đi, app vẫn chạy dưới khay. Muốn tắt hẳn thì chọn `Thoát` trong menu khay.

## Tự build

Cần .NET 10.

```bash
dotnet run --project src/MessengerBubbles
```

```bash
dotnet test
```

Đóng gói thành một file `publish\MessengerBubbles.exe`:

```bash
dotnet publish src/MessengerBubbles -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

Đẩy một tag `v*` lên thì GitHub Actions tự chạy test, build file đó và tạo bản phát hành.

## Dữ liệu

Mọi thứ nằm trong `%LOCALAPPDATA%\MessengerBubbles\`:

- `profile\` là hồ sơ trình duyệt của WebView2, có phiên đăng nhập và dữ liệu Facebook lưu lại, có thể có cả tin nhắn và ảnh. Đừng đưa thư mục này cho ai.
- `settings.json` lưu tùy chọn và vị trí bóng.
- `app.log` chỉ ghi trạng thái và lỗi, không có tên hay nội dung tin.

Muốn gỡ thì đăng xuất Facebook trong app (`Mở Facebook` ở khay), tắt `Khởi động cùng Windows`, chọn `Thoát`, rồi xóa thư mục trên và file `.exe`.

## License

Chưa có, nên hiện chưa ai được phép dùng lại mã nguồn.
