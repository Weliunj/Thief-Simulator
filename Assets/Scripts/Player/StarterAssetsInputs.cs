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
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;
        
        // --- THAY ĐỔI 1: Thêm biến chỉnh độ nhạy ---
        [Tooltip("Điều chỉnh tốc độ chuột/camera")]
        public float lookSensitivity = 1.0f;

        [Header("🕹️ Mobile Touch Controls")]
        public DynamicJoystick dynamicJoystick;
        public TouchLookZone touchLookZone;
        public MobileActionButtons mobileActions;

        private void Start()
        {
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

            UpdateSensitivity(SettingsManager.Instance != null ? SettingsManager.Instance.Sensitivity : lookSensitivity);
        }

        private void OnEnable()
        {
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
            // --- Mobile Joystick: Di chuyển ---
            if (dynamicJoystick != null)
            {
                if (dynamicJoystick.IsPressed)
                {
                    MoveInput(dynamicJoystick.Direction);
                }
                else
                {
                    // Khi thả tay → reset movement về zero để nhân vật dừng lại
                    MoveInput(Vector2.zero);
                }
            }

            // --- Mobile Touch: Xoay camera ---
            if (touchLookZone != null)
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
            if (mobileActions != null)
            {
                // Jump: bấm 1 lần → set true (ThirdPersonController sẽ tự reset)
                if (mobileActions.jumpPressed)
                {
                    JumpInput(true);
                }

                // Sprint: giữ = true, thả = false
                SprintInput(mobileActions.sprintHeld);

                // Crouch: giữ/toggle = true, thả = false
                CrouchInput(mobileActions.crouchHeld);
            }
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if(cursorInputForLook)
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
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}