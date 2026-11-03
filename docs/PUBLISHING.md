# Trước khi công khai repo

Ghi lại ngày 03/11/2026. Đây là ghi chú khi đọc điều khoản, không phải tư vấn pháp lý.

## Điều khoản Facebook

[Điều khoản của Facebook](https://www.facebook.com/terms.php), mục 3.2, cấm truy cập hoặc thu thập dữ liệu bằng phương tiện tự động nếu chưa được cho phép.

App chạy bằng phiên đăng nhập của chính người dùng và không gửi dữ liệu đi đâu. Nhưng `probe.js` tự đọc danh sách chat ít nhất 5 giây một lần, nên có thể bị coi là truy cập tự động. Không tìm được quy định hay trường hợp nào nói rõ về app bọc trang web như thế này.

Hậu quả có thể xảy ra là tài khoản bị hạn chế. Người dùng phải được báo rõ điều này trước khi cài.

## Tên "Messenger"

[Trang nhãn hiệu của Meta](https://www.meta.com/brand/resources/meta/our-trademarks/) không cho dùng nhãn hiệu của Meta trong tên công ty, tên thương mại, tên người dùng hay tên miền nếu chưa được phép. Không tìm thấy dòng nào nói riêng về tên app, nhưng `Messenger Bubbles` khó có thể coi là an toàn.

Không được dùng logo của Messenger, và không được làm người khác tưởng app là của Meta.

## Việc cần làm nếu công khai

1. Đổi tên app và repo sang tên không chứa "Messenger" hay "Facebook".
2. Chọn license.
3. Thêm báo bản mới trên GitHub Releases.
4. Thử file phát hành trên một máy sạch.

Không công khai thì không cần làm mục 1 và 3.
