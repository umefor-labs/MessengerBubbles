# Lộ trình Messenger Bubbles

Mỗi phiên bản là một đợt phát hành nhỏ, có tiêu chí đạt rõ ràng. Số phiên bản theo dạng `0.x.y`: `x` tăng khi có đợt mới, `y` tăng khi chỉ sửa lỗi. Xong đợt nào thì gắn tag đợt đó (`v0.2.0`, ...).

Các tính năng ở v0.3 trở đi là đề xuất, chưa được người dùng chốt. Mỗi đợt nên chốt lại phạm vi trước khi làm.

## v0.1.0: Bản dùng được (hiện tại)

Đã có và đã thấy chạy với tin thật:

- Mỗi cuộc chat có tin mới bật một bóng riêng, có số tin và âm báo.
- Bấm bóng vào thẳng cuộc chat, không tải lại trang, chỉ hiện khung chat.
- Xem trước nội dung, không báo cuộc chat đang mở.
- Mở app không làm tin thành đã đọc.

Đã có nhưng chưa được kiểm tra với tin thật: chat nhóm, chat mã hóa, kéo xuống đáy để đóng, menu khay, chạy qua đêm.

## v0.2: Ổn định

Mục tiêu: dùng cả ngày mà không phải để ý đến app.

- [x] Đổi lên .NET 10, vì .NET 8 hết hỗ trợ ngày 10/11/2026.
- [ ] Kiểm tra hết các mục còn thiếu của v0.1.0, sửa lỗi phát sinh.
- [ ] Đo bộ nhớ riêng của app khi chạy lâu; nếu trang tăng dần thì tải lại định kỳ.
- [x] Báo lỗi ở khay khi khung chat không mở được cuộc trò chuyện.
- [x] Rút hoặc cắm màn hình lúc đang chạy: bóng phải về màn hình còn lại.
- [x] Bóng được đưa lên từ `+N` phải có avatar ngay.
- [x] Lùi bằng lịch sử trình duyệt (`history.back()`) không được làm khung trắng.
- [x] Đối chiếu nhãn thật của Facebook trên một mẫu: "Đang hoạt động" và thời gian khớp. Nhãn "chưa đọc" và "Hoạt động … trước" vẫn là mẫu đoán, vì chưa gặp.

**Đạt khi:** chạy qua đêm, tin đến vẫn bật bóng trong vòng 10 giây; chat nhóm và chat mã hóa đọc, trả lời được; một tuần dùng không bỏ sót tin và không báo nhầm.

## v0.3: Trải nghiệm giống Android

Mục tiêu: cảm giác như chat head thật.

- [x] Hiệu ứng: bóng bật ra, trượt hít vào cạnh, hút vào vùng đóng. Tắt theo cài đặt "Animation effects" của Windows. Chưa có độ nảy.
- [ ] Gom bóng: nhiều bóng xếp chồng, bấm vào thì xòe ra.
- [x] Tùy chọn tắt âm báo (menu khay).
- [ ] Không bật bóng và không kêu với cuộc chat đã tắt thông báo trên Facebook.
- [x] Đổi kích thước khung chat bằng menu khay (Nhỏ, Vừa, Lớn) và nhớ lại. Chưa kéo cạnh được, vì WebView2 che mất vùng kéo nếu không chừa viền.
- [x] Rời khung chat khi đang mở cuộc chat khác thì bóng đổi sang cuộc chat đó.
- [ ] Đếm đúng khi hai tin giống hệt nhau đến liên tiếp.

**Đạt khi:** người dùng thấy dùng thay được cửa sổ Facebook hằng ngày.

## v0.4: Đóng gói

Mục tiêu: cài và cập nhật dễ, không cần `dotnet`.

- [ ] Một file `.exe` chạy được trên máy chưa cài .NET.
- [ ] Biểu tượng riêng cho app và khay, thay cho chấm tròn hiện tại.
- [ ] GitHub Actions: chạy test mỗi lần đẩy code, tạo bản phát hành khi gắn tag.
- [ ] Báo khi có bản mới trên GitHub Releases.

**Đạt khi:** tải file từ GitHub, chạy trên một máy sạch, đăng nhập và nhận được tin.

## v1.0: Sẵn sàng phát hành

- [ ] Quyết định có công khai repo không. Trước đó phải xem điều khoản của Meta và việc dùng tên "Messenger", vì tên này là nhãn hiệu của Meta.
- [ ] Chọn license, viết README cho người ngoài, nêu rõ rủi ro với tài khoản.
- [ ] Facebook đổi giao diện thì sửa được nhanh: gom các chỗ phụ thuộc cấu trúc trang vào `probe.js` và `panel.js`, có danh sách kiểm tra lại.

**Đạt khi:** dùng ổn định một tháng và đã có quyết định về công khai.

## Rủi ro xuyên suốt

- App đọc giao diện Facebook, không có API chính thức, nên có thể hỏng bất cứ lúc nào.
- Chưa kiểm tra app có phù hợp điều khoản của Meta không.
- Dùng tên "Messenger" chưa phải vấn đề khi repo riêng tư, nhưng phải xem lại trước khi công khai.
