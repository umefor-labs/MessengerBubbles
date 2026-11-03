# Messenger Bubbles

Bong bóng chat Facebook nổi trên Windows 11, mỗi người một bóng, giống chat head trên Android. App không phải của Meta và không liên kết với Meta.

## Trước khi dùng

- **Tài khoản:** app tự đọc trang Facebook, trong khi [điều khoản của Facebook](https://www.facebook.com/terms.php) cấm truy cập bằng phương tiện tự động nếu chưa được phép. Tài khoản có thể bị hạn chế. Tự cân nhắc trước khi dùng.
- **Có thể hỏng:** không có API chính thức, nên Facebook đổi giao diện là phần phát hiện tin có thể hỏng. Khi đó icon khay chuyển đỏ và báo. Xem [danh sách kiểm tra](docs/FACEBOOK-CHECKLIST.md).
- **Dữ liệu:** app không gửi dữ liệu đi đâu ngoài Facebook. Phiên đăng nhập lưu trên máy, xem mục Dữ liệu.

## Tính năng

- Có tin mới thì bật bóng có avatar, số tin và âm báo riêng.
- Bấm bóng vào thẳng cuộc chat, không tải lại trang, chỉ hiện khung chat.
- Mở app không làm tin thành đã đọc. Tin chỉ thành đã đọc khi mở cuộc chat đó.
- Kéo bóng để đổi chỗ, kéo xuống đáy để đóng.

Các tùy chọn nằm trong menu icon khay: bóng thường trực, gom bóng, xem trước nội dung, âm báo, kích thước khung chat, khởi động cùng Windows.

## Chạy

Tải file `.exe` ở GitHub Releases rồi chạy, không cần cài .NET. Máy cần có WebView2 Runtime; Windows 11 có sẵn.

Tự build thì cần .NET 10:

```bash
dotnet run --project src/MessengerBubbles
```

Đóng gói thành một file `publish\MessengerBubbles.exe`:

```bash
dotnet publish src/MessengerBubbles -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
```

Gắn tag `v*` và đẩy lên thì GitHub Actions tự chạy test, build file đó và tạo bản phát hành.

Lần đầu, tự đăng nhập Facebook trong cửa sổ hiện ra (nhập cả mã PIN tin nhắn mã hóa nếu được hỏi), rồi đóng cửa sổ đó. App vẫn chạy ở khay hệ thống; chọn `Thoát` trong menu khay để tắt.

## Kiểm thử

```bash
dotnet test
```

## Dữ liệu

Tất cả nằm ở `%LOCALAPPDATA%\MessengerBubbles\`:

- `profile\`: hồ sơ trình duyệt của WebView2, gồm phiên đăng nhập và dữ liệu Facebook lưu lại, có thể có cả tin nhắn và ảnh. Đừng chia sẻ thư mục này.
- `settings.json`: tùy chọn và vị trí bóng.
- `app.log`: trạng thái và lỗi, không có tên hay nội dung tin.

Gỡ app: đăng xuất Facebook trong app (`Mở Facebook` ở khay), tắt `Khởi động cùng Windows`, chọn `Thoát`, rồi xóa thư mục trên và file `.exe`.

## License

Chưa có license, nên mặc định chưa cấp quyền dùng lại mã nguồn.
