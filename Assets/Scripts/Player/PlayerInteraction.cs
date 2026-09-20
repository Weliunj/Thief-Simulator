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

    // Đối tượng đang được nhắm trúng (Hiển thị trên HUD và sẽ nhặt khi bấm nút)
    private IInteractable currentInteractable;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        mobileActions = FindFirstObjectByType<MobileActionButtons>();
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

    void Update()
    {
        PerformInteractionCheck();
        HandleInteractionInput();
    }

    /// <summary>
    /// Bắn SphereCast quét các vật phẩm trong vùng nhìn.
    /// Nếu có nhiều vật phẩm, ưu tiên vật phẩm nằm gần tâm ngắm (crosshair) nhất.
    /// </summary>
    private void PerformInteractionCheck()
    {
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

        RaycastHit[] hits = Physics.SphereCastAll(ray, sphereCastRadius, maxCameraRayDistance, interactLayer, QueryTriggerInteraction.Collide);

        IInteractable bestInteractable = null;
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

                // 2. Tìm component IInteractable
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponent<IInteractable>();
                }
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInChildren<IInteractable>();
                }

                if (interactable != null)
                {
                    // 3. Kiểm tra khoảng cách từ người chơi đến vật phẩm
                    float distToPlayer = Vector3.Distance(transform.position, hit.collider.transform.position);
                    if (distToPlayer <= maxPlayerReach)
                    {
                        // 4. XỬ LÝ KHI CÓ NHIỀU ITEM TRONG VÙNG QUÉT:
                        // Tính khoảng cách vuông góc từ vị trí Item đến trục tâm ngắm (Ray)
                        Vector3 toItem = hit.collider.transform.position - ray.origin;
                        float distAlongRay = Vector3.Dot(toItem, ray.direction);
                        Vector3 pointOnRay = ray.origin + ray.direction * distAlongRay;
                        float distanceFromCrosshairCenter = Vector3.Distance(hit.collider.transform.position, pointOnRay);

                        // Điểm ưu tiên: Ưu tiên số 1 là sát tâm ngắm nhất, ưu tiên số 2 là gần player hơn
                        float score = distanceFromCrosshairCenter + (hit.distance * 0.05f);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestInteractable = interactable;
                        }
                    }
                }
            }
        }

        currentInteractable = bestInteractable;

        // Cập nhật thông tin vật phẩm được chọn lên UI HUD
        if (currentInteractable != null)
        {
            bool canInteract = currentInteractable.CanInteract(playerController, out string failReason);
            if (itemInfoHUD != null)
            {
                itemInfoHUD.ShowInteractable(currentInteractable, canInteract, failReason);
            }
            if (mobileActions != null && mobileActions.interactButton != null)
            {
                if (!mobileActions.interactButton.gameObject.activeSelf)
                    mobileActions.interactButton.gameObject.SetActive(true);
            }
        }
        else
        {
            if (itemInfoHUD != null)
            {
                itemInfoHUD.Hide();
            }
            if (mobileActions != null && mobileActions.interactButton != null)
            {
                if (mobileActions.interactButton.gameObject.activeSelf)
                    mobileActions.interactButton.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Xử lý bấm phím nhặt / tương tác.
    /// Đảm bảo: Thông tin gì đang hiện trên HUD thì bấm nút sẽ nhặt/tương tác đúng vật phẩm đó.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (currentInteractable == null || playerController == null) return;

        bool isInteractInput = Input.GetKeyDown(KeyCode.E) || (mobileActions != null && mobileActions.interactPressed);

        if (isInteractInput)
        {
            if (currentInteractable.CanInteract(playerController, out _))
            {
                // Thực hiện tương tác trực tiếp với vật phẩm đang chọn
                currentInteractable.Interact(playerController);
            }
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


