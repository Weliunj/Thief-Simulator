using StarterAssets;
using UnityEngine;

/// <summary>
/// Hệ thống tương tác quét SphereCast từ tâm góc nhìn Camera.
/// Quét các đối tượng IInteractable (Item, LockpickDoor, Ladder) và hiển thị thông tin lên ItemInfoHUD.
/// Khi có nhiều vật phẩm trong vùng quét, tự động ưu tiên vật phẩm nằm sát tâm ngắm nhất.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("🎯 SphereCast Settings")]
    [Tooltip("Bán kính luồng quét SphereCast từ tâm ngắm")]
    [Range(0.05f, 1.0f)]
    public float sphereCastRadius = 0.3f;

    [Tooltip("Khoảng cách tối đa từ Player đến vật phẩm để có thể tương tác")]
    public float maxPlayerReach = 3.5f;

    [Tooltip("Khoảng cách tối đa của tia quét từ Camera để nhắm trúng mục tiêu")]
    public float maxCameraRayDistance = 30f;

    [Tooltip("LayerMask quét các đối tượng tương tác")]
    public LayerMask interactLayer = ~0;

    [Header("🖥️ UI HUD Reference")]
    public ItemInfoHUD itemInfoHUD;

    private Camera mainCamera;
    private PlayerController playerController;
    private MobileActionButtons mobileActions;

    // Đối tượng đang được nhắm trúng
    private IInteractable currentLootItem;
    private IInteractable currentSpecialInteractable;
    private IInteractable currentInteractable;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (mobileActions == null)
        {
            mobileActions = FindFirstObjectByType<MobileActionButtons>(FindObjectsInactive.Include);
        }
    }

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
        }

        if (itemInfoHUD == null)
        {
            itemInfoHUD = FindFirstObjectByType<ItemInfoHUD>(FindObjectsInactive.Include);
        }
    }

    public static float interactionCooldownUntil = 0f;

    /// <summary>
    /// Đặt thời gian hồi tạm thời để tránh click nút UI kích hoạt nhầm Raycast tương tác
    /// </summary>
    public static void SetInteractionCooldown(float duration = 0.35f)
    {
        interactionCooldownUntil = Time.time + duration;
    }

    void Update()
    {
        // Khi đang chơi Minigame hoặc trong thời gian hồi sau khi đóng bảng: Dừng quét và không nhận input
        if (UI_Manager.isSolving || Time.time < interactionCooldownUntil)
        {
            currentLootItem = null;
            currentSpecialInteractable = null;
            currentInteractable = null;

            if (itemInfoHUD != null) itemInfoHUD.Hide();
            if (mobileActions != null)
            {
                if (mobileActions.pickupButton != null && mobileActions.pickupButton.gameObject.activeSelf)
                    mobileActions.pickupButton.gameObject.SetActive(false);
                if (mobileActions.interactButton != null && mobileActions.interactButton.gameObject.activeSelf)
                    mobileActions.interactButton.gameObject.SetActive(false);
            }
            return;
        }

        PerformInteractionCheck();
        HandleInteractionInput();
    }

    /// <summary>
    /// Bắn SphereCast quét các vật phẩm trong vùng nhìn.
    /// Tự động tách biệt Loot Item (nhặt đồ) và Special Interactable (thang, cửa).
    /// </summary>
    private void PerformInteractionCheck()
    {
        // Khi đang leo thang: Tắt toàn bộ HUD và nút Pickup / Interact để tránh nhặt nhầm thang đang bám
        if (playerController != null && playerController.isClimbingLadder)
        {
            currentLootItem = null;
            currentSpecialInteractable = null;
            currentInteractable = null;

            if (itemInfoHUD != null) itemInfoHUD.Hide();
            if (mobileActions != null)
            {
                if (mobileActions.pickupButton != null && mobileActions.pickupButton.gameObject.activeSelf)
                    mobileActions.pickupButton.gameObject.SetActive(false);
                if (mobileActions.interactButton != null && mobileActions.interactButton.gameObject.activeSelf)
                    mobileActions.interactButton.gameObject.SetActive(false);
            }
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
                if (mainCamera == null) return;
            }
        }

        // Tạo tia Ray từ chính giữa màn hình (0.5, 0.5)
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 1. Bắn tia Raycast trực tiếp từ tâm ngắm để xác định vật cản che khuất (Line of Sight)
        float maxAllowedDistance = maxPlayerReach;
        if (Physics.Raycast(ray, out RaycastHit directHit, maxCameraRayDistance, interactLayer, QueryTriggerInteraction.Collide))
        {
            // Nếu tâm ngắm rọi trực tiếp vào một vật thể không phải interactable (như tường, sàn, cột)
            IInteractable directInteractable = directHit.collider.GetComponentInParent<IInteractable>() ?? directHit.collider.GetComponent<IInteractable>();
            if (directInteractable == null)
            {
                // Giới hạn tầm quét không được xuyên qua tường/vật cản phía trước
                maxAllowedDistance = Mathf.Min(maxAllowedDistance, directHit.distance + 0.1f);
            }
        }

        RaycastHit[] hits = Physics.SphereCastAll(ray, sphereCastRadius, maxAllowedDistance, interactLayer, QueryTriggerInteraction.Collide);

        IInteractable bestLoot = null;
        IInteractable bestSpecial = null;
        float bestScore = float.MaxValue; // Càng nhỏ càng ưu tiên (gần tâm ngắm nhất)

        if (hits != null && hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                // 1. Bỏ qua collider của chính Player
                if (hit.collider.transform.root == transform.root || hit.collider.CompareTag("Player"))
                {
                    continue;
                }

                // 2. Tìm tất cả components IInteractable trên chính Collider hoặc cấp cha (Item + LadderController...)
                IInteractable[] interactables = hit.collider.GetComponentsInParent<IInteractable>();
                if (interactables == null || interactables.Length == 0)
                {
                    interactables = hit.collider.GetComponents<IInteractable>();
                }

                if (interactables != null && interactables.Length > 0)
                {
                    // 3. Kiểm tra khoảng cách từ người chơi đến điểm tiếp xúc (hit.point) hoặc Collider bề mặt
                    Vector3 contactPoint = (hit.point != Vector3.zero) ? hit.point : hit.collider.ClosestPoint(transform.position);
                    float distToPlayer = Vector3.Distance(transform.position, contactPoint);

                    if (distToPlayer <= maxPlayerReach || hit.distance <= maxPlayerReach)
                    {
                        // 4. XỬ LÝ KHI CÓ NHIỀU ITEM TRONG VÙNG QUÉT (Ưu tiên sát tâm ngắm nhất)
                        Vector3 toContact = contactPoint - ray.origin;
                        float distAlongRay = Vector3.Dot(toContact, ray.direction);
                        Vector3 pointOnRay = ray.origin + ray.direction * distAlongRay;
                        float distanceFromCrosshairCenter = Vector3.Distance(contactPoint, pointOnRay);

                        float score = distanceFromCrosshairCenter + (hit.distance * 0.05f);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestLoot = null;
                            bestSpecial = null;

                            foreach (var it in interactables)
                            {
                                if (it == null) continue;
                                bool canInteract = it.CanInteract(playerController, out string failReason);
                                if (!canInteract && failReason == "Already Unlocked") continue;

                                if (it.IsLootItem())
                                {
                                    if (bestLoot == null) bestLoot = it;
                                }
                                else
                                {
                                    if (bestSpecial == null) bestSpecial = it;
                                }
                            }
                        }
                    }
                }
            }
        }

        currentLootItem = bestLoot;
        currentSpecialInteractable = bestSpecial;
        currentInteractable = bestSpecial ?? bestLoot;

        // Cập nhật thông tin vật phẩm được chọn lên UI HUD và Mobile Action Buttons
        if (currentLootItem != null || currentSpecialInteractable != null)
        {
            IInteractable primary = currentSpecialInteractable ?? currentLootItem;
            bool canInteract = primary.CanInteract(playerController, out string failReason);

            if (itemInfoHUD != null)
            {
                itemInfoHUD.ShowInteractable(primary, canInteract, failReason);
            }

            // Xử lý bật/tắt nút Mobile: Nếu vừa có Item vừa có Thang -> Bật CẢ 2 NÚT!
            if (mobileActions != null)
            {
                // Nút Pickup (nhặt đồ vào túi)
                if (mobileActions.pickupButton != null)
                {
                    bool showPickup = (currentLootItem != null);
                    if (mobileActions.pickupButton.gameObject.activeSelf != showPickup)
                        mobileActions.pickupButton.gameObject.SetActive(showPickup);
                }

                // Nút Interact (tương tác đặc biệt: Thang, Cửa, v.v.)
                if (mobileActions.interactButton != null)
                {
                    bool showInteract = (currentSpecialInteractable != null && currentSpecialInteractable.CanInteract(playerController, out _));
                    if (mobileActions.interactButton.gameObject.activeSelf != showInteract)
                        mobileActions.interactButton.gameObject.SetActive(showInteract);
                }
            }
        }
        else
        {
            if (itemInfoHUD != null)
            {
                itemInfoHUD.Hide();
            }
            if (mobileActions != null)
            {
                if (mobileActions.pickupButton != null && mobileActions.pickupButton.gameObject.activeSelf)
                    mobileActions.pickupButton.gameObject.SetActive(false);
                if (mobileActions.interactButton != null && mobileActions.interactButton.gameObject.activeSelf)
                    mobileActions.interactButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Xử lý bấm phím nhặt / tương tác.
    /// - Phím E (hoặc nút Pickup): Nhặt Item vào Hotbar.
    /// - Phím F (hoặc nút Interact): Leo thang / Mở cửa.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (playerController == null || playerController.isClimbingLadder) return;

        bool pickupInput = Input.GetKeyDown(KeyCode.E) || (mobileActions != null && mobileActions.pickupPressed);
        bool interactInput = Input.GetKeyDown(KeyCode.F) || (mobileActions != null && mobileActions.interactPressed);

        // Fallback: Nếu đối tượng chỉ có duy nhất tương tác đặc biệt (như cửa chỉ có LockpickDoor) và bấm E
        if (currentLootItem == null && currentSpecialInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            interactInput = true;
        }

        // 1. Thực hiện Nhặt đồ (Pickup)
        if (pickupInput && currentLootItem != null)
        {
            if (currentLootItem.CanInteract(playerController, out string failReason))
            {
                currentLootItem.Interact(playerController);
            }
            else
            {
                if (itemInfoHUD != null && !string.IsNullOrEmpty(failReason))
                {
                    itemInfoHUD.ShowWarning(failReason);
                }
            }
            return;
        }

        // 2. Thực hiện Tương tác đặc biệt (Leo thang / Mở cửa)
        if (interactInput && currentSpecialInteractable != null)
        {
            if (currentSpecialInteractable.CanInteract(playerController, out string failReason))
            {
                currentSpecialInteractable.Interact(playerController);
            }
            else
            {
                if (itemInfoHUD != null && !string.IsNullOrEmpty(failReason))
                {
                    itemInfoHUD.ShowWarning(failReason);
                }
            }
            return;
        }
    }

    // =========================================================================
    //                            GIZMOS VISUALIZATION
    // =========================================================================

    private void OnDrawGizmos()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float drawDist = maxPlayerReach;

        // Đổi màu Gizmos khi gặp Interactable:
        // - Xanh lá: Rọi trúng Item/Cửa/Thang trong tầm nhặt
        // - Cyan (Xanh dương sáng): Không có Item
        if (currentInteractable != null)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.7f);
        }

        Vector3 startPos = ray.origin;
        Vector3 endPos = ray.origin + ray.direction * drawDist;

        // Vẽ tia ray chính giữa
        Gizmos.DrawRay(ray.origin, ray.direction * drawDist);

        // Vẽ 2 khối cầu SphereCast ở đầu và cuối tầm ngắm
        Gizmos.DrawWireSphere(startPos + ray.direction * 0.3f, sphereCastRadius);
        Gizmos.DrawWireSphere(endPos, sphereCastRadius);

        // Vẽ 4 đường nối tạo thành hình ống quét SphereCast
        Vector3 upOffset = mainCamera.transform.up * sphereCastRadius;
        Vector3 rightOffset = mainCamera.transform.right * sphereCastRadius;

        Gizmos.DrawLine(startPos + upOffset, endPos + upOffset);
        Gizmos.DrawLine(startPos - upOffset, endPos - upOffset);
        Gizmos.DrawLine(startPos + rightOffset, endPos + rightOffset);
        Gizmos.DrawLine(startPos - rightOffset, endPos - rightOffset);
    }
}


