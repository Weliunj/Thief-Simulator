using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Component chuyên trách quản lý toàn bộ hệ thống Vật phẩm (Inventory) của Nhân vật:
/// - Quản lý danh sách vật phẩm đang giữ (heldItems).
/// - Xử lý nhặt đồ (TryPickupItem) kèm animation, kiểm tra tải trọng (Weight) và slot Hotbar.
/// - Quản lý trạng thái cầm đồ (isTaking, takeDuration, takeTimer) và ẩn các item không active.
/// - Xử lý thả/ném đồ thủ công (Drop / Toss).
/// - Xử lý rơi rải rác toàn bộ đồ khi nhân vật tử vong (DropAllItemsOnDeath).
/// - Đồng bộ tải trọng balo với PlayerStats.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerInventory : MonoBehaviour
{
    [Header("🎒 Inventory Items")]
    [Tooltip("Danh sách các GameObject vật phẩm người chơi đang giữ trong túi/hotbar")]
    public List<GameObject> heldItems = new List<GameObject>();

    [Header("⏱️ Pickup & Taking Settings")]
    [Tooltip("Thời gian animation/độ trễ khi nhặt vật phẩm")]
    public float takeDuration = 0.5f;
    [HideInInspector] public float takeTimer = 0f;
    [HideInInspector] public bool isTaking = false;

    [Header("💀 Death Drop Settings (Rớt đồ khi chết)")]
    [Tooltip("Bán kính rải rác vật phẩm xung quanh vị trí thi thể")]
    public float dropRadius = 2f;
    [Tooltip("Khoảng cách tối thiểu giữa các vật phẩm rơi để tránh chồng đè")]
    public float minItemDistance = 0.5f;
    [Tooltip("Độ cao xuất hiện của vật phẩm so với tâm nhân vật khi rớt")]
    public float dropHeight = 1.5f;

    [Header("🔗 Component References")]
    public PlayerController playerController;
    public PlayerStats playerStats;
    public HotbarManager hotbarManager;
    public MobileActionButtons mobileActions;
    public Animator animator;

    // Compatibility property
    public List<GameObject> heldItem => heldItems;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (animator == null) TryGetComponent(out animator);

        if (heldItems == null) heldItems = new List<GameObject>();
        else heldItems.RemoveAll(x => x == null);
    }

    private void Start()
    {
        if (heldItems != null) heldItems.RemoveAll(x => x == null);

        if (hotbarManager == null)
        {
            hotbarManager = FindFirstObjectByType<HotbarManager>(FindObjectsInactive.Include);
        }
        if (mobileActions == null)
        {
            mobileActions = FindFirstObjectByType<MobileActionButtons>(FindObjectsInactive.Include);
        }
    }

    private void Update()
    {
        var net = GetComponent<NetworkPlayerSync>();
        if (net != null && !net.IsLocalPlayer)
        {
            return;
        }

        UpdateHoldingState();
    }

    /// <summary>
    /// Cập nhật trạng thái item đang cầm và đếm ngược thời gian nhặt (takeTimer).
    /// Ẩn tất cả model item ngoại trừ item đang được HotbarManager chọn hiển thị.
    /// </summary>
    public void UpdateHoldingState()
    {
        GameObject currentlyHeldByHotbar = null;
        if (hotbarManager != null && hotbarManager.currentSelectedIndex >= 0)
        {
            currentlyHeldByHotbar = hotbarManager.GetCurrentHeldModel();
        }

        // Ẩn tất cả item trong danh sách, chỉ hiện item đang active trên tay
        for (int i = 0; i < heldItems.Count; i++)
        {
            GameObject item = heldItems[i];
            if (item != null && item != currentlyHeldByHotbar)
            {
                if (item.activeSelf)
                {
                    item.SetActive(false);
                }
            }
        }

        // Đếm ngược thời gian nhặt
        if (isTaking)
        {
            takeTimer -= Time.deltaTime;
            if (takeTimer <= 0f)
            {
                isTaking = false;
            }
        }
    }

    /// <summary>
    /// Thử nhặt một vật phẩm khi tương tác
    /// </summary>
    public bool TryPickupItem(GameObject itemObj, int itemKg)
    {
        if (isTaking) return false;
        if (itemObj == null) return false;

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats == null) return false;

        // 1. Kiểm tra giới hạn tải trọng (Overweight)
        if (playerStats.currentWeight + itemKg > playerStats.maxWeight)
        {
            Debug.Log($"[PlayerInventory] Quá tải! Không thể nhặt {itemObj.name} ({itemKg}kg). Hiện tại: {playerStats.currentWeight}/{playerStats.maxWeight}kg.");
            return false;
        }

        // 2. Kiểm tra slot Hotbar
        if (hotbarManager == null)
        {
            hotbarManager = FindFirstObjectByType<HotbarManager>(FindObjectsInactive.Include);
        }

        if (hotbarManager != null && !hotbarManager.HasEmptySlot())
        {
            Debug.Log("[PlayerInventory] Hotbar đã đầy! Không thể nhặt thêm vật phẩm.");
            return false;
        }

        // 3. Thêm vào túi đồ
        if (!heldItems.Contains(itemObj))
        {
            heldItems.Add(itemObj);
            playerStats.currentWeight += itemKg;
            UpdateWeight();

            Item itemComp = itemObj.GetComponent<Item>();
            var ladderComp = itemObj.GetComponent<LadderController>() ?? itemObj.GetComponentInChildren<LadderController>();
            if (ladderComp != null)
            {
                ladderComp.SetPlaced(false);
            }

            if (hotbarManager != null)
            {
                hotbarManager.AddItem(itemObj, itemComp);
            }
            else
            {
                Rigidbody rbPick = itemObj.GetComponent<Rigidbody>();
                if (rbPick != null)
                {
                    if (!rbPick.isKinematic)
                    {
                        rbPick.linearVelocity = Vector3.zero;
                        rbPick.angularVelocity = Vector3.zero;
                    }
                    rbPick.isKinematic = true;
                }
                itemObj.SetActive(false);
            }

            Debug.Log($"[PlayerInventory] Đã nhặt: {itemObj.name} ({itemKg}kg) -> Tổng tải trọng: {playerStats.currentWeight}/{playerStats.maxWeight}kg");

            // Đồng bộ nhặt đồ qua mạng Photon Fusion nếu đang trong phòng multiplayer
            NetworkItemSync.SyncPickupItem(itemObj);
        }

        // 4. Kích hoạt Animation & trạng thái Taking
        isTaking = true;
        takeTimer = takeDuration;
        if (animator != null)
        {
            animator.SetTrigger("Take");
        }

        return true;
    }

    /// <summary>
    /// Thả vật phẩm cuối cùng (Fallback khi không sử dụng Hotbar)
    /// </summary>
    public void DropLastItem()
    {
        if (heldItems.Count == 0 || isTaking) return;

        int lastIndex = heldItems.Count - 1;
        GameObject itemToDrop = heldItems[lastIndex];

        if (itemToDrop != null)
        {
            Item itemComp = itemToDrop.GetComponent<Item>();
            if (itemComp != null && playerStats != null)
            {
                playerStats.currentWeight -= itemComp.kg;
                if (playerStats.currentWeight < 0) playerStats.currentWeight = 0;
                UpdateWeight();
            }

            itemToDrop.transform.SetParent(null);
            itemToDrop.transform.position = transform.position + transform.forward * 1f + Vector3.up * 0.5f;

            bool isLadder = itemToDrop.GetComponent<LadderController>() != null || itemToDrop.GetComponentInChildren<LadderController>() != null;
            if (isLadder)
            {
                itemToDrop.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
            else
            {
                itemToDrop.transform.rotation = Quaternion.identity;
            }

            itemToDrop.SetActive(true);

            Rigidbody rbDrop = itemToDrop.GetComponent<Rigidbody>();
            Vector3 linVel = Vector3.zero;
            Vector3 angVel = Vector3.zero;
            if (rbDrop != null)
            {
                rbDrop.isKinematic = false;
                rbDrop.linearVelocity = Vector3.zero;
                rbDrop.angularVelocity = Vector3.zero;

                if (isLadder)
                {
                    rbDrop.constraints = RigidbodyConstraints.None;
                    var ladder = itemToDrop.GetComponent<LadderController>() ?? itemToDrop.GetComponentInChildren<LadderController>();
                    if (ladder != null) ladder.SetPlaced(false);
                    rbDrop.AddForce(transform.forward * 0.5f, ForceMode.Impulse);
                }
                else
                {
                    rbDrop.constraints = RigidbodyConstraints.None;
                    Vector3 dropForce = (transform.forward + Vector3.up * 0.5f) * Random.Range(1.5f, 3f);
                    rbDrop.AddForce(dropForce, ForceMode.Impulse);
                }

                linVel = rbDrop.linearVelocity;
                angVel = rbDrop.angularVelocity;
            }

            NetworkItemSync.SyncDropItem(itemToDrop, itemToDrop.transform.position, itemToDrop.transform.rotation, linVel, angVel, isLadder, false);

            heldItems.RemoveAt(lastIndex);
        }
        else
        {
            heldItems.RemoveAt(lastIndex);
        }
    }

    /// <summary>
    /// Rớt toàn bộ vật phẩm đang giữ rải rác xung quanh vị trí thi thể khi Player tử vong
    /// </summary>
    public void DropAllItemsOnDeath()
    {
        // Tách item đang cầm ra khỏi Socket của Player trước và không ẩn SetActive
        if (hotbarManager != null)
        {
            hotbarManager.ClearAllSlots(false);
        }

        if (heldItems == null || heldItems.Count == 0)
        {
            Debug.Log("[PlayerInventory] DropAllItemsOnDeath: Không có vật phẩm nào đang giữ.");
            return;
        }

        Vector3 dropCenter = transform.position + Vector3.up * dropHeight;
        List<Vector3> spawnedPositions = new List<Vector3>();
        List<GameObject> itemsToDrop = new List<GameObject>(heldItems);
        int droppedCount = 0;

        foreach (var item in itemsToDrop)
        {
            if (item == null) continue;

            item.transform.SetParent(null);

            Vector3 randomPosition = GetRandomDropPosition(dropCenter, dropRadius, spawnedPositions, minItemDistance);

            // 1. Kiểm tra va chạm tường ngang để tránh văng xuyên qua tường nhà
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            Vector3 rayDir = randomPosition - rayStart;
            float rayDist = rayDir.magnitude;
            if (rayDist > 0.01f && Physics.Raycast(rayStart, rayDir.normalized, out RaycastHit wallHit, rayDist, ~LayerMask.GetMask("Player", "Npc", "Ignore Raycast")))
            {
                randomPosition = wallHit.point - rayDir.normalized * 0.15f;
            }

            // 2. Bắn Raycast từ ngang hông (0.5m) thẳng xuống dưới để luôn dính đúng mặt sàn của tầng hiện tại
            if (Physics.Raycast(randomPosition + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 3.0f, ~LayerMask.GetMask("Player", "Npc", "Ignore Raycast")))
            {
                randomPosition = groundHit.point + Vector3.up * 0.05f;
            }

            item.transform.position = randomPosition;

            bool isLadder = item.GetComponent<LadderController>() != null || item.GetComponentInChildren<LadderController>() != null;
            if (isLadder)
            {
                item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            // Bật lại collider, renderer và kích hoạt GameObject
            var colliders = item.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = true;
            var renderers = item.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;

            item.SetActive(true);
            spawnedPositions.Add(randomPosition);

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.None;

                if (isLadder)
                {
                    var ladder = item.GetComponent<LadderController>() ?? item.GetComponentInChildren<LadderController>();
                    if (ladder != null) ladder.SetPlaced(false);
                }
            }

            NetworkItemSync.SyncDropItem(item, randomPosition, item.transform.rotation, Vector3.zero, Vector3.zero, isLadder, false);

            Debug.Log($"[PlayerInventory] Rớt vật phẩm khi chết: {item.name} tại {randomPosition}");
            droppedCount++;
        }

        heldItems.RemoveAll(x => itemsToDrop.Contains(x));

        if (playerStats != null)
        {
            playerStats.currentWeight = 0;
            UpdateWeight();
        }

        Debug.Log($"[PlayerInventory] Đã rớt {droppedCount} vật phẩm xung quanh thi thể người chơi.");
    }

    /// <summary>
    /// Tìm vị trí rơi ngẫu nhiên tránh va đè lên nhau
    /// </summary>
    private Vector3 GetRandomDropPosition(Vector3 center, float radius, List<Vector3> existingPositions, float minDistance)
    {
        Vector3 randomPos;
        int attempts = 0;
        int maxAttempts = 50;

        do
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, radius);

            randomPos = center + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            attempts++;

            bool tooClose = false;
            foreach (var existingPos in existingPositions)
            {
                if (Vector3.Distance(randomPos, existingPos) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) break;

        } while (attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, radius);
            randomPos = center + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
        }

        return randomPos;
    }

    /// <summary>
    /// Đồng bộ tải trọng với PlayerStats để tính toán giảm tốc độ
    /// </summary>
    public void UpdateWeight()
    {
        if (playerStats != null)
        {
            playerStats.CalculateWeightSpeedPenalty();
        }
    }

    /// <summary>
    /// Xóa toàn bộ vật phẩm trong túi và reset cân nặng
    /// </summary>
    public void ClearInventory()
    {
        heldItems.Clear();
        if (playerStats != null)
        {
            playerStats.currentWeight = 0;
            UpdateWeight();
        }
        if (hotbarManager != null)
        {
            hotbarManager.ClearAllSlots();
        }
    }
}
