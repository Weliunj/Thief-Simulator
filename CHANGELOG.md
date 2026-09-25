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
- **Quy tắc khung giờ (3 tiếng)**: Các thay đổi/tác vụ diễn ra trong vòng **3 tiếng** sẽ được viết gộp chung vào cùng 1 mục log để tránh làm file quá dài.
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

---

### [2026-09-21 12:13] — fix(interaction): fix duplicate pickup bypass when hotbar full and show warning only on button press
- **Tác vụ**:
  - **Khắc phục lỗi vẫn nhặt được khi Hotbar đầy**:
    - `PlayerController.TakeItem()` cũ chạy `OverlapSphere` độc lập và trực tiếp thêm item vào túi mà không thông qua `TryPickupItem()`.
    - Đã sửa `TakeItem()` để tắt trùng lặp khi có `PlayerInteraction`, và chuyển qua `TryPickupItem()` kiểm tra nghiêm ngặt `!hotbarManager.HasEmptySlot()`.
  - **Sửa hiển thị cảnh báo**:
    - Khi chỉ rọi tâm ngắm nhìn vào item: HUD hiển thị thông tin sạch sẽ (Tên, Giá, Cân nặng, Độ hiếm) mà **không hiện sẵn chữ cảnh báo đỏ**.
    - Chỉ khi người chơi **bấm phím `E` / nút `Pick Up`** mà không đủ điều kiện (Hotbar đầy hoặc quá tải): HUD mới kích hoạt Coroutine `ShowWarning` hiện dòng chữ cảnh báo đỏ (`Hotbar Full!`) trong 2 giây rồi tự ẩn.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 352-378)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Lines 170-184)
  - `Assets/Scripts/UI/ItemInfoHUD.cs` (Lines 202-315)
- **Ảnh hưởng**:
  - Ngăn chặn hoàn toàn việc nhặt vượt quá 6 slot và giao diện cảnh báo hiển thị đúng lúc người chơi thao tác.

---

### [2026-09-21 12:25] — feat(ladder): modularize ladder into standard item with 2-way climbable component
- **Tác vụ**:
  - Tái cấu trúc [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs) thành component chuyên xử lý leo trèo độc lập và gọn gàng:
    - Loại bỏ code cũ phụ thuộc cứng `Range_Interaction`.
    - Hỗ trợ leo 2 chiều (`A -> B` khi ở chân thang và `B -> A` khi ở đỉnh thang).
    - Tự động tìm kiếm hoặc khởi tạo điểm `PointA` (chân thang) và `PointB` (đỉnh thang).
    - Tự động bước/nhảy lên phía trước khi leo lên đến đỉnh (`topExitForwardOffset`).
    - Vẽ Gizmos trực quan đường leo trong Scene View.
  - Thang giờ đây hoạt động như một **Item bình thường** (gắn `Item.cs`): có thể nhặt vào Hotbar, cầm trên tay (Hold), ném/thả/đặt ra ngoài thế giới để sử dụng leo trèo vượt tường/leo nóc nhà.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Rewritten)
- **Ảnh hưởng**:
  - Thang trở thành một công cụ cơ động (Portable Tool) trong kho đồ của tên trộm.

---

### [2026-09-21 12:40] — feat(ladder): implement interactive manual climbing, 1-clip reverse/pause animation, and jump-off mechanic
- **Tác vụ**:
  - Chuyển toàn bộ logic điều khiển trèo thang sang [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs) giúp [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) gọn gàng, tách biệt trách nhiệm.
  - Hỗ trợ điều khiển leo thang thủ công bằng Input (W / S hoặc Virtual Joystick trên Mobile):
    - **Bấm W (Lên)**: Di chuyển lên phía đỉnh thang B, phát animation tiến xuôi (`animator.speed = animSpeedMultiplier`).
    - **Bấm S (Xuống)**: Di chuyển xuống phía chân thang A, phát animation tua ngược (`animator.speed = -animSpeedMultiplier`).
    - **Thả phím (Đứng yên)**: Dừng di chuyển trên thang, đóng băng animation tại frame hiện tại (`animator.speed = 0`).
  - Hỗ trợ **phím Nhảy (Space / Jump Mobile) để thoát thang**:
    - Ngắt trạng thái leo, khôi phục tốc độ `animator.speed = 1.0f` và `Climb = false`.
    - Thêm lực đẩy lùi ra phía sau lưng người chơi (`jumpOffPushForce`) kèm nâng nhẹ (`jumpOffUpForce`) để nhảy tách ra khỏi thang.
  - Bổ sung các biến tùy chỉnh tiện lợi trong Inspector của [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs): `climbSpeed`, `animSpeedMultiplier`, `jumpOffPushForce`, `jumpOffUpForce`, `topExitForwardOffset`, `interactDistance`, `climbKey`.
  - Cập nhật [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) để quản lý an toàn trạng thái `isClimbingLadder` và khôi phục tốc độ `_animator.speed` khi thoát thang.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - Tận dụng tối đa 1 animation "Climb" duy nhất mà vẫn có đầy đủ 3 trạng thái: leo lên, leo xuống, đứng im và nhảy thoát ra sau bất kỳ lúc nào.

---

### [2026-09-21 12:55] — feat(ladder): mobile auto-climb on trigger/raycast + joystick, and fix reverse animation playback
- **Tác vụ**:
  - **Tự động bám thang cho Mobile (`autoClimbOnJoystick`)**:
    - Khi người chơi chạm vào Trigger / Collider của thang hoặc đứng gần và nhìn/rọi tâm ngắm vào thang (`IsPlayerFacingOrLookingAtLadder`), chỉ cần **gạt Joystick Lên (W) hoặc Xuống (S)** là tự động bám vào thang và bắt đầu leo (không bắt buộc phải bấm thêm nút phụ).
    - Vẫn giữ phím bấm thủ công `F` / nút tương tác Mobile cho các trường hợp muốn tương tác chủ động.
  - **Khắc phục lỗi Animation leo xuống không chạy**:
    - Nguyên nhân trong Unity Mecanim: Khi clip đang ở `normalizedTime = 0.0f`, gán `animator.speed = -1` bị kẹt ở frame 0 vì không thể tua ngược vượt qua thời gian 0.
    - Giải pháp đã áp dụng:
      1. Khởi tạo `normalizedTime = 0.99f` khi bắt đầu leo xuống từ đỉnh thang B.
      2. Tự động Wrap frame: Khi tua ngược chạm frame đầu (`normalizedTime <= 0.02f`), code tự động đưa về frame cuối (`0.99f`) để tiếp tục chu kỳ tua ngược liên tục, mượt mà.
      3. Tự động kiểm tra và set parameter `ClimbSpeed` nếu Animator Controller có cài đặt Speed Multiplier.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
- **Ảnh hưởng**:
  - Tối ưu hóa trải nghiệm điều khiển cảm ứng trên Mobile (mượt mà như các game mobile nổi tiếng) và đảm bảo animation 1 clip tua xuôi / tua ngược hoạt động 100%.

---

### [2026-09-21 13:03] — fix(animation): fix Unity negative Animator.speed error via normalized time scrubbing and parameter multiplier
- **Tác vụ**:
  - Sửa lỗi runtime `Animator.speed can only be negative when Animator recorder is enabled`:
    - Unity Mecanim chặn gán giá trị âm trực tiếp vào thuộc tính toàn cục `Animator.speed` trong chế độ chơi thông thường.
    - Đã cập nhật [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs):
      1. Luôn giữ `Animator.speed >= 0`.
      2. Khi leo xuống: Sử dụng cơ chế điều khiển `climbNormalizedTime` lùi dần và gọi `_animator.Play(0, 0, climbNormalizedTime)` trực tiếp (hoặc set qua Float parameter `ClimbSpeed` nếu có cấu hình trong Animator Controller).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Lines 345-410)
- **Ảnh hưởng**:
  - Triệt tiêu hoàn toàn lỗi console trong Unity khi leo thang xuống, animation tua ngược mượt mà không cần can thiệp offline recording.

---

### [2026-09-21 13:35] — feat(ui, ladder): integrate dedicated mobile InteractBtn and raycast PointA/PointB ladder climbing
- **Tác vụ**:
  - **Tách biệt nút `PickupBtn` và `InteractBtn` trên Mobile**:
    - Nâng cấp [MobileActionButtons.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/MobileActionButtons.cs): Thêm trường `pickupButton` (nhặt item vào túi) và `interactButton` (tương tác đặc biệt: Thang, Cửa, v.v.).
    - Cập nhật [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs):
      - Khi rọi tâm ngắm vào Item Loot (`IsLootItem() == true`): Tự động bật `PickupBtn`, ẩn `InteractBtn`.
      - Khi rọi tâm ngắm vào đối tượng đặc biệt như Thang (`IsLootItem() == false`): Tự động bật `InteractBtn`, ẩn `PickupBtn`.
      - Khi không nhắm vào vật thể nào: Tự động ẩn cả 2 nút.
  - **Tích hợp `IInteractable` và Raycast điểm A / điểm B cho [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs)**:
    - Thêm Trigger Collider cho `PointA` và `PointB` để tâm ngắm Raycast từ cả trên nóc nhà nhìn xuống (điểm B) hay dưới đất nhìn lên (điểm A) đều bắt trúng dễ dàng.
    - Khi bấm `InteractBtn` (hoặc phím `E` / `F`):
      - Đứng ở trên nóc nhà / nhìn gần điểm B -> Gợi ý `"Climb Down"`, tự động bám vào đỉnh thang và leo xuống.
      - Đứng ở dưới đất / nhìn gần điểm A -> Gợi ý `"Climb Up"`, tự động bám vào chân thang và leo lên.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Modified)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Modified)
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
- **Ảnh hưởng**:
  - Giao diện nút bấm trên Mobile hiển thị đúng ngữ cảnh (nút Nhặt cho đồ, nút Tương tác cho Thang/Cửa), xử lý leo thang 2 chiều từ trên cao xuống hoặc từ dưới lên cực kỳ trực quan và nhạy bén.

---

### [2026-09-21 13:42] — fix(ladder): remove auto-climb trigger and enforce explicit Interact button press
- **Tác vụ**:
  - Loại bỏ hoàn toàn cơ chế tự động bám thang khi di chuyển Joystick (`autoClimbOnJoystick` và trigger overlap).
  - Thang giờ đây chỉ bắt đầu leo khi người chơi **chủ động bấm nút `InteractBtn` trên Mobile (hoặc phím `E` / `F` trên PC)**:
    - Rọi tâm ngắm vào thang (hoặc đứng gần chân/đỉnh thang) -> Nút `InteractBtn` sáng lên.
    - Nhấn `InteractBtn` -> Player bám vào thang.
    - Khi đã bám thang -> Dùng Joystick W/S để leo lên/xuống, bấm Space/Jump để nhảy thoát ra sau.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Cleaned up auto-climb logic)
- **Ảnh hưởng**:
  - Tránh việc người chơi vô tình bị hút/dính vào thang khi chỉ đi ngang qua; mọi thao tác leo thang đều được kiểm soát chủ động qua nút bấm.

---

### [2026-09-21 13:52] — feat(ladder): instant climb idle pose on interact and independent Up/Down animation speeds
- **Tác vụ**:
  - **Tư thế bám thang tức thì (Climb Idle Pose)**:
    - Khi bấm `InteractBtn` để bám thang, gọi trực tiếp `_animator.Play("Climb", 0, climbNormalizedTime)` kèm `_animator.Update(0f)` để nhân vật ngay lập tức chuyển sang tư thế tay chân bám chắc trên bậc thang thay vì bị kẹt ở tư thế đứng Idle dưới đất.
  - **Tách biệt 2 biến tốc độ Animation Lên và Xuống**:
    - Thêm `animClimbUpSpeed` (mặc định `1.0`): Tùy chỉnh tốc độ animation khi trèo LÊN.
    - Thêm `animClimbDownSpeed` (mặc định `1.0`): Tùy chỉnh tốc độ animation khi trèo XUỐNG.
    - Cân bằng tốc độ phát animation của cả 2 chiều di chuyển, loại bỏ hệ số chênh lệch cứng.
- **Ảnh hưởng**:
  - Nhân vật vào tư thế bám thang đẹp mắt ngay khi bấm nút, và người dùng có thể thoải mái tinh chỉnh tốc độ animation trèo lên / trèo xuống độc lập theo ý muốn trong Inspector.

---

### [2026-09-21 13:55] — feat(ladder): rail-based movement for inclined ladders, collision ignore, fast slide down, and jump cooldown
- **Tác vụ**:
  - **Khắc phục triệt để lỗi kẹt thang khi đặt thang nghiêng**:
    - Chuyển sang cơ chế di chuyển theo **Ray Toán Học (`climbProgress` từ 0 đến 1)** nội suy giữa `PointA` và `PointB`: Player di chuyển chính xác 100% dọc theo đường ray thang ở bất kỳ góc nghiêng nào (30°, 60°, 75°...) mà không phụ thuộc vào ma sát vật lý.
    - Tự động gọi `Physics.IgnoreCollision(true/false)` giữa Player và toàn bộ Collider của Thang khi đang bám thang -> Triệt tiêu hoàn toàn va chạm gây khựng/kẹt.
  - **Thêm tính năng Tuột thang nhanh (`slideSpeedMultiplier`)**:
    - Khi trèo xuống, nếu giữ phím **Crouch (`Ctrl` / Nút Cúi)** hoặc **Sprint (`Shift` / Nút Chạy)** hoặc kéo mạnh Joystick xuống -> Kích hoạt chế độ **Tuột thang nhanh (Slide Down)** với tốc độ gấp `slideSpeedMultiplier` lần (mặc định x2.2 lần).
  - **Khắc phục lỗi spam nhảy liên tục trên thang**:
    - Thêm biến `reClimbCooldown` (mặc định `0.45s`) ngăn Player bị hút/bám lại thang ngay lập tức sau khi bấm Nhảy rời thang.
    - Xóa cờ `starterInputs.jump` và áp dụng lực đẩy lùi an toàn `pushVector * 0.2f` đưa Player tách xa hẳn khỏi thang.
---

### [2026-09-21 14:24] — fix(ladder, camera): top rung attachment inset and ladder camera yaw clamping
- **Tác vụ**:
  - **Khắc phục lỗi bám vào hư không khi leo xuống từ đỉnh thang**:
    - Thêm `topAttachProgress` (mặc định `0.90`) và `bottomAttachProgress` (mặc định `0.05`) vào [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs).
    - Khi bấm tương tác để leo xuống từ đỉnh thang, nhân vật sẽ được đặt trực tiếp vào vị trí bậc thang phía dưới đỉnh (thay vì vị trí 1.0 trên đỉnh mép tường), giúp tay chân bám chắc chắn vào rungs của thang mà không bị trôi lơ lửng giữa không trung.
  - **Đồng bộ hướng nhìn và giới hạn góc quay Camera khi leo thang**:
    - Thêm `SetLadderCameraFacing(targetYaw)` và `ladderYawClamp` (mặc định `70°`) vào [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs).
    - Khi bám vào thang, tự động xoay mặt nhân vật và camera hướng thẳng vào mặt thang.
    - Trong suốt quá trình đang leo (`isClimbingLadder == true`), khóa không cho xoay camera tự do 360° ra sau lưng nhân vật, giới hạn góc quay trái/phải trong phạm vi an toàn ±70°.
    - Khi nhảy rời thang hoặc leo xong tới đỉnh/chân thang, khôi phục góc nhìn 360° tự do bình thường.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
- **Ảnh hưởng**:
  - Tư thế bám thang chân thực hơn, giải quyết triệt để lỗi góc nhìn 360° bất thường và lỗi bám mép không trung khi trèo xuống.

---

### [2026-09-21 14:35] — feat(ladder): fix top exit step direction and add canClimb permission toggle
- **Tác vụ**:
  - **Sửa hướng bước ra ở đỉnh thang (`ExitTopLadder`)**:
    - Điều chỉnh hướng bước ra ở đỉnh thang theo hướng mặt trước mà Player đang nhìn (`playerController.transform.forward` / `-transform.forward`), đảm bảo khi leo lên đến đỉnh thang nhân vật sẽ bước thẳng về phía trước lên sàn/mái nhà thay vì bị bước ngược ra sau lưng.
    - Cập nhật tia Gizmos màu vàng ở đỉnh thang chỉ đúng hướng bước lên sàn.
  - **Bổ sung cờ quyền leo thang `canClimb` (Hỗ trợ Online / Debuff / Trạng thái)**:
    - Thêm `canClimb` vào [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs) và [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs).
    - Nếu `canClimb == false`, ngăn không cho tương tác hoặc bắt đầu leo thang.
    - Nếu `canClimb` bị chuyển sang `false` trong khi Player đang bám trên thang (do bị stun, bị mất quyền leo, hoặc server online ngắt), Player sẽ tự động kích hoạt `FallOffLadder()` buông tay rơi tự do xuống dưới với trọng lực tự nhiên.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
- **Ảnh hưởng**:
  - Khắc phục triệt để lỗi bước ra ngược ở đỉnh thang và sẵn sàng tích hợp logic multiplayer / trạng thái gameplay.

---

### [2026-09-21 14:40] — fix(hotbar, ladder): auto hide held 3D item model while climbing ladder
- **Tác vụ**:
  - **Tự động cất/ẩn model item đang cầm khi leo thang**:
    - Khi bắt đầu leo thang (`StartClimbing`), gọi `playerController.hotbarManager.DeselectAll()` và ẩn tất cả model trong `heldItem`.
    - Ẩn model 3D vật phẩm trước mặt để 2 tay nhân vật rảnh bám vào thang leo tự nhiên.
    - Vật phẩm vẫn được lưu giữ an toàn tuyệt đối trong các ô Hotbar / túi đồ của Player, không bị rơi hay mất đi.
  - **Khóa thao tác chọn/cầm item trên Hotbar khi đang bám thang**:
    - Cập nhật [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Khi `playerController.isClimbingLadder == true`, bỏ qua các thao tác click slot, phím số 1-9 và phím Drop Q/G.
    - Sau khi leo xong hoặc nhảy rời thang, Player có thể thoải mái bấm chọn lại bất kỳ ô item nào trên Hotbar để cầm lại đồ.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
- **Ảnh hưởng**:
  - Loại bỏ hoàn toàn hiện tượng model đồ vật bị cầm lơ lửng trước mặt che khuất tầm nhìn và bậc thang khi đang leo.

---

### [2026-09-21 14:52] — feat(interaction): support dual Pickup and Climb buttons for objects having both Item and Ladder
- **Tác vụ**:
  - **Tách biệt và hỗ trợ đa tương tác (Multi-Interactable) trên cùng một GameObject**:
    - Nâng cấp [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs) quét và nhận diện đồng thời cả component `Item` (Loot Item) và `Ladder` (hoặc các tương tác đặc biệt khác).
    - **Khi đối tượng có cả `Item` và `Ladder` (như Thang xách tay)**:
      - **Trên Mobile**: Tự động bật **CẢ 2 NÚT** (`Pickup` để nhặt và `Interact` để leo thang).
      - **Trên PC**:
        - Phím **`E`** (hoặc nút `Pickup`): Nhặt Thang vào Hotbar/Túi đồ (thực thi qua `Item.cs`).
        - Phím **`F`** (hoặc nút `Interact`): Bám vào Thang để Leo (thực thi qua `Ladder.cs`).
  - **Giữ nguyên thiết kế gốc chuẩn Clean Architecture**:
    - [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs) thuần túy phụ trách logic leo thang (`IsLootItem() = false`).
---

### [2026-09-21 14:56] — fix(hotbar, ladder): align drop rotation with player view and dynamic approach side climbing
- **Tác vụ**:
  - **Sửa góc xoay khi thả (Drop) Thang / Item theo hướng nhìn của Player**:
    - Trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs), thay thế `Quaternion.identity` bằng `Quaternion.LookRotation(forwardDir)` dựa theo góc nhìn hiện tại của người chơi.
    - Bất kể người chơi quay mặt về hướng nào (Bắc, Nam, Đông, Tây) khi bấm thả đồ, Thang sẽ luôn được xoay thẳng mặt theo góc nhìn của người chơi.
    - Giảm lực đẩy xung lượng khi thả Thang để Thang đứng vững trên mặt sàn, tránh bị văng lật ngã.
  - **Khắc phục lỗi người chơi trèo bị bay sang mặt đối diện (`Ladder.cs`)**:
    - Trong [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs), tính toán vector hướng tiếp cận giữa Player và Thang.
    - Tự động phát hiện người chơi đang đứng ở mặt trước hay mặt sau của thang để xoay mặt người chơi và góc camera hướng thẳng vào mặt thang phía tiếp cận (thay vì luôn cố định cứng 1 hướng duy nhất làm người chơi bị lật 180° sang phía bên kia).
---

### [2026-09-21 15:01] — fix(interaction, ladder): disable pickup and auto-stop climbing on ladder disable to prevent floating in midair
- **Tác vụ**:
  - **Tự động ẩn nút Pickup và chặn nhặt đồ khi đang leo thang**:
    - Trong [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs), kiểm tra `playerController.isClimbingLadder`: Tắt toàn bộ HUD, nút `Pickup` và nút `Interact`, đồng thời chặn nhận phím `E` / `F`.
    - Trong [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs), thêm kiểm tra `isClimbingLadder` vào `CanInteract()` để từ chối mọi yêu cầu nhặt đồ khi người chơi đang bám trên thang.
    - Cập nhật trong `LateUpdate()` của [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Vị trí cầm item di chuyển lên/xuống mượt mà theo góc nhìn Camera, nhưng góc xoay (Rotation) của vật phẩm luôn được giữ cố định trục Y hướng thẳng đứng (`Vector3.up`).
    - Khi người chơi ngước camera lên trời hoặc cúi xuống đất, item chỉ di chuyển vị trí theo tầm mắt mà không bị chúi/nghiêng theo camera, luôn giữ dáng đứng thẳng.
  - **Khôi phục vật lý tự nhiên bình thường khi Thả/Ném (Drop)**:
    - Khi thả vật phẩm ra thế giới (`DropSelectedItem`), đặt `rb.constraints = RigidbodyConstraints.None`.
    - Cho phép vật thể tự do tương tác vật lý (rơi, va chạm, lăn, ngã tự nhiên) theo trọng lực và lực ném thông thường thay vì bị khóa cứng góc xoay.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
- **Ảnh hưởng**:
  - Item trên tay hiển thị trực quan và thẳng đứng khi xoay camera ngước lên/xuống.

---

### [2026-09-21 15:50] — refactor(items, ui): rename to DoorController and LadderController, and cleanup obsolete Range_Interaction
- **Tác vụ**:
  - **Tái cấu trúc và đổi tên Controller chuẩn hóa**:
    - Đổi tên và di chuyển `LockpickDoor.cs` từ `Assets/Scripts/UI/` sang [DoorController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/DoorController.cs) nằm trong thư mục `Assets/Scripts/Items/`.
    - Cập nhật tham chiếu `StartLockpicking(DoorController door)` trong [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs).
    - Đổi tên `Ladder.cs` thành [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs).
  - **Dọn dẹp mã nguồn cũ `Range_Interaction`**:
    - Loại bỏ hoàn toàn các trường và logic tạm tắt `Range_Interaction` trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs) sau khi toàn bộ hệ thống tương tác đã được chuyển sang `PlayerInteraction` và `ItemInfoHUD` 2D.
    - Cập nhật kiểm tra `GetComponent<LadderController>()` trong hàm `DropSelectedItem()`.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/DoorController.cs` (Renamed/Moved)
  - `Assets/Scripts/Items/LadderController.cs` (Renamed)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified)
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
- **Ảnh hưởng**:
  - Cấu trúc thư mục và tên class rõ ràng, chuẩn phong cách kiến trúc Unity.
  - Loại bỏ hoàn toàn lỗi compile `CS0246`.

---

### [2026-09-21 16:05] — feat(door): replace door destruction with smooth child model opening and 3D spatial audio
- **Tác vụ**:
  - **Thay thế hoàn toàn cơ chế phá hủy cửa cũ (`Destroy(gameObject)`)**:
    - Xóa bỏ các cơ chế cũ không cần thiết (`destroyOnUnlock`, `DestroyDoorWithDelay`, `interactRadius`, `CheckPlayerDistance`, `Update` distance check).
    - Tự động tắt toàn bộ `Collider` trên cánh cửa khi mở khóa thành công để Player và NPC có thể đi qua tự nhiên.
    - Bổ sung cơ chế mở cửa xoay mượt mà (`SmoothOpenDoorRoutine` / `openRotationOffset`, `openSpeed`) tác động trực tiếp lên model con (`doorChildModel`).
  - **Chuyển toàn bộ phát âm thanh bẻ khóa từ 2D UI sang 3D Spatial Audio trên Cửa**:
    - Thêm các hàm `PlayHitSound()`, `PlayMissSound()`, `PlayVictorySound()` phát qua `AudioSource` 3D đặt ngay tại vị trí cánh cửa trong không gian.
- **Tác vụ**:
  - Sửa lỗi runtime `Animator.speed can only be negative when Animator recorder is enabled`:
    - Unity Mecanim chặn gán giá trị âm trực tiếp vào thuộc tính toàn cục `Animator.speed` trong chế độ chơi thông thường.
    - Đã cập nhật [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs):
      1. Luôn giữ `Animator.speed >= 0`.
      2. Khi leo xuống: Sử dụng cơ chế điều khiển `climbNormalizedTime` lùi dần và gọi `_animator.Play(0, 0, climbNormalizedTime)` trực tiếp (hoặc set qua Float parameter `ClimbSpeed` nếu có cấu hình trong Animator Controller).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Lines 345-410)
- **Ảnh hưởng**:
  - Triệt tiêu hoàn toàn lỗi console trong Unity khi leo thang xuống, animation tua ngược mượt mà không cần can thiệp offline recording.

---

### [2026-09-21 13:35] — feat(ui, ladder): integrate dedicated mobile InteractBtn and raycast PointA/PointB ladder climbing
- **Tác vụ**:
  - **Tách biệt nút `PickupBtn` và `InteractBtn` trên Mobile**:
    - Nâng cấp [MobileActionButtons.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/MobileActionButtons.cs): Thêm trường `pickupButton` (nhặt item vào túi) và `interactButton` (tương tác đặc biệt: Thang, Cửa, v.v.).
    - Cập nhật [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs):
      - Khi rọi tâm ngắm vào Item Loot (`IsLootItem() == true`): Tự động bật `PickupBtn`, ẩn `InteractBtn`.
      - Khi rọi tâm ngắm vào đối tượng đặc biệt như Thang (`IsLootItem() == false`): Tự động bật `InteractBtn`, ẩn `PickupBtn`.
      - Khi không nhắm vào vật thể nào: Tự động ẩn cả 2 nút.
  - **Tích hợp `IInteractable` và Raycast điểm A / điểm B cho [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs)**:
    - Thêm Trigger Collider cho `PointA` và `PointB` để tâm ngắm Raycast từ cả trên nóc nhà nhìn xuống (điểm B) hay dưới đất nhìn lên (điểm A) đều bắt trúng dễ dàng.
    - Khi bấm `InteractBtn` (hoặc phím `E` / `F`):
      - Đứng ở trên nóc nhà / nhìn gần điểm B -> Gợi ý `"Climb Down"`, tự động bám vào đỉnh thang và leo xuống.
      - Đứng ở dưới đất / nhìn gần điểm A -> Gợi ý `"Climb Up"`, tự động bám vào chân thang và leo lên.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Modified)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Modified)
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
- **Ảnh hưởng**:
  - Giao diện nút bấm trên Mobile hiển thị đúng ngữ cảnh (nút Nhặt cho đồ, nút Tương tác cho Thang/Cửa), xử lý leo thang 2 chiều từ trên cao xuống hoặc từ dưới lên cực kỳ trực quan và nhạy bén.

---

### [2026-09-21 13:42] — fix(ladder): remove auto-climb trigger and enforce explicit Interact button press
- **Tác vụ**:
  - Loại bỏ hoàn toàn cơ chế tự động bám thang khi di chuyển Joystick (`autoClimbOnJoystick` và trigger overlap).
  - Thang giờ đây chỉ bắt đầu leo khi người chơi **chủ động bấm nút `InteractBtn` trên Mobile (hoặc phím `E` / `F` trên PC)**:
    - Rọi tâm ngắm vào thang (hoặc đứng gần chân/đỉnh thang) -> Nút `InteractBtn` sáng lên.
    - Nhấn `InteractBtn` -> Player bám vào thang.
    - Khi đã bám thang -> Dùng Joystick W/S để leo lên/xuống, bấm Space/Jump để nhảy thoát ra sau.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Cleaned up auto-climb logic)
- **Ảnh hưởng**:
  - Tránh việc người chơi vô tình bị hút/dính vào thang khi chỉ đi ngang qua; mọi thao tác leo thang đều được kiểm soát chủ động qua nút bấm.

---

### [2026-09-21 13:52] — feat(ladder): instant climb idle pose on interact and independent Up/Down animation speeds
- **Tác vụ**:
  - **Tư thế bám thang tức thì (Climb Idle Pose)**:
    - Khi bấm `InteractBtn` để bám thang, gọi trực tiếp `_animator.Play("Climb", 0, climbNormalizedTime)` kèm `_animator.Update(0f)` để nhân vật ngay lập tức chuyển sang tư thế tay chân bám chắc trên bậc thang thay vì bị kẹt ở tư thế đứng Idle dưới đất.
  - **Tách biệt 2 biến tốc độ Animation Lên và Xuống**:
    - Thêm `animClimbUpSpeed` (mặc định `1.0`): Tùy chỉnh tốc độ animation khi trèo LÊN.
    - Thêm `animClimbDownSpeed` (mặc định `1.0`): Tùy chỉnh tốc độ animation khi trèo XUỐNG.
    - Cân bằng tốc độ phát animation của cả 2 chiều di chuyển, loại bỏ hệ số chênh lệch cứng.
- **Ảnh hưởng**:
  - Nhân vật vào tư thế bám thang đẹp mắt ngay khi bấm nút, và người dùng có thể thoải mái tinh chỉnh tốc độ animation trèo lên / trèo xuống độc lập theo ý muốn trong Inspector.

---

### [2026-09-21 13:55] — feat(ladder): rail-based movement for inclined ladders, collision ignore, fast slide down, and jump cooldown
- **Tác vụ**:
  - **Khắc phục triệt để lỗi kẹt thang khi đặt thang nghiêng**:
    - Chuyển sang cơ chế di chuyển theo **Ray Toán Học (`climbProgress` từ 0 đến 1)** nội suy giữa `PointA` và `PointB`: Player di chuyển chính xác 100% dọc theo đường ray thang ở bất kỳ góc nghiêng nào (30°, 60°, 75°...) mà không phụ thuộc vào ma sát vật lý.
    - Tự động gọi `Physics.IgnoreCollision(true/false)` giữa Player và toàn bộ Collider của Thang khi đang bám thang -> Triệt tiêu hoàn toàn va chạm gây khựng/kẹt.
  - **Thêm tính năng Tuột thang nhanh (`slideSpeedMultiplier`)**:
    - Khi trèo xuống, nếu giữ phím **Crouch (`Ctrl` / Nút Cúi)** hoặc **Sprint (`Shift` / Nút Chạy)** hoặc kéo mạnh Joystick xuống -> Kích hoạt chế độ **Tuột thang nhanh (Slide Down)** với tốc độ gấp `slideSpeedMultiplier` lần (mặc định x2.2 lần).
  - **Khắc phục lỗi spam nhảy liên tục trên thang**:
    - Thêm biến `reClimbCooldown` (mặc định `0.45s`) ngăn Player bị hút/bám lại thang ngay lập tức sau khi bấm Nhảy rời thang.
    - Xóa cờ `starterInputs.jump` và áp dụng lực đẩy lùi an toàn `pushVector * 0.2f` đưa Player tách xa hẳn khỏi thang.
---

### [2026-09-21 14:24] — fix(ladder, camera): top rung attachment inset and ladder camera yaw clamping
- **Tác vụ**:
  - **Khắc phục lỗi bám vào hư không khi leo xuống từ đỉnh thang**:
    - Thêm `topAttachProgress` (mặc định `0.90`) và `bottomAttachProgress` (mặc định `0.05`) vào [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs).
    - Khi bấm tương tác để leo xuống từ đỉnh thang, nhân vật sẽ được đặt trực tiếp vào vị trí bậc thang phía dưới đỉnh (thay vì vị trí 1.0 trên đỉnh mép tường), giúp tay chân bám chắc chắn vào rungs của thang mà không bị trôi lơ lửng giữa không trung.
  - **Đồng bộ hướng nhìn và giới hạn góc quay Camera khi leo thang**:
    - Thêm `SetLadderCameraFacing(targetYaw)` và `ladderYawClamp` (mặc định `70°`) vào [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs).
    - Khi bám vào thang, tự động xoay mặt nhân vật và camera hướng thẳng vào mặt thang.
    - Trong suốt quá trình đang leo (`isClimbingLadder == true`), khóa không cho xoay camera tự do 360° ra sau lưng nhân vật, giới hạn góc quay trái/phải trong phạm vi an toàn ±70°.
    - Khi nhảy rời thang hoặc leo xong tới đỉnh/chân thang, khôi phục góc nhìn 360° tự do bình thường.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
- **Ảnh hưởng**:
  - Tư thế bám thang chân thực hơn, giải quyết triệt để lỗi góc nhìn 360° bất thường và lỗi bám mép không trung khi trèo xuống.

---

### [2026-09-21 14:35] — feat(ladder): fix top exit step direction and add canClimb permission toggle
- **Tác vụ**:
  - **Sửa hướng bước ra ở đỉnh thang (`ExitTopLadder`)**:
    - Điều chỉnh hướng bước ra ở đỉnh thang theo hướng mặt trước mà Player đang nhìn (`playerController.transform.forward` / `-transform.forward`), đảm bảo khi leo lên đến đỉnh thang nhân vật sẽ bước thẳng về phía trước lên sàn/mái nhà thay vì bị bước ngược ra sau lưng.
    - Cập nhật tia Gizmos màu vàng ở đỉnh thang chỉ đúng hướng bước lên sàn.
  - **Bổ sung cờ quyền leo thang `canClimb` (Hỗ trợ Online / Debuff / Trạng thái)**:
    - Thêm `canClimb` vào [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs) và [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs).
    - Nếu `canClimb == false`, ngăn không cho tương tác hoặc bắt đầu leo thang.
    - Nếu `canClimb` bị chuyển sang `false` trong khi Player đang bám trên thang (do bị stun, bị mất quyền leo, hoặc server online ngắt), Player sẽ tự động kích hoạt `FallOffLadder()` buông tay rơi tự do xuống dưới với trọng lực tự nhiên.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
- **Ảnh hưởng**:
  - Khắc phục triệt để lỗi bước ra ngược ở đỉnh thang và sẵn sàng tích hợp logic multiplayer / trạng thái gameplay.

---

### [2026-09-21 14:40] — fix(hotbar, ladder): auto hide held 3D item model while climbing ladder
- **Tác vụ**:
  - **Tự động cất/ẩn model item đang cầm khi leo thang**:
    - Khi bắt đầu leo thang (`StartClimbing`), gọi `playerController.hotbarManager.DeselectAll()` và ẩn tất cả model trong `heldItem`.
    - Ẩn model 3D vật phẩm trước mặt để 2 tay nhân vật rảnh bám vào thang leo tự nhiên.
    - Vật phẩm vẫn được lưu giữ an toàn tuyệt đối trong các ô Hotbar / túi đồ của Player, không bị rơi hay mất đi.
  - **Khóa thao tác chọn/cầm item trên Hotbar khi đang bám thang**:
    - Cập nhật [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Khi `playerController.isClimbingLadder == true`, bỏ qua các thao tác click slot, phím số 1-9 và phím Drop Q/G.
    - Sau khi leo xong hoặc nhảy rời thang, Player có thể thoải mái bấm chọn lại bất kỳ ô item nào trên Hotbar để cầm lại đồ.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/Ladder.cs` (Modified)
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
- **Ảnh hưởng**:
  - Loại bỏ hoàn toàn hiện tượng model đồ vật bị cầm lơ lửng trước mặt che khuất tầm nhìn và bậc thang khi đang leo.

---

### [2026-09-21 14:52] — feat(interaction): support dual Pickup and Climb buttons for objects having both Item and Ladder
- **Tác vụ**:
  - **Tách biệt và hỗ trợ đa tương tác (Multi-Interactable) trên cùng một GameObject**:
    - Nâng cấp [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs) quét và nhận diện đồng thời cả component `Item` (Loot Item) và `Ladder` (hoặc các tương tác đặc biệt khác).
    - **Khi đối tượng có cả `Item` và `Ladder` (như Thang xách tay)**:
      - **Trên Mobile**: Tự động bật **CẢ 2 NÚT** (`Pickup` để nhặt và `Interact` để leo thang).
      - **Trên PC**:
        - Phím **`E`** (hoặc nút `Pickup`): Nhặt Thang vào Hotbar/Túi đồ (thực thi qua `Item.cs`).
        - Phím **`F`** (hoặc nút `Interact`): Bám vào Thang để Leo (thực thi qua `Ladder.cs`).
  - **Giữ nguyên thiết kế gốc chuẩn Clean Architecture**:
    - [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs) thuần túy phụ trách logic leo thang (`IsLootItem() = false`).
---

### [2026-09-21 14:56] — fix(hotbar, ladder): align drop rotation with player view and dynamic approach side climbing
- **Tác vụ**:
  - **Sửa góc xoay khi thả (Drop) Thang / Item theo hướng nhìn của Player**:
    - Trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs), thay thế `Quaternion.identity` bằng `Quaternion.LookRotation(forwardDir)` dựa theo góc nhìn hiện tại của người chơi.
    - Bất kể người chơi quay mặt về hướng nào (Bắc, Nam, Đông, Tây) khi bấm thả đồ, Thang sẽ luôn được xoay thẳng mặt theo góc nhìn của người chơi.
    - Giảm lực đẩy xung lượng khi thả Thang để Thang đứng vững trên mặt sàn, tránh bị văng lật ngã.
  - **Khắc phục lỗi người chơi trèo bị bay sang mặt đối diện (`Ladder.cs`)**:
    - Trong [Ladder.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Ladder.cs), tính toán vector hướng tiếp cận giữa Player và Thang.
    - Tự động phát hiện người chơi đang đứng ở mặt trước hay mặt sau của thang để xoay mặt người chơi và góc camera hướng thẳng vào mặt thang phía tiếp cận (thay vì luôn cố định cứng 1 hướng duy nhất làm người chơi bị lật 180° sang phía bên kia).
---

### [2026-09-21 15:01] — fix(interaction, ladder): disable pickup and auto-stop climbing on ladder disable to prevent floating in midair
- **Tác vụ**:
  - **Tự động ẩn nút Pickup và chặn nhặt đồ khi đang leo thang**:
    - Trong [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs), kiểm tra `playerController.isClimbingLadder`: Tắt toàn bộ HUD, nút `Pickup` và nút `Interact`, đồng thời chặn nhận phím `E` / `F`.
    - Trong [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs), thêm kiểm tra `isClimbingLadder` vào `CanInteract()` để từ chối mọi yêu cầu nhặt đồ khi người chơi đang bám trên thang.
    - Cập nhật trong `LateUpdate()` của [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Vị trí cầm item di chuyển lên/xuống mượt mà theo góc nhìn Camera, nhưng góc xoay (Rotation) của vật phẩm luôn được giữ cố định trục Y hướng thẳng đứng (`Vector3.up`).
    - Khi người chơi ngước camera lên trời hoặc cúi xuống đất, item chỉ di chuyển vị trí theo tầm mắt mà không bị chúi/nghiêng theo camera, luôn giữ dáng đứng thẳng.
  - **Khôi phục vật lý tự nhiên bình thường khi Thả/Ném (Drop)**:
    - Khi thả vật phẩm ra thế giới (`DropSelectedItem`), đặt `rb.constraints = RigidbodyConstraints.None`.
    - Cho phép vật thể tự do tương tác vật lý (rơi, va chạm, lăn, ngã tự nhiên) theo trọng lực và lực ném thông thường thay vì bị khóa cứng góc xoay.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
- **Ảnh hưởng**:
  - Item trên tay hiển thị trực quan và thẳng đứng khi xoay camera ngước lên/xuống.

---

### [2026-09-21 15:50] — refactor(items, ui): rename to DoorController and LadderController, and cleanup obsolete Range_Interaction
- **Tác vụ**:
  - **Tái cấu trúc và đổi tên Controller chuẩn hóa**:
    - Đổi tên và di chuyển `LockpickDoor.cs` từ `Assets/Scripts/UI/` sang [DoorController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/DoorController.cs) nằm trong thư mục `Assets/Scripts/Items/`.
    - Cập nhật tham chiếu `StartLockpicking(DoorController door)` trong [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs).
    - Đổi tên `Ladder.cs` thành [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs).
  - **Dọn dẹp mã nguồn cũ `Range_Interaction`**:
    - Loại bỏ hoàn toàn các trường và logic tạm tắt `Range_Interaction` trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs) sau khi toàn bộ hệ thống tương tác đã được chuyển sang `PlayerInteraction` và `ItemInfoHUD` 2D.
    - Cập nhật kiểm tra `GetComponent<LadderController>()` trong hàm `DropSelectedItem()`.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/DoorController.cs` (Renamed/Moved)
  - `Assets/Scripts/Items/LadderController.cs` (Renamed)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified)
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
- **Ảnh hưởng**:
  - Cấu trúc thư mục và tên class rõ ràng, chuẩn phong cách kiến trúc Unity.
  - Loại bỏ hoàn toàn lỗi compile `CS0246`.

---

### [2026-09-21 16:05] — feat(door): replace door destruction with smooth child model opening and 3D spatial audio
- **Tác vụ**:
  - **Thay thế hoàn toàn cơ chế phá hủy cửa cũ (`Destroy(gameObject)`)**:
    - Xóa bỏ các cơ chế cũ không cần thiết (`destroyOnUnlock`, `DestroyDoorWithDelay`, `interactRadius`, `CheckPlayerDistance`, `Update` distance check).
    - Tự động tắt toàn bộ `Collider` trên cánh cửa khi mở khóa thành công để Player và NPC có thể đi qua tự nhiên.
    - Bổ sung cơ chế mở cửa xoay mượt mà (`SmoothOpenDoorRoutine` / `openRotationOffset`, `openSpeed`) tác động trực tiếp lên model con (`doorChildModel`).
  - **Chuyển toàn bộ phát âm thanh bẻ khóa từ 2D UI sang 3D Spatial Audio trên Cửa**:
    - Thêm các hàm `PlayHitSound()`, `PlayMissSound()`, `PlayVictorySound()` phát qua `AudioSource` 3D đặt ngay tại vị trí cánh cửa trong không gian.
    - Cập nhật [LockpickMinigame.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/LockpickMinigame.cs) và [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs) để kích hoạt âm thanh 3D trên `DoorController` đang tương tác.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Items/DoorController.cs` (Modified)
  - `Assets/Scripts/UI/LockpickMinigame.cs` (Modified)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified)
- **Ảnh hưởng**:
  - Cánh cửa giữ nguyên trong scene, xoay mở chân thực thay vì biến mất đột ngột.
  - Trải nghiệm âm thanh vòm 3D chân thực, phát ra đúng từ vị trí ổ khóa cửa.

---

### [2026-09-21 16:30] — fix(interaction, ui): raycast surface distance, modularize MainHUD and PauseHUD with nested Settings
- **Tác vụ**:
  - **Sửa lỗi khoảng cách tương tác trên Cửa và các vật thể lớn ([PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs))**:
    - **Nguyên nhân**: Trước đây tính khoảng cách tới `transform.position` (tọa độ Pivot gốc của cánh cửa - thường nằm ở góc bản lề/khung cửa), khiến người chơi phải đứng áp sát vào bản lề mới hiện nút tương tác.
    - **Khắc phục**: Chuyển sang tính khoảng cách từ Player đến **điểm tiếp xúc bề mặt thực tế (`hit.point` / `ClosestPoint`)** của tia Raycast trên cánh cửa.
    - Giúp người chơi có thể đứng từ khoảng cách nhặt đồ bình thường (`maxPlayerReach = 3.5m`) và rọi tia Ray vào bất kỳ vị trí nào trên cánh cửa để bắt đầu bẻ khóa ngay lập tức.
  - **Tách module MainHUD độc lập ([MainHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/MainHUD.cs) & [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs))**:
    - Tạo mới script `MainHUD.cs` chuyên trách hiển thị chỉ số trong trận: Thể lực (`Stamina`), Cân nặng (`Kg`), Điểm (`Point`), Thời gian đếm ngược (`Time_T`), Âm thanh cảnh báo hết giờ (`Alarm`) và nút Tạm dừng (`PauseBtn`).
    - Hỗ trợ cơ chế `AutoFindUIElements()` tự động quét và kết nối các thành phần UI con trong cây phân cấp của `mainHUD`.
  - **Tách module PauseHUD độc lập ([PauseHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/PauseHUD.cs) & [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs))**:
    - Tạo mới script `PauseHUD.cs` quản lý menu tạm dừng trong trận: Nút Resume, Restart/Replay, Settings, Menu.
    - Tự động quét và liên kết các Button con trong hierarchy (`AutoFindUIElements`).
    - Hỗ trợ đóng mở lồng bảng `SettingsHUD`: Khi bấm nút Settings trên Pause menu -> mở SettingsHUD và ẩn PauseHUD; khi bấm Close trên SettingsHUD -> tự động đóng SettingsHUD và khôi phục lại PauseHUD.
    - Bổ sung các phương thức `OpenSettings()` và `CloseSettings()` trong [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs).
  - **Sửa lỗi tuần tự hóa ChapterSO và Overrides Prefab ([ChapterSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ChapterSO.cs) & [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs))**:
    - **ChapterSO**: Loại bỏ hoàn toàn trường Player spawn khỏi `ChapterSO` để chuẩn bị cho kiến trúc Online/Multiplayer (tránh trùng điểm spawn nhiều người chơi và phân định rõ metadata vs scene spawn). Chỉ giữ lại thiết lập spawn vật phẩm (`itemSpawnPositions`, `spawnableItems`).
    - **Khắc phục lỗi không Override/Apply All được Prefab Player**: Các trường UI (`dynamicJoystick`, `touchLookZone`, `mobileActions`, `itemInfoHUD`) được cấu hình tự động tìm kiếm đối tượng trong Scene lúc runtime (`FindFirstObjectByType`), giúp Prefab Asset không bị gán cứng Scene References bên ngoài, cho phép Apply All / Overrides Prefab thoải mái mà không bị Unity chặn.
  - **Tách kiến trúc Dữ liệu tĩnh (PlayerSO) và Trạng thái động runtime (PlayerStats)**:
    - **Tạo mới [PlayerStats.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerStats.cs)**: MonoBehaviour gắn trên Player GameObject quản lý toàn bộ trạng thái thay đổi liên tục trong trận:
      - `currentWeight` / `maxWeight` và tự động tính toán giảm tốc độ tuyến tính (`CalculateWeightSpeedPenalty`).
      - `currentStamina` / `maxStamina`, tiêu hao khi chạy nhanh (`sprint`), hồi phục sau `staminaRegenCooldown` (`HandleStamina`).
      - Điểm số (`currPoint` / `totalPoint`), thời gian đếm ngược (`currentTime`), trạng thái tử vong (`isDied`).
      - Cung cấp đầy đủ các thuộc tính tương thích ngược (`currweight`, `_stamina`, `_MoveSpeed`, `_SprintSpeed`, `isDied`, `currpoint`...) để không gây lỗi biên dịch ở bất kỳ script liên quan nào.
    - **Tối ưu hóa [PlayerSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerSO.cs)**: Chỉ chứa cấu hình gốc và hồ sơ nhân vật tĩnh (`characterId`, `characterName`, `avatar`, `description`, `baseMoveSpeed`, `baseSprintSpeed`, `baseCrouchSpeed`, `baseJumpHeight`, `baseMaxStamina`, `staminaDepletionRate`, `staminaRegenRate`, `staminaRegenCooldown`, `baseMaxWeight`).
    - **Cập nhật [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs)**: Nhận `playerData` (PlayerSO) và `stats` (PlayerStats). Tự động khởi tạo `PlayerStats` trong `Awake()` và ủy quyền tính toán Stamina, Weight sang `PlayerStats`.
    - **Tích hợp cơ chế FPS vào hệ thống Settings và xóa bỏ GameSettings.cs**:
      - Chuyển toàn bộ cơ chế quản lý FPS (`targetFrameRate = 60`, `vSyncCount = 0`, `ResizeBuffers`, bộ đếm FPS OnGUI góc màn hình và log console) từ `GameSettings.cs` sang **[SettingsManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/SettingsManager.cs)** (Persistent Singleton).
      - Định dạng FPS hiển thị dạng chữ trắng cơ bản (không đổi màu phức tạp), cập nhật cố định chính xác 1 giây 1 lần (`fpsDisplayText`).
      - Bổ sung trường `targetFPS`, `showFPSOnScreen`, `vSync`, `renderScale` vào `SettingsData` được tự động lưu và load từ `game_settings.json`.
      - Bổ sung hỗ trợ UI (Toggle bật/tắt FPS on-screen, Dropdown chọn mức FPS 30/45/60/90/120) vào **[SettingsHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/SettingsHUD.cs)** kèm âm thanh click khi tương tác Toggle và Dropdown.
    - **Xây dựng hệ thống GameSession truyền dữ liệu Chapter và Character xuyên Scene**:
      - Tạo mới **[GameSession.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/GameSession.cs)** (static session holder) lưu trữ `SelectedChapter`, `NextChapter`, `SelectedPlayer`.
      - Cập nhật **[ChapterSelectManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ChapterSelectManager.cs)**: Khi bấm Play $\rightarrow$ tự động nạp Chapter được chọn và `defaultPlayerData` (tạm thời hardcode khi chưa có UI chọn nhân vật) vào `GameSession`.
      - Cập nhật **[PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs)**: Trong `Awake()` $\rightarrow$ tự động nhận `GameSession.SelectedPlayer` để nạp chỉ số vào `PlayerStats`.
      - Cập nhật **[UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs)**: Trong `Start()` $\rightarrow$ tự động nhận `GameSession.SelectedChapter` và `GameSession.NextChapter` để nạp vào game session; tự động `ClearSession()` khi quay về Menu.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerSO.cs` (New / Refactored)
  - `Assets/Scripts/Player/PlayerStats.cs` (New)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Modified)
  - `Assets/Scripts/Player/StarterAssetsInputs.cs` (Modified)
  - `Assets/Scripts/Player/PlayerDeathHandler.cs` (Modified)
  - `Assets/Scripts/Player/Flashlight.cs` (Modified)
  - `Assets/Scripts/AI/AI_Move.cs` (Modified)
  - `Assets/Scripts/Items/Item.cs` (Modified)
  - `Assets/Scripts/UI/ChapterSO.cs` (Modified)
  - `Assets/Scripts/UI/MainHUD.cs` (New)
  - `Assets/Scripts/UI/PauseHUD.cs` (New)
  - `Assets/Scripts/UI/SettingsHUD.cs` (Renamed/Refactored)
  - `Assets/Scripts/UI/HomeScreen.cs` (Modified)
    - **Tạo mới hệ thống SceneItemSpawner hỗ trợ danh sách vị trí trong Scene và kiểm tra Rarity**:
      - Tạo mới **[SceneItemSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/SceneItemSpawner.cs)**: MonoBehaviour gắn trực tiếp trong Scene gameplay, quản lý toàn bộ các điểm spawn vật phẩm (`spawnPoints`, `additionalPositions`).
      - Tự động liên kết `ChapterSO` (từ `GameSession.SelectedChapter` hoặc cấu hình Inspector).
      - Tích hợp thuật toán chọn đồ theo xác suất trọng số độ hiếm (**Weighted Rarity Random**): `Trash` (20%), `Common` (45%), `Uncommon` (20%), `Rare` (10%), `Epic` (4%), `Legendary` (1%), `Mythic` (0.2%).
      - Tự động phân loại `spawnableItems` của Chapter theo độ hiếm và khởi tạo chỉ số ngẫu nhiên (`item.InitializeStats()`).
      - Hỗ trợ Gizmos trực quan và Context Menu Editor tiện lợi (`Auto Collect Child Spawn Points`, `Spawn All Items Now`, `Clear Spawned Items`).
    - **Tối ưu cơ chế rớt đồ khi Player chết (Cách 1)**:
      - Đơn giản hóa [PlayerController.DropItemsOnDeath()](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs): khi người chơi tử vong, toàn bộ vật phẩm đang cầm sẽ rơi rải rác xung quanh vị trí thi thể với hiệu ứng vật lý tự nhiên (impulse force & torque) thay vì quay về các điểm spawn trong Scene.
    - **Sửa lỗi nút Crouch trên Mobile và chuẩn hóa luồng Input Cúi người**:
      - Bổ sung `crouch` input và sự kiện `OnCrouch` vào [StarterAssetsInputs.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/StarterAssetsInputs.cs).
      - Tự động liên kết `mobileActions` trong `Start()` của [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) tránh bị `null` khi load nhân vật từ Prefab.
    - **Tách hệ thống Item, Inventory, Nhặt đồ, Thả đồ và Tải trọng sang [PlayerInventory.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInventory.cs)**:
      - Tạo mới component `PlayerInventory.cs` chuyên trách quản lý: túi đồ `heldItems`, nhặt đồ `TryPickupItem`, trạng thái cầm đồ `isTaking` / `UpdateHoldingState`, thả đồ thủ công `DropLastItem`, rớt đồ khi tử vong `DropAllItemsOnDeath`, và tính toán tải trọng balo `UpdateWeight`.
      - Tinh gọn [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) (giảm ~200 dòng code dư thừa), chỉ tập trung vào Controller Movement, Jump, Crouch, Ladder, Footstep.
    - **Tạo mới hệ thống [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs)**:
      - Quản lý danh sách các điểm xuất phát (`spawnPoints`) trong Scene gameplay.
      - Bổ sung trường `characterPrefab` vào [PlayerSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerSO.cs).
      - Tự động lấy nhân vật từ `GameSession.SelectedPlayer` (hoặc `defaultPlayerData`) để sinh đúng Prefab nhân vật lúc màn chơi bắt đầu (`Start()`).
      - Tự động kết nối `CinemachineVirtualCamera.Follow` / `LookAt` và `UI_Manager.playerStats` vào nhân vật vừa sinh.
      - Hỗ trợ Gizmos màu xanh lá trực quan và Context Menu Editor (`Auto Collect Child Spawn Points`, `Spawn Player Now`).
      - Bổ sung cơ chế tự động tìm và bổ sung `AudioListener` vào `Camera.main` trong [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs) để ngăn chặn hoàn toàn lỗi thiếu AudioListener khi chuyển Scene.
      - Tự động kích hoạt Global Volume, Post Processing trên URP Camera và liên kết PlayerController trong [PostProcess.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/PostProcess.cs) và [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs).
      - Cải tiến [LockpickMinigame.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/LockpickMinigame.cs): Chỉ cho phép bẻ khóa khi bấm đúng `lockpickButton` (Mobile) hoặc phím Space/E (PC), loại bỏ hoàn toàn việc click/chạm bừa trên màn hình; bổ sung `interactionCooldown` ngăn chặn hiện tượng bấm nút Close bị xuyên thấu mở lại Minigame.
      - Tối ưu hiển thị FPS trong [SettingsManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/SettingsManager.cs): Chỉ hiển thị số nguyên, giảm 1 nửa kích thước font và chuyển xuống góc dưới màn hình.
      - Tối ưu vật lý Thang trong [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs), [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs) và [PlayerInventory.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInventory.cs): Khóa trục nghiêng `FreezeRotationX | FreezeRotationZ`, đảm bảo Thang luôn luôn đứng thẳng 100% khi đặt/thả ra thế giới mà không bao giờ bị đổ ngã.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerSO.cs` (New / Refactored)
  - `Assets/Scripts/Player/PlayerStats.cs` (New)
  - `Assets/Scripts/Player/PlayerInventory.cs` (New)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (New)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Modified)
  - `Assets/Scripts/Player/StarterAssetsInputs.cs` (Modified)
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Modified)
  - `Assets/Scripts/Player/PlayerDeathHandler.cs` (Modified)
  - `Assets/Scripts/Player/Flashlight.cs` (Modified)
  - `Assets/Scripts/AI/AI_Move.cs` (Modified)
  - `Assets/Scripts/Items/Item.cs` (Modified)
  - `Assets/Scripts/Items/LadderController.cs` (Modified)
  - `Assets/Scripts/Items/SceneItemSpawner.cs` (New)
  - `Assets/Scripts/Items/ItemSpawner.cs` (Deleted)
  - `Assets/Scripts/Items/ItemLibrary.cs` (Deleted)
  - `Assets/Scripts/UI/ChapterSO.cs` (Modified)
  - `Assets/Scripts/UI/LockpickMinigame.cs` (Modified)
  - `Assets/Scripts/UI/MainHUD.cs` (New)
  - `Assets/Scripts/UI/PauseHUD.cs` (New)
  - `Assets/Scripts/UI/SettingsHUD.cs` (Renamed/Refactored)
  - `Assets/Scripts/UI/HomeScreen.cs` (Modified)
  - `Assets/Scripts/UI/ChapterSelectManager.cs` (Modified)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified)
  - `Assets/Scripts/Cutscenes/CutsceneManager.cs` (Modified)
  - `Assets/Scripts/Utilities/GameSession.cs` (New)
  - `Assets/Scripts/Utilities/PostProcess.cs` (Modified)
  - `Assets/Scripts/Utilities/SettingsManager.cs` (Modified)
  - `Assets/Scripts/Utilities/GameSettings.cs` (Deleted)
- **Ảnh hưởng**:
  - Dữ liệu Chapter và Character được truyền tự động, mượt mà từ HomeMenu $\rightarrow$ Scene Cutscene/Intro $\rightarrow$ Scene Gameplay mà không bị mất dữ liệu giữa chừng.
  - Loại bỏ các script thừa thãi (`ItemLibrary`, `ItemSpawner`), thay thế hoàn toàn bằng `SceneItemSpawner` chuẩn hóa theo Rarity và ChapterSO.
  - Thống nhất toàn bộ thiết lập Game và phiên chơi ổn định xuyên suốt mọi Scene.
  - Nút Crouch trên Mobile và bàn phím (C / Ctrl) hoạt động mượt mà, chuyển đổi tư thế và hạ thấp camera chính xác.
  - Cấu trúc Player chuẩn Single Responsibility Principle: tách biệt hoàn toàn giữa Điều khiển di chuyển (PlayerController), Chỉ số trạng thái (PlayerStats) và Quản lý vật phẩm balo (PlayerInventory).
  - Khởi tạo và sinh nhân vật động linh hoạt thông qua `ScenePlayerSpawner`, sẵn sàng cho cấu trúc Multiplayer / PUN2 về sau.

---

### [2026-09-22 08:28] — feat(ui, input): enable PC mouse clicks, persistent unlocked cursor, and on-screen button testing
- **Tác vụ**:
  - Tạo mới [UIEventSystemFixer.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UIEventSystemFixer.cs) tự động đảm bảo EventSystem có đầy đủ Input Module hỗ trợ cả New Input System và Old Input Manager, tự động mở khóa chuột và kiểm tra GraphicRaycaster trên Canvas giúp click chuột trực tiếp trong Game View / Standalone EXE 100% không cần bật Device Simulator.
  - Tích hợp mở khóa con trỏ chuột xuyên suốt toàn bộ game (`Cursor.lockState = CursorLockMode.None`, `Cursor.visible = true`) trong `Awake()`, `Start()`, `OnEnable()` của [HomeScreen.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HomeScreen.cs), [ChapterSelectManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ChapterSelectManager.cs), [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) và [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs).
  - Tái cấu trúc [StarterAssetsInputs.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/StarterAssetsInputs.cs):
    - Giữ nguyên hiển thị toàn bộ các nút ảo/giao diện trên màn hình để tester có thể dùng chuột click trực tiếp vào nút như thao tác cảm ứng trên Mobile.
    - Hỗ trợ di chuyển bằng bàn phím `WASD` / Mũi tên và nhảy bằng phím `Space` song song với Joystick và nút Jump trên màn hình.
    - Khắc phục lỗi Joystick và TouchZone ghi đè Vector2.zero mỗi frame khi không chạm.
    - Hỗ trợ phím `Escape` / `P` để mở menu Pause trong trận.
  - Cập nhật [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Hỗ trợ lăn chuột (`Mouse ScrollWheel`) để chuyển đổi qua lại giữa các ô Hotbar trên PC.
  - Cập nhật [LockpickMinigame.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/LockpickMinigame.cs):
    - Khắc phục lỗi bấm trúng quá dễ do kiểm tra va chạm mép ngoài (`BoundingBox` mép-với-mép của 2 hình tròn).
    - Thay thế bằng thuật toán đo khoảng cách tâm (`hitPrecision = 0.65f`), yêu cầu vòng tròn Indicator phải lồng sâu vào trong vùng Target mới tính là trúng đích, chạm nhẹ viền ngoài sẽ tính là trượt.
    - Hỗ trợ thêm phím `F` bên cạnh `Space` và `E` để bẻ khóa trên PC.
  - Tối ưu và tinh gọn [MainHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/MainHUD.cs):
    - Loại bỏ hoàn toàn các trường `maxStaminaText`, `maxKgText`, `targetPointText` dư thừa.
    - Tinh gọn Inspector chỉ còn 3 trường text chính (`currStamina`, `currKg`, `currPointText`) tự động hiển thị chuỗi gộp `HiệnTại/TốiĐa` (`10/10`, `0/100Kg`, `0/400`).
  - Cải tiến cơ chế Đặt (Place) / Ném (Throw) và vật lý Thang trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs) & [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs):
    - Khi cầm item rọi Raycast vào tường/mặt phẳng: Tự động chuyển icon nút bấm sang **`placeIcon`**, xoay Z **180 độ** (`placeRotationZ`) và bật hiện GameObject UI **`placementIndicator`** trên màn hình (`SetActive(true)`).
    - Khi rọi vào không gian thoáng: Tự động chuyển icon nút bấm sang **`throwIcon`**, xoay Z **90 độ** (`throwRotationZ`) và tự động ẩn GameObject UI **`placementIndicator`** (`SetActive(false)`).
    - Đối với Thang (`LadderController`):
      - Khi **Đặt** (Raycast chạm tường/sàn): Khóa hoàn toàn trục `FreezeRotationX | FreezeRotationZ`, không áp lực đẩy để thang đứng vững vàng 100% không bị ngã đổ.
      - Khi **Ném** (Raycast không chạm vật cản): Mở khóa toàn bộ `RigidbodyConstraints.None`, áp dụng lực ném `dropForwardForce` về phía trước giúp thang lật xoay và đổ ngã vật lý tự nhiên.
      - Khóa hoàn toàn va chạm vật lý giữa Player và Thang (`Physics.IgnoreCollision`) giúp người chơi không thể đi/chạy bộ lên thang nghiêng như dốc cầu thang, buộc phải bấm phím leo (`F` / `Interact`) mới có thể trèo lên cao.
      - Hỗ trợ đầy đủ đa tương tác trên cùng một đối tượng (Multi-IInteractable): Khi nhìn vào Thang vừa hiển thị nút **Pickup** (nhặt thang vào Hotbar) vừa hiển thị nút **Interact** (leo thang), khắc phục lỗi thang khi đặt (`Place`) bị mất nút leo.
  - Sửa lỗi tương tác Cửa (Door) và Minigame trong [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs) & [LockpickMinigame.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/LockpickMinigame.cs):
    - Khắc phục lỗi nhìn vào tường nhà vẫn bắt trúng Cửa: Loại bỏ hàm tìm con `GetComponentsInChildren` nguy hiểm trên Collider va chạm (trước đây bắn trúng tường nhà `House` sẽ quét xuống tất cả cửa con bên trong).
    - Bổ sung cơ chế kiểm tra tầm nhìn (Line-of-Sight occlusion check): Nếu có tường/vật cản che chắn trực tiếp trước mặt, tia quét sẽ bị chặn lại và không thể tương tác xuyên thấu qua tường.
  - Phát triển hệ thống tương tác vật phẩm cầm tay (Held Item Interaction) và [FlashlightController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/FlashlightController.cs):
    - Tạo mới interface [IHeldInteractable.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/IHeldInteractable.cs) chuẩn hóa cho mọi vật phẩm có thể tương tác trực tiếp khi đang cầm trên tay (Đèn pin, Thuốc, Súng, Scanner...).
    - Tái cấu trúc [FlashlightController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/FlashlightController.cs) & [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs):
      - Đồng bộ hóa 100% việc lấy dữ liệu (Tên, Mô tả, Icon, Giá tiền, Cân nặng, Độ hiếm) từ component `Item` và ScriptableObject (`ItemSO`).
      - Bổ sung `lightLocalOffset` và `lightLocalRotation` cho Đèn pin: Dễ dàng vi chỉnh vị trí và góc xoay hướng chiếu sáng.
    - Cập nhật [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs) & [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
      - Hỗ trợ `followCameraPitch`, `customHoldOffset`, `customHoldRotation` riêng cho từng Item.
      - Đối với Đèn pin (`followCameraPitch = true`): Góc xoay ngửa lên / cúi xuống bám sát 100% theo hướng nhìn Camera.
    - Cập nhật [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs):
      - Dùng chung 1 nút `Interact` duy nhất (phím F / nút Interact trên Mobile) theo cơ chế chuyển đổi ngữ cảnh thông minh: ưu tiên mở cửa / leo thang khi nhìn vào vật thể, và bật/tắt vật phẩm cầm tay khi không nhìn vào vật tương tác.
    - Cập nhật [ItemInfoHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ItemInfoHUD.cs) & [MobileActionButtons.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/MobileActionButtons.cs):
      - Phân tách riêng biệt giữa **Tên đối tượng (`nameText`)** và **Hành động (`actionPromptText`)**.
      - `MobileActionButtons.SetInteractPrompt()` tự động quét tìm child object `Prompt` để gán chính xác câu lệnh hành động (`"Turn On"`, `"Turn Off"`, `"Climb Up"`, `"Pick Lock"`) lên nút Mobile UI.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/UIEventSystemFixer.cs` (New)
  - `Assets/Scripts/Items/IHeldInteractable.cs` (New)
  - `Assets/Scripts/Items/FlashlightController.cs` (New)
  - `Assets/Scripts/Environment/DoorController.cs` (Moved & Enhanced)
  - `Assets/Scripts/Items/Item.cs` (Lines 15-35)
  - `Assets/Scripts/UI/HomeScreen.cs` (Lines 20-35)
  - `Assets/Scripts/UI/ChapterSelectManager.cs` (Lines 40-55)
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 200-210)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Lines 30-320)
  - `Assets/Scripts/UI/UI_Manager.cs` (Lines 290-415, 440-445)
  - `Assets/Scripts/Player/StarterAssetsInputs.cs` (Rewritten / Enhanced)
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 50-310, 580-605, 760-815)
  - `Assets/Scripts/Items/LadderController.cs` (Lines 90-145, 595-605)
  - `Assets/Scripts/UI/LockpickMinigame.cs` (Lines 45-55, 225-305)
  - `Assets/Scripts/UI/MainHUD.cs` (Lines 15-200)
  - `Assets/Scripts/UI/ItemInfoHUD.cs` (Lines 200-220)
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Lines 180-210)
- **Ảnh hưởng**:
  - Chuột luôn luôn hiển thị và không bị khóa trong suốt toàn bộ quá trình chơi game từ Menu đến Gameplay.
  - Tester có thể vừa dùng WASD + Space để di chuyển/nhảy, vừa dùng chuột click trực tiếp vào mọi nút trên màn hình y hệt như thao tác chạm trên điện thoại, giúp test trực tiếp trên PC Editor cực nhanh.
  - Minigame bẻ khóa có độ chính xác và thử thách chuẩn hơn, tránh bấm ăn may mép ngoài.
  - Giao diện HUD chính hiển thị trực quan và tinh gọn.
  - Thao tác đặt/ném đồ phân biệt rõ ràng giữa hành vi Đặt vào tường (khóa đứng) và Ném ra khoảng không (vật lý rơi đổ).
  - Đèn pin hoạt động như một Special Item hoàn chỉnh: chùm sáng bám theo góc nhìn camera, bật/tắt bằng phím F / nút Interact mà không xung đột với Thang và Cửa.

---

### [2026-09-22 14:10] — feat(audio, settings): implement dual AudioMixer, 3D spatial sounds, and settings persistence
- **Tác vụ**:
  - **Xây dựng hệ thống AudioMixer 2 kênh độc lập (BGM & SFX)**:
    - Tạo và cấu hình `MainMixer.mixer` với 2 nhóm con `BGM` (nhạc nền) và `SFX` (hiệu ứng âm thanh) dưới Master Group.
    - Expose 2 tham số thể tích chuẩn: `BGMVolume` và `SFXVolume`.
    - Cập nhật [SettingsManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/SettingsManager.cs): Quản lý `bgmVolume` và `sfxVolume` theo thang đo Logarithmic Decibel chuẩn Unity (`(vol <= 0.0001f) ? -80f : Mathf.Log10(vol) * 20f`) và lưu trữ tự động vào `game_settings.json`.
    - Cập nhật [SettingsHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/SettingsHUD.cs): Tự động tìm kiếm Slider BGM và SFX, hiển thị phần trăm (0% -> 100%), hỗ trợ nghe thử thời gian thực khi kéo thanh trượt và hoàn tác (revert) nếu đóng bảng mà chưa bấm Lưu.
  - **Chuẩn hóa 3D Spatial Audio cho toàn bộ Entity & Items**:
    - [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs): Tự động khởi tạo `AudioSource` 3D, phát loop `climbSound` khi di chuyển, tạm dừng khi đứng im và reset về 0 khi thoát thang; định tuyến ra `sfxGroup`.
    - [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs): Loại bỏ hoàn toàn `AudioSource.PlayClipAtPoint()` gây rác bộ nhớ (GC Allocation); thay bằng `footstepAudioSource` 3D cố định trên Player phát qua `PlayOneShot()`.
    - [PlayerDeathHandler.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerDeathHandler.cs): Nối `deathAudioSource` và `policeSirenAudioSource` (3D Spatial Audio) trực tiếp vào `sfxGroup`.
    - [FlashlightController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/FlashlightController.cs) & [DoorController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Environment/DoorController.cs): Tự động nối `outputAudioMixerGroup` sang `sfxGroup`.
    - [MainHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/MainHUD.cs): Bổ sung `clickAudioSource`, `clickAudioClip` và gọi `PlayClickSound()` ngay khi click vào nút Tạm dừng (PauseBtn).
  - **Khắc phục cảnh báo 2 AudioListener khi chuyển Scene**:
    - Cập nhật [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs) và [HomeScreen.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HomeScreen.cs): Bổ sung hàm `EnsureAudioListener()` tự động quét toàn bộ `AudioListener` trong Scene, giữ lại đúng 1 cái duy nhất trên `Camera.main` và tự động xóa các listener thừa.
  - **Đảm bảo tính bền vững của Singleton `SettingsManager`**:
    - Tự động tách ra Root (`transform.SetParent(null)`) trong `Awake()` để lệnh `DontDestroyOnLoad` luôn có hiệu lực kể cả khi được xếp làm con của `GameManager`.
    - Bổ sung cơ chế chuyển giao thông minh: Nếu instance đang chạy thiếu tham chiếu mà có GameObject trong Scene mang Mixer/Group, hệ thống tự động kế thừa toàn bộ tham chiếu trước khi dọn dẹp đối tượng trùng lặp.
    - Thêm cơ chế tự động tìm `EnsureMixerReferences()`: Tự động tìm `FindMatchingGroups("BGM")`, `FindMatchingGroups("SFX")` kể cả khi chưa kéo thả vào Inspector.
    - Bổ sung `AutoRouteAllSceneAudioSources()`: Tự động quét toàn bộ `AudioSource` trong Scene chưa có Output Group để gán tự động (`loop = true` / nhạc nền $\rightarrow$ `bgmGroup`, click / hiệu ứng $\rightarrow$ `sfxGroup`).
  - **Tối ưu hóa UI Mobile & Icon nút Jump khi leo thang**:
    - [MobileActionButtons.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/MobileActionButtons.cs) & [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs):
      - Khi bắt đầu trèo thang, tự động ẩn các nút không thể thao tác (`Sprint`, `Crouch`, `Drop`, `Pickup`, `Interact`), chỉ giữ lại nút `Jump` và đổi icon sang icon nhảy thoát thang (`ladderJumpIcon`), tự động khôi phục khi rời thang.
      - Bổ sung `AutoFindJumpImage()`: Tự động quét child Image bên trong JumpButton hoặc lấy chính Image của nút để gán vào `jumpButtonImage`, cập nhật đồng thời cả `sprite` và `overrideSprite`.
- **Danh sách file thay đổi**:
  - `Assets/Audio/MainMixer.mixer` (New / Configured)
  - `Assets/Scripts/Utilities/SettingsManager.cs` (Enhanced Singleton & AudioMixer routing)
  - `Assets/Scripts/UI/SettingsHUD.cs` (Enhanced Dual Sliders & Realtime Preview)
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 40-42, 250-275, 765-785)
  - `Assets/Scripts/Player/PlayerDeathHandler.cs` (Lines 60-75)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Lines 60-95)
  - `Assets/Scripts/UI/HomeScreen.cs` (Lines 20-30, 50-70)
  - `Assets/Scripts/UI/MainHUD.cs` (Lines 30-35, 220-250)
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Lines 95-155)
  - `Assets/Scripts/Items/LadderController.cs` (Lines 205-215, 330-360)
  - `Assets/Scripts/Items/FlashlightController.cs` (Lines 40-55)
  - `Assets/Scripts/Environment/DoorController.cs` (Lines 45-60)
- **Ảnh hưởng**:
  - `SettingsManager` luôn giữ vững tham chiếu `MainMixer.mixer`, `BGM` Group và `SFX` Group từ Inspector xuyên suốt mọi Scene mà không bị mất tham chiếu (`None`).
  - Khi kéo thanh Slider BGM/SFX, âm lượng thay đổi tức thì theo thời gian thực và ghi nhận log chính xác trên Console.
  - Loại bỏ hoàn toàn lỗi rác bộ nhớ do `PlayClipAtPoint` và cảnh báo 2 AudioListener khi quay lại Menu.
  - Toàn bộ âm thanh trong game (tiếng bước chân, tiếng còi cảnh sát, tiếng leo thang, tiếng click UI, tiếng đèn pin, mở cửa) đều được phân luồng chuẩn xác vào AudioMixer.

---

### [2026-09-22 16:30] — feat(player, animation): procedural head and spine camera tracking (head look)
- **Tác vụ**:
  - Tạo mới component [PlayerHeadLook.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerHeadLook.cs):
    - Tự động quét tìm xương `Head` và `Spine`/`Chest` từ Humanoid Avatar (`_animator.GetBoneTransform`) hoặc quét đệ quy qua cây phân cấp Hierarchy theo từ khóa `"head"`, `"spine"`, `"chest"`.
    - Tính toán góc nhìn ngang (Yaw) và góc ngửa/cúi (Pitch) của Camera trong `LateUpdate()` sau khi Animator hoàn tất tính toán Animation cho frame.
    - Phân bổ chuyển động tự nhiên giữa Đầu (`headWeight = 0.75f`) và Thân trên/Ngực (`spineWeight = 0.25f`).
    - Giới hạn góc an toàn (Angle Clamping): Ngửa lên tối đa 60° (`maxUpPitch`), cúi xuống tối đa 45° (`maxDownPitch`), liếc trái/phải tối đa 75° (`maxYawAngle`) để tránh tình trạng vặn cổ phi thực tế khi quay camera ra sau lưng.
    - Tích hợp nội suy làm mượt (`smoothSpeed = 12.0f`) và chuyển đổi trọng số (`weightTransitionSpeed = 6.0f`).
    - Tự động tạm dừng xoay đầu khi nhân vật chết (`isDied`), khi giải đố minigame bẻ khóa (`isSolving`) hoặc khi đang leo thang (`isClimbingLadder`).
  - Cập nhật [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs):
    - Bổ sung trường `headLook` và tự động gắn component `PlayerHeadLook` khi khởi chạy nếu chưa có trên GameObject.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerHeadLook.cs` (New)
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 125-130, 245-255)
- **Ảnh hưởng**:
  - Đầu và ngực nhân vật Player xoay mượt mà theo góc ngửa/cúi và liếc theo hướng camera của người chơi, mang lại cảm giác nhân vật sống động và chân thực như game góc nhìn thứ 3 AAA.
  - Hoạt động ổn định với mọi rig nhân vật và không phụ thuộc vào thiết lập IK Pass của Animator.

---

### [2026-09-22 17:15] — feat(player, character): dynamic material and texture skinning via PlayerSO
- **Tác vụ**:
  - Bổ sung các trường `characterMaterial` và `characterTexture` vào [PlayerSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerSO.cs), cho phép mỗi cấu hình nhân vật sử dụng chung một Prefab 3D gốc (`PlayerManager.prefab`) nhưng mang bộ trang phục / bảng màu Material khác nhau.
  - Cập nhật [PlayerStats.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerStats.cs):
    - Tích hợp phương thức `ApplyCharacterSkin(PlayerSO data)` tự động thay thế Material / Texture trên các `Renderer` / `SkinnedMeshRenderer` của nhân vật ngay khi khởi tạo (`InitializeFromData`), tự động bỏ qua các vật phẩm cầm tay trong Hotbar, Đèn pin, Particle và UI.
    - Cung cấp hàm tiện ích tĩnh `PlayerStats.ApplySkinToModel(GameObject modelRoot, Material characterMaterial, Texture2D characterTexture)` hỗ trợ đổi skin nhanh cho Dummy 3D Model trong UI Menu Chọn Nhân Vật (Character Selection UI).
  - Cập nhật [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs):
    - Hỗ trợ đa tầng fallback Prefab: Tự động dùng `defaultPlayerData.characterPrefab` hoặc `fallbackPlayerPrefab` khi `characterPrefab` trong `PlayerSO` để trống (`null`).
  - Cập nhật [Char1.asset](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Data/Characters/Char1.asset) mẫu với `palette1.mat`.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerSO.cs` (Lines 28-36)
  - `Assets/Scripts/Player/PlayerStats.cs` (Lines 102-152)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Lines 156-170)
  - `Assets/Data/Characters/Char1.asset` (Lines 18-22)
- **Ảnh hưởng**:
  - Người phát triển có thể tạo vô số nhân vật mới (Char2, Char3, Char4...) chỉ bằng cách tạo 1 file `PlayerSO` (.asset) và kéo file Material (`palette2.mat`, `palette3.mat`...) vào mà không cần phải nhân bản thêm bất kỳ Prefab `PlayerManager` nào.
  ### [2026-09-23 00:20] — feat(ui, character): character selection HUD system with gender tabs, slot selection, and mesh swap
- **Tác vụ**:
  - Tạo mới [CharacterSelectionHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/CharacterSelectionHUD.cs) quản lý toàn bộ giao diện Chọn Nhân Vật:
    - `InfoPanel`: Hiển thị Tên (`NameText`), Mô tả (`DescText`), cùng 4 thanh chỉ số trực quan (`MoveSpeed`, `RunSpeed`, `Stamina`, `CarryWeight`) kèm Slider và Text số liệu tương ứng.
    - `BottomPanel`: Xử lý Tab chuyển đổi Giới tính (`MaleBtn`, `FemaleBtn`) đổi màu nền con (Xanh dương đậm cho Nam, Hồng đậm cho Nữ), nút Lưu (`SaveBtn`) kèm hiệu ứng phản hồi visual `"Saved!"`, nút Đóng (`CloseBtn`), và nút `SkinBtn`.
    - `ScrollView`: Quản lý danh sách các ô chọn nhân vật (`ContentMale`, `ContentFemale`), tự động highlight màu Vàng rực rỡ cho ô được chọn và trả các ô khác về màu mặc định.
    - Hỗ trợ xem trước (Preview) 3D Model trong Scene HomeMenu theo thời gian thực khi bấm chọn nhân vật.
    - Tự động dò tìm phân cấp Hierarchy (`AutoBindHierarchy`) giúp giảm thiểu thao tác kéo thả thủ công.
    - Tích hợp hệ thống Âm thanh UI (`clickAudioSource`, `clickSoundClip`, `tabSwitchSoundClip`, `saveSoundClip`) kết nối trực tiếp với kênh SFX AudioMixerGroup của SettingsManager.
  - Cập nhật [PlayerSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerSO.cs): Bổ sung `maleMesh` và `femaleMesh` để hỗ trợ đa dạng hóa ngoại hình theo giới tính.
  - Cập nhật [PlayerStats.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerStats.cs): Tích hợp phương thức `ApplyCharacterMeshAndSkin()` và tiện ích tĩnh `ApplyMeshToModel()` để thay thế Mesh trực tiếp trên `SkinnedMeshRenderer` / `MeshFilter`.
  - Cập nhật [GameSession.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/GameSession.cs) và [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs) để tự động lưu và đồng bộ lựa chọn nhân vật / giới tính xuyên Scene qua `PlayerPrefs` và `GameSession`.
  - Cập nhật [HomeScreen.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HomeScreen.cs): Bổ sung `characterButton`, `characterSelectPanel`, và các hàm `Character_Clicked()`, `CloseCharacterSelect()` để mở và đóng Bảng Chọn Nhân Vật trực tiếp từ Menu chính.
  - Cập nhật [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs):
    - Tự động dò tìm AudioSource tại `AudioManager/FootStep` hoặc các GameObject con của Player.
    - Bổ sung cơ chế phát tiếng bước chân theo nhịp di chuyển (`HandleProceduralFootsteps`), tự động hoạt động mượt mà cho mọi animation (kể cả animation tải về từ Mixamo không có `AnimationEvent`).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/CharacterSelectionHUD.cs` (New)
  - `Assets/Scripts/UI/HomeScreen.cs` (Lines 15-25, 80-140)
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 265-285, 660-670, 785-830)
  - `Assets/Scripts/Player/PlayerSO.cs` (Lines 28-55)
  - `Assets/Scripts/Player/PlayerStats.cs` (Lines 99-138)
  - `Assets/Scripts/Utilities/GameSession.cs` (Lines 24-30)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Lines 15-25, 152-175)
- **Ảnh hưởng**:
  - Hoàn thiện trọn vẹn hệ thống UI Chọn Nhân Vật theo đúng cấu trúc Hierarchy Canvas của Project.

---

### [2026-09-23 08:33] — feat(items): enhance immersive item descriptions tailored to item rarities and functions
- **Tác vụ**:
  - Viết lại toàn bộ mô tả (`description`) của tất cả 10 vật phẩm ScriptableObject (`.asset`) trong game theo phong cách tự nhiên, cuốn hút, bám sát cấp độ hiếm (`ItemRarity`) và công dụng trong gameplay Thief Simulator:
    - `InkWell` (Trash): Bình mực cũ phủ bụi, giá trị thấp nhưng không bỏ sót.
    - `CeramicVase` (Common): Bình gốm trang trí phổ thông, dễ vỡ nhưng dễ cầm đồ.
    - `Ladder` (Common): Thang leo di động dùng vượt tường và trèo lên nóc nhà.
    - `HealthKit` (Common): Hộp sơ cứu y tế gia đình nhỏ gọn.
    - `FlashLight` (Uncommon): Đèn pin dã chiến chuyên dụng soi góc khuất ban đêm.
    - `Knife` (Uncommon): Dao gọt đa năng tiện dụng phòng thân.
    - `Cash` (Epic): Bọc tiền mặt giá trị cao không tính tải trọng.
    - `GoldTrophy` (Epic): Cúp mạ vàng danh giá cho giới sưu tầm cổ vật (đồng thời sửa chính tả tên vật phẩm).
    - `TreasureScroll` (Legendary): Mật tịch cổ ghi chép tọa độ các hầm kho báu.
    - `GoldIngotStack` (Mythic): Chồng thỏi vàng 24K nguyên khối cực nặng nhưng trị giá cả một gia tài.
- **Danh sách file thay đổi**:
  - `Assets/Data/Items/InkWell.asset` (Line 18)
  - `Assets/Data/Items/CeramicVase.asset` (Line 18)
  - `Assets/Data/Items/Ladder.asset` (Line 18)
  - `Assets/Data/Items/HealthKit.asset` (Line 18)
  - `Assets/Data/Items/FlashLight.asset` (Line 18)
  - `Assets/Data/Items/Knife.asset` (Line 18)
  - `Assets/Data/Items/Cash.asset` (Line 18)

---

### [2026-09-23 08:50] — feat(ui, character): character unlocking system, default unlock flag, and slot lock icon management
- **Tác vụ**:
  - Cập nhật [PlayerSO.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerSO.cs):
    - Thêm `isUnlockedByDefault` (cho phép nhân vật mặc định được mở khóa sẵn từ đầu) và `unlockPrice` (giá tiền mở khóa nhân vật).
    - Tinh gọn hàm `GetMesh()` lấy trực tiếp từ 2 danh sách biến thể `maleMeshes` và `femaleMeshes`.
  - Cập nhật [NormalHuman.asset](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Data/Characters/NormalHuman.asset), [FatHuman.asset](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Data/Characters/FatHuman.asset), [StrongHuman.asset](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Data/Characters/StrongHuman.asset): Cấu hình `NormalHuman` mở khóa mặc định (`isUnlockedByDefault: 1`), `FatHuman` ($1000) và `StrongHuman` ($1500).
  - Cập nhật [CharacterSelectionHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/CharacterSelectionHUD.cs):
    - Tự động quét tìm GameObject cha **`LockImg`** (chứa child **`Image`** có component Button và child **`Price`** hiển thị giá) trên từng Slot ở cả 2 tab `ContentMale` và `ContentFemale`.
    - Tự động ẩn `LockImg` (`SetActive(false)`) cho các nhân vật đã mở khóa (như Slot 1 của `NormalHuman`), và hiện `LockImg` kèm tự động cập nhật text giá tiền `Price` (`$1000`, `$3000`) cho các nhân vật chưa sở hữu.
    - Đăng ký sự kiện click trực tiếp vào nút Button `Image` con bên trong `LockImg` để mở khóa tức thì (lưu `PlayerPrefs`), tự động ẩn `LockImg` và chọn nhân vật.
  - Cập nhật mô tả (`description`) cho 3 nhân vật:
    - `NormalHuman`: Tên trộm cân bằng toàn diện, chỉ số cơ bản tiêu chuẩn cho người mới bắt đầu.
    - `FatHuman`: Chỉ số di chuyển và thể lực thấp hơn nhưng sở hữu sức chứa balo khổng lồ (60 kg - gấp đôi bình thường).
    - `StrongHuman`: Tên trộm ưu tú vượt trội về mọi mặt (tốc độ chạy nhanh nhất, thể lực dồi dào, sức chứa 80 kg).
  - Tối ưu hóa phản hồi nút Lưu (**`SaveBtn`**):
    - Khi chọn nhân vật ĐÃ mở khóa: Bấm Save hiển thị text `"Saved"` (màu trắng nguyên bản, không dấu `!`), sau 1.5s tự động trả về `"Save"`.
    - Khi chọn nhân vật ĐANG BỊ KHÓA: Bấm Save giữ nguyên chữ `"Save"`, đồng thời hệ thống tự động lưu nhân vật mặc định ở Slot 1 (`NormalHuman`) vào `PlayerPrefs` và `GameSession` để đảm bảo game luôn có nhân vật hợp lệ khi vào trận.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerSO.cs` (Lines 32-58)
  - `Assets/Scripts/UI/CharacterSelectionHUD.cs` (Lines 265-385, 460-535)
  - `Assets/Data/Characters/NormalHuman.asset` (Lines 18-21)
  - `Assets/Data/Characters/FatHuman.asset` (Lines 18-21)
  - `Assets/Data/Characters/StrongHuman.asset` (Lines 18-21)
- **Ảnh hưởng**:
  - Nhân vật mặc định (Slot 1) tự động tắt icon ổ khóa cho cả giới tính Nam và Nữ.
  - Các nhân vật bị khóa sẽ hiển thị icon ổ khóa và tự động mở khóa mượt mà khi người chơi bấm vào slot.
  - Bảng InfoPanel hiển thị mô tả rõ ràng ưu/nhược điểm và vai trò chiến thuật của từng nhân vật.
  - Trải nghiệm bấm Save mượt mà, không bị đổi màu lòe loẹt và luôn an toàn fallback về nhân vật mặc định nếu người chơi chưa sở hữu nhân vật đang xem.

---

### [2026-09-23 09:18] — feat(ui, character): character JSON persistence, lobby model auto-load, and preview visibility toggle
- **Tác vụ**:
  - **Hệ thống Lưu trữ JSON (`character_save.json`)**:
    - Khởi tạo class `CharacterSaveData` tuần tự hóa lưu trữ thông tin:
      - `selectedCharacterId`: ID nhân vật đang chọn (VD: `char_01`, `char_02`, `char_03`).
      - `selectedCharacterIndex`: Index của nhân vật trong danh sách.
      - `isMale`: Giới tính đang chọn (`true` = Nam, `false` = Nữ).
      - `unlockedCharacterIds`: Danh sách toàn bộ các ID nhân vật đã được mở khóa.
    - Cài đặt 2 phương thức `LoadSavedSelection()` và `SaveCharacterDataToJSON()` sử dụng `JsonUtility` đọc/ghi trực tiếp vào đường dẫn `Application.persistentDataPath/character_save.json`.
    - Tự động đồng bộ song song với `GameSession` và `PlayerPrefs` để tương thích toàn bộ hệ thống gameplay.
    - Bổ sung Context Menu `Reset All Character Unlocks (For Testing)` trên component để xóa cache JSON & PlayerPrefs về mặc định.
  - **Tự động áp dụng trang phục cho 3D Model ngoài sảnh (`ApplyLobbyModelVisuals`)**:
    - Ngay khi vào màn hình chính `HomeMenu` (trong `HomeScreen.Start()` và `CharacterSelectionHUD.Awake()`), model 3D đứng ngoài sảnh (`previewModelRoot`) sẽ được nạp ngay lập tức dữ liệu nhân vật đã lưu trong JSON (Mesh nam/nữ + Material/Texture tương ứng) mà không cần người chơi phải mở bảng chọn nhân vật mới cập nhật.
  - **Tùy chọn ẩn model ngoài sảnh khi mở bảng chọn nhân vật (`hidePreviewModelWhenPanelOpens`)**:
    - Thêm biến `public bool hidePreviewModelWhenPanelOpens` trong Inspector của `CharacterSelectionHUD`.
    - Khi mở bảng chọn (`OnEnable`), nếu bật tùy chọn này sẽ tự động ẩn model ngoài sảnh (`previewModelRoot.SetActive(false)`), và khi đóng bảng (`OnDisable` / `OnCloseButtonClicked` / `HomeScreen.CloseCharacterSelect`) sẽ tự động khôi phục hiển thị model theo nhân vật đã lưu, hỗ trợ chuẩn bị cho việc hiển thị model sảnh Online co-op sau này.
    - Bắt buộc ẩn 3D model ngoài sảnh (`previewModelRoot.SetActive(false)`) ngay khi mở bảng Character Selection, Settings, và Chapter Selection, loại bỏ hoàn toàn khả năng bị bật đè do cache scene Inspector.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/CharacterSelectionHUD.cs` (Lines 39-47, 160-230, 904-920)
  - `Assets/Scripts/Player/PlayerStats.cs` (Lines 122-185)
  - `Assets/Scripts/UI/HomeScreen.cs` (Lines 85-94, 140-210)
  - `Assets/Scripts/UI/SettingsHUD.cs` (Lines 370-390)
  - `Assets/Scripts/UI/ChapterSelectManager.cs` (Lines 70-90)
- **Ảnh hưởng**:
  - Dữ liệu nhân vật và mở khóa được tổ chức trong file JSON sạch sẽ, dễ đồng bộ lên Firebase/Photon PUN2.
  - Model 3D chỉ xuất hiện tại Sảnh chính (MainMenu) và sảnh Online (sau này), hoàn toàn không che khuất tầm nhìn khi người chơi mở Settings, Chapter Selection hay Character Selection.
  - Bục đứng / Chỗ đứng không còn bị đổi nhầm Mesh hoặc bị áp đè Material của nhân vật.

---

### [2026-09-23 10:50] — feat(network, auth): implement Firebase Auth, Realtime Database sync, and AuthHUD UI
- **Tác vụ**:
  - **Tích hợp Firebase Authentication & Database (Task 1.1 & 1.2)**:
    - Tạo `UserGameProfile.cs` định dạng dữ liệu người chơi chuẩn hóa trên Cloud (UID, Username, Email, Cash, Nhân vật đang chọn, Danh sách nhân vật đã mở khóa, Chapter tiến trình).
    - Tạo `FirebaseAuthService.cs` quản lý toàn bộ luồng xác thực: Đăng ký (Email/Password), Đăng nhập, Đăng nhập Khách (Guest / Anonymous), Quên mật khẩu (Reset Password qua Email), Đăng xuất.
    - Xử lý ánh xạ các mã lỗi `AuthError` của Firebase sang thông báo tiếng Việt thân thiện, rõ ràng.
  - **Đồng bộ Dữ liệu Người chơi 2 chiều (Task 1.3)**:
    - Tạo `FirebaseDataService.cs` kết nối Firebase Realtime Database (`users/{uid}`).
    - Tự động đồng bộ 2 chiều giữa dữ liệu đám mây (Cloud) và file lưu cục bộ `character_save.json` + `GameSession`.
    - Hỗ trợ các tiện ích gameplay: `AddCash()`, `TrySpendCash()`, `UnlockCharacter()`, `SetSelectedCharacter()`.
  - **Giao diện Đăng nhập & Đăng ký (AuthHUD) & Quản lý nút Multiplayer theo mạng**:
    - Tạo `AuthHUD.cs` điều phối chuyển đổi mượt mà giữa form Đăng nhập, form Đăng ký, và form Quên mật khẩu.
    - Xử lý trạng thái Loading Spinner và tự động ẩn khi đăng nhập thành công để mở Main Menu.
    - Tạo `UISpinnerRotator.cs` xoay ảnh spinner dạng giật từng bước (mặc định 2 FPS, góc -45°/bước) mang phong cách cổ điển.
    - Thêm cơ chế tự động làm mờ và khóa `multiplayerButton` khi mất mạng trong `HomeScreen.cs`, kèm biến `simulateOffline` cho phép test bật/tắt mạng trực tiếp trong Unity Editor Inspector.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Network/UserGameProfile.cs` (New)
  - `Assets/Scripts/Network/FirebaseAuthService.cs` (New/Modified: Tự động gửi Email Verification chống email ảo, kiểm tra IsEmailVerified trước khi cho phép đăng nhập, hỗ trợ Re-send verification link)
  - `Assets/Scripts/Network/FirebaseDataService.cs` (New/Modified: Hỗ trợ cấu hình Realtime Database khu vực Singapore asia-southeast1, tối ưu an toàn Null-Safe)
  - `Assets/Scripts/UI/AuthHUD.cs` (New/Modified: Chuyển hướng người chơi sau khi đăng ký chờ xác thực email)
  - `Assets/Scripts/UI/CharacterSelectionHUD.cs` (Lines 120-185, 410-435, 550-645: Đồng bộ chọn & mở khóa nhân vật 2 chiều với Firebase Realtime Database, fix hiển thị 3D preview model)
  - `Assets/Scripts/UI/ProfileHUD.cs` (New: Quản lý InfoPanel hồ sơ cá nhân, đổi tên, đăng xuất, đồng bộ dữ liệu đám mây thủ công và modal xóa tài khoản an toàn)
  - `Assets/Scripts/UI/UISpinnerRotator.cs` (New)
  - `Assets/Scripts/UI/HomeScreen.cs` (Lines 13-30, 150-250, 310-360: Thêm InfoButton, PlayerNameText hiển thị tên trên sảnh chính, quản lý mở/đóng InfoPanel và tự động đồng bộ tên khi đổi profile)
  - `.gitignore` (Lines 1-50)
- **Ảnh hưởng**:
  - Hoàn tất toàn bộ Giai đoạn 1 (Task 1.1, 1.2, 1.3) chuẩn bị cho hệ thống Multiplayer và Profile người chơi trực tuyến.
  - Ngăn chặn hoàn toàn việc spam đăng ký tài khoản ảo nhờ quy trình Email Verification của Firebase.
  - Nhân vật chọn và mở khóa được lưu trữ vĩnh viễn trên Cloud Firebase của người chơi.
  - Người chơi có toàn quyền quản lý hồ sơ: đổi tên, đồng bộ dữ liệu hoặc xóa tài khoản trực tiếp từ HomeScreen.

---

### [2026-09-23 22:45] — fix(inventory, camera, auth): fix persistent held item across scenes, item disappearance, add mesh follow delay, and skip menu loading HUD

- **Tác vụ**:
  - **Khắc phục lỗi vật phẩm kẹt trước màn hình khi về Menu rồi vào lại Map**:
    - [ScreenshotUtility.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Utilities/ScreenshotUtility.cs): Ngăn chặn `DontDestroyOnLoad(gameObject)` khi script được gắn trực tiếp trên `MainCamera` trong Scene (`HomeMenu`, `Chapter2`, `Anhtho`), ngăn Camera và các đối tượng con runtime (`ItemHoldPoint` + vật phẩm đang cầm) bị lưu giữ xuyên Scene.
    - [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs): Tự động dọn dẹp Hotbar (`hotbar.ClearAllSlots()`) và ẩn model đang cầm trước khi rời Scene về `HomeMenu`.
  - **Khắc phục lỗi Loading HUD xuất hiện lại khi thoát từ gameplay về Menu**:
    - [AuthHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/AuthHUD.cs): Trong `Start()`, kiểm tra nếu người chơi đã đăng nhập (`FirebaseAuthService.Instance.IsLoggedIn == true`), tự động tắt `loadingPanel` và bỏ qua `AutoLoginRoutine()`, chuyển thẳng vào `MainMenuPanel`.
  - **Khắc phục lỗi vật phẩm bị biến mất vĩnh viễn khi đổi slot Hotbar**:
    - [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Trong `HideHeldModel()`, tách an toàn model vật phẩm ra Root (`transform.SetParent(null)`) khi cất item; loại bỏ lệnh `Destroy` nhầm đối tượng con trong `InitializeHoldPoint()` để bảo vệ an toàn cho item trong túi đồ.
  - **Nâng cấp độ trễ xoay mượt và cơ chế bám thân nhân vật (HoldFollowMode.PlayerMesh)**:
    - [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
      - Bổ sung `enum HoldFollowMode` gồm `PlayerMesh` (bám theo thân người chơi) và `SmoothCamera` (bám theo camera có độ trễ mượt).
      - Chế độ `PlayerMesh`: Đồng bộ góc xoay Y của vật phẩm theo thân nhân vật (`playerController.transform.rotation`), giúp vật phẩm xoay với độ trễ tự nhiên theo góc quay của Mesh thay vì quay giật theo tốc độ camera.
      - Bổ sung nội suy `Vector3.Lerp` và `Quaternion.Slerp` với các tham số `rotationSmoothSpeed` (12) và `positionSmoothSpeed` (15) tạo quán tính tự nhiên khi di chuyển/đổi hướng.
      - Thêm cờ `isFirstFrameHeld` để gán tức thì vị trí khi vừa rút item từ hotbar, tránh hiện tượng vật phẩm bay lướt từ xa tới.
  - **Chỉ cho phép tương tác/leo thang khi thang được ĐẶT (Place vào tường/vật cản) thay vì NÉM/THẢ tự do (Drop)**:
    - [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs):
      - Bổ sung biến trạng thái `isPlaced` (mặc định `true` cho thang dựng sẵn trong Scene map).
      - Bổ sung phương thức `SetPlaced(bool placed)` để cập nhật trạng thái dựng thang và tự động khóa/mở trục xoay X & Z (`SetUprightLocked`).
      - Trong `CanInteract()`, `CheckClimbInput()`, `StartClimbing()` và `Interact()`: Bắt buộc kiểm tra `if (!isPlaced) return;`, ngăn chặn hoàn toàn việc bám/leo thang khi thang bị thả/ném tự do nằm dưới đất.
    - [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
      - Khi thả thang bằng nút Drop: Nếu trước mặt có vật cản/tường (`hasObstacle == true`), kích hoạt `ladder.SetPlaced(true)` dựng thẳng thang và cho phép leo trèo.
      - Nếu không gian thoáng (`hasObstacle == false`), kích hoạt `ladder.SetPlaced(false)` kèm lực đẩy và xoay tự nhiên (`rb.AddTorque`), thang rơi lật tự do và không thể leo.
    - [PlayerInventory.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInventory.cs):
      - Đặt `ladder.SetPlaced(false)` khi nhặt thang vào túi đồ (`TryPickupItem`) hoặc khi rớt vật phẩm lúc tử vong (`DropAllItemsOnDeath`) / vứt đồ trực tiếp (`DropLastItem`).
    - [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs):
      - Tối ưu lựa chọn `primary`: Khi nhìn vào thang đang bị vứt dưới đất (`!isPlaced`), hệ thống sẽ tự động ưu tiên `currentLootItem` để hiển thị prompt nhặt đồ `[E] Pick Up - Ladder` (và hiển thị nút Pick Up trên Mobile) thay vì hiển thị cảnh báo không thể leo.
  - **Nâng cấp Cửa thông minh (Smart Door): Cơ chế 1 Collider duy nhất, tự động mở/đóng cho NPC và Player đã bẻ khóa, tự động chắn tầm nhìn**:
    - [DoorController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Environment/DoorController.cs):
      - Loại bỏ hoàn toàn Trigger Collider phụ (tránh xung đột và vướng 2 Collider trên cửa, không làm lệch tia Raycast tâm ngắm).
      - Sử dụng cơ chế quét khoảng cách `Physics.OverlapSphereNonAlloc` định kỳ (`checkInterval = 0.15s`) trong bán kính `autoOpenRadius` (2.5m) với tâm quét tùy chỉnh `detectionCenterOffset` (mặc định `(0, 1.0, 0)` nâng tâm lên ngang người) kèm Gizmo trực quan trong Scene:
        - **NPC (kid, adult, Npc)**: Khi đi tới gần, cửa tự động nhận diện và xoay mở mượt mà (`SetDoorOpen(true)`), NPC không bị kẹt khi tuần tra hoặc truy đuổi.
        - **Player**: Khi cửa chưa bẻ khóa (`!isUnlocked`), cửa giữ nguyên trạng thái đóng và hiển thị prompt `[F] Pick Lock`. Khi đã bẻ khóa (`isUnlocked`), Player chỉ cần đi lại gần là cửa tự động mở.
        - **Khi rời xa cửa**: Khi không còn ai đứng trong bán kính quét, cửa tự động xoay đóng lại (`SetDoorOpen(false)`).
      - Bổ sung cờ `disableColliderWhenOpen = true` và phương thức `SetDoorCollidersActive`: Tự động tắt Collider khi cửa mở (đảm bảo Player/NPC đi qua thông thoáng 100% không bị vướng mép) và tự động bật lại Collider khi cửa đã đóng kín để chắn đường và cản tầm nhìn (Line of Sight của NPC).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Utilities/ScreenshotUtility.cs` (Lines 29-58)
  - `Assets/Scripts/UI/UI_Manager.cs` (Lines 440-455)
  - `Assets/Scripts/UI/AuthHUD.cs` (Lines 102-118)
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 40-95, 290-375, 450-480, 725-805, 895-935)
  - `Assets/Scripts/Items/LadderController.cs` (Lines 80-86, 155-175, 305-345, 770-805)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Lines 228-245)
  - `Assets/Scripts/Player/PlayerInventory.cs` (Lines 145-155, 220-230, 285-295)
  - `Assets/Scripts/Environment/DoorController.cs` (Lines 1-285)
- **Ảnh hưởng**:
  - Khi thoát về Menu và vào lại Map, không còn hiện tượng model vật phẩm cũ kẹt lơ lửng trước màn hình.
  - Không còn màn hình Loading HUD chạy lại khi thoát từ trận đấu về sảnh chính nếu tài khoản đã đăng nhập.
  - Chuyển đổi giữa các slot trong Hotbar mượt mà, không bị mất/xóa nhầm vật phẩm.
  - Vật phẩm cầm trên tay di chuyển và xoay có độ trễ quán tính tự nhiên, ăn khớp với chuyển động quay thân của nhân vật thay vì bị khóa cứng theo camera.
  - Thang chỉ có thể tương tác leo trèo khi được người chơi chủ động đặt (Place) vào bề mặt tường/sàn vật cản. Nếu ném/vứt tự do (Drop) ra đất, thang sẽ áp dụng vật lý tự do ngã đổ và chỉ có thể tương tác để nhặt lại vào túi (Pick Up) chứ không thể leo.
---

### [2026-09-24 01:25] — feat(multiplayer): integrate Photon Fusion multiplayer, matchmaking lobby, and networked interactions
- **Tác vụ**:
  - **Task 2.1 — Photon Fusion Lobby & Matchmaking**:
    - Thiết lập App ID Fusion mới vào [PhotonAppSettings.asset](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Photon/Fusion/Resources/PhotonAppSettings.asset).
    - Tạo mới [FusionConnectionManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/FusionConnectionManager.cs) kế thừa Singleton và `INetworkRunnerCallbacks` quản lý toàn bộ vòng đời kết nối Photon Fusion (Lobby, Create Session, Join Session, Join by Code, Quick Match, Load Gameplay Scene).
    - Tạo mới [NetworkRoomItem.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkRoomItem.cs) hiển thị danh sách phòng trực tiếp (Tên phòng, Số lượng người chơi, Bản đồ, Nút Tham gia).
    - Tạo mới [NetworkLobbyHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkLobbyHUD.cs) giao diện đa màn hình: Sảnh chính (Lobby Main), Tạo phòng (Create Room Modal), Nhập mã phòng (Join Code Modal), và Phòng chờ (Waiting Room với nút Sẵn sàng / Bắt đầu game).
    - Tích hợp nút `Multiplayer` tại [HomeScreen.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HomeScreen.cs) để mở trực tiếp `NetworkLobbyHUD`.
  - **Task 2.2 — Network Player Synchronization**:
    - Nâng cấp [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs): Khi Photon Fusion đang chạy, spawn Player thông qua `runner.Spawn(playerPrefab, pos, rot, runner.LocalPlayer)`. Nếu không chạy mạng (Offline test), fallback về `Instantiate` mượt mà.
    - Tạo mới [NetworkPlayerSync.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/NetworkPlayerSync.cs): Phân quyền điều khiển camera/input (`HasInputAuthority`), đồng bộ Name Tag trên đầu nhân vật, và đồng bộ Skin/Giới tính nhân vật qua biến `[Networked] CharacterSkinIndex`.
  - **Task 2.3 — Network Interaction & Items**:
    - Tạo mới [NetworkItemSync.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/NetworkItemSync.cs) và tích hợp vào [PlayerInventory.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInventory.cs): Khi một người chơi nhặt vật phẩm, phát RPC thông báo tất cả các máy khác trong phòng ẩn/xóa vật phẩm khỏi Scene để tránh bị nhặt trùng.
    - Tạo mới [NetworkDoorSync.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/NetworkDoorSync.cs) và tích hợp vào [DoorController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Environment/DoorController.cs): Đồng bộ trạng thái mở khóa (`RpcSyncUnlockDoor`) và trạng thái mở/đóng cánh cửa (`RpcSyncSetDoorOpen`) tức thì cho toàn bộ người chơi trong phòng.
- **Danh sách file thay đổi**:
  - `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` (Modified)
  - `Assets/Scripts/Network/FusionConnectionManager.cs` (New)
  - `Assets/Scripts/Network/NetworkPlayerSync.cs` (New)
  - `Assets/Scripts/Network/NetworkDoorSync.cs` (New)
  - `Assets/Scripts/Network/NetworkItemSync.cs` (New)
  - `Assets/Scripts/UI/NetworkRoomItem.cs` (New)
  - `Assets/Scripts/UI/NetworkLobbyHUD.cs` (New)
  - `Assets/Scripts/UI/HomeScreen.cs` (Modified)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Modified)
  - `Assets/Scripts/Player/PlayerInventory.cs` (Modified)
  - `Assets/Scripts/Environment/DoorController.cs` (Modified)
- **Ảnh hưởng**:
  - Toàn bộ hệ thống sảnh (Lobby), tạo phòng, tìm trận, phòng chờ, spawn nhân vật qua mạng, đồng bộ di chuyển, nhặt đồ và tương tác cửa đã sẵn sàng và hoạt động mượt mà cả ở chế độ Online lẫn Offline test.

---

### [2026-09-24 09:10] — feat(ui, network): chapter background preview for lobby room items & waiting room, and online pause timeScale fix
- **Tác vụ**:
  - **Ảnh Chapter Background cho Room Item Prefab & Sảnh Chờ (Waiting Room)**:
    - [NetworkRoomItem.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkRoomItem.cs):
      - Bổ sung `mapBgImage` và `chapterNameText` để hiển thị ảnh bìa và tên Chapter của phòng.
      - Nâng cấp `Setup()` nhận tham số `Sprite mapSprite` và `string mapTitle` để cập nhật visual tương ứng cho từng dòng phòng.
    - [FusionConnectionManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/FusionConnectionManager.cs):
      - Cập nhật `CreateSession()` đính kèm `SessionProperties["map"]` để đồng bộ thông tin bản đồ được chọn lên Photon Session.
    - [NetworkLobbyHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkLobbyHUD.cs):
      - Bổ sung `waitingRoomMapImage` và `waitingRoomMapTitleText` hiển thị ảnh và tên Chapter trong màn hình phòng chờ (`WaitingRoomPanel`).
      - Hỗ trợ danh sách `chapterList` (`ChapterSO`) và `mapPreviewSprites`.
      - Đăng ký sự kiện `onValueChanged` trên `mapSelectDropdown`: Khi Host đổi Chapter trong Sảnh chờ, ảnh preview `waitingRoomMapImage` tự động thay đổi theo thời gian thực.
      - Trong `UpdateRoomListUI()`: Đọc thuộc tính `"map"` từ `SessionInfo.Properties` để gán đúng ảnh bìa Chapter tương ứng cho từng `roomItemPrefab`.
  - **Khắc phục lỗi Đóng băng thời gian (`timeScale = 0`) khi người chơi Online bấm Pause**:
    - [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs):
      - Trong `PauseGame()`: Kiểm tra nếu `FusionConnectionManager.Instance.currentRunner.IsRunning == true` (đang chơi Online) $\rightarrow$ giữ nguyên `Time.timeScale = 1f` để game và các gói tin Photon Fusion vẫn hoạt động liên tục trong nền, ngăn ngừa hoàn toàn lỗi giật lag, đơ nhân vật và mất đồng bộ mạng.
      - Nếu chơi Offline (Singleplayer) $\rightarrow$ giữ nguyên cơ chế dừng game `Time.timeScale = 0f`.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/NetworkRoomItem.cs` (Lines 10-35)
  - `Assets/Scripts/Network/FusionConnectionManager.cs` (Lines 126-165)
  - `Assets/Scripts/UI/NetworkLobbyHUD.cs` (Lines 48-150, 200-245, 335-375)
  - `Assets/Scripts/UI/UI_Manager.cs` (Lines 248-275)
- **Ảnh hưởng**:
  - Room Item hiển thị ảnh bìa trực quan, bắt mắt của Chapter.
  - Sảnh chờ Waiting Room cập nhật ảnh Map động khi Host thay đổi lựa chọn.
  - Bấm Pause khi chơi Online không làm gián đoạn hay kẹt mạng của phòng chơi.

---

### [2026-09-24 11:10] — feat(ui, multiplayer): implement NetworkPlayerSlot and dynamic waiting room player list
- **Tác vụ**:
  - Tạo mới [NetworkPlayerSlot.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkPlayerSlot.cs):
    - Tự động dò tìm phân cấp Hierarchy (`AutoBind`): `Avatar/PlayerImg`, `PlayerNameText`, `HostImg`, `GuestImg`, `HostBadgeText`, `ReadyStatusText`.
    - Quản lý hiển thị Tên người chơi, Avatar, huy hiệu Host/Guest, và trạng thái Sẵn sàng (`READY ✓` / `WAITING...`).
  - Nâng cấp [NetworkLobbyHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkLobbyHUD.cs):
    - Đăng ký sự kiện `OnPlayerJoinedEvent` và `OnPlayerLeftEvent` để cập nhật danh sách người chơi ngay khi có người vào/ra phòng.
    - Cài đặt hàm `UpdateWaitingRoomPlayerList()`: Duyệt qua `runner.ActivePlayers`, tự động spawn `playerSlotPrefab` và gọi `slot.Setup()` đồng bộ tên người chơi và vai trò Host/Client.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/NetworkPlayerSlot.cs` (New)
  - `Assets/Scripts/UI/NetworkLobbyHUD.cs` (Lines 165-185, 440-510)
- **Ảnh hưởng**:
  - Danh sách thành viên trong phòng chờ cập nhật tự động thời gian thực khi có người chơi tham gia hoặc rời phòng.

---

### [2026-09-24 11:20] — feat(ui, network): dynamic room ID display and restart button safety in Pause HUD
- **Tác vụ**:
  - [PauseHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/PauseHUD.cs):
    - Bổ sung `public TextMeshProUGUI roomCodeText` và cơ chế tự động dò tìm (`AutoFindUIElements`).
    - Khi bấm Pause trong trận đấu: Nếu đang chơi Online qua Photon Fusion, tự động hiển thị chuỗi `Room ID: <Tên/Mã phòng>` để người chơi dễ dàng đọc mã mời bạn bè. Nếu chơi Offline, text này tự động ẩn đi.
    - Tự động ẩn nút `Restart` khi đang chơi Online để ngăn chặn việc khởi động lại trận đấu của phòng nhiều người chơi.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/PauseHUD.cs` (Lines 15-30, 70-85, 115-145)
- **Ảnh hưởng**:
  - Người chơi trong trận đấu có thể mở menu Pause để xem nhanh Room ID bất kỳ lúc nào.

---

### [2026-09-24 11:26] — feat(network, auth): universal single-session kick across online gameplay, offline levels, and menus
- **Tác vụ**:
  - [FirebaseDataService.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/FirebaseDataService.cs):
    - Nâng cấp hàm callback phát hiện xung đột phiên (`RegisterAndListenSessionAsync`):
      - Khi phát hiện tài khoản đăng nhập trên thiết bị mới:
        1. Rời khỏi phòng multiplayer nếu đang trong trận đấu (`FusionConnectionManager.Instance.LeaveSession()`).
        2. Đăng xuất Firebase an toàn (`FirebaseAuthService.Instance.Auth.SignOut()`).
        3. Tự động tải lại Scene `HomeMenu` nếu người chơi đang ở trong bất kỳ màn chơi gameplay nào (`Chapter1`, `Chapter2`, v.v.).
        4. Kích hoạt sự kiện `OnLoggedOutFromAnotherDevice` để hiển thị cảnh báo đỏ và đưa người chơi về màn hình đăng nhập `AuthHUD`.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Network/FirebaseDataService.cs` (Lines 565-595)
- **Ảnh hưởng**:
  - Bảo vệ toàn diện 100% tài khoản: Bất kể người chơi đang ở ngoài Menu, đang trong Sảnh chờ, đang chơi đơn Offline hay đang trong trận đấu Co-op Online, nếu có máy khác đăng nhập thì máy cũ sẽ bị đá ra ngay lập tức và đưa về màn hình đăng nhập.

---

### [2026-09-24 12:07] — feat(ui, audio): add click sound support and enhanced room title display to NetworkLobbyHUD
- **Tác vụ**:
  - [NetworkLobbyHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkLobbyHUD.cs):
    - Tích hợp `clickAudioSource` và `clickSoundClip` phát âm thanh click UI qua kênh SFX của `SettingsManager`.
    - Gắn `PlayClickSound()` vào tất cả các nút: Refresh, Tạo phòng, Nhập Code, Quick Play, Ready, Start Game, Rời phòng, Đóng Lobby.
    - Cải tiến `ShowWaitingRoom()` phân tách rõ ràng:
      - `waitingRoomTitleText`: Hiển thị tên phòng thân thiện (VD: `"Hi's Room"` hoặc `"Quick Match Room"`).
      - `waitingRoomCodeText`: Hiển thị mã số phòng (`ID: Room_1234`).
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/NetworkLobbyHUD.cs` (Lines 70-115, 260-475)
- **Ảnh hưởng**:
  - Giao diện Lobby phản hồi âm thanh sống động khi bấm nút và hiển thị tiêu đề phòng chuẩn xác, đẹp mắt.

---

### [2026-09-24 13:20] — fix(network, ui, player): fix Fusion synchronous spawn exception, real-time profile name sync, and dim Pause restart button
- **Tác vụ**:
  - **Khắc phục lỗi `NetworkObjectSpawnException: Failed to load prefab synchronously` & lỗi không di chuyển được**:
    - Trong [NetworkProjectConfig.fusion](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion): Bật `"EnqueueIncompleteSynchronousSpawns": true` để Fusion tự động xếp hàng và xử lý spawn đồng bộ an toàn, không bị crash ngoại lệ.
    - Trong [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs): Thêm khối `try-catch` và hỗ trợ gọi `SpawnAsync` dự phòng khi spawn đồng bộ gặp sự cố.
  - **Khắc phục lỗi Tên Profile không cập nhật ngay sau khi đăng xuất/đăng nhập tài khoản mới**:
    - Trong [FirebaseDataService.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/FirebaseDataService.cs): Đảm bảo các sự kiện `OnUserProfileLoaded` và `OnUserProfileUpdated` luôn được kích hoạt trên Main Thread Unity (`RunOnMainThread`) để UI (`TextMeshProUGUI`) cập nhật tức thì mà không bị chặn luồng. Đồng thời kích hoạt thông báo reset profile khi gọi `OnUserLogout()`.
    - Trong [AuthHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/AuthHUD.cs): Tự động gọi `home.UpdatePlayerProfileVisuals()` và `home.SyncSavedCharacter()` ngay trong `OnAuthSuccess()`.
  - **Làm mờ và vô hiệu hóa nút Restart trong Pause HUD khi chơi Online**:
    - Trong [PauseHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/PauseHUD.cs): Thay vì ẩn nút (`SetActive(false)`), giữ nút Restart luôn hiển thị nhưng làm mờ (`alpha = 0.4f`) và tắt tương tác (`interactable = false`) khi đang ở trong phòng chơi mạng Fusion.
- **Danh sách file thay đổi**:
  - `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` (Modified)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Modified)
  - `Assets/Scripts/Network/FirebaseDataService.cs` (Modified)
  - `Assets/Scripts/UI/AuthHUD.cs` (Modified)
  - `Assets/Scripts/UI/PauseHUD.cs` (Modified)
- **Ảnh hưởng**:
  - Người chơi vào phòng Online sinh nhân vật mượt mà, di chuyển bình thường, tên hiển thị chuẩn xác ngay sau đăng nhập và giao diện Pause menu chuyên nghiệp.

---

### [2026-09-24 13:35] — feat(ui, lobby): customize player slot role visibility and ready status display
- **Tác vụ**:
  - [NetworkPlayerSlot.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkPlayerSlot.cs):
    - Cập nhật logic phân quyền hiển thị theo vai trò (Host vs Guest):
      - **Chủ phòng (`isHost == true`)**:
        - `hostImg.SetActive(true)`, `guestImg.SetActive(false)`.
        - `hostBadgeText`: Bật hiển thị (`SetActive(true)`), hiển thị chữ `"HOST"`.
        - `readyStatusText`: Tự động ẩn (`SetActive(false)`), không hiển thị chữ trạng thái Ready của chủ phòng.
      - **Khách vào (`isHost == false`)**:
        - `guestImg.SetActive(true)`, `hostImg.SetActive(false)`.
        - `hostBadgeText`: Tự động ẩn (`SetActive(false)`).
        - `readyStatusText`: Bật hiển thị (`SetActive(true)`), cập nhật đổi màu & nội dung linh hoạt:
          - Khi đã sẵn sàng: `<color=#00FF88>READY ✓</color>`
          - Khi chưa sẵn sàng: `<color=#FF4D4D>NOT READY</color>`
    - Mở rộng hàm `AutoBind()` tự động dò tìm thông minh nhiều biến thể tên của các phần tử con trong Hierarchy.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/NetworkPlayerSlot.cs` (Lines 40-115)
- **Ảnh hưởng**:
  - Danh sách người chơi trong Sảnh chờ (Waiting Room) hiển thị trực quan, đúng vai trò chủ phòng và khách, trạng thái Sẵn sàng / Chưa sẵn sàng rõ ràng.

---

### [2026-09-24 14:15] — fix(multiplayer, player, ui): fix multiplayer freeze/movement, sync ready states, lock Start Game, and support slot panel hiding
- **Tác vụ**:
  - **Khắc phục triệt để lỗi không di chuyển được khi vào trận Co-op Multiplayer**:
    - Trong [ScenePlayerSpawner.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/ScenePlayerSpawner.cs):
      - Sửa điều kiện `preventDuplicateIfPlayerExists`: Chỉ áp dụng khi chơi Offline đơn lẻ; ở chế độ Multiplayer, mọi người chơi (Host lẫn Guest) luôn tự động spawn nhân vật mạng của chính mình qua Fusion với quyền `runner.LocalPlayer`.
      - Phân bổ vị trí xuất phát (`spawnPoints`) so le theo `PlayerId` để tránh người chơi spawn đè vào nhau.
      - Gọi `ui.BindPlayer(stats)` để gán và khởi tạo trọn vẹn thông số Chapter (thời gian, điểm số mục tiêu, thanh stamina, máu).
    - Trong [NetworkPlayerSync.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/NetworkPlayerSync.cs):
      - Trên Remote Player (người chơi khác): Vô hiệu hóa `playerController`, `characterController`, `playerInput`, `starterInputs`, `PlayerInteraction`, `PlayerHeadLook` và xóa `AudioListener` thừa để tránh xung đột vật lý và camera với Local Player.
      - Trên Local Player: Kích hoạt đầy đủ toàn bộ bộ điều khiển và gắn kết Camera Cinemachine.
    - Trong [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs):
      - Khắc phục điều kiện thắng game `player.currpoint == player.totalpoint`: Bổ sung thêm điều kiện bảo vệ `player.totalpoint > 0` để tránh trường hợp khởi tạo ban đầu (0/0) làm nhân vật bị khóa cứng đơ không di chuyển được.
    - Trong [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs):
      - Bổ sung hàm công khai `BindPlayer(PlayerStats stats)` tự động nạp `InitializeChapter` và khởi tạo HUD ngay khi Player spawn trễ qua mạng.
  - **Đồng bộ trạng thái Ready qua mạng và khóa nút Start Game của Chủ phòng**:
    - Trong [FusionConnectionManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Network/FusionConnectionManager.cs):
      - Bổ sung sự kiện `OnPlayerReadyStatusReceived` và hàm `SendReadyStatus(bool isReady)` truyền tải dữ liệu tin cậy (Reliable Data) thời gian thực giữa các máy.
    - Trong [NetworkLobbyHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkLobbyHUD.cs):
      - Quản lý từ điển trạng thái `playerReadyStates` cho từng người chơi.
      - Chủ phòng (`Host`): Khóa nút `Start Game` (`interactable = false`, mờ `alpha = 0.45f`) khi còn khách chưa bấm Ready. Chỉ mở khóa cho phép vào trận khi 100% người chơi trong phòng đã Ready.
  - **Cải tiến ẩn/hiện Panel Status Message trong NetworkLobbyHUD**:
    - Trong [NetworkLobbyHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkLobbyHUD.cs):
      - Bổ sung `public GameObject statusMessagePanel;` dưới mục `[Header("📢 Status Message")]`.
      - Tự động ẩn cả Panel nền và Text khi khởi chạy, chỉ hiện lên khi có thông báo (`ShowStatus`), tự động ẩn sạch sẽ sau delay (`ClearStatusAfterDelay`), chống hoàn toàn việc panel trống đè lên các UI khác trong sảnh chờ.
    - Trong [NetworkPlayerSlot.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/NetworkPlayerSlot.cs):
      - Giữ cấu trúc gọn gàng, bật/tắt trực tiếp `hostBadgeText`, `readyStatusText`, `hostImg`, `guestImg` mà không cần bọc thêm panel phụ thừa.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Modified — Fixed CS0128 duplicate variable)
  - `Assets/Scripts/Network/NetworkPlayerSync.cs` (Modified)
  - `Assets/Scripts/Player/PlayerController.cs` (Modified)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified)
  - `Assets/Scripts/Network/FusionConnectionManager.cs` (Modified)
  - `Assets/Scripts/UI/NetworkLobbyHUD.cs` (Modified)
  - `Assets/Scripts/UI/NetworkPlayerSlot.cs` (Modified)
  - **Hỗ trợ 3D Player Model & Mesh ngoài HomeScreen Lobby**:
    - Trong [HomeScreen.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HomeScreen.cs):
      - Bổ sung trường `[Header("🧍 3D Lobby Player Model")] public GameObject lobbyPlayerModel;`.
      - Tự động ẩn Model khi mở các Panel con (Character Select, Multiplayer Lobby, Profile Info, Setting, Chapter Select, hoặc khi load sang gameplay) và chỉ hiện lại khi đang ở sảnh chính `HomeMenu`.
    - Trong [CharacterSelectionHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/CharacterSelectionHUD.cs):
      - Bổ sung `public GameObject lobbyPlayerModel;` và `public SkinnedMeshRenderer lobbySkinnedMesh;`.
      - Hoàn thiện phương thức `ApplyLobbyModelVisuals()` và `SetLobbyModelVisible(bool visible)`: tự động cập nhật Mesh (Nam/Nữ) và Material/Texture của nhân vật đã lưu lên 3D Model ngoài sảnh HomeMenu.
  - `Assets/Scripts/UI/HomeScreen.cs` (Modified)
  - `Assets/Scripts/UI/NetworkLobbyHUD.cs` (Modified — Restore lobby model on back)
  - `Assets/Scripts/UI/CharacterSelectionHUD.cs` (Modified)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified)
  - `Assets/Scripts/Player/ScenePlayerSpawner.cs` (Modified)
  - `Assets/Scripts/Network/NetworkPlayerSync.cs` (Modified)
  - `Assets/Scripts/Player/StarterAssetsInputs.cs` (Modified)
- **Ảnh hưởng**:
  - Model 3D nhân vật hiển thị trực quan ngoài sảnh HomeScreen, tự động đổi ngoại hình theo nhân vật đã chọn và luôn hiển thị lại chính xác khi thoát từ Multiplayer Lobby hoặc bất kỳ menu con nào về HomeMenu.

---

### [2026-09-25 13:30] — feat(network, gameplay, ui): network item sync, flashlight rpc, pause player count, on-screen status banner, and synchronized catch/death system
- **Tác vụ**:
  - **Đồng bộ nhặt / ném / thả vật phẩm qua mạng Photon Fusion (Network Item Sync)**:
    - Chuyển `NetworkItemSync` thành static helper đáng tin cậy (`GetLocalPlayerSync()`) để phát RPC thông qua `NetworkPlayerSync` của Local Player.
    - Bổ sung `RpcDespawnSceneItem`: Khi 1 người chơi nhặt đồ, ẩn ngay lập tức vật phẩm trên toàn bộ các máy khác trong phòng mà không cần đợi cầm lên tay.
    - Bổ sung `RpcShowDroppedSceneItem`: Đồng bộ hiển thị lại vật phẩm, gán vị trí/góc xoay và áp dụng vector vận tốc ném / lực xoáy vật lý (`linearVelocity`, `angularVelocity`) trên tất cả máy khi có người thả/ném đồ.
    - Tinh chỉnh `ClearRemoteHeldItem()`: Chỉ ẩn GameObject khi item vẫn đang nằm trên socket tay của Player, triệt tiêu lỗi item sau khi ném ra bị biến mất sau 1 giây.
    - Chuẩn hóa vị trí cầm item: Cố định cứng tọa độ `(0, 1.2, 0.5)` đồng bộ 100% giữa máy mình (`HotbarManager.cs`, `PlayerUI.prefab`) và máy người khác (`NetworkPlayerSync.cs`).
    - Cố định góc ngửa/cúi đèn pin bám theo góc nhìn Camera của người chơi qua mạng (`-NetworkHeadPitch`).
  - **Đồng bộ Đèn pin (Flashlight Controller)**:
    - Bổ sung `RpcSetFlashlightState`: Đồng bộ trạng thái Bật/Tắt chùm sáng (`Spotlight`) và âm thanh Click On/Off 3D Spatial Audio cho toàn bộ phòng.
  - **Hệ thống Status Banner trên màn hình & Cập nhật Pause Menu**:
    - Tạo mới `GameStatusHUD.cs`: Banner thông báo động nổi phía trên màn hình (với hiệu ứng Fade In/Out mượt mà) cho các sự kiện quan trọng trong trận (VD: `"[PlayerName] got caught by [NPCName]!"`).
    - Bổ sung `RpcBroadcastStatusMessage` trong `NetworkPlayerSync.cs` để phát thông báo đồng bộ lên màn hình của mọi người chơi trong phòng.
    - Cập nhật `PauseHUD.cs`: Hiển thị thông tin mã phòng và số lượng người chơi thời gian thực (`Room: Room_123 | Players: 3/4`).
    - Tinh chỉnh `UI_Manager.cs`: Nút `Menu()` tự động ngắt kết nối sạch sẽ qua `LeaveSession()` khi thoát trận để giải phóng slot cho người khác.
  - **Cơ chế Bắt giữ & Đồng bộ Hiệu ứng Tử vong (NPC Catch & Death System)**:
    - Tạo mới `NPCCatchPlayerTrigger.cs` (hỗ trợ quét bán kính khoảng cách hoặc va chạm Collider/Trigger): Mô phỏng AI/NPC/Bẫy chạm vào Player sẽ kích hoạt bắt giữ.
    - Bổ sung `RpcTriggerPlayerDeath` trong `NetworkPlayerSync.cs` và nâng cấp `PlayerDeathHandler.cs`:
      - Khi bị bắt: Kích hoạt animation chết (`Die`), bật đèn cảnh báo đỏ (`Warning Light`), và phát lực đẩy vật lý văng toàn bộ vật phẩm đang giữ trong balo ra sàn (`DropAllItemsOnDeath`).
      - Đồng bộ Còi cảnh sát hú (`Police Siren`) và âm thanh kêu chết (`Death Sound`) trên toàn bộ loa của người chơi trong phòng.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Network/NetworkItemSync.cs` (Rewritten static helper)
  - `Assets/Scripts/Network/NetworkPlayerSync.cs` (Added RpcDespawnSceneItem, RpcShowDroppedSceneItem, RpcSetFlashlightState, RpcBroadcastStatusMessage, RpcTriggerPlayerDeath)
  - `Assets/Scripts/Items/FlashlightController.cs` (Modified)
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified)
  - `Assets/Prefabs/HUD/PlayerUI.prefab` (Modified — fixedHoldPosition to 0, 1.2, 0.5)
  - `Assets/Scripts/UI/GameStatusHUD.cs` (New)
  - `Assets/Scripts/AI/NPCCatchPlayerTrigger.cs` (New)
  - `Assets/Scripts/Player/PlayerDeathHandler.cs` (Modified — ExecuteDeath & Network RPC)
  - `Assets/Scripts/UI/PauseHUD.cs` (Modified — Realtime player count & Room ID)
  - `Assets/Scripts/UI/UI_Manager.cs` (Modified — Clean LeaveSession on Menu)
- **Ảnh hưởng**:
  - Toàn bộ cơ chế nhặt/ném đồ, đèn pin, giao diện thông báo, còi cảnh sát và chết/bị bắt đều được đồng bộ hóa hoàn chỉnh qua mạng Photon Fusion.

### [2026-09-25 13:45] — feat(ladder): Synchronize LadderController for Multiplayer (Lock, 3D Sound, Fall On Pickup, Place/Drop State)
- **Tác vụ**:
  - **Cơ chế Thang giống Cửa & Vật phẩm đa năng**: Thang có thể nhặt vào túi/hotbar, bán trong shop, thả rơi tự do hoặc dựng vào tường/sàn để leo trèo.
  - **Khóa tương tác đơn người (Concurrency Lock)**: Chỉ cho phép 1 người leo thang cùng lúc (`isOccupied`). Người khác khi lại gần sẽ thấy thông báo *"Ladder is in use"* và bị chặn tương tác.
  - **Đồng bộ Âm thanh leo thang 3D (Spatial Audio Sync)**: Khi người chơi di chuyển lên/xuống thang, âm thanh bước chân leo thang được phát đồng bộ qua `RpcSetLadderAudio` cho tất cả người chơi trong phạm vi 3D.
  - **Tự động buông tay rơi tự do (Fall Off Ladder on Pickup/Drop)**: Nếu người chơi đang leo mà có người khác nhặt thang vào balo (hoặc thang bị ném/vứt/hủy kích hoạt), người đang leo sẽ lập tức buông tay (`FallOffLadder()`), bật lại vật lý trọng lực và rơi xuống tự nhiên mà không bị kẹt hay treo lơ lửng trên không trung.
  - **Phân biệt & Đồng bộ Đặt (Place) vs Ném (Drop)**:
    - **Đặt thang (Place)**: Khi nhắm vào chân tường/sàn, thang dựng thẳng đứng (`isPlaced = true`), khóa trục X/Z (`constraints`) và cho phép leo (`canClimb = true`).
    - **Ném thang (Drop/Throw)**: Khi ném ra không trung, thang ở trạng thái vật lý tự do (`isPlaced = false`, `constraints = None`), chỉ có thể nhặt lại chứ không thể leo trèo.
  - **Khóa trục thang khi đặt (Place Constraints) & Chống đẩy va chạm người chơi (Anti-push Collider)**:
    - Khi **Đặt thang (Place)**: Khóa cứng `FreezePositionX | FreezePositionZ | FreezeRotation` để thang đứng thẳng vững chắc, không bao giờ bị xô lệch hay bị đẩy khi người chơi va chạm, nhưng **thả tự do trục Y** (`PositionY`) để thang tự động rơi tiếp đất thẳng đứng nếu được đặt trên không trung.
    - Khi **Ném/Thả tự do (Drop)**: Bỏ qua va chạm vật lý giữa item và toàn bộ `PlayerController` (`IgnoreCollisionWithAllPlayers`), triệt tiêu hoàn toàn lỗi ném item trúng người chơi khác khiến item văng sai chỗ hoặc đẩy xô người chơi.
  - **Sửa lỗi Guest không trèo được thang sau khi nhặt & đặt lại**:
    - Nâng cấp `EnsureLocalPlayerReferences`: Đảm bảo `LadderController` luôn trỏ chính xác vào đúng thực thể Local Player của máy hiện tại thay vì trỏ nhầm sang Host (hoặc máy khác).
  - **Sửa lỗi Nút Drop/Place bị hiện sai sau khi thoát khỏi Thang**:
    - Trong [MobileActionButtons.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/MobileActionButtons.cs) (`SetClimbingMode`): Thay vì ép bật lại `dropButton` khi thoát thang, code kiểm tra xem Hotbar có đang thực sự chọn một vật phẩm không.
    - Trong [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs): Đảm bảo tự động tắt `dropButton` mỗi frame nếu không có slot vật phẩm nào đang được chọn.
  - **Đồng bộ Giá trị Vật phẩm & Điểm số khi Bán ($ Point Sync)**:
    - **Khởi tạo Giá ngẫu nhiên đồng bộ (Deterministic Random Seed)**: Trong [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs) (`InitializeStats`), sử dụng `Random.InitState(gameObject.name.GetHashCode())` để đảm bảo Host và Guest trên mọi máy đều tạo ra cùng một giá trị `Price` và `kg` chính xác 100% cho mỗi vật phẩm trong Scene.
    - **Đồng bộ Điểm bán qua Mạng (`RpcSyncSellItem`)**: Khi 1 người ném đồ vào vùng xe giao hàng (`home`), RPC `RpcSyncSellItem` được kích hoạt trên toàn phòng:
      - Cộng chính xác và bằng nhau 100% số điểm (`currpoint += earnedPoints`) cho tất cả người chơi.
      - Hiện banner thông báo nổi trên màn hình (`"Sold [ItemName] (+$[Price])!"`).
      - Tự động hủy sạch đối tượng vật phẩm trên mọi máy và ngăn chặn kích hoạt bán lặp lại (`_isSold`).
  - **Hiển thị Toàn thân Nhân vật khi Chết/Bị bắt (Death Model Visibility)**:
    - Trong [PlayerDeathHandler.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerDeathHandler.cs) (`ExecuteDeath`): Khi người chơi bị bắt hoặc chết, hệ thống lập tức gọi `netSync.SetLocalMeshVisibility(true)` và kích hoạt lại toàn bộ `SkinnedMeshRenderer` / `Renderer` của bản thân (`shadowCastingMode = On`).
    - Giúp người chơi ở góc nhìn FPV (vốn ẩn mesh của bản thân khi chơi) nhìn thấy rõ ràng toàn bộ cơ thể nhân vật và chuyển động ngã gục khi Camera kéo lùi ra sau.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/Player/PlayerDeathHandler.cs` (Modified — Full mesh visibility on ExecuteDeath and Start fallback)
  - `Assets/Scripts/Items/Item.cs` (Modified — Deterministic seed InitializeStats, _isSold guard, SyncSellItem on trigger)
  - `Assets/Scripts/Network/NetworkPlayerSync.cs` (Modified — Added RpcSyncSellItem)
  - `Assets/Scripts/Network/NetworkItemSync.cs` (Modified — Added SyncSellItem)
  - `Assets/Scripts/Items/LadderController.cs` (Modified — EnsureUpright freeze X/Z/Rot, EnsureLocalPlayerReferences, clean SetPlaced reset, StartClimbing parameter)
  - `Assets/Scripts/UI/HotbarManager.cs` (Modified — Added IgnoreCollisionWithAllPlayers, Place vs Throw logic, customHoldOffset/Rotation, Update dropButton hide)
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Modified — Fix dropButton visibility in SetClimbingMode)
- **Ảnh hưởng**:
  - Khi chết hoặc bị bắt, toàn bộ người chơi (cả bản thân và người khác) đều thấy rõ mô hình 3D ngã gục cùng góc nhìn camera kéo lùi mượt mà.





























