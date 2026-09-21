# 📜 Changelog — Bullet Hell Project

All notable changes to this project will be documented in this file. This repository adheres to **Keep a Changelog** standards and maps directly to **Conventional Commits**.

---

## 🔒 Quy Tắc Cập Nhật Dành Cho AI Agent

Khi hoàn thành bất kỳ tính năng (`feat`), sửa lỗi (`fix`), tái cấu trúc (`refactor`) nào, AI Agent **BẮT BUỘC** phải cập nhật file `CHANGELOG.md` này bằng cách thêm một mục mới vào phần tương ứng theo cấu trúc:

```markdown
### [YYYY-MM-DD HH:mm] — <type>(<scope>): <short description in English>
- **Tác vụ**: Mô tả chi tiết việc đã làm.
- **Danh sách file thay đổi**:
  - `Assets/_Project/.../Filename.cs` (Dòng modified/new)
- **Ảnh hưởng**: Những tính năng hay cấu trúc nào bị ảnh hưởng.
- **Lưu ý**: log mới hãy ghi xuống cuối file.
```

---

### [2026-09-21 08:30] — feat(inventory): implement hotbar system, item descriptions, and safe raycast drop
- **Tác vụ**:
  - Bổ sung trường dữ liệu `description`, phương thức `GetDescription()` và `GetIcon()` vào [IInteractable.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/IInteractable.cs) và [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs).
  - Cập nhật [ItemInfoHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ItemInfoHUD.cs) để hiển thị chi tiết mô tả khi rọi tâm ngắm vào vật phẩm.
  - Tạo mới [HotbarSlot.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarSlot.cs) quản lý từng slot UI (hiển thị icon, đổi màu nền vàng khi được chọn).
  - Tạo mới [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs) để:
    - Thêm vật phẩm vào slot trống theo thứ tự từ trái qua phải.
    - Hiển thị mô hình 3D của vật phẩm trước mặt Player khi bấm chọn slot.
    - Chỉ hiển thị nút Drop khi đang chọn ô có vật phẩm.
    - Thả vật phẩm thông minh: Bắn Raycast về phía trước, nếu đứng sát tường sẽ thả tại giao điểm trừ khoảng cách an toàn (tránh xuyên/lọt tường).
  - Tái cấu trúc [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) để kết nối logic nhặt đồ và xóa Hotbar khi Player tử vong.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarSlot.cs` (New)
  - `Assets/Scripts/UI/HotbarManager.cs` (New)
  - `Assets/Scripts/Items/IInteractable.cs` (Modified)
  - `Assets/Scripts/Items/Item.cs` (Modified)
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/UI/LockpickDoor.cs` (Modified)
  - `Assets/Scripts/UI/ItemInfoHUD.cs` (Modified)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
- **Ảnh hưởng**:
  - Tách rời và nâng cấp cơ chế nhặt đồ cũ sang hệ thống quản lý từng slot Hotbar có chọn lọc và hiển thị trực quan trước mặt.
  - Vẫn giữ fallback cơ chế cũ nếu scene chưa có component `HotbarManager`.

---

### [2026-09-21 08:46] — fix(physics): resolve kinematic rigidbody linear velocity warning
- **Tác vụ**:
  - Sửa lỗi cảnh báo Unity `Setting linear velocity of a kinematic body is not supported` khi nhặt hoặc hiển thị model item trong Hotbar.
  - Kiểm tra trạng thái `!rb.isKinematic` trước khi gán `linearVelocity = Vector3.zero` và `angularVelocity = Vector3.zero`.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 185-195, 305-315)
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 475-485)
- **Ảnh hưởng**:
  - Loại bỏ hoàn toàn cảnh báo lỗi console của Unity Physics khi chọn hoặc đổi slot item.

---

### [2026-09-21 09:25] — fix(ui, hotbar): fix crosshair idle lerp, held item 3D visibility, and enhance drop physics
- **Tác vụ**:
  - Tách biệt `infoContentPanel` và `crosshairImage` trong [ItemInfoHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ItemInfoHUD.cs) để không tắt cả GameObject HUD khi `Hide()`, đảm bảo tâm ngắm luôn lerp mượt mà về trạng thái idle (nhỏ lại và mờ đi) khi thoát khỏi việc nhìn item.
  - Cải tiến [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
    - Đảm bảo `ItemHoldPoint` gắn trực tiếp theo góc nhìn `Camera.main` với góc xoay và vị trí cầm tay hiển thị 3D rõ ràng (`holdPointOffset`, `holdPointRotation`).
    - Bật sáng các `Renderer`, tạm ẩn script `Range_Interaction` (chữ 3D) trên item để không che khuất màn hình khi đang cầm trên tay.
    - Nâng cấp cơ chế Drop với lực ném và lực nâng tự nhiên (`dropForwardForce`, `dropUpwardForce`, `dropTorque`), tự động thả nhẹ rơi xuống khi đứng sát tường.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/ItemInfoHUD.cs` (Lines 60-140, 240-280)
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 20-40, 130-160, 280-340, 370-440)
- **Ảnh hưởng**:
  - Tâm ngắm phản hồi chính xác và mượt mà.
  - Model 3D của item hiển thị rõ trước góc nhìn Camera khi chọn slot.
  - Thao tác thả đồ có lực ném/rơi tự nhiên và an toàn.

---

### [2026-09-21 09:55] — fix(hotbar): fix held item 3D model always stays SetActive(false) despite hotbar selection
- **Tác vụ**:
  - **Root Cause**: `PlayerController.TakeItem()` chạy **MỖI FRAME** trong `Update()` và gọi `SetActive(false)` lên **TẤT CẢ** item trong `heldItem` list — bao gồm cả item mà `HotbarManager.ShowHeldModel()` vừa bật lên `SetActive(true)`. Kết quả: item bị tắt lại ngay frame tiếp theo.
  - **Fix**: Sửa vòng lặp trong `TakeItem()` để **bỏ qua** item đang được `HotbarManager` hiển thị trước mặt (thông qua `GetCurrentHeldModel()`).
  - Thêm method `GetCurrentHeldModel()` vào `HotbarManager` để `PlayerController` truy vấn item nào đang được cầm.
  - Revert `InitializeHoldPoint` về gắn `ItemHoldPoint` dưới `Camera.main` (theo yêu cầu user).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 334-349 — TakeItem method)
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 149-200 — reverted InitializeHoldPoint, added GetCurrentHeldModel)
- **Ảnh hưởng**:
  - Model 3D của item giờ hiển thị chính xác khi chọn slot trên Hotbar, không còn bị TakeItem() tắt mỗi frame.

---

### [2026-09-21 10:16] — feat(hotbar): simplify drop mechanics with raycast obstacle placement and forward throw
- **Tác vụ**:
  - Đơn giản hóa cơ chế Drop trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
    - Khi bắn Raycast kiểm tra phía trước, nếu chạm vào vật cản/tường -> đặt item ngay tại điểm va chạm (`hit.point`) kèm bù nhẹ pháp tuyến.
    - Nếu không có vật cản phía trước -> ném thẳng item về phía trước theo hướng nhìn với lực `dropForwardForce`.
    - Loại bỏ các biến lực phức tạp không cần thiết (`dropUpwardForce`, `dropTorque`, `wallSafetyOffset`).
    - Thêm Debug trực quan:
      - Vẽ tia Raycast trực tiếp thời gian thực trong Scene View (Màu Đỏ = Có vật cản, Màu Xanh Lá = Không gian thoáng).
      - In log chi tiết tên GameObject va chạm, khoảng cách và hành động thả tương ứng lên Console.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 26-56, 110-128, 443-495)
- **Ảnh hưởng**:
  - Dễ dàng quan sát và kiểm chứng tia quét Raycast kiểm tra vật cản trực quan trong Unity Editor.

---

### [2026-09-21 10:26] — fix(hotbar): ignore held item and player colliders during drop raycast check
- **Tác vụ**:
  - **Nguyên nhân**: Khi bấm Thả đồ (Drop), tia Raycast bắn từ Camera ra phía trước va trúng ngay chính collider của vật phẩm đang cầm trên tay (`CeramicVase`) ở cự ly 0.65m, khiến hệ thống hiểu nhầm vật phẩm đang cầm là vật cản trước mặt và đặt tại chỗ thay vì ném ra.
  - **Khắc phục**:
    - Sử dụng `Physics.RaycastAll` và tự động lọc bỏ (bỏ qua) mọi collider thuộc về chính item đang cầm (`itemObj`) và `Player`.
    - Tính toán vị trí thả trước, sau đó mới bật lại các collider của item.
    - Cập nhật cả tia debug trong `Update()` để không bị chuyển màu đỏ do chính item đang cầm.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 114-142, 465-535)
- **Ảnh hưởng**:
  - Khi đứng ở không gian thoáng, vật phẩm sẽ luôn được ném về phía trước một cách chính xác mà không bị chặn bởi chính model của nó.

---

### [2026-09-21 10:34] — feat(ui, hotbar): display held item details on hotbar UI (name, price, weight)
- **Tác vụ**:
  - Thêm các trường `TextMeshProUGUI` (`nameSlotItem`, `priceSlotItem`, `weightSlotItem`) và `heldItemInfoPanel` vào [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs).
  - Tự động tìm kiếm các text UI (`nameSlotItem`, `priceSlotItem`, `weightSlotItem`) trong cây phân cấp nếu chưa được kéo thả trong Inspector.
  - Khi chọn slot có vật phẩm (`SelectSlot`): hiển thị Tên, Giá tiền (`$XX`) và Cân nặng (`XXKg`) tương ứng lên UI.
  - Khi bỏ chọn slot hoặc thả vật phẩm (`DeselectAll`, `HideHeldModel`, `DropSelectedItem`): tự động ẩn/xóa trắng các text thông tin.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 20-35, 230-310, 325-360)
- **Ảnh hưởng**:
  - Thanh Hotbar HUD hiển thị đầy đủ thông tin trực quan của vật phẩm đang được người chơi lựa chọn.

---

### [2026-09-21 10:48] — feat(items): introduce item rarity enum with Trash and Mythic tiers
- **Tác vụ**:
  - Khởi tạo `enum ItemRarity` trong [ItemSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/ItemSO.cs) với 7 cấp độ: `Trash`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythic`.
  - Bổ sung class `ItemRarityExtensions` cung cấp mã màu UI (`GetColor()`) và tên tiếng Việt (`GetDisplayName()`) cho từng bậc hiếm.
  - Thêm thuộc tính `rarity` vào [ItemSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/ItemSO.cs) và đồng bộ sang [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs) lúc runtime.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/ItemSO.cs` (Lines 3-55)
  - `Assets/Scripts/Items/Item.cs` (Lines 10-15, 37-43)
- **Ảnh hưởng**:
  - Hệ thống dữ liệu vật phẩm đã có đầy đủ phân loại độ hiếm sẵn sàng cho hệ thống Chapter Loot / Spawn Table tính toán tỉ lệ xuất hiện.

---

### [2026-09-21 11:06] — feat(ui): add item rarity text and color coding to ItemInfoHUD and HotbarHUD
- **Tác vụ**:
  - Bổ sung phương thức `GetRarity()` vào [IInteractable.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/IInteractable.cs), [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs), [LockpickDoor.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/LockpickDoor.cs) và [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs).
  - Cập nhật [ItemInfoHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ItemInfoHUD.cs):
    - Thêm `rarityText`, tự động tìm component nếu chưa gán trong Inspector.
    - Hiển thị trực tiếp tên độ hiếm (VD: `Common`, `Rare`, `Mythic`...) không kèm tiền tố `"Rarity:"`.
    - Tự động đổi màu chữ theo mã màu tương ứng của độ hiếm (`rarity.GetColor()`).
  - Cập nhật [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
    - Thêm `raritySlotItem`, tự động tìm component trong phân cấp Hotbar HUD.
    - Cập nhật tên độ hiếm và màu sắc tương ứng khi chọn item trên Hotbar.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/IInteractable.cs` (Lines 47-53)
  - `Assets/Scripts/Items/Item.cs` (Lines 96-102)
  - `Assets/Scripts/Items/Ladder.cs` (Lines 85-90)
  - `Assets/Scripts/UI/LockpickDoor.cs` (Lines 207-212)
  - `Assets/Scripts/UI/ItemInfoHUD.cs` (Lines 30-40, 125-140, 240-330)
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 30-45, 235-315)
- **Ảnh hưởng**:
  - Cả HUD rọi tâm ngắm và Hotbar đều hiển thị trực quan cấp độ hiếm với màu sắc đồng bộ, bắt mắt.

---

### [2026-09-21 11:41] — feat(interaction): prevent pickup and show warning when all hotbar slots are full
- **Tác vụ**:
  - Cập nhật hàm `CanInteract` trong [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs) để kiểm tra trạng thái Hotbar (`!hotbarManager.HasEmptySlot()`).
  - Khi 6 slot Hotbar đã đầy, hệ thống tự động:
    - Báo lý do không thể tương tác: `failReason = "Hotbar Full!"`.
    - Hiển thị dòng cảnh báo chữ màu đỏ `warningText` trên `ItemInfoHUD`.
    - Chặn bấm nhặt item từ cả bàn phím (`E`) lẫn nút Mobile (`Pick Up`).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Item.cs` (Lines 112-124)
- **Ảnh hưởng**:
  - Người chơi được cảnh báo rõ ràng khi túi đồ đầy, không bị mất item hay nhặt đè không kiểm soát.






