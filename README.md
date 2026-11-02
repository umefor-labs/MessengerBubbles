# Messenger Bubbles

Bong bóng chat Facebook nổi trên Windows 11. App bọc `facebook.com/messages` trong WebView2, nên Facebook đổi giao diện thì phần phát hiện tin có thể hỏng; khi đó icon khay chuyển đỏ và báo.

## Chạy

Cần .NET 10 và WebView2 Runtime.

```bash
dotnet run --project src/MessengerBubbles
```

Hoặc đóng gói rồi chạy `publish\MessengerBubbles.exe`:

```bash
dotnet publish src/MessengerBubbles -c Release -o publish
```

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

Muốn xóa: đăng xuất Facebook trong app (`Mở Facebook` ở khay), tắt `Khởi động cùng Windows`, chọn `Thoát`, rồi xóa thư mục trên.
