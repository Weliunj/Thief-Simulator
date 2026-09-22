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
  - Tích hợp mở khóa con trỏ chuột xuyên suốt toàn bộ game (`Cursor.lockState = CursorLockMode.None`, `Cursor.visible = true`) trong `Awake()`, `Start()`, `OnEnable()` của [HomeScreen.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HomeScreen.cs), [ChapterSelectManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ChapterSelectManager.cs), [SettingsHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/SettingsHUD.cs), [PlayerController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerController.cs) và [UI_Manager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/UI_Manager.cs).
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
      - Khi **Đặt** (Raycast chạm tường/sàn): Gọi `ladder.SetUprightLocked(true)` khóa hoàn toàn trục `FreezeRotationX | FreezeRotationZ`, không áp lực đẩy để thang đứng vững vàng 100% không bị ngã đổ.
      - Khi **Ném** (Raycast không chạm vật cản): Gọi `ladder.SetUprightLocked(false)` mở khóa toàn bộ `RigidbodyConstraints.None`, áp dụng lực ném `dropForwardForce` về phía trước giúp thang lật xoay và đổ ngã vật lý tự nhiên.
      - Khóa hoàn toàn va chạm vật lý giữa Player và Thang (`Physics.IgnoreCollision`) giúp người chơi không thể đi/chạy bộ lên thang nghiêng như dốc cầu thang, buộc phải bấm phím leo (`F` / `Interact`) mới có thể trèo lên cao.
      - Hỗ trợ đầy đủ đa tương tác trên cùng một đối tượng (Multi-IInteractable): Khi nhìn vào Thang vừa hiển thị nút **Pickup** (nhặt thang vào Hotbar) vừa hiển thị nút **Interact** (leo thang), khắc phục lỗi thang khi đặt (`Place`) bị mất nút leo.
  - Sửa lỗi tương tác Cửa (Door) và Minigame trong [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs) & [LockpickMinigame.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/LockpickMinigame.cs):
    - Khắc phục lỗi nhìn vào tường nhà vẫn bắt trúng Cửa: Loại bỏ hàm tìm con `GetComponentsInChildren` nguy hiểm trên Collider va chạm (trước đây bắn trúng tường nhà `House` sẽ quét xuống tất cả cửa con bên trong).
    - Bổ sung cơ chế kiểm tra tầm nhìn (Line-of-Sight occlusion check): Nếu có tường/vật cản che chắn trực tiếp trước mặt, tia quét sẽ bị chặn lại và không thể tương tác xuyên thấu qua tường.
  - Phát triển hệ thống tương tác vật phẩm cầm tay (Held Item Interaction) và [FlashlightController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/FlashlightController.cs):
    - Tạo mới interface [IHeldInteractable.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/IHeldInteractable.cs) chuẩn hóa cho mọi vật phẩm có thể tương tác trực tiếp khi đang cầm trên tay (Đèn pin, Thuốc, Súng, Scanner...).
    - Tái cấu trúc [FlashlightController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/FlashlightController.cs) & [LadderController.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/LadderController.cs):
      - Loại bỏ hoàn toàn các biến trùng lặp, đồng bộ hóa 100% việc lấy dữ liệu (Tên, Mô tả, Icon, Giá tiền, Cân nặng, Độ hiếm) từ component `Item` và ScriptableObject (`ItemSO`).
      - Bổ sung `lightLocalOffset` và `lightLocalRotation` cho Đèn pin: Giúp dễ dàng vi chỉnh vị trí và góc xoay hướng chiếu sáng (xoay 90 độ nếu nòng 3D Model bị dựng đứng trên trục Y) mà không cần tạo GameObject phụ thủ công.
    - Cập nhật [Item.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Items/Item.cs) & [HotbarManager.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/HotbarManager.cs):
      - Hỗ trợ `followCameraPitch`, `customHoldOffset`, `customHoldRotation` riêng cho từng Item.
      - Đối với Đèn pin (`followCameraPitch = true`): Góc xoay ngửa lên / cúi xuống bám sát 100% theo hướng nhìn Camera, đảm bảo luồng sáng luôn chiếu đúng tâm ngắm.
    - Cập nhật [PlayerInteraction.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/PlayerInteraction.cs):
      - Giải quyết triệt để xung đột nút tương tác: Dùng chung 1 nút `Interact` duy nhất (phím F / nút Interact trên Mobile) theo cơ chế chuyển đổi ngữ cảnh thông minh:
        - Khi nhìn vào Thang / Cửa: Tự động ưu tiên **Leo Thang / Mở Cửa**.
    - Cập nhật [ItemInfoHUD.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/UI/ItemInfoHUD.cs) & [MobileActionButtons.cs](file:///c:/Users/Hi/Documents/Unity%20Project/Thief-Simulator/Assets/Scripts/Player/MobileActionButtons.cs):
      - Phân tách riêng biệt giữa **Tên đối tượng (`nameText`)** và **Hành động (`actionPromptText`)**. `nameText` chỉ thuần túy chứa tên đối tượng (VD: *"Flashlight"*, *"Ladder"*, *"Wooden Locked Door"*).
      - `MobileActionButtons.SetInteractPrompt()` tự động quét tìm child object `Prompt` (như `InteractBtn -> Prompt`) để gán chính xác câu lệnh hành động (như `"Turn On"`, `"Turn Off"`, `"Climb Up"`, `"Pick Lock"`) lên nút Mobile UI.
- **Danh sách file thay đổi**:
  - `Assets/Scripts/UI/UIEventSystemFixer.cs` (New)
  - `Assets/Scripts/Items/IHeldInteractable.cs` (New)
  - `Assets/Scripts/Items/FlashlightController.cs` (New)
  - `Assets/Scripts/Items/Item.cs` (Lines 15-35)
  - `Assets/Scripts/UI/HomeScreen.cs` (Lines 20-35)
  - `Assets/Scripts/UI/ChapterSelectManager.cs` (Lines 40-55)
  - `Assets/Scripts/UI/SettingsHUD.cs` (Lines 105-115)
  - `Assets/Scripts/Player/PlayerController.cs` (Lines 200-210)
  - `Assets/Scripts/Player/PlayerInteraction.cs` (Lines 30-320)
  - `Assets/Scripts/UI/UI_Manager.cs` (Lines 290-415, 440-445)
  - `Assets/Scripts/Player/StarterAssetsInputs.cs` (Rewritten / Enhanced)
  - `Assets/Scripts/UI/HotbarManager.cs` (Lines 50-310, 580-605, 760-815)
  - `Assets/Scripts/Items/LadderController.cs` (Lines 90-145, 595-605)
  - `Assets/Scripts/UI/LockpickMinigame.cs` (Lines 45-55, 225-305)
  - `Assets/Scripts/UI/MainHUD.cs` (Lines 15-200 — Streamlined)
  - `Assets/Scripts/UI/ItemInfoHUD.cs` (Lines 200-220)
  - `Assets/Scripts/Player/MobileActionButtons.cs` (Lines 180-210)
- **Ảnh hưởng**:
  - Chuột luôn luôn hiển thị và không bị khóa trong suốt toàn bộ quá trình chơi game từ Menu đến Gameplay.
  - Tester có thể vừa dùng WASD + Space để di chuyển/nhảy, vừa dùng chuột click trực tiếp vào mọi nút trên màn hình (Pickup, Interact, Drop, Sprint, Crouch, Flashlight, Hotbar, Pause) y hệt như thao tác chạm trên điện thoại, giúp quá trình build EXE và test nhanh hơn rất nhiều so me với build APK.
  - Minigame bẻ khóa có độ chính xác và thử thách chuẩn hơn, tránh việc người chơi bấm ăn may khi 2 hình tròn vừa chạm mép nhau.
  - Giao diện HUD chính hiển thị trực quan và tinh gọn, loại bỏ các biến rác trong Inspector.
  - Thao tác đặt/ném đồ trực quan, phân biệt rõ ràng giữa hành vi Đặt vào tường (khóa đứng thang) và Ném ra ngoài khoảng không (lật đổ vật lý tự nhiên).
  - Tương tác cửa chuẩn xác 100%: không bị quét xuyên tường khi nhìn chỗ khác, cửa mở xong sẽ không hiện minigame bẻ khóa lại và âm thanh chiến thắng chỉ phát đúng 1 lần.
  - Thang đặt ra thế giới luôn hiện đầy đủ nút Leo trèo (Interact) và nút Nhặt đồ (Pickup), đồng thời khóa va chạm để không bị đi bộ dẫm lên thang nghiêng.
  - Đèn pin hoạt động như một Special Item hoàn chỉnh: ngửa camera lên xuống thì chùm sáng bám theo chuẩn xác, bật/tắt bằng phím F / nút Interact mà không hề bị xung đột với Thang và Cửa.
  - Chuỗi hành động động `GetActionPrompt()` và `GetHeldActionPrompt()` hiển thị đầy đủ, chính xác lên HUD và nhãn nút Mobile giúp người chơi biết rõ bấm phím/nút sẽ thực hiện hành động gì.






