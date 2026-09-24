using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool crouch;

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        [Tooltip("Giữ chuột luôn mở khóa (None) để click các nút trên màn hình như cảm ứng")]
        public bool cursorLocked = false;
        public bool cursorInputForLook = false;
        
        [Tooltip("Điều chỉnh tốc độ chuột/camera")]
        public float lookSensitivity = 1.0f;

        [Header("🕹️ Mobile Touch Controls")]
        [Tooltip("Luôn hiển thị các nút ảo trên màn hình để click chuột test")]
        public bool autoHideMobileControlsOnPC = false;
        public DynamicJoystick dynamicJoystick;
        public TouchLookZone touchLookZone;
        public MobileActionButtons mobileActions;

        private void Awake()
        {
            // Luôn mở khóa và hiển thị chuột xuyên suốt game
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Tự động tìm các UI Mobile nếu chưa kéo thả
            if (touchLookZone == null)
            {
                touchLookZone = FindFirstObjectByType<TouchLookZone>(FindObjectsInactive.Include);
            }
            if (dynamicJoystick == null)
            {
                dynamicJoystick = FindFirstObjectByType<DynamicJoystick>(FindObjectsInactive.Include);
            }
            if (mobileActions == null)
            {
                mobileActions = FindFirstObjectByType<MobileActionButtons>(FindObjectsInactive.Include);
            }
        }

        private void Start()
        {
            // Luôn mở khóa và hiển thị chuột xuyên suốt game
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateSensitivity(SettingsManager.Instance != null ? SettingsManager.Instance.Sensitivity : lookSensitivity);
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SettingsManager.OnSensitivityChanged += UpdateSensitivity;
        }

        private void OnDestroy()
        {
            SettingsManager.OnSensitivityChanged -= UpdateSensitivity;
        }

        private void UpdateSensitivity(float newSensitivity)
        {
            lookSensitivity = newSensitivity;
        }

        private void Update()
        {
            // Luôn đảm bảo chuột hiển thị và không bị khóa
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // --- 1. Phím tắt PC: Mở Menu Pause khi bấm ESC hoặc P ---
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                UI_Manager uiManager = FindFirstObjectByType<UI_Manager>();
                if (uiManager != null && !UI_Manager.isSolving)
                {
                    if (uiManager.isPaused) uiManager.ResumeGame();
                    else uiManager.PauseGame();
                }
            }

            // --- 2. Xử lý Input từ Bàn Phím PC (WASD di chuyển, Space nhảy) ---
            HandlePCKeyboardInput();

            // --- 3. Xử lý Input từ Mobile UI (Joystick, Touch Look Zone, Buttons) ---
            HandleMobileTouchInput();
        }

        private void HandlePCKeyboardInput()
        {
            // Di chuyển bằng phím WASD / Mũi tên (Legacy Input fallback)
            try
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                {
                    move = new Vector2(h, v).normalized;
                }
                else if (dynamicJoystick == null || !dynamicJoystick.gameObject.activeInHierarchy || !dynamicJoystick.IsPressed)
                {
                    // Khi không nhấn phím nào VÀ joystick không bấm -> dừng di chuyển
                    #if !ENABLE_INPUT_SYSTEM
                    move = Vector2.zero;
                    #endif
                }
            }
            catch
            {
                // Fallback nếu Project Settings chỉ bật New Input System
            }

            // Nhảy bằng phím Space
            try
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    jump = true;
                }

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    sprint = true;
                }
                else if (mobileActions == null || !mobileActions.sprintHeld)
                {
                    sprint = false;
                }

                if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl))
                {
                    crouch = true;
                }
                else if (mobileActions == null || !mobileActions.crouchHeld)
                {
                    crouch = false;
                }
            }
            catch
            {
                // Ignore legacy input exceptions
            }
        }

        private void HandleMobileTouchInput()
        {
            // --- Mobile Joystick: Di chuyển ---
            if (dynamicJoystick != null && dynamicJoystick.gameObject.activeInHierarchy)
            {
                if (dynamicJoystick.IsPressed)
                {
                    MoveInput(dynamicJoystick.Direction);
                }
                else
                {
                    // Khi buông ngón tay khỏi Joystick, reset move về 0 nếu không có phím WASD nào đang bấm
                    try
                    {
                        float h = Input.GetAxisRaw("Horizontal");
                        float v = Input.GetAxisRaw("Vertical");
                        if (Mathf.Abs(h) <= 0.01f && Mathf.Abs(v) <= 0.01f)
                        {
                            MoveInput(Vector2.zero);
                        }
                    }
                    catch
                    {
                        MoveInput(Vector2.zero);
                    }
                }
            }

            // --- Mobile Touch Look Zone: Vuốt chuột/ngón tay ở nửa phải màn hình để xoay camera ---
            if (touchLookZone != null && touchLookZone.gameObject.activeInHierarchy)
            {
                if (touchLookZone.IsTouching)
                {
                    LookInput(touchLookZone.LookDelta);
                }
                else
                {
                    LookInput(Vector2.zero);
                }
            }

            // --- Mobile Action Buttons: Jump, Sprint, Crouch ---
            if (mobileActions != null && mobileActions.gameObject.activeInHierarchy)
            {
                if (mobileActions.jumpPressed)
                {
                    JumpInput(true);
                }

                if (mobileActions.sprintHeld)
                {
                    SprintInput(true);
                }

                if (mobileActions.crouchHeld)
                {
                    CrouchInput(true);
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
        {
            Vector2 v = value.Get<Vector2>();
            if (v.sqrMagnitude > 0.01f || (dynamicJoystick == null || !dynamicJoystick.IsPressed))
            {
                MoveInput(v);
            }
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>() * lookSensitivity);
            }
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void OnCrouch(InputValue value)
        {
            CrouchInput(value.isPressed);
        }
#endif

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        } 

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        public void CrouchInput(bool newCrouchState)
        {
            crouch = newCrouchState;
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetCursorLocked(bool locked)
        {
            cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}