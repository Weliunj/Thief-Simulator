# Thief Simulator 3D

## 📝 Mô tả Game

**Thief Simulator** là một trò chơi nhập vai phiêu lưu 3D đầy kịch tính. Người chơi vào vai một tên trộm xâm nhập vào khu vực được bảo vệ để thu thập các vật phẩm có giá trị, vượt qua các cánh cửa bị khóa bằng kỹ năng phá khóa (Lockpicking), đồng thời lẩn tránh sự truy đuổi ráo riết từ các NPC tuần tra trong thời gian giới hạn.

---

## 🎮 Cách Chơi

### 🎯 Mục tiêu
- **Nhặt vật phẩm**: Tìm kiếm và thu thập các vật phẩm có giá trị điểm rải rác trên bản đồ để tích lũy đủ điểm mục tiêu (*Target Point*).
- **Phá khóa cửa**: Sử dụng Minigame Lockpick để mở các cánh cửa bị khóa.
- **Ẩn nấp & Né tránh**: Tránh để các NPC Adult phát hiện hoặc nghe thấy tiếng động.

### 🏆 Điều kiện Thắng / Thua
- **Thắng**: Thu thập đủ số điểm chỉ tiêu trước khi đồng hồ đếm ngược trở về `00:00`.
- **Thua**: Hết thời gian đếm ngược hoặc bị NPC Adult phát hiện và bắt giữ.

---

## ⌨️ Bảng Phím Điều Khiển

| Phím | Hành động |
| :--- | :--- |
| **WASD / Mũi tên** | Di chuyển nhân vật |
| **Shift** | Chạy nhanh (*Sprinting* - tiêu hao Stamina) |
| **Space** | Nhảy / Bấm nhịp canh vạch trong Minigame Phá khóa |
| **E** | Tương tác (Nhặt vật phẩm / Bắt đầu phá khóa cửa) |
| **Chuột trái / Space / E** | Thực hiện nhịp bấm trong Minigame Phá khóa |
| **F** | Bật / Tắt đèn pin (*Flashlight*) |
| **P** | Chuyển đổi góc nhìn Camera (Thứ nhất / Thứ ba) |
| **Ctrl / Crouch** | Ngồi xuống (Ẩn nấp tốt hơn, hồi Stamina) |
| **H** | Bật / Tắt bảng hướng dẫn (*Guide Panel*) |
| **ESC** | Tạm dừng game & Mở Menu Cài đặt (*Settings / Pause*) |

---

## 🌟 Tính năng Chính

- 🗝️ **Minigame Phá khóa (Lockpicking System)**:
  - Cần canh thời gian bấm chính xác khi thanh di chuyển đi qua vùng xanh (Success Zone).
  - Vượt qua **3 Stages** với tốc độ tăng dần và độ rộng vùng xanh giảm dần.
  - Phá khóa thất bại sẽ phát ra âm thanh cảnh báo kích hoạt chế độ rượt đuổi (*Chase Mode*) của các NPC Adult trong phạm vi lân cận.
  - Có nút **Close UI** tiện lợi nếu không muốn tiếp tục phá khóa.

- 🎒 **Hệ thống Vật phẩm & Tải trọng**:
  - Vật phẩm đa dạng với giá trị điểm số và trọng lượng (kg) khác nhau.
  - Quản lý tải trọng tối đa, giao diện UI cảnh báo màu đỏ khi sắp vượt giới hạn hoặc cạn kiệt thể lực.

- 👮 **Hệ thống AI NPC Thông minh (NavMesh)**:
  - **Adult NPC**: Tự động tuần tra theo tuyến đường, phát hiện người chơi khi vào tầm nhìn/nghe tiếng báo động và rượt đuổi với hiệu ứng âm thanh hồi hộp.
  - **Kid NPC**: Các NPC trẻ em đi chuyển ngẫu nhiên trong bản đồ.

- ⚙️ **Menu Cài đặt & Tạm dừng Game (Settings & Pause)**:
  - Bấm phím `ESC` hoặc nút Settings để tạm dừng toàn bộ diễn biến game (`Time.timeScale = 0`).
  - Cung cấp các tùy chọn chơi lại nhanh (**Replay**) hoặc quay về màn hình chính (**Main Menu**).

- 🎥 **Góc nhìn Linh hoạt & Hiệu ứng Hình ảnh**:
  - Tự do chuyển đổi giữa góc nhìn thứ 3 (Third-Person) và góc nhìn thứ 1 (First-Person).
  - Hiệu ứng hậu kỳ Post-Processing (Vignette, Chromatic Aberration, Lighting) sống động.

---

## 🛠️ Cài đặt & Khởi chạy

1. Yêu cầu **Unity version**: `Unity 6000.3.9f1` (hoặc tương đương).
2. Tải hoặc `git clone` dự án về máy.
3. Mở dự án bằng Unity Hub.
4. Mở Scene chính tại: `Assets/Scenes/HomeMenu.unity` hoặc `Assets/Scenes/Lv1.unity`.
5. Nhấn nút **Play** để trải nghiệm game.

---

*Chúc bạn có những giây phút trải nghiệm game vui vẻ!*