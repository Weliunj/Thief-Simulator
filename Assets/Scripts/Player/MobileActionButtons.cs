using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý các nút action trên mobile: Jump, Sprint, Crouch, Pickup, Interact, Drop, Flashlight.
/// Gắn script này vào GameObject MobileActionButtons trong Canvas HUD.
/// 
/// Phân biệt:
/// - PickupBtn: Dành riêng cho nhặt Item (Loot) vào Hotbar.
/// - InteractBtn: Dành riêng cho tương tác với các đối tượng đặc biệt (Ladder, Door, v.v.).
/// </summary>
public class MobileActionButtons : MonoBehaviour
{
    [Header("🎮 Action Buttons (Tap)")]
    public Button jumpButton;
    public Button pickupButton;    // Nút nhặt đồ (Item Loot)
    public Button interactButton;  // Nút tương tác đặc biệt (Thang, Cửa, v.v.)
    public Button dropButton;

    [Header("🏃 Sprint Button (Toggle)")]
    public Button sprintButton;

    [Header("🧎 Crouch Button (Toggle)")]
    public Button crouchButton;

    [Header("🔦 Flashlight Button (Toggle)")]
    public Button flashlightButton;

    // --- Trạng thái public để ThirdPersonController, PlayerInteraction đọc ---
    [HideInInspector] public bool jumpPressed = false;
    [HideInInspector] public bool sprintHeld = false;
    [HideInInspector] public bool crouchHeld = false;
    [HideInInspector] public bool pickupPressed = false;
    [HideInInspector] public bool interactPressed = false;
    [HideInInspector] public bool dropPressed = false;
    [HideInInspector] public bool flashlightPressed = false;

    void Start()
    {
        // Tự động tìm Button con nếu chưa kéo thả trong Inspector
        AutoFindButtons();

        // --- Đăng ký sự kiện tap ---
        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpTap);

        if (pickupButton != null)
        {
            pickupButton.onClick.AddListener(OnPickupTap);
            pickupButton.gameObject.SetActive(false);
        }

        if (interactButton != null)
        {
            interactButton.onClick.AddListener(OnInteractTap);
            interactButton.gameObject.SetActive(false);
        }

        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropTap);

        // --- Đăng ký sự kiện toggle ---
        if (sprintButton != null)
            sprintButton.onClick.AddListener(OnSprintToggle);

        if (crouchButton != null)
            crouchButton.onClick.AddListener(OnCrouchToggle);

        if (flashlightButton != null)
            flashlightButton.onClick.AddListener(OnFlashlightTap);
    }

    private void AutoFindButtons()
    {
        if (jumpButton == null)
        {
            Transform t = transform.Find("JumpBtn") ?? transform.Find("JumpButton");
            if (t != null) jumpButton = t.GetComponent<Button>();
        }
        if (pickupButton == null)
        {
            Transform t = transform.Find("PickupBtn") ?? transform.Find("PickupButton");
            if (t != null) pickupButton = t.GetComponent<Button>();
        }
        if (interactButton == null)
        {
            Transform t = transform.Find("InteractBtn") ?? transform.Find("InteractButton");
            if (t != null) interactButton = t.GetComponent<Button>();
        }
        if (dropButton == null)
        {
            Transform t = transform.Find("TossBtn") ?? transform.Find("DropBtn") ?? transform.Find("DropButton");
            if (t != null) dropButton = t.GetComponent<Button>();
        }
        if (sprintButton == null)
        {
            Transform t = transform.Find("SprintBtn") ?? transform.Find("SprintButton");
            if (t != null) sprintButton = t.GetComponent<Button>();
        }
        if (crouchButton == null)
        {
            Transform t = transform.Find("CrouchBtn") ?? transform.Find("CrouchButton");
            if (t != null) crouchButton = t.GetComponent<Button>();
        }
        if (flashlightButton == null)
        {
            Transform t = transform.Find("FlaskLightBtn") ?? transform.Find("FlashlightBtn");
            if (t != null) flashlightButton = t.GetComponent<Button>();
        }
    }

    // =========================================================================
    //                           TAP HANDLERS
    // =========================================================================

    private void OnJumpTap()
    {
        jumpPressed = true;
    }

    private void OnPickupTap()
    {
        pickupPressed = true;
    }

    private void OnInteractTap()
    {
        interactPressed = true;
    }

    private void OnDropTap()
    {
        dropPressed = true;
    }

    private void OnFlashlightTap()
    {
        flashlightPressed = true;
    }

    // =========================================================================
    //                      TOGGLE HANDLERS (Sprint)
    // =========================================================================

    private void OnSprintToggle()
    {
        sprintHeld = !sprintHeld;

        // Bật Sprint → tự tắt Crouch
        if (sprintHeld && crouchHeld)
        {
            crouchHeld = false;
        }
    }

    // =========================================================================
    //                      TOGGLE HANDLERS (Crouch)
    // =========================================================================

    private void OnCrouchToggle()
    {
        crouchHeld = !crouchHeld;

        // Bật Crouch → tự tắt Sprint
        if (crouchHeld && sprintHeld)
        {
            sprintHeld = false;
        }
    }

    // =========================================================================
    //                      LATE UPDATE: Reset 1-frame flags
    // =========================================================================

    void LateUpdate()
    {
        // Các nút tap chỉ true trong 1 frame, sau đó reset
        jumpPressed = false;
        pickupPressed = false;
        interactPressed = false;
        dropPressed = false;
        flashlightPressed = false;
    }
}

