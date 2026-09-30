# 🥷 Thief Simulator 3D

## 📝 Giới Thiệu Dự Án

**Thief Simulator 3D** là tựa game hành động lén lút (Stealth Action) và co-op thời gian thực trên nền tảng **Unity 6**. Người chơi hóa thân thành những tên trộm chuyên nghiệp, đột nhập vào các căn biệt thự được canh gác nghiêm ngặt để trộm các món đồ quý giá, bẻ khóa tủ/két sắt, trèo thang vượt tường, ẩn nấp trong tủ quần áo và thoát ra xe tẩu thoát an toàn trước khi bị NPC phát hiện hoặc hết giờ.

Dự án hỗ trợ đầy đủ cả **Chơi đơn (Offline)** lẫn **Chơi nhiều người (Online Co-op qua Photon Fusion)** trên cả **PC (Windows)** và **Điện thoại di động (Android APK)**.

---

## 🎮 Cơ Chế Chơi & Gameplay Loops

### 1. 🎯 Mục tiêu & Điều kiện Thắng / Thua
* **Mục tiêu**: Thu thập đủ giá trị điểm số mục tiêu (**Target Point** - được tính toán tự động dựa trên tổng lượng đồ xuất hiện trong Chapter) và mang đồ về bán tại khu vực an toàn.
* **Khu vực Bán đồ ([HomeSellZone](file:///Assets/Scripts/Environment/HomeSellZone.cs))**: Ném hoặc đặt đồ cướp được vào vùng thùng xe để cộng điểm dồn vào quỹ điểm chung của phòng.
* **Khu vực Tẩu thoát ([EscapeZone](file:///Assets/Scripts/Environment/EscapeZone.cs))**: Bảng điểm 3D Billboard hiển thị tiến độ `$currPoint / $totalPoint`. Khi đạt đủ 100% chỉ tiêu, nút **Escape** sẽ sáng lên.
* **Thắng**: Đứng trong xe tẩu thoát và bấm **Escape** để tẩu thoát thành công. Tự động quy đổi **50% giá trị cướp được thành Tiền mặt (Cash)** lưu vĩnh viễn lên Firebase Cloud Database.
* **Thua**: Bị NPC Người lớn bắt giữ hoặc hết thời gian làm nhiệm vụ.

---

### 2. 🎒 Hệ Thống Túi Đồ (Hotbar & Inventory)
* Quản lý **6 ô trang bị nhanh (Hotbar)**.
* **Hiển thị 3D trực quan**: Khi chọn bất kỳ ô trang bị nào, mô hình 3D của vật phẩm sẽ hiển thị chân thực trước góc nhìn Camera kèm thông tin: *Tên, Giá trị ($), Khối lượng (Kg), Độ hiếm (Rarity)*.
* **Cơ chế Thả đồ an toàn (Safe Raycast Drop)**: 
  * Bắn tia quét kiểm tra trước mặt: Nếu không gian thoáng $\rightarrow$ Ném vật phẩm về phía trước với lực quán tính.
  * Nếu đứng sát tường hoặc vật cản $\rightarrow$ Đặt nhẹ vật phẩm tại điểm va chạm để chống lọt hoặc xuyên tường.
* **7 Cấp độ hiếm**: `Trash` (Rác) $\rightarrow$ `Common` (Thường) $\rightarrow$ `Uncommon` (Hiếm nhẹ) $\rightarrow$ `Rare` (Hiếm) $\rightarrow$ `Epic` (Sử thi) $\rightarrow$ `Legendary` (Huyền thoại) $\rightarrow$ `Mythic` (Thần thoại).

---

### 3. 🧗 Thang Cơ Động (Portable Ladder)
* Thang hoạt động như một **công cụ cơ động**: Có thể nhặt vào Hotbar, cầm trên tay và ném/đặt ra bất kỳ địa hình nào để leo trèo.
* **Leo 2 chiều thông minh**: Hỗ trợ leo từ chân lên đỉnh và từ đỉnh tụt xuống chân.
* **Cảm ứng leo tự động**: Trên điện thoại di động, chỉ cần đứng gần thang và gạt Joystick Lên/Xuống là nhân vật tự bám thang và leo.
* **Nhảy thoát thang**: Bấm phím Nhảy (Space / Jump) bất kỳ lúc nào để nhảy tách ra khỏi thang.

---

### 4. 🚪 Ẩn Nấp Tủ Đồ (Wardrobe Hiding System)
* Người chơi có thể mở tủ và chui vào trong trốn NPC.
* Cánh tủ tự động hé mở một góc $15^\circ$ để người chơi quan sát ra bên ngoài qua khe cửa.
* Khi đang trốn: Toàn bộ HUD rườm rà được ẩn đi, góc nhìn camera khóa theo hướng cửa tủ và NPC tuần tra hoàn toàn không thể phát hiện.
* Hỗ trợ nút **[ RỜI KHỎI TỦ ]** trên màn hình hoặc nhấn phím nóng để bước ra ngoài.

---

### 5. 🔐 Minigame Bẻ Khóa Cửa & Rương Két ([LockedContainerController](file:///Assets/Scripts/Environment/LockedContainerController.cs))
* Tích hợp minigame canh nhịp bẻ khóa qua 3 nấc độ khó tăng dần.
* **Mở cửa & Rương/Két sắt**: Hỗ trợ cả cơ chế xoay cánh cửa (Rotate) và kéo trượt ngăn kéo (Slide).
* **Vật phẩm thưởng (Bonus Loot)**: Mở két sắt / rương khóa sẽ sinh ra các món đồ giá trị cao ngẫu nhiên (không tính vào điểm total bắt buộc của màn).
* **Cảnh báo tiếng động**: Bẻ khóa sai sẽ phát ra tiếng cọt kẹt gọi NPC tuần tra gần đó tới vị trí khả nghi.

---

### 6. 🤖 Trí Tuệ Nhân Tạo NPC (Guard & Runner AI)
* **Người lớn ([AdultGuardNPC](file:///Assets/Scripts/AI/AdultGuardNPC.cs))**: Canh gác/tuần tra theo lộ trình, quét tầm nhìn 9 tia Raycast phủ kín không gian, tự động giảm $50\%$ tầm nhìn khi người chơi cúi (Crouch), rượt đuổi và bắt giữ người chơi.
* **Trẻ em ([KidRunnerNPC](file:///Assets/Scripts/AI/KidRunnerNPC.cs))**: Hoảng hốt bỏ chạy khi phát hiện trộm và la hét gọi Người lớn đến kiểm tra vị trí.

---

### 7. 🌐 Chơi Mạng Co-op (Photon Fusion Shared Mode)
* Đồng bộ phòng chờ (Lobby), danh sách phòng, mã phòng riêng tư, trạng thái Ready/Start.
* Đồng bộ di chuyển, nhặt/thả đồ, trạng thái cửa, bẻ khóa két, người trốn tủ và điểm số ván chơi theo thời gian thực.
* Đồng bộ Seed ngẫu nhiên theo tên phòng để mọi người chơi trong phòng đều có cấu trúc vật phẩm xuất hiện giống nhau 100%.

---

## 🎮 Bảng Phím & Nút Điều Khiển (PC vs. Mobile APK)

Game hỗ trợ song song cả bàn phím/chuột PC và toàn bộ giao diện nút ảo cảm ứng trên Android:

| Hành động | 💻 Bàn Phím PC / WebGL | 📱 Màn Hình Cảm Ứng Mobile APK |
| :--- | :--- | :--- |
| **Di chuyển** | `W, A, S, D` / Phím mũi tên | Cần gạt ảo **Dynamic Joystick** (Nửa trái màn hình) |
| **Xoay Camera** | Di chuyển chuột | **Touch Look Zone** (Vuốt ngón tay ở nửa phải màn hình) |
| **Nhảy** | `Space` | Nút ảo **Jump (🦘)** |
| **Chạy nhanh (Sprint)** | Giữ phím `Shift` | Nút gạt **Sprint (🏃)** *(Bật/Tắt chế độ chạy)* |
| **Ngồi / Cúi người (Crouch)** | Phím `C` hoặc `Ctrl` | Nút gạt **Crouch (🧎)** *(Ẩn nấp, né tầm nhìn NPC)* |
| **Nhặt vật phẩm (Pick Up)** | Phím `E` | Nút ảo **Pick Up (🎒)** *(Chỉ hiện khi nhìn vào Item)* |
| **Tương tác đặc biệt (Interact)** | Phím `E` | Nút ảo **Interact (🖐️)** *(Mở cửa, Trốn tủ, Bẻ khóa...)* |
| **Thả đồ (Drop Item)** | Phím `Q` / `G` | Nút ảo **Drop (⬇️)** *(Thả/ném món đồ đang cầm trên Hotbar)* |
| **Bật/Tắt Đèn pin** | Phím `F` | Nút ảo **Flashlight (🔦)** trên HUD |
| **Chọn Slot đồ Hotbar** | Phím số `1`, `2`, `3`, `4`, `5`, `6` | Chạm trực tiếp vào từng ô **Hotbar Slot** ở đáy màn hình |
| **Leo Thang (Climb)** | Bấm `W` (Lên) / `S` (Xuống) | Gạt **Joystick Lên/Xuống** *(Tự động bám thang)* |
| **Nhảy thoát khỏi thang** | Phím `Space` | Nút ảo **Jump** *(Tự đổi icon sang thoát thang khi đang leo)* |
| **Thoát khỏi tủ trốn** | Phím `E` / `Space` / `F` | Chạm nút **[ RỜI KHỎI TỦ ]** trên màn hình |
| **Nhịp bẻ khóa Minigame** | `Space` / `Chuột trái` / `E` | Chạm vào vùng vòng tròn cảm ứng trên màn hình |
| **Tẩu thoát qua màn** | Click nút Escape trên màn hình | Chạm nút **[ ESCAPE ]** *(Hiện khi đứng trong xe đủ điểm)* |
| **Menu Tạm dừng (Pause)** | Phím `ESC` hoặc `P` | Chạm nút **Pause (⏸️)** ở góc trên màn hình |

---

## 🛠️ Cài Đặt & Môi Trường Phát Triển

* **Unity Engine**: `Unity 6000.3.9f1` (hoặc Unity 6 bản LTS mới nhất).
* **Mạng Co-op**: Photon Fusion 2 (Shared Mode).
* **Cơ sở dữ liệu đám mây**: Firebase Authentication & Realtime Database.
* **Target Platforms**: 
  * `Windows Standalone (x86_64)`
  * `Android (APK / AAB) - IL2CPP, ARM64`
* **Scene khởi đầu**: `Assets/Scenes/HomeMenu.unity`