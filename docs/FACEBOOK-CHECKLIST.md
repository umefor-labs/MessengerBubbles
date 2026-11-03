# Khi Facebook đổi giao diện

App không có API chính thức, chỉ đọc trang web của Facebook. Tài liệu này ghi app dựa vào những gì của trang, và cách kiểm tra lại.

## Dấu hiệu hỏng

| Dấu hiệu | Thường do |
|---|---|
| Icon khay đỏ, báo "Không đọc được danh sách chat" | `probe.js` không tìm thấy dòng chat nào trong 2 phút |
| Báo "Không mở được khung chat" | `__cbOpen` hoặc `__cbCurrent` trong `panel.js` |
| Khung chat hiện cả thanh Facebook, hoặc có khoảng trống | CSS trong `panel.js` |
| Có tin mà không bật bóng, hoặc bóng bật không có tin | Cách nhận biết in đậm, hoặc nhãn trong `RowText.cs` |
| Xem trước hiện sai tên hoặc lẫn thời gian | Nhãn trong `RowText.cs` |
| Bóng không có avatar, chỉ có chữ cái | Vị trí ảnh trong `probe.js` |

## App dựa vào đâu

| Chỗ | Dựa vào | Dùng để |
|---|---|---|
| `probe.js`, `panel.js` | `a[href*="/messages/t/"]`, `a[href*="/messages/e2ee/t/"]` | Tìm dòng chat |
| `probe.js` | Có `span` với `font-weight` từ 600 trở lên | Biết dòng chưa đọc |
| `probe.js` | `img` hoặc `image` trong dòng chat | Lấy avatar |
| `panel.js` | `[role=banner]` | Ẩn thanh Facebook |
| `panel.js` | `[role=main]`, `[role=log]` | Khung cuộc chat, biết cuộc chat có đang hiện không |
| `panel.js` | `[role=navigation]`, `[role=grid]` | Danh sách chat |
| `panel.js` | `[role=main] [role=textbox]` | Đưa con trỏ vào ô soạn tin |
| `RowText.cs` | Nhãn tiếng Việt và tiếng Anh: thời gian, "Đang hoạt động", "Tin nhắn chưa đọc", dấu `·` | Tách tên và nội dung |
| `Helpers.cs` | `/login`, `/checkpoint`, `/two_step_verification` | Biết cần đăng nhập lại |
| `AppPaths.cs` | `https://www.facebook.com/messages` | Trang được mở |

Nhãn "chưa đọc" và "Hoạt động … trước" là mẫu đoán, chưa gặp trên trang thật.

## Kiểm tra lại sau khi sửa

Cần một người nhắn thử từ tài khoản khác.

1. `dotnet test`
2. Mở app, chờ 30 giây: icon khay không đỏ, `app.log` không có `health broken=True`.
3. Nhận tin: bóng bật trong 10 giây, đúng avatar, có số, có âm báo, xem trước đúng tên và nội dung.
4. Điện thoại vẫn thấy tin chưa đọc khi chưa bấm bóng.
5. Bấm bóng: vào thẳng cuộc chat, không tải lại, chỉ thấy khung chat, không có khoảng trống.
6. Bấm nút quay lại: thấy danh sách kín khung, không có khoảng trống.
7. Đang mở cuộc chat đó, nhận tin: không báo.
8. Từ danh sách mở cuộc chat khác, rồi chuyển sang app khác: bóng đổi sang cuộc chat đó.
9. Lặp lại bước 3 và 5 với chat nhóm và chat mã hóa.
10. Đăng xuất trong `Mở Facebook`: khay báo cần đăng nhập, cửa sổ đăng nhập hiện ra.
