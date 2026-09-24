using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class PlayerController : MonoBehaviour
    {
        [Header("👤 Player Configuration & Runtime Stats")]
        [Tooltip("ScriptableObject chứa hồ sơ và chỉ số gốc của nhân vật (Tên, Speed, Stamina, Weight...)")]
        public PlayerSO playerData;

        [Tooltip("Component quản lý toàn bộ chỉ số runtime đang diễn ra (Stamina, Weight, Speed...)")]
        public PlayerStats stats;

        /// <summary>
        /// Thuộc tính tương thích ngược để các script và hàm hiện tại truy cập player.xxx không bị lỗi.
        /// </summary>
        public PlayerStats player => stats;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        [Tooltip("AudioSource phát tiếng bước chân và tiếp đất (tự động lấy hoặc tạo trên Player)")]
        public AudioSource footstepAudioSource;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("Độ hạ thấp vị trí Camera Target khi đang cúi (Crouching)")]
        public float crouchCameraYOffset = -0.45f;
        private Vector3 _startCameraTargetLocalPos;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        public float targetSpeed = 0;
        private float _speed;
        [HideInInspector] public float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        [Header("Setting")]
        private UI_Manager ui;
        public float rangeInteract = 2f;
        public HotbarManager hotbarManager;

        [Header("🎒 Inventory Component")]
        public PlayerInventory inventory;
        public List<GameObject> heldItem => (inventory != null) ? inventory.heldItems : _emptyHeldList;
        private static readonly List<GameObject> _emptyHeldList = new List<GameObject>();
        public bool isTaking => inventory != null && inventory.isTaking;

        [Header("📱 Mobile Action Buttons")]
        public MobileActionButtons mobileActions;

        [Header("👀 Head Look Settings")]
        public PlayerHeadLook headLook;

        private CharacterController characterController;
        private Vector3 StartCenter;
        private float StartHeight;

        // Biến mới để xử lý Stamina Cooldown
        private float _staminaRegenTimer;
        private bool _canSprint = true;

        public GameObject lightD;
        public GameObject[] audioSource;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        [HideInInspector] public Animator _animator;
        private CharacterController _controller;
        [HideInInspector] public StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            // Tự động tìm hoặc gắn component PlayerStats
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
                if (stats == null)
                {
                    stats = gameObject.AddComponent<PlayerStats>();
                }
            }

            // Tự động tìm hoặc gắn component PlayerInventory
            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
                if (inventory == null)
                {
                    inventory = gameObject.AddComponent<PlayerInventory>();
                }
            }

            // Nạp dữ liệu cấu hình từ GameSession (nếu có) hoặc playerData từ Inspector
            if (GameSession.SelectedPlayer != null)
            {
                playerData = GameSession.SelectedPlayer;
            }

            if (stats != null && playerData != null)
            {
                stats.InitializeFromData(playerData);
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            if (CinemachineCameraTarget != null)
            {
                _startCameraTargetLocalPos = CinemachineCameraTarget.transform.localPosition;
            }
            characterController = GetComponent<CharacterController>();
            deathHandler = GetComponent<PlayerDeathHandler>();
            ui = FindFirstObjectByType<UI_Manager>();
            if (hotbarManager == null)
            {
                hotbarManager = FindFirstObjectByType<HotbarManager>(FindObjectsInactive.Include);
            }
            if (mobileActions == null)
            {
                mobileActions = FindFirstObjectByType<MobileActionButtons>(FindObjectsInactive.Include);
            }
            StartCenter = characterController.center;
            StartHeight = characterController.height;

            lightD.SetActive(false);
            die = false;

            if (stats != null)
            {
                stats.isDied = false;
                stats.currweight = 0;
                stats.currpoint = 0;
                stats.currentStamina = stats.MaxStamina;
                stats.CalculateWeightSpeedPenalty();
            }

            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), false);

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            if (GetComponent<PlayerInteraction>() == null)
            {
                gameObject.AddComponent<PlayerInteraction>();
            }
            if (GetComponent<PlayerHeadLook>() == null)
            {
                headLook = gameObject.AddComponent<PlayerHeadLook>();
            }
            else
            {
                headLook = GetComponent<PlayerHeadLook>();
            }
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // Khởi tạo AudioSource cho tiếng bước chân / tiếp đất
            if (footstepAudioSource == null)
            {
                Transform footstepChild = transform.Find("AudioManager/FootStep") ?? transform.Find("FootStep") ?? transform.Find("AudioManager");
                if (footstepChild != null)
                {
                    footstepAudioSource = footstepChild.GetComponent<AudioSource>();
                }
                if (footstepAudioSource == null)
                {
                    footstepAudioSource = GetComponentInChildren<AudioSource>(true);
                }
                if (footstepAudioSource == null)
                {
                    footstepAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (footstepAudioSource != null)
            {
                footstepAudioSource.spatialBlend = 1.0f; // 3D Spatial Audio
                footstepAudioSource.playOnAwake = false;
                footstepAudioSource.minDistance = 1.0f;
                footstepAudioSource.maxDistance = 15.0f;
                footstepAudioSource.rolloffMode = AudioRolloffMode.Linear;

                if (SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
                {
                    footstepAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
                }
            }

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }
        private bool die = false;

        [HideInInspector] public PlayerDeathHandler deathHandler;

        private void Update()
        {
            var netSync = GetComponent<NetworkPlayerSync>();
            if (netSync != null && !netSync.IsLocalPlayer)
            {
                return; // Remote players are controlled over network, do not run local Update
            }

            if (player != null && player.currentTime <= 0f && player.maxTime > 0f) { player.isDied = true; }

            if (player.isDied)
            {
                if (deathHandler != null)
                {
                    deathHandler.TriggerDeath();
                }
                else if (!die)
                {
                    if (_animator != null) { _animator.SetTrigger("Die"); }
                    Debug.Log($"Player died. Dropping items.");
                    if (inventory != null)
                    {
                        inventory.DropAllItemsOnDeath();
                    }
                    player.currweight = 0;
                    if (audioSource != null && audioSource.Length > 0 && audioSource[0] != null) audioSource[0].SetActive(true);
                    if (lightD != null) lightD.SetActive(true);
                    die = true;

                    Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), true);
                }

                // Continue applying gravity and move the controller down so the player falls instead of hovering.
                JumpAndGravity();
                GroundedCheck();
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

                // Ensure player stays snapped to ground when landed
                if (Grounded && _verticalVelocity <= 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                return;
            }
            else if (player != null && player.totalpoint > 0 && player.currpoint >= player.totalpoint)
            {
                return;
            }

            _hasAnimator = TryGetComponent(out _animator);

            HandleStamina(); // Gọi hàm xử lý Stamina
            LadderClimb(); if (isClimbingLadder) { return; }
            JumpAndGravity();
            GroundedCheck();
            Move();
            if (inventory != null)
            {
                inventory.UpdateHoldingState();
            }
        }

        // --- HÀM XỬ LÝ STAMINA (Ủy quyền cho PlayerStats) ---
        private void HandleStamina()
        {
            if (stats != null)
            {
                bool isMoving = _input.move.magnitude > 0 && !Crouching;
                stats.HandleStamina(_input.sprint, isMoving, Crouching, Time.deltaTime);
                _canSprint = stats.canSprint;
            }
            else
            {
                _canSprint = true;
            }
        }

        // =========================================================================
        //               INVENTORY FORWARDERS (Ủy quyền cho PlayerInventory)
        // =========================================================================

        public void WeightCacul()
        {
            if (inventory != null)
            {
                inventory.UpdateWeight();
            }
            else if (stats != null)
            {
                stats.CalculateWeightSpeedPenalty();
            }
        }

        public void TakeItem()
        {
            if (inventory != null)
            {
                inventory.UpdateHoldingState();
            }
        }

        public bool TryPickupItem(GameObject itemObj, int itemKg)
        {
            if (inventory != null)
            {
                return inventory.TryPickupItem(itemObj, itemKg);
            }
            return false;
        }

        public void DropLastItem()
        {
            if (inventory != null)
            {
                inventory.DropLastItem();
            }
        }

        [Header("🪜 Ladder Settings")]
        [Tooltip("Cho phép leo thang hay không (canClimb). Nếu bị tắt (ví dụ do stun, online sync, mất quyền leo), người chơi sẽ lập tức buông tay rơi khỏi thang")]
        public bool canClimb = true;
        public bool isClimbingLadder = false;
        public void LadderClimb()
        {
            if (_animator == null) return;

            if (isClimbingLadder)
            {
                _animator.SetBool("Climb", true);
            }
            else
            {
                _animator.SetBool("Climb", false);
                if (_animator.speed <= 0.01f)
                {
                    _animator.speed = 1.0f;
                }
            }
        }
        private void LateUpdate()
        {
            HandleCameraInput();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        [Header("🪜 Ladder Camera Settings")]
        [Tooltip("Giới hạn góc quay camera trái/phải (độ) khi đang leo thang")]
        public float ladderYawClamp = 100.0f;
        private float _ladderBaseYaw = 0.0f;

        /// <summary>
        /// Khóa góc nhìn camera hướng vào mặt thang và kích hoạt giới hạn góc quay trái/phải
        /// </summary>
        public void SetLadderCameraFacing(float targetYaw)
        {
            _cinemachineTargetYaw = targetYaw;
            _ladderBaseYaw = targetYaw;
            _cinemachineTargetPitch = 0.0f;
        }

        /// <summary>
        /// Hàm xử lý riêng cho Input và xoay Camera
        /// </summary>
        public void HandleCameraInput()
        {
            // Kiểm tra trạng thái giải đố bẻ khóa, chết hoặc thắng game -> tạm dừng xoay camera
            if (UI_Manager.isSolving || (player != null && (player.isDied || (player.totalpoint > 0 && player.currpoint >= player.totalpoint))))
            {
                return;
            }

            if (CinemachineCameraTarget == null) return;

            // Xử lý góc xoay camera từ tín hiệu đầu vào mouse/look
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // Cả Mouse lẫn TouchLookZone đều là delta theo pixel/frame,
                // chỉ Gamepad (analog stick) mới cần nhân Time.deltaTime.
                bool isDirectDelta = IsCurrentDeviceMouse || (_input != null && _input.touchLookZone != null && _input.touchLookZone.IsTouching);
                float deltaTimeMultiplier = isDirectDelta ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // Giới hạn góc xoay cho Yaw và Pitch
            if (isClimbingLadder)
            {
                // Khi đang leo thang: Giới hạn góc quay camera trái/phải quanh hướng thang (không cho quay 360 độ ra sau)
                _cinemachineTargetYaw = Mathf.Clamp(_cinemachineTargetYaw, _ladderBaseYaw - ladderYawClamp, _ladderBaseYaw + ladderYawClamp);
            }
            else
            {
                // Giới hạn góc xoay 360 độ thông thường cho Yaw
                _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            }
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cập nhật góc xoay cho đối tượng theo dõi Cinemachine
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f
            );
        }

        [HideInInspector] public bool Crouching = false;
        private bool _lastCrouchState = false;
        private void Move()
        {
            if (UI_Manager.isSolving || (player != null && player.totalpoint > 0 && player.currpoint >= player.totalpoint))
            {
                _speed = 0f;
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, 0f);
                    _animator.SetFloat(_animIDMotionSpeed, 1f);
                }
                return;
            }

            // 1. Cập nhật trạng thái Crouching (Keyboard + Mobile)
            bool isCrouchInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
            if (_input != null && _input.crouch)
                isCrouchInput = true;
            if (mobileActions != null && mobileActions.crouchHeld)
                isCrouchInput = true;

            if (isCrouchInput && Grounded)
            {
                // Kích hoạt Crouch
                Crouching = true;
            }
            else
            {
                Crouching = false;
                // set target speed based on move speed, sprint speed and if sprint is pressed

                bool isSprintingAllowed = _input.sprint && _canSprint;
                targetSpeed = isSprintingAllowed ? player._SprintSpeed : player._MoveSpeed;
            }

            // Chỉ thay đổi CharacterController center và height khi thay đổi trạng thái Crouch (tránh reset PhysX capsule mỗi frame)
            if (Crouching != _lastCrouchState)
            {
                _lastCrouchState = Crouching;
                if (Crouching)
                {
                    characterController.center = new Vector3(StartCenter.x, 0.77f, StartCenter.z);
                    characterController.height = 1.46f;
                }
                else
                {
                    characterController.center = new Vector3(StartCenter.x, StartCenter.y, StartCenter.z);
                    characterController.height = StartHeight;
                }
            }

            if (Crouching)
            {
                if (CinemachineCameraTarget != null)
                {
                    Vector3 crouchTargetPos = _startCameraTargetLocalPos + new Vector3(0f, crouchCameraYOffset, 0f);
                    CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                        CinemachineCameraTarget.transform.localPosition,
                        crouchTargetPos,
                        Time.deltaTime * 8f
                    );
                }

                _animator.SetBool("Crouch", true);
                targetSpeed = (_input.move == Vector2.zero) ? 0.0f : player.crouchSpeed;
                _animator.SetBool("IsCrouching", targetSpeed >= 0.1f);
            }
            else
            {
                if (CinemachineCameraTarget != null)
                {
                    CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                        CinemachineCameraTarget.transform.localPosition,
                        _startCameraTargetLocalPos,
                        Time.deltaTime * 8f
                    );
                }

                _animator.SetBool("IsCrouching", false);
                _animator.SetBool("Crouch", false);

                bool isSprintingAllowed = _input.sprint && _canSprint;
                targetSpeed = isSprintingAllowed ? player._SprintSpeed : player._MoveSpeed;
            }

            WeightCacul();
            float rawMoveMagnitude = _input.move.magnitude;
            if (rawMoveMagnitude < 0.01f)
            {
                targetSpeed = 0.0f;
            }
            if (inventory != null && inventory.isTaking)
            {
                targetSpeed = Mathf.Min(targetSpeed, 0.3f); // bị ép chậm nhưng animation blend vẫn mượt
            }

            float inputMagnitude = _input.analogMovement ? rawMoveMagnitude : (rawMoveMagnitude > 0.01f ? 1f : 0f);
            float desiredSpeed = targetSpeed * inputMagnitude;

            // Tăng tốc hoặc giảm tốc mượt mà dựa trên _speed hiện tại (dừng dứt khoát khi nhả nút/joystick)
            if (desiredSpeed <= 0.001f)
            {
                _speed = Mathf.Lerp(_speed, 0f, Time.deltaTime * SpeedChangeRate * 2.5f);
                if (_speed < 0.05f) _speed = 0f;

                _animationBlend = Mathf.Lerp(_animationBlend, 0f, Time.deltaTime * SpeedChangeRate * 2.5f);
                if (_animationBlend < 0.05f) _animationBlend = 0f;
            }
            else
            {
                _speed = Mathf.Lerp(_speed, desiredSpeed, Time.deltaTime * SpeedChangeRate);
                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            }

            // Player luôn xoay mặt theo góc Y của Camera (kể cả khi đứng yên hay di chuyển)
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera") ?? (Camera.main != null ? Camera.main.gameObject : null);
            }

            _targetRotation = _mainCamera != null ? _mainCamera.transform.eulerAngles.y : transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

            // Tính hướng di chuyển tương đối theo góc nhìn Camera khi bấm W, A, S, D
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * inputDirection;

            // Di chuyển CharacterController
            _controller.Move(targetDirection * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }

            // Xử lý phát tiếng bước chân (Tự động thích ứng cả khi Animation không có AnimationEvent)
            HandleProceduralFootsteps();
        }

        private void JumpAndGravity()
        {
            if (UI_Manager.isSolving)
            {
                _input.jump = false;
            }

            // NGĂN NHẢY KHI ĐANG CÚI
            if (Crouching)
            {
                _input.jump = false;
                _animator.SetBool(_animIDFreeFall, false);
                return;
            }

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !Crouching)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);

            // Vẽ gizmos phạm vi tương tác
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, rangeInteract);
        }

        // ⭐ HÀM PUBLIC ĐỂ PLAYERDEATHHANDLER GỌI KHI PLAYER CHẾT
        public void DropItemsOnDeathPublic()
        {
            if (inventory != null)
            {
                inventory.DropAllItemsOnDeath();
            }
        }

        private float _footstepTimer = 0.2f;
        private bool _hasReceivedAnimFootstepEvent = false;

        /// <summary>
        /// Xử lý phát tiếng bước chân theo nhịp di chuyển (hoạt động kể cả khi animation clip không có Animation Event)
        /// </summary>
        private void HandleProceduralFootsteps()
        {
            if (_hasReceivedAnimFootstepEvent) return; // Ưu tiên Animation Event nếu clip đã có

            if (Grounded && _input != null && _input.move.sqrMagnitude > 0.01f && _speed > 0.4f)
            {
                _footstepTimer -= Time.deltaTime;
                float stepInterval = _input.sprint ? 0.32f : (Crouching ? 0.58f : 0.44f);

                if (_footstepTimer <= 0f)
                {
                    _footstepTimer = stepInterval;
                    PlayFootstepSound();
                }
            }
            else
            {
                _footstepTimer = 0.15f;
            }
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
            _hasReceivedAnimFootstepEvent = true;
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                PlayFootstepSound();
            }
        }

        public void OnFootstep()
        {
            _hasReceivedAnimFootstepEvent = true;
            PlayFootstepSound();
        }

        private void PlayFootstepSound()
        {
            AudioClip clipToPlay = null;

            if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                var validClips = System.Array.FindAll(FootstepAudioClips, c => c != null);
                if (validClips.Length > 0)
                {
                    clipToPlay = validClips[Random.Range(0, validClips.Length)];
                }
            }

            if (clipToPlay == null && footstepAudioSource != null && footstepAudioSource.clip != null)
            {
                clipToPlay = footstepAudioSource.clip;
            }

            if (clipToPlay != null)
            {
                float vol = FootstepAudioVolume > 0.01f ? FootstepAudioVolume : 0.5f;

                if (footstepAudioSource != null)
                {
                    footstepAudioSource.PlayOneShot(clipToPlay, vol);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clipToPlay, transform.TransformPoint(_controller.center), vol);
                }
            }
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                TriggerLandSound();
            }
        }

        public void OnLand()
        {
            TriggerLandSound();
        }

        private void TriggerLandSound()
        {
            if (LandingAudioClip != null)
            {
                float vol = FootstepAudioVolume > 0.01f ? FootstepAudioVolume : 0.5f;
                if (footstepAudioSource != null)
                {
                    footstepAudioSource.PlayOneShot(LandingAudioClip, vol);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), vol);
                }
            }
        }
    }
}