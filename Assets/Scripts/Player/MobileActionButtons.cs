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

    // --- Trạng thái public để ThirdPersonController, PlayerInteraction đọc ---
    [HideInInspector] public bool jumpPressed = false;
    [HideInInspector] public bool sprintHeld = false;
    [HideInInspector] public bool crouchHeld = false;
    [HideInInspector] public bool pickupPressed = false;
    [HideInInspector] public bool interactPressed = false;
    [HideInInspector] public bool dropPressed = false;

    void Start()
    {
        // Tự động tìm Button con nếu chưa kéo thả trong Inspector
        AutoFindButtons();
        UpdateToggleVisuals();

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
    }

    private void AutoFindButtons()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        if (jumpButton == null)
            jumpButton = FindButtonByName(allButtons, "jump");
        if (pickupButton == null)
            pickupButton = FindButtonByName(allButtons, "pickup");
        if (interactButton == null)
            interactButton = FindButtonByName(allButtons, "interact");
        if (dropButton == null)
            dropButton = FindButtonByName(allButtons, "toss", "drop");
        if (sprintButton == null)
            sprintButton = FindButtonByName(allButtons, "sprint");
        if (crouchButton == null)
            crouchButton = FindButtonByName(allButtons, "crouch");
    }

    private Button FindButtonByName(Button[] buttons, params string[] keywords)
    {
        if (buttons == null) return null;
        foreach (var b in buttons)
        {
            if (b == null) continue;
            string objName = b.gameObject.name.ToLower();
            foreach (var kw in keywords)
            {
                if (objName.Contains(kw.ToLower()))
                {
                    return b;
                }
            }
        }
        return null;
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

    // =========================================================================
    //                      TOGGLE HANDLERS (Sprint & Crouch)
    // =========================================================================

    private void OnSprintToggle()
    {
        sprintHeld = !sprintHeld;

        // Bật Sprint → tự tắt Crouch
        if (sprintHeld && crouchHeld)
        {
            crouchHeld = false;
        }

        UpdateToggleVisuals();
    }

    private void OnCrouchToggle()
    {
        crouchHeld = !crouchHeld;

        // Bật Crouch → tự tắt Sprint
        if (crouchHeld && sprintHeld)
        {
            sprintHeld = false;
        }

        UpdateToggleVisuals();
    }

    private void UpdateToggleVisuals()
    {
        if (crouchButton != null && crouchButton.image != null)
        {
            crouchButton.image.color = crouchHeld ? new Color(0.9f, 0.9f, 0.5f, 0.8f) : new Color(1f, 1f, 1f, 0.5f);
        }
        if (sprintButton != null && sprintButton.image != null)
        {
            sprintButton.image.color = sprintHeld ? new Color(0.9f, 0.9f, 0.5f, 0.8f) : new Color(1f, 1f, 1f, 0.5f);
        }
    }

    /// <summary>
    /// Cập nhật nhãn chữ trên nút Interact (VD: Climb Up, Climb Down, Pick Lock, Turn On, Turn Off)
    /// Tự động tìm child 'Prompt' để gán chữ chuẩn xác
    /// </summary>
    public void SetInteractPrompt(string prompt)
    {
        UpdateButtonText(interactButton, prompt);
    }

    /// <summary>
    /// Cập nhật nhãn chữ trên nút Pickup (VD: Pick Up, Take Flashlight)
    /// </summary>
    public void SetPickupPrompt(string prompt)
    {
        UpdateButtonText(pickupButton, prompt);
    }

    private void UpdateButtonText(Button btn, string prompt)
    {
        if (btn == null || string.IsNullOrEmpty(prompt)) return;

        // 1. Ưu tiên tìm child GameObject có tên 'Prompt' hoặc chứa 'prompt'
        Transform promptChild = btn.transform.Find("Prompt");
        if (promptChild == null)
        {
            foreach (Transform child in btn.transform)
            {
                if (child.name.ToLower().Contains("prompt"))
                {
                    promptChild = child;
                    break;
                }
            }
        }

        Transform targetTransform = promptChild ?? btn.transform;

        // 2. Gán text cho TextMeshProUGUI hoặc UI Text
        var tmp = targetTransform.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = prompt;
            return;
        }

        var txt = targetTransform.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (txt != null)
        {
            txt.text = prompt;
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
    }
}

