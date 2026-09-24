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
        if (currentHeldModel == null || itemHoldPoint == null) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        }

        EnsureLocalPlayerController();

        Item item = currentHeldModel.GetComponent<Item>();
        Vector3 activeRotation = (item != null && item.useCustomHoldRotation) ? item.customHoldRotation : holdPointRotation;
        bool followPitch = (item != null && item.followCameraPitch);

        Vector3 targetPos;
        Quaternion targetRot;

        if (followMode == HoldFollowMode.PlayerMesh && playerController != null)
        {
            // BÁM THEO THÂN NHÂN VẬT (PLAYER MESH):
            // Góc xoay Y lấy từ playerController.transform.rotation (vốn đã xoay mượt với RotationSmoothTime)
            // Nhờ đó item có độ trễ xoay mượt y hệt góc quay của mesh người chơi, không bị quay giật cứng theo camera!
            Transform pTrans = playerController.transform;
            Vector3 pPos = pTrans.position;
            Vector3 pFwd = pTrans.forward;
            Vector3 pRight = pTrans.right;
            Vector3 pUp = pTrans.up;

            Vector3 offset = (item != null && item.useCustomHoldOffset) ? item.customHoldOffset : playerHoldOffset;
            targetPos = pPos + (pRight * offset.x) + (pUp * offset.y) + (pFwd * offset.z);

            if (followPitch && mainCamera != null)
            {
                // Nếu là đèn pin/súng: xoay Y theo người, ngửa X theo camera pitch
                float pitch = mainCamera.transform.eulerAngles.x;
                targetRot = Quaternion.Euler(pitch, pTrans.eulerAngles.y, 0f) * Quaternion.Euler(activeRotation);
            }
            else
            {
                targetRot = pTrans.rotation * Quaternion.Euler(activeRotation);
            }
        }
        else if (mainCamera != null)
        {
            // BÁM THEO CAMERA VỚI ĐỘ TRỄ MƯỢT (SMOOTH CAMERA):
            Vector3 camPos = mainCamera.transform.position;
            Vector3 camFwd = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            Vector3 camUp = mainCamera.transform.up;

            Vector3 offset = (item != null && item.useCustomHoldOffset) ? item.customHoldOffset : holdPointOffset;
            targetPos = camPos + (camRight * offset.x) + (camUp * offset.y) + (camFwd * offset.z);

            if (followPitch)
            {
                targetRot = mainCamera.transform.rotation * Quaternion.Euler(activeRotation);
            }
            else
            {
                Vector3 horizontalFwd = camFwd;
                horizontalFwd.y = 0f;
                if (horizontalFwd.sqrMagnitude > 0.0001f)
                {
                    targetRot = Quaternion.LookRotation(horizontalFwd.normalized, Vector3.up) * Quaternion.Euler(0f, activeRotation.y, 0f);
                }
                else
                {
                    targetRot = itemHoldPoint.rotation;
                }
            }
        }
        else
        {
            return;
        }

        // Áp dụng nội suy mượt (Smooth Interpolation / Delay / Sway)
        if (isFirstFrameHeld)
        {
            itemHoldPoint.position = targetPos;
            itemHoldPoint.rotation = targetRot;
            isFirstFrameHeld = false;
        }
        else
        {
            float posLerp = Time.deltaTime * positionSmoothSpeed;
            float rotLerp = Time.deltaTime * rotationSmoothSpeed;
            itemHoldPoint.position = Vector3.Lerp(itemHoldPoint.position, targetPos, Mathf.Clamp01(posLerp));
            itemHoldPoint.rotation = Quaternion.Slerp(itemHoldPoint.rotation, targetRot, Mathf.Clamp01(rotLerp));
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

    /// <summary>
    /// Hiển thị mô hình 3D của item trước mặt player/camera
    /// </summary>
    private void ShowHeldModel(GameObject itemObj)
    {
        HideHeldModel();

        if (itemObj == null) return;
        if (itemHoldPoint == null) InitializeHoldPoint();

        currentHeldModel = itemObj;
        isFirstFrameHeld = true;

        // 1. Tạm thời tắt colliders trên item để tránh cấn va chạm vào Player
        disabledColliders.Clear();
        Collider[] colliders = itemObj.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col != null && col.enabled)
            {
                col.enabled = false;
                disabledColliders.Add(col);
            }
        }

        // 2. Đảm bảo tất cả Renderers trên item đều được bật sáng
        var renderers = itemObj.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend != null) rend.enabled = true;
        }

        // 3. Reset Rigidbody
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

        // 4. Gắn vào HoldPoint trước mặt player
        if (itemHoldPoint != null)
        {
            itemObj.transform.SetParent(itemHoldPoint, false);
            itemObj.transform.localPosition = Vector3.zero;
            itemObj.transform.localRotation = Quaternion.identity;
            itemObj.transform.localScale = Vector3.one;
            UpdateHeldModelTransform();
        }
        else
        {
            Debug.LogError($"[HotbarManager] itemHoldPoint is NULL! Item '{itemObj.name}' will appear at old world position.");
        }

        itemObj.SetActive(true);

        EnsureLocalPlayerController();
        if (playerController != null && playerController.TryGetComponent<NetworkPlayerSync>(out var netSync))
        {
            netSync.SetHeldItem(itemObj);
        }

        Debug.Log($"<color=cyan>[HotbarManager] ShowHeldModel: '{itemObj.name}' active={itemObj.activeSelf}, " +
                  $"worldPos={itemObj.transform.position}, parent={itemObj.transform.parent?.name ?? "NULL"}</color>");
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

        HotbarSlot slot = slots[currentSelectedIndex];
        if (slot == null || !slot.HasItem())
        {
            if (slot != null) slot.ClearSlot();
            DeselectAll();
            return;
        }

        GameObject itemObj = slot.itemObject;
        Item item = slot.itemData;

        if (itemObj == null)
        {
            slot.ClearSlot();
            DeselectAll();
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        }

        // 🎯 TÍNH TOÁN VỊ TRÍ THẢ BẰNG RAYCAST (Bỏ qua colliders của chính item đang cầm và Player)
        Vector3 dropDirection = (mainCamera != null) ? mainCamera.transform.forward : transform.forward;
        Vector3 rayOrigin = (mainCamera != null) ? mainCamera.transform.position : transform.position + Vector3.up * 1.5f;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, dropDirection, maxDropDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit hit = default;
        bool hasObstacle = false;

        foreach (var h in hits)
        {
            if (h.collider != null && (h.collider.transform.IsChildOf(itemObj.transform) || (playerController != null && h.collider.transform.IsChildOf(playerController.transform))))
            {
                continue;
            }

            hit = h;
            hasObstacle = true;
            break;
        }

        Vector3 dropPosition;

        if (hasObstacle)
        {
            // Nếu trước mặt là vật cản -> Đặt item tại điểm va chạm Raycast (lùi nhẹ theo pháp tuyến tránh lọt vào trong)
            dropPosition = hit.point + hit.normal * 0.05f;
            Debug.DrawLine(rayOrigin, hit.point, Color.red, 2.5f);
            Debug.Log($"<color=#FF5555><b>[Hotbar Drop]</b> PHÁT HIỆN VẬT CẢN: <b>{hit.collider.gameObject.name}</b> (Khoảng cách: {hit.distance:F2}m) -> Đặt item tại {dropPosition}</color>");
        }
        else
        {
            // Nếu không có vật cản -> Đặt tại vị trí cầm item hoặc phía trước player
            if (itemHoldPoint != null)
            {
                dropPosition = itemHoldPoint.position;
            }
            else
            {
                dropPosition = rayOrigin + dropDirection * 1.0f;
            }
            Debug.DrawRay(rayOrigin, dropDirection * maxDropDistance, Color.green, 2.5f);
            Debug.Log($"<color=#55FF55><b>[Hotbar Drop]</b> KHÔNG GIAN THOÁNG (Không có vật cản trong {maxDropDistance}m) -> Ném item về phía trước với lực {dropForwardForce}</color>");
        }

        // Bật lại collider đã tạm tắt khi cầm
        foreach (var col in disabledColliders)
        {
            if (col != null) col.enabled = true;
        }
        disabledColliders.Clear();

        // 🎯 TÍNH TOÁN HƯỚNG VÀ GÓC XOAY BAN ĐẦU KHI THẢ
        Vector3 forwardDir = (mainCamera != null) ? mainCamera.transform.forward : (playerController != null ? playerController.transform.forward : dropDirection);
        forwardDir.y = 0f; // Triệt tiêu độ nghiêng theo phương thẳng đứng khi spawn
        Quaternion dropRot = (forwardDir.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(forwardDir) : Quaternion.identity;

        LadderController ladder = itemObj.GetComponent<LadderController>() ?? itemObj.GetComponentInChildren<LadderController>();
        bool isLadder = ladder != null;
        if (isLadder)
        {
            if (hasObstacle)
            {
                dropRot = Quaternion.Euler(0f, dropRot.eulerAngles.y, 0f);
            }
            else
            {
                dropRot = (mainCamera != null) ? mainCamera.transform.rotation : dropRot;
            }
        }

        // Thả item ra ngoài thế giới
        itemObj.transform.SetParent(null);
        itemObj.transform.position = dropPosition;
        itemObj.transform.rotation = dropRot;
        itemObj.transform.localScale = Vector3.one;
        itemObj.SetActive(true);

        // Kích hoạt vật lý tự nhiên bình thường khi thả/ném
        Rigidbody rb = itemObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (isLadder)
            {
                if (hasObstacle)
                {
                    // Chạm tường/sàn -> Đặt thang cẩn thận (Place): Khóa đứng thẳng và bật quyền leo trèo
                    ladder.SetPlaced(true);
                }
                else
                {
                    // Ném ra không gian thoáng -> Ném tự do (Drop): Vật lý tự do, thang lật đổ và KHÔNG cho phép leo
                    ladder.SetPlaced(false);
                    rb.AddForce(forwardDir * dropForwardForce, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
                }
            }
            else
            {
                // Các vật phẩm thường: Cho phép tự do xoay, nghiêng, lăn và va chạm tự nhiên
                rb.constraints = RigidbodyConstraints.None;

                // Nếu không có vật cản trước mặt -> Ném nhẹ theo hướng nhìn
                if (!hasObstacle)
                {
                    rb.AddForce(dropDirection * dropForwardForce, ForceMode.Impulse);
                }
            }
        }

        // Đồng bộ vật phẩm rơi qua mạng Photon Fusion cho tất cả người chơi khác
        Vector3 linVel = (rb != null) ? rb.linearVelocity : Vector3.zero;
        Vector3 angVel = (rb != null) ? rb.angularVelocity : Vector3.zero;
        bool isLadderPlaced = (isLadder && ladder != null) ? ladder.isPlaced : false;
        NetworkItemSync.SyncDropItem(itemObj, dropPosition, dropRot, linVel, angVel, isLadder, isLadderPlaced);

        // Cập nhật khối lượng và danh sách Player
        EnsureLocalPlayerController();
        if (playerController != null)
        {
            if (playerController.player != null)
            {
                int itemWeight = (item != null) ? item.kg : 0;
                playerController.player.currweight -= itemWeight;
                if (playerController.player.currweight < 0) playerController.player.currweight = 0;
            }

            if (playerController.heldItem != null)
            {
                playerController.heldItem.Remove(itemObj);
            }
        }

        currentHeldModel = null;
        slot.ClearSlot();
        DeselectAll();

        Debug.Log($"<color=yellow>Đã thả {itemObj.name} tại {dropPosition}</color>");
    }

    /// <summary>
    /// Xóa toàn bộ item khỏi Hotbar (khi chết hoặc reset game)
    /// </summary>
    public void ClearAllSlots()
    {
        HideHeldModel();
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }

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
