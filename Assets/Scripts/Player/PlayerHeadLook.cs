using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// Component điều khiển đầu (và thân trên) của nhân vật xoay tự nhiên theo hướng nhìn của Camera (Procedural Head Look).
    /// Hoạt động trong LateUpdate() sau khi Animator đã tính toán xong animation của frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHeadLook : MonoBehaviour
    {
        [Header("🎯 Head Look Settings")]
        [Tooltip("Bật/tắt tính năng xoay đầu theo Camera")]
        public bool enableHeadLook = true;

        [Tooltip("Camera chính để theo dõi hướng nhìn (nếu để trống sẽ tự lấy Camera.main)")]
        public Camera targetCamera;

        [Header("🦴 Bones References")]
        [Tooltip("Transform xương đầu (tự động phát hiện nếu để trống)")]
        public Transform headBone;

        [Tooltip("Transform xương ngực/cột sống (tùy chọn, giúp chia đều chuyển động tự nhiên)")]
        public Transform spineBone;

        [Header("⚖️ Weight & Distribution")]
        [Range(0f, 1f)]
        [Tooltip("Trọng số tổng thể của việc nhìn theo Camera")]
        public float lookWeight = 1.0f;

        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ phân bổ góc quay cho xương Head")]
        public float headWeight = 0.75f;

        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ phân bổ góc quay cho xương Spine/Chest")]
        public float spineWeight = 0.25f;

        [Tooltip("Tốc độ xoay mượt của đầu bám theo Camera")]
        public float smoothSpeed = 12.0f;

        [Tooltip("Tốc độ chuyển đổi trọng số khi bật/tắt (ví dụ khi chết hoặc vào Minigame)")]
        public float weightTransitionSpeed = 6.0f;

        [Header("📐 Angle Limits (Góc giới hạn)")]
        [Range(10f, 90f)]
        [Tooltip("Góc xoay trái/phải tối đa của đầu so với hướng thân người (tránh gãy cổ)")]
        public float maxYawAngle = 75.0f;

        [Range(10f, 80f)]
        [Tooltip("Góc ngửa đầu lên tối đa")]
        public float maxUpPitch = 60.0f;

        [Range(10f, 80f)]
        [Tooltip("Góc cúi đầu xuống tối đa")]
        public float maxDownPitch = 45.0f;

        [Header("🎮 Context Control")]
        [Tooltip("Cho phép xoay đầu khi đang leo thang")]
        public bool enableWhileClimbing = false;

        [Tooltip("Cho phép xoay đầu khi đang di chuyển (chạy/đi bộ)")]
        public bool enableWhileMoving = true;

        // References nội bộ
        private PlayerController _playerController;
        private PlayerStats _playerStats;
        private Animator _animator;

        // Runtime states
        private float _currentYaw = 0f;
        private float _currentPitch = 0f;
        private float _currentWeight = 0f;

        public float CurrentYaw => _currentYaw;
        public float CurrentPitch => _currentPitch;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerStats = GetComponent<PlayerStats>();
            _animator = GetComponentInChildren<Animator>();

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            AutoFindBones();
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        /// <summary>
        /// Tự động quét và tìm xương Head / Spine từ Animator hoặc Hierarchy
        /// </summary>
        public void AutoFindBones()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            // 1. Thử lấy từ Humanoid Avatar
            if (_animator != null && _animator.isHuman)
            {
                if (headBone == null)
                {
                    headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
                }

                if (spineBone == null)
                {
                    spineBone = _animator.GetBoneTransform(HumanBodyBones.Chest) 
                             ?? _animator.GetBoneTransform(HumanBodyBones.Spine);
                }
            }

            // 2. Fallback: Quét theo tên trong Hierarchy
            if (headBone == null)
            {
                headBone = FindDeepChild(transform, "head");
            }

            if (spineBone == null)
            {
                spineBone = FindDeepChild(transform, "spine") 
                         ?? FindDeepChild(transform, "chest");
            }
        }

        private Transform FindDeepChild(Transform parent, string keyword)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(keyword.ToLower()))
                {
                    return child;
                }

                Transform result = FindDeepChild(child, keyword);
                if (result != null) return result;
            }
            return null;
        }

        private void LateUpdate()
        {
            // Tự động tìm lại camera nếu bị mất (ví dụ khi load scene)
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            // Đảm bảo có xương head
            if (headBone == null)
            {
                AutoFindBones();
                if (headBone == null) return;
            }

            // Kiểm tra trạng thái hợp lệ để kích hoạt nhìn theo Camera
            bool shouldLook = enableHeadLook;

            // Tạm dừng khi chết
            if (_playerStats != null && _playerStats.isDied) shouldLook = false;
            if (_playerController != null)
            {
                if (_playerController.stats != null && _playerController.stats.isDied) shouldLook = false;
                if (_playerController.deathHandler != null && _playerController.deathHandler.isDeadProcessed) shouldLook = false;
            }

            // Tạm dừng khi đang giải đố Minigame bẻ khóa
            if (UI_Manager.isSolving) shouldLook = false;

            // Tạm dừng khi leo thang nếu không được cho phép
            if (!enableWhileClimbing && _playerController != null && _playerController.isClimbingLadder)
            {
                shouldLook = false;
            }

            // Tính toán trọng số mục tiêu
            float targetWeight = shouldLook ? lookWeight : 0f;
            _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, Time.deltaTime * weightTransitionSpeed);

            if (_currentWeight <= 0.001f)
            {
                _currentYaw = Mathf.Lerp(_currentYaw, 0f, Time.deltaTime * smoothSpeed);
                _currentPitch = Mathf.Lerp(_currentPitch, 0f, Time.deltaTime * smoothSpeed);
                return;
            }

            // 1. Tính toán hướng nhìn của Camera
            Vector3 camForward = targetCamera.transform.forward;

            // 2. Chiếu hướng nhìn lên mặt phẳng ngang của nhân vật để tính Yaw (trái/phải)
            Vector3 horizontalCamForward = Vector3.ProjectOnPlane(camForward, transform.up).normalized;
            if (horizontalCamForward.sqrMagnitude < 0.0001f)
            {
                horizontalCamForward = transform.forward;
            }

            float angleYaw = Vector3.SignedAngle(transform.forward, horizontalCamForward, transform.up);

            // 3. Tính Pitch (ngửa lên / cúi xuống)
            // Khi nhìn lên: camForward.y > 0 -> pitch dương
            // Khi nhìn xuống: camForward.y < 0 -> pitch âm
            float rawPitch = Mathf.Asin(Mathf.Clamp(camForward.y, -1f, 1f)) * Mathf.Rad2Deg;

            // 4. Áp dụng giới hạn góc (Clamping)
            float clampedYaw = Mathf.Clamp(angleYaw, -maxYawAngle, maxYawAngle);
            float clampedPitch = Mathf.Clamp(rawPitch, -maxDownPitch, maxUpPitch);

            // 5. Nội suy mượt mà
            _currentYaw = Mathf.Lerp(_currentYaw, clampedYaw, Time.deltaTime * smoothSpeed);
            _currentPitch = Mathf.Lerp(_currentPitch, clampedPitch, Time.deltaTime * smoothSpeed);

            // 6. Áp dụng xoay lên xương (Spine & Head)
            // Xoay Spine (nếu có)
            if (spineBone != null && spineWeight > 0.001f)
            {
                float sWeight = spineWeight * _currentWeight;
                Quaternion spineRot = Quaternion.AngleAxis(_currentYaw * sWeight, transform.up)
                                    * Quaternion.AngleAxis(-_currentPitch * sWeight, transform.right);

                spineBone.rotation = spineRot * spineBone.rotation;
            }

            // Xoay Head
            if (headBone != null && headWeight > 0.001f)
            {
                float hWeight = headWeight * _currentWeight;
                Quaternion headRot = Quaternion.AngleAxis(_currentYaw * hWeight, transform.up)
                                   * Quaternion.AngleAxis(-_currentPitch * hWeight, transform.right);

                headBone.rotation = headRot * headBone.rotation;
            }
        }
    }
}
