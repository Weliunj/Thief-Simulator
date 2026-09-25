using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hệ thống Hotbar:
/// - Thêm item vào slot từ trái qua phải khi nhặt
/// - Chọn item: đổi màu nền slot sang Vàng, hiển thị model 3D trước mặt player/camera
/// - Hiển thị thông tin Item đang chọn (Tên, Giá, Cân nặng) lên nameSlotItem, priceSlotItem, weightSlotItem
/// - Hiện nút Drop khi đang chọn item
/// - Thả item: Tùy chỉnh lực ném/thả tự nhiên + Raycast chống thả xuyên/lọt tường
/// </summary>
public class HotbarManager : MonoBehaviour
{
    [Header("📌 Slot References")]
    [Tooltip("Danh sách các slot trong Hotbar (tự động tìm từ trái qua phải nếu để trống)")]
    public List<HotbarSlot> slots = new List<HotbarSlot>();

    [Tooltip("Container chứa các slot (VD: GroupLayout)")]
    public Transform slotsContainer;

    [Header("📝 Held Item Info UI (Hiển thị thông tin item đang chọn)")]
    [Tooltip("Panel chứa thông tin item (nếu có)")]
    public GameObject heldItemInfoPanel;

    [Tooltip("Text hiển thị tên của item đang chọn")]
    public TextMeshProUGUI nameSlotItem;

    [Tooltip("Text hiển thị giá ($) của item đang chọn")]
    public TextMeshProUGUI priceSlotItem;

    [Tooltip("Text hiển thị khối lượng (Kg) của item đang chọn")]
    public TextMeshProUGUI weightSlotItem;

    [Tooltip("Text hiển thị độ hiếm của item đang chọn")]
    public TextMeshProUGUI raritySlotItem;

    public enum HoldFollowMode
    {
        PlayerMesh,     // Bám theo thân nhân vật (góc xoay và di chuyển có độ trễ mượt giống hệt góc quay của Mesh người chơi)
        SmoothCamera    // Bám theo Camera nhưng có độ trễ mượt (Sway / Inertia) không bị khóa cứng vào camera
    }

    [Header("📦 Item Hold Settings (Hiển thị model trước mặt)")]
    [Tooltip("Chế độ bám của vật phẩm: PlayerMesh (mượt theo thân người chơi) hoặc SmoothCamera")]
    public HoldFollowMode followMode = HoldFollowMode.PlayerMesh;

    [Tooltip("Vị trí hiển thị model vật phẩm trước mặt (Camera hoặc Player)")]
    public Transform itemHoldPoint;

    [Tooltip("Offset vị trí cầm item so với thân nhân vật Player (chế độ PlayerMesh)")]
    public Vector3 playerHoldOffset = new Vector3(0.25f, 1.05f, 0.45f);

    [Tooltip("Offset vị trí cầm item so với Camera (chế độ SmoothCamera)")]
    public Vector3 holdPointOffset = new Vector3(0.2f, -0.25f, 0.55f);

    [Tooltip("Góc xoay 3D của vật phẩm khi cầm trên tay")]
    public Vector3 holdPointRotation = new Vector3(5f, -15f, 0f);

    [Tooltip("Tốc độ trễ xoay mượt (càng nhỏ càng trễ nhiều, tạo quán tính giống mesh)")]
    [Range(1f, 30f)]
    public float rotationSmoothSpeed = 12f;

    [Tooltip("Tốc độ trễ di chuyển mượt")]
    [Range(1f, 30f)]
    public float positionSmoothSpeed = 15f;

    [Header("🔽 Drop & Placement Settings")]
    [Tooltip("Nút bấm Thả/Đặt (Drop/Place) - chỉ hiện khi đang chọn item")]
    public Button dropButton;

    [Tooltip("Image component hiển thị icon trên nút Drop/Place")]
    public Image dropButtonIcon;

    [Tooltip("Sprite icon hiển thị khi ở trạng thái Ném (không có vật cản)")]
    public Sprite throwIcon;

    [Tooltip("Sprite icon hiển thị khi ở trạng thái Đặt (chạm tường/sàn)")]
    public Sprite placeIcon;

    [Tooltip("Góc xoay Z của icon khi ở trạng thái Ném (mặc định 90 độ)")]
    public float throwRotationZ = 90f;

    [Tooltip("Góc xoay Z của icon khi ở trạng thái Đặt (mặc định 180 độ)")]
    public float placeRotationZ = 180f;

    [Tooltip("GameObject UI hiển thị trên màn hình khi ở trạng thái Đặt (Place), tự động ẩn/hiện như nút Pickup")]
    public GameObject placementIndicator;

    [Tooltip("Lực ném về phía trước khi không có vật cản")]
    [Range(0f, 10f)]
    public float dropForwardForce = 2.5f;

    [Tooltip("Khoảng cách tối đa kiểm tra tia raycast phía trước")]
    public float maxDropDistance = 2.5f;

    [Tooltip("LayerMask quét vật cản/tường khi thả item")]
    public LayerMask obstacleLayerMask = ~0;

    [Tooltip("Vẽ tia Raycast debug trong Scene View khi đang chọn item")]
    public bool showDebugRay = true;

    [Header("📱 Mobile & Player References")]
    public MobileActionButtons mobileActions;
    public PlayerController playerController;

    [Header("🎯 Trạng thái")]
    public int currentSelectedIndex = -1;

    private Camera mainCamera;
    private GameObject currentHeldModel;
    private List<Collider> disabledColliders = new List<Collider>();
    private bool isFirstFrameHeld = false;
    private bool lastFoundObstacle = false;
    private RaycastHit lastValidHit;

    void Awake()
    {
        InitializeSlots();
        InitializeHoldPoint();
        InitializeHeldItemInfoUI();
    }

    void OnDestroy()
    {
        HideHeldModel();

        if (itemHoldPoint != null)
        {
            for (int i = itemHoldPoint.childCount - 1; i >= 0; i--)
            {
                Transform child = itemHoldPoint.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            // Hủy luôn GameObject ItemHoldPoint nếu được tạo động dưới Camera để tránh rác
            if (itemHoldPoint.gameObject != null)
            {
                Destroy(itemHoldPoint.gameObject);
            }
            itemHoldPoint = null;
        }
    }

    /// <summary>
    /// Bỏ qua va chạm vật lý giữa vật phẩm và tất cả người chơi để chống đẩy văng/kẹt người khi ném/thả đồ
    /// </summary>
    public static void IgnoreCollisionWithAllPlayers(GameObject itemObj, bool ignore = true)
    {
        if (itemObj == null) return;
        var itemColliders = itemObj.GetComponentsInChildren<Collider>(true);
        if (itemColliders == null || itemColliders.Length == 0) return;

        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player == null) continue;
            var playerColliders = player.GetComponentsInChildren<Collider>(true);
            foreach (var pCol in playerColliders)
            {
                if (pCol == null) continue;
                foreach (var iCol in itemColliders)
                {
                    if (iCol == null || iCol == pCol) continue;
                    Physics.IgnoreCollision(pCol, iCol, ignore);
                }
            }
        }
    }

    /// <summary>
    /// Đảm bảo luôn trỏ đúng vào Local Player (tránh trỏ nhầm sang người chơi khác trong phòng Multiplayer)
    /// </summary>
    public void EnsureLocalPlayerController()
    {
        if (playerController != null)
        {
            var net = playerController.GetComponent<NetworkPlayerSync>();
            if (net == null || net.IsLocalPlayer)
            {
                return;
            }
        }

        var allControllers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pc in allControllers)
        {
            var net = pc.GetComponent<NetworkPlayerSync>();
            if (net == null || net.IsLocalPlayer)
            {
                playerController = pc;
                return;
            }
        }

        if (allControllers.Length > 0 && playerController == null)
        {
            playerController = allControllers[0];
        }
    }

    public void RebindPlayer(PlayerController pc)
    {
        if (pc != null)
        {
            playerController = pc;
            InitializeHoldPoint();
        }
    }

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        }

        EnsureLocalPlayerController();

        if (mobileActions == null)
        {
            mobileActions = FindFirstObjectByType<MobileActionButtons>();
        }

        if (dropButton == null && mobileActions != null)
        {
            dropButton = mobileActions.dropButton;
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(DropSelectedItem);
            dropButton.gameObject.SetActive(false); // Ẩn mặc định

            if (dropButtonIcon == null)
            {
                var imgs = dropButton.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img != dropButton.image)
                    {
                        dropButtonIcon = img;
                        break;
                    }
                }
                if (dropButtonIcon == null) dropButtonIcon = dropButton.image;
            }
        }

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }

        // LayerMask mặc định loại trừ Player, UI, TransparentFX
        if (obstacleLayerMask == ~0)
        {
            obstacleLayerMask = ~(LayerMask.GetMask("Player", "Ignore Raycast", "UI", "TransparentFX"));
        }
    }

    void Update()
    {
        HandleKeyboardInput();

        // Kiểm tra nút drop từ MobileActionButtons
        if (mobileActions != null && mobileActions.dropPressed)
        {
            DropSelectedItem();
        }

        // Kiểm tra raycast kiểm tra vật cản và cập nhật UI Đặt / Ném
        if (currentSelectedIndex >= 0 && mainCamera != null)
        {
            Vector3 dir = mainCamera.transform.forward;
            Vector3 origin = mainCamera.transform.position;
            GameObject currentItem = GetCurrentHeldModel();

            RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDropDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit validHit = default;
            bool foundObstacle = false;

            foreach (var h in hits)
            {
                if (currentItem != null && h.collider.transform.IsChildOf(currentItem.transform)) continue;
                if (playerController != null && h.collider.transform.IsChildOf(playerController.transform)) continue;

                validHit = h;
                foundObstacle = true;
                break;
            }

            lastFoundObstacle = foundObstacle;
            lastValidHit = validHit;

            // Cập nhật icon nút Drop (Đặt vs Ném)
            UpdateDropButtonVisual(foundObstacle);

            // Bật/tắt GameObject hiển thị trên UI khi ở trạng thái Đặt (Place)
            if (placementIndicator != null)
            {
                if (placementIndicator.activeSelf != foundObstacle)
                {
                    placementIndicator.SetActive(foundObstacle);
                }
            }

            if (showDebugRay)
            {
                if (foundObstacle)
                {
                    // Màu Đỏ: Phát hiện vật cản trước mặt (sẽ đặt item tại chỗ chạm)
                    Debug.DrawLine(origin, validHit.point, Color.red);
                }
                else
                {
                    // Màu Xanh Lá: Không gian thoáng (sẽ ném item về phía trước)
                    Debug.DrawRay(origin, dir * maxDropDistance, Color.green);
                }
            }
        }
        else
        {
            if (placementIndicator != null && placementIndicator.activeSelf)
            {
                placementIndicator.SetActive(false);
            }
            if (dropButton != null && dropButton.gameObject.activeSelf)
            {
                dropButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Cập nhật hiển thị icon nút thả (Ném vs Đặt) và góc xoay tương ứng
    /// </summary>
    public void UpdateDropButtonVisual(bool hasObstacle)
    {
        if (dropButtonIcon != null)
        {
            if (hasObstacle)
            {
                if (placeIcon != null) dropButtonIcon.sprite = placeIcon;
                dropButtonIcon.rectTransform.localEulerAngles = new Vector3(0f, 0f, placeRotationZ);
            }
            else
            {
                if (throwIcon != null) dropButtonIcon.sprite = throwIcon;
                dropButtonIcon.rectTransform.localEulerAngles = new Vector3(0f, 0f, throwRotationZ);
            }
        }
    }

    void LateUpdate()
    {
        UpdateHeldModelTransform();
    }

    /// <summary>
    /// Cập nhật vị trí và góc xoay của item đang cầm mỗi frame:
    /// - PlayerMesh: Bám theo thân nhân vật với độ trễ xoay tự nhiên giống hệt góc quay của Mesh người chơi.
    /// - SmoothCamera: Bám theo Camera nhưng có độ trễ mượt (Interpolation / Sway / Inertia), không bị khóa cứng góc.
    /// </summary>
    public void UpdateHeldModelTransform()
    {
        if (currentHeldModel == null || !currentHeldModel.activeSelf) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        }

        EnsureLocalPlayerController();

        Item item = currentHeldModel.GetComponent<Item>();
        bool followPitch = (item != null && item.followCameraPitch);

        Transform holdSocket = EnsureFixedHoldPoint();
        if (holdSocket != null)
        {
            if (followPitch && mainCamera != null)
            {
                Vector3 camForward = mainCamera.transform.forward;
                float rawPitch = Mathf.Asin(Mathf.Clamp(camForward.y, -1f, 1f)) * Mathf.Rad2Deg;
                holdSocket.localRotation = Quaternion.Euler(-rawPitch, 0f, 0f);
            }
            else
            {
                holdSocket.localRotation = Quaternion.Euler(fixedHoldRotation);
            }
        }
    }

    /// <summary>
    /// Lấy component IHeldInteractable của vật phẩm đang cầm trên tay (nếu có)
    /// </summary>
    public IHeldInteractable GetHeldInteractable()
    {
        if (currentHeldModel == null) return null;
        return currentHeldModel.GetComponent<IHeldInteractable>() ?? currentHeldModel.GetComponentInChildren<IHeldInteractable>();
    }

    /// <summary>
    /// Khởi tạo và liên kết các HotbarSlot trong GroupLayout
    /// </summary>
    public void InitializeSlots()
    {
        if (slots == null) slots = new List<HotbarSlot>();

        if (slots.Count == 0)
        {
            Transform container = (slotsContainer != null) ? slotsContainer : transform;
            HotbarSlot[] foundSlots = container.GetComponentsInChildren<HotbarSlot>(true);

            if (foundSlots != null && foundSlots.Length > 0)
            {
                slots.AddRange(foundSlots);
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Initialize(this, i);
            }
        }
    }

    private void InitializeHoldPoint()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        }

        EnsureLocalPlayerController();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        }

        if (itemHoldPoint == null)
        {
            GameObject hpObj = new GameObject("ItemHoldPoint");
            hpObj.transform.SetParent(null); // World space - tách độc lập khỏi Camera để nội suy mượt theo góc mesh
            itemHoldPoint = hpObj.transform;
        }

        // Tách an toàn mọi child nếu có (KHÔNG gọi Destroy để tuyệt đối không xóa nhầm item trong inventory)
        if (currentHeldModel == null && itemHoldPoint != null)
        {
            for (int i = itemHoldPoint.childCount - 1; i >= 0; i--)
            {
                Transform child = itemHoldPoint.GetChild(i);
                if (child != null)
                {
                    child.SetParent(null);
                    child.gameObject.SetActive(false);
                }
            }
        }

        currentHeldModel = null;
        UpdateHeldModelTransform();
    }

    /// <summary>
    /// Khởi tạo và tự động tìm kiếm các Text UI thông tin vật phẩm đang cầm nếu chưa gán
    /// </summary>
    private void InitializeHeldItemInfoUI()
    {
        if (nameSlotItem == null || priceSlotItem == null || weightSlotItem == null || raritySlotItem == null)
        {
            Transform searchRoot = transform.root != null ? transform.root : transform;
            var tmps = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                string lower = t.gameObject.name.ToLower();
                if (nameSlotItem == null && (lower == "nameslotitem" || lower.Contains("nameslot")))
                    nameSlotItem = t;
                else if (priceSlotItem == null && (lower == "priceslotitem" || lower.Contains("priceslot")))
                    priceSlotItem = t;
                else if (weightSlotItem == null && (lower == "weightslotitem" || lower.Contains("weightslot")))
                    weightSlotItem = t;
                else if (raritySlotItem == null && (lower == "rarityslotitem" || lower.Contains("rarity")))
                    raritySlotItem = t;
            }
        }

        UpdateHeldItemInfoUI(null, null);
    }

    /// <summary>
    /// Cập nhật hiển thị Tên, Giá, Cân nặng, Độ hiếm của item đang được chọn/cầm lên UI
    /// </summary>
    public void UpdateHeldItemInfoUI(GameObject itemObj, Item item)
    {
        if (item == null && itemObj != null)
        {
            item = itemObj.GetComponent<Item>();
        }

        if (itemObj != null && item != null)
        {
            if (heldItemInfoPanel != null) heldItemInfoPanel.SetActive(true);

            if (nameSlotItem != null)
            {
                nameSlotItem.gameObject.SetActive(true);
                nameSlotItem.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName : itemObj.name.Replace("(Clone)", "").Trim();
            }

            if (priceSlotItem != null)
            {
                priceSlotItem.gameObject.SetActive(true);
                priceSlotItem.text = $"${item.Price:F0}";
            }

            if (weightSlotItem != null)
            {
                weightSlotItem.gameObject.SetActive(true);
                weightSlotItem.text = $"{item.kg}Kg";
            }

            if (raritySlotItem != null)
            {
                raritySlotItem.gameObject.SetActive(true);
                raritySlotItem.text = item.rarity.GetDisplayName();
                raritySlotItem.color = item.rarity.GetColor();
            }
        }
        else
        {
            if (heldItemInfoPanel != null) heldItemInfoPanel.SetActive(false);

            if (nameSlotItem != null)
            {
                nameSlotItem.text = "";
                nameSlotItem.gameObject.SetActive(false);
            }

            if (priceSlotItem != null)
            {
                priceSlotItem.text = "";
                priceSlotItem.gameObject.SetActive(false);
            }

            if (weightSlotItem != null)
            {
                weightSlotItem.text = "";
                weightSlotItem.gameObject.SetActive(false);
            }

            if (raritySlotItem != null)
            {
                raritySlotItem.text = "";
                raritySlotItem.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Trả về GameObject item đang được hiển thị trước mặt (held model).
    /// PlayerController.TakeItem() cần dùng để KHÔNG tắt item này.
    /// </summary>
    public GameObject GetCurrentHeldModel()
    {
        return currentHeldModel;
    }

    /// <summary>
    /// Kiểm tra xem Hotbar còn slot trống không
    /// </summary>
    public bool HasEmptySlot()
    {
        foreach (var slot in slots)
        {
            if (slot != null && !slot.HasItem()) return true;
        }
        return false;
    }

    /// <summary>
    /// Thêm vật phẩm vào slot trống đầu tiên từ trái sang phải
    /// </summary>
    public bool AddItem(GameObject itemObj, Item item)
    {
        if (itemObj == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && !slots[i].HasItem())
            {
                slots[i].SetItem(item, itemObj);

                // Tắt physics và ẩn object an toàn
                Rigidbody rb = itemObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.isKinematic = true;
                }
                itemObj.SetActive(false);

                Debug.Log($"<color=cyan>Đã thêm {itemObj.name} vào Hotbar Slot {i + 1}</color>");
                return true;
            }
        }

        Debug.LogWarning("Hotbar đã đầy!");
        return false;
    }

    /// <summary>
    /// Xử lý khi click vào 1 slot
    /// </summary>
    public void OnSlotClicked(int index)
    {
        if (index < 0 || index >= slots.Count) return;

        // Đang leo thang -> Không cho cầm/đổi item trên tay
        if (playerController != null && playerController.isClimbingLadder) return;

        if (currentSelectedIndex == index)
        {
            // Bấm lại vào slot đang chọn -> Bỏ chọn
            DeselectAll();
        }
        else
        {
            SelectSlot(index);
        }
    }

    /// <summary>
    /// Chọn 1 slot theo index
    /// </summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;

        // Đang leo thang -> Không cho cầm item trên tay
        if (playerController != null && playerController.isClimbingLadder) return;

        // Bỏ chọn slot cũ
        if (currentSelectedIndex >= 0 && currentSelectedIndex < slots.Count)
        {
            if (slots[currentSelectedIndex] != null)
                slots[currentSelectedIndex].SetSelected(false);
        }

        currentSelectedIndex = index;
        HotbarSlot activeSlot = slots[index];

        if (activeSlot != null)
        {
            activeSlot.SetSelected(true);

            if (activeSlot.HasItem())
            {
                // Hiển thị model 3D trước mặt
                ShowHeldModel(activeSlot.itemObject);

                // Cập nhật thông tin Tên, Giá, Cân nặng lên UI
                UpdateHeldItemInfoUI(activeSlot.itemObject, activeSlot.itemData);

                // Hiện nút Drop
                if (dropButton != null) dropButton.gameObject.SetActive(true);
            }
            else
            {
                // Slot trống -> ẩn model, ẩn info và nút Drop
                HideHeldModel();
                UpdateHeldItemInfoUI(null, null);
                if (dropButton != null) dropButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Bỏ chọn tất cả slot
    /// </summary>
    public void DeselectAll()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < slots.Count)
        {
            if (slots[currentSelectedIndex] != null)
                slots[currentSelectedIndex].SetSelected(false);
        }

        currentSelectedIndex = -1;
        HideHeldModel();
        UpdateHeldItemInfoUI(null, null);

        if (placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }

        if (dropButton != null)
        {
            dropButton.gameObject.SetActive(false);
        }
    }

    [Header("🎯 Held Item Settings (Cố định vị trí trước mặt)")]
    public Vector3 fixedHoldPosition = new Vector3(0f, 1.2f, 0.5f);
    public Vector3 fixedHoldRotation = Vector3.zero;
    private Transform EnsureFixedHoldPoint()
    {
        EnsureLocalPlayerController();
        if (playerController == null) return null;
        Transform playerTransform = playerController.transform;
        // Tìm hoặc tạo mới điểm neo cố định dưới Player
        Transform holdPoint = playerTransform.Find("FixedItemHoldSocket");
        if (holdPoint == null)
        {
            GameObject socketObj = new GameObject("FixedItemHoldSocket");
            socketObj.transform.SetParent(playerTransform, false);
            holdPoint = socketObj.transform;
        }
        // Luôn ghim cứng vị trí và góc xoay
        holdPoint.localPosition = fixedHoldPosition;
        holdPoint.localRotation = Quaternion.Euler(fixedHoldRotation);
        holdPoint.localScale = Vector3.one;
        return holdPoint;
    }

    /// <summary>
    /// Hiển thị mô hình 3D của item trước mặt player/camera
    /// </summary>
    private void ShowHeldModel(GameObject itemObj)
    {
        HideHeldModel();
        if (itemObj == null) return;
        Transform holdSocket = EnsureFixedHoldPoint();
        if (holdSocket == null) return;
        currentHeldModel = itemObj;
        // Reset sạch sẽ physics cũ
        var rb = itemObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }
        // Tắt collider để tránh cấn va đập với player
        disabledColliders.Clear();
        var colliders = itemObj.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (c != null && c.enabled)
            {
                c.enabled = false;
                disabledColliders.Add(c);
            }
        }
        // Gắn vào socket và áp dụng Custom Hold Offset & Rotation nếu item có cài đặt riêng
        Item itemData = itemObj.GetComponent<Item>() ?? itemObj.GetComponentInChildren<Item>();
        Vector3 customPos = (itemData != null && itemData.useCustomHoldOffset) ? itemData.customHoldOffset : Vector3.zero;
        Quaternion customRot = (itemData != null && itemData.useCustomHoldRotation) ? Quaternion.Euler(itemData.customHoldRotation) : Quaternion.identity;

        itemObj.transform.SetParent(holdSocket, false);
        itemObj.transform.localPosition = customPos;
        itemObj.transform.localRotation = customRot;
        itemObj.transform.localScale = Vector3.one;
        // Bù trừ scale nếu Player cha bị scale khác (1,1,1)
        Vector3 parentLossy = holdSocket.lossyScale;
        if (parentLossy.x != 0 && parentLossy.y != 0 && parentLossy.z != 0)
        {
            itemObj.transform.localScale = new Vector3(1f / parentLossy.x, 1f / parentLossy.y, 1f / parentLossy.z);
        }
        var renderers = itemObj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) if (r != null) r.enabled = true;
        itemObj.SetActive(true);
        // Đồng bộ mạng Photon Fusion
        EnsureLocalPlayerController();
        if (playerController != null && playerController.TryGetComponent<NetworkPlayerSync>(out var netSync))
        {
            netSync.SetHeldItem(itemObj);
        }
    }

    /// <summary>
    /// Ẩn mô hình 3D đang cầm trên tay
    /// </summary>
    private void HideHeldModel()
    {
        if (currentHeldModel != null)
        {
            EnsureLocalPlayerController();
            if (playerController != null && playerController.TryGetComponent<NetworkPlayerSync>(out var netSync))
            {
                netSync.SetHeldItem(null);
            }

            // Bật lại collider đã tắt
            foreach (var col in disabledColliders)
            {
                if (col != null) col.enabled = true;
            }
            disabledColliders.Clear();

            currentHeldModel.SetActive(false);
            currentHeldModel.transform.SetParent(null); // Tách an toàn khỏi itemHoldPoint để không bị xóa nhầm
            currentHeldModel = null;
        }
    }

    /// <summary>
    /// Thả vật phẩm đang được chọn
    /// Có Raycast kiểm tra va chạm phía trước + thêm lực ném/thả tự nhiên
    /// </summary>
    public void DropSelectedItem()
    {
        if (currentSelectedIndex < 0 || currentSelectedIndex >= slots.Count) return;
        HotbarSlot selectedSlot = slots[currentSelectedIndex];
        if (selectedSlot == null || !selectedSlot.HasItem())
        {
            if (selectedSlot != null) selectedSlot.ClearSlot();
            DeselectAll();
            return;
        }
        GameObject itemObj = selectedSlot.itemObject;
        Item itemComp = selectedSlot.itemData;
        // 1. Xóa khỏi UI Slot
        selectedSlot.ClearSlot();
        if (itemObj != null)
        {
            // Bật lại collider & renderer
            foreach (var col in disabledColliders)
            {
                if (col != null) col.enabled = true;
            }
            disabledColliders.Clear();
            var colliders = itemObj.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = true;
            var renderers = itemObj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;
            // Tách khỏi Socket
            itemObj.transform.SetParent(null);

            Transform cam = (Camera.main != null) ? Camera.main.transform : (playerController != null ? playerController.transform : transform);
            Vector3 dropPos;
            Quaternion dropRot;
            Vector3 throwVelocity = Vector3.zero;
            Vector3 throwAngularVel = Vector3.zero;

            LadderController ladder = itemObj.GetComponent<LadderController>() ?? itemObj.GetComponentInChildren<LadderController>();
            bool isLadder = ladder != null;
            bool isLadderPlaced = false;

            if (lastFoundObstacle && lastValidHit.collider != null)
            {
                // Hành động ĐẶT (PLACE)
                dropPos = lastValidHit.point + (lastValidHit.normal * 0.1f);
                dropRot = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
                isLadderPlaced = true;
                if (ladder != null)
                {
                    ladder.SetPlaced(true);
                }
            }
            else
            {
                // Hành động NÉM / THẢ (THROW / DROP)
                dropPos = cam.position + (cam.forward * 0.8f);
                dropRot = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
                isLadderPlaced = false;
                if (ladder != null)
                {
                    ladder.SetPlaced(false);
                }
                IgnoreCollisionWithAllPlayers(itemObj, true);
            }

            itemObj.transform.position = dropPos;
            itemObj.transform.rotation = dropRot;
            itemObj.transform.localScale = Vector3.one;
            itemObj.SetActive(true);

            // Áp dụng lực vật lý
            Rigidbody rb = itemObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.WakeUp();
                if (isLadderPlaced)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    Vector3 forceDirection = cam.forward * 4.5f + Vector3.up * 1.5f;
                    rb.linearVelocity = forceDirection;
                    rb.angularVelocity = Random.insideUnitSphere * 2f;
                    throwVelocity = rb.linearVelocity;
                    throwAngularVel = rb.angularVelocity;
                }
            }

            // Đồng bộ thả/đặt item qua mạng
            NetworkItemSync.SyncDropItem(itemObj, dropPos, dropRot, throwVelocity, throwAngularVel, isLadder, isLadderPlaced);
            // Trừ khối lượng balo & xóa khỏi danh sách inventory của Player
            EnsureLocalPlayerController();
            if (playerController != null)
            {
                if (playerController.player != null)
                {
                    int itemWeight = (itemComp != null) ? itemComp.kg : 0;
                    playerController.player.currweight -= itemWeight;
                    if (playerController.player.currweight < 0) playerController.player.currweight = 0;
                }
                if (playerController.heldItem != null)
                {
                    playerController.heldItem.Remove(itemObj);
                }
                if (playerController.TryGetComponent<NetworkPlayerSync>(out var netSync))
                {
                    netSync.SetHeldItem(null);
                }
            }
        }
        currentHeldModel = null;
        DeselectAll();
    }

    /// <summary>
    /// Xóa toàn bộ item khỏi Hotbar (khi chết hoặc reset game)
    /// </summary>
    public void ClearAllSlots(bool disableHeldModel = false)
    {
        if (currentHeldModel != null)
        {
            // Bật lại collider đã tắt
            foreach (var col in disabledColliders)
            {
                if (col != null) col.enabled = true;
            }
            disabledColliders.Clear();

            var colliders = currentHeldModel.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = true;
            var renderers = currentHeldModel.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;

            EnsureLocalPlayerController();
            if (playerController != null && playerController.TryGetComponent<NetworkPlayerSync>(out var netSync))
            {
                netSync.SetHeldItem(null);
            }

            if (disableHeldModel)
            {
                currentHeldModel.SetActive(false);
            }
            else
            {
                currentHeldModel.SetActive(true);
            }

            currentHeldModel.transform.SetParent(null);
            currentHeldModel = null;
        }

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }

        DeselectAll();
    }

    private void HandleKeyboardInput()
    {
        // Đang leo thang -> Bỏ qua phím tắt Hotbar
        if (playerController != null && playerController.isClimbingLadder) return;

        // Phím số 1 -> 9 để chọn slot
        for (int i = 0; i < slots.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                OnSlotClicked(i);
            }
        }

        // Lăn chuột (Mouse Scroll Wheel) để cuộn chọn slot trên PC
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (slots.Count > 0 && Mathf.Abs(scroll) > 0.01f)
        {
            int nextIndex = currentSelectedIndex;
            if (scroll < 0f) // Lăn xuống -> Slot tiếp theo
            {
                nextIndex = (currentSelectedIndex < 0) ? 0 : (currentSelectedIndex + 1) % slots.Count;
            }
            else if (scroll > 0f) // Lăn lên -> Slot trước đó
            {
                nextIndex = (currentSelectedIndex < 0) ? (slots.Count - 1) : (currentSelectedIndex - 1 + slots.Count) % slots.Count;
            }
            OnSlotClicked(nextIndex);
        }

        // Phím Q hoặc G để Drop item đang chọn
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.G))
        {
            if (currentSelectedIndex >= 0)
            {
                DropSelectedItem();
            }
        }
    }
}
