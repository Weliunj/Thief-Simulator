# Game3D_Coban

## Mô tả Game

**Thief Simulator** là một trò chơi phiêu lưu giải đố 3D đầy thách thức, kết hợp giữa việc thu thập vật phẩm, giải câu đố và tránh né kẻ thù. Người chơi phải thu thập đủ điểm từ các vật phẩm rải rác trong thị trấn, đồng thời giải các câu đố tiếng Việt để mở cửa chặn đường, tất cả trong thời gian giới hạn. Hãy cẩn thận với các NPC trưởng thành đang rình rập!

## Cách Chơi

### Mục tiêu
- Thu thập vật phẩm để đạt số điểm trước khi hết thời gian.
- Giải câu đố để mở cửa chặn đường.
- Tránh bị bắt bởi các NPC trưởng thành.

### Điều kiện Thắng/Thua
- **Thắng**: Thu thập đủ số điểm trước khi hết 5 phút.
- **Thua**: Hết thời gian hoặc bị NPC bắt.

## Điều khiển

| Phím | Hành động |
|------|-----------|
| WASD / Mũi tên | Di chuyển |
| Shift | Chạy nhanh (tiêu hao stamina) |
| Space | Nhảy |
| E | Tương tác với cửa và vật phẩm |
| F | Bật/tắt đèn pin |
| P | Chuyển đổi góc nhìn (3rd/1st person) |
| Crouch | Ngồi xuống (giảm tầm nhìn, tiết kiệm stamina) |

## Tính năng Chính

- **Hệ thống Câu đố**: Giải 3 câu đố liên tiếp để mở mỗi cửa. Sai câu trả lời sẽ kích hoạt cảnh báo AI!
- **Thu thập Vật phẩm**: Các vật phẩm có giá trị điểm và trọng lượng khác nhau. Giới hạn mang vác: 100kg.
- **AI NPC**: Các NPC trưởng thành sẽ đuổi theo khi phát hiện. Trẻ em chỉ đi lang thang vô hại.
- **Quản lý Stamina**: Chạy nhanh tiêu hao stamina, ngồi xuống tiết kiệm năng lượng.
- **Chuyển đổi Góc nhìn**: Chuyển giữa góc nhìn thứ ba và thứ nhất để chiến thuật tốt hơn.
- **Hiệu ứng Hậu kỳ**: Chromatic aberration và vignette thay đổi theo hành động của người chơi.

## Cài đặt và Chạy

1. Mở dự án trong Unity 6000.3.9f1.
2. Mở scene "Lv1" từ thư mục Assets/Scenes.
3. Nhấn Play để bắt đầu chơi.

## Ghi chú Phát triển

- Dự án sử dụng NavMesh cho AI.
- Câu đố được lưu trong QuestionDataSO.asset.
- Hệ thống UI sử dụng TextMesh Pro.

Chúc bạn phát triển và kiểm thử hiệu quả!</content>
<parameter name="filePath">e:\Project\Unity_Project\Game3D_Coban\README.md