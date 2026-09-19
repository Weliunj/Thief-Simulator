using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý các nút action trên mobile: Jump, Sprint, Crouch, Pickup, Drop.
/// Gắn script này vào 1 GameObject trống trong Canvas HUD.
/// 
/// Quy tắc:
/// - Jump, Pickup, Drop: Bấm 1 lần (tap)
/// - Sprint, Crouch: Toggle (bấm bật, bấm lại tắt)
/// - Bật Sprint → tự tắt Crouch, và ngược lại
/// </summary>
public class MobileActionButtons : MonoBehaviour
{
    [Header("🎮 Action Buttons (Tap)")]
    public Button jumpButton;
    public Button interactButton;  // Pickup + Ladder + bất kỳ tương tác nào
    public Button dropButton;

    [Header("🏃 Sprint Button (Toggle)")]
    public Button sprintButton;

    [Header("🧎 Crouch Button (Toggle)")]
    public Button crouchButton;

    // --- Trạng thái public để ThirdPersonController và StarterAssetsInputs đọc ---
    [HideInInspector] public bool jumpPressed = false;
    [HideInInspector] public bool sprintHeld = false;
    [HideInInspector] public bool crouchHeld = false;
    [HideInInspector] public bool interactPressed = false;
    [HideInInspector] public bool dropPressed = false;

    void Start()
    {
        // --- Tap buttons ---
        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpTap);

        if (interactButton != null)
            interactButton.onClick.AddListener(OnInteractTap);

        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropTap);

        // --- Toggle buttons ---
        if (sprintButton != null)
            sprintButton.onClick.AddListener(OnSprintToggle);

        if (crouchButton != null)
            crouchButton.onClick.AddListener(OnCrouchToggle);
    }

    // =========================================================================
    //                           TAP HANDLERS
    // =========================================================================

    private void OnJumpTap()
    {
        jumpPressed = true;
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
        interactPressed = false;
        dropPressed = false;
    }
}
