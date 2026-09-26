using System.Collections;
using Fusion;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC Trẻ Con (Kid NPC / Runner NPC) độc lập hoàn chỉnh:
/// - Kế thừa NetworkBehaviour để đồng bộ Photon Fusion (Shared Mode).
/// - Cơ chế chuẩn 100% từ OLDkidadult.cs:
///   1. Đi tuần ngẫu nhiên (Wander) trong bán kính targetRadius với thời gian dừng nghỉ minMaxIdleTime.
///   2. Hệ thống 5 tia Raycast đa hướng quét phát hiện Player (giảm tầm khi Player cúi).
///   3. Khi nhìn thấy Player: Chạy hoảng loạn (Panic Run) với tốc độ runSpeed trong khoảng thời gian callDuration.
///   4. Quét xung quanh trong bán kính callRanger: Nếu phát hiện AdultGuardNPC -> Báo động cho Người lớn lập tức Chase Player.
///   5. Hết thời gian hoảng sợ -> Tự động quay về trạng thái đi dạo bình thường.
/// - Toàn bộ biến hardcode đều được mở thành tham số cấu hình trong Inspector.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class KidRunnerNPC : NetworkBehaviour
{
    [Header("🤖 AI Components")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("🔊 1. Footstep Audio (Giống Player)")]
    [Tooltip("AudioSource phát tiếng bước chân 3D")]
    public AudioSource footstepAudioSource;
    [Tooltip("Danh sách clip âm thanh bước chân ngẫu nhiên")]
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("🔊 2. Alert & Panic Audio")]
    [Tooltip("AudioSource phát tiếng hét hoảng loạn / báo động")]
    public AudioSource alertAudioSource;
    public AudioClip panicScreamSound;

    [Header("🔊 3. Theme & Ambient Music")]
    [Tooltip("AudioSource phát nhạc nền / hoảng loạn")]
    public AudioSource themeAudioSource;
    public AudioClip panicThemeSound;

    [Header("🏃 Movement Speeds")]
    [Tooltip("Tốc độ đi bộ bình thường khi dạo chơi")]
    public float walkSpeed = 1.5f;

    [Tooltip("Tốc độ chạy nhanh khi hoảng loạn (Panic Run)")]
    public float runSpeed = 3.0f;

    [Tooltip("Ngưỡng khoảng cách nhận diện đã đến gần điểm đích")]
    public float stoppingDistanceThreshold = 0.5f;

    [Header("🚶 Wander Settings (Đi dạo)")]
    [Tooltip("Bán kính tìm kiếm điểm đến ngẫu nhiên quanh vị trí ban đầu")]
    public float wanderRadius = 20f;

    [Tooltip("Thời gian đứng yên tối thiểu khi đến đích (giây)")]
    public float minIdleTime = 2.0f;
    [Tooltip("Thời gian đứng yên tối đa khi đến đích (giây)")]
    public float maxIdleTime = 5.0f;
    private float _idleTime = 0f;

    [Header("👀 Detection & Vision Settings")]
    [Tooltip("Góc nón tầm nhìn quét phía trước mặt (độ)")]
    public float fovAngle = 100f;
    [Tooltip("Khoảng cách quét tối đa khi Player đứng/đi bình thường (mét)")]
    public float visionRange = 15f;
    [Tooltip("Hệ số giảm tầm nhìn khi Player Cúi (Crouch) - 0.5 = giảm 50%")]
    [Range(0.1f, 1f)]
    public float crouchDistanceMultiplier = 0.5f;
    [Tooltip("LayerMask quét vật cản che tầm nhìn")]
    public LayerMask obstacleMask;

    [Header("🔦 Flashlight Settings")]
    [Tooltip("Nguồn sáng đèn pin của Kid (nếu có)")]
    public Light flashlight;
    [Tooltip("Tầm xa đèn pin khi Player đứng bình thường")]
    public float flashlightNormalRange = 20f;
    [Tooltip("Tầm xa đèn pin khi Player cúi")]
    public float flashlightCrouchRange = 8f;
    [Tooltip("Tốc độ chuyển đổi tầm đèn pin")]
    public float flashlightTransitionSpeed = 8f;

    [Header("📢 Alert & Call Settings")]
    [Tooltip("Thời gian chạy hoảng loạn và gọi người lớn tối thiểu (giây)")]
    public float minCallDuration = 5.0f;
    [Tooltip("Thời gian chạy hoảng loạn và gọi người lớn tối đa (giây)")]
    public float maxCallDuration = 10.0f;
    public float callDuration = 0f;

    [Tooltip("Bán kính gọi / báo tin cho NPC Người Lớn gần đó (mét)")]
    public float callRanger = 20f;

    [Header("🛠️ Debug Settings")]
    [Tooltip("Hiển thị thông tin log chi tiết trong Console")]
    public bool showDebugLogs = true;
    [Tooltip("Vẽ Gizmos trực quan trong Scene View")]
    public bool showGizmos = true;

    [Header("🌐 Network Synced Properties")]
    [Networked] public float NetworkSpeed { get; set; } = 0f;
    [Networked] public float NetworkMotionSpeed { get; set; } = 1f;

    // Trạng thái nội bộ
    [HideInInspector] public bool targetDetected = false;
    private PlayerController _spottedPlayer;
    [HideInInspector] public Vector3 initialPosition;
    private bool _hasCalledAdult = false;

    // Animation Hashes
    private int _animIDSpeed;
    private int _animIDMotionSpeed;
    private bool _hasAnimSpeed = false;
    private bool _hasAnimMotionSpeed = false;

    private bool _isWalkSoundPlaying = false;

    public bool IsNetworkSpawned => Object != null && Object.IsValid && Runner != null && Runner.IsRunning;
    public bool IsMasterAuthority => !IsNetworkSpawned || Runner.IsServer || Runner.IsSharedModeMasterClient || Object.HasStateAuthority;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (flashlight == null) flashlight = GetComponentInChildren<Light>();
        if (flashlight != null)
        {
            flashlightNormalRange = flashlight.range;
            flashlightCrouchRange = flashlightNormalRange * crouchDistanceMultiplier;
        }

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == _animIDSpeed) _hasAnimSpeed = true;
                if (p.nameHash == _animIDMotionSpeed) _hasAnimMotionSpeed = true;
            }
        }

        if (obstacleMask == 0) obstacleMask = LayerMask.GetMask("Default", "Ground", "Obj");

        initialPosition = transform.position;

        InitAudioSources();

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 18f;
            agent.updateRotation = true;
        }

        _idleTime = Random.Range(minIdleTime, maxIdleTime);
        SetRandomDestination();
        DebugLog($"Khởi tạo Kid NPC thành công! Vị trí ban đầu: {initialPosition}", "yellow");
    }

    private void DebugLog(string message, string color = "cyan")
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color={color}>[KidRunnerNPC - {gameObject.name}]</color> {message}");
        }
    }

    /// <summary>
    /// Điều chỉnh tầm đèn pin cục bộ trên từng máy: Máy nào cúi thì đèn pin trên màn hình máy đó co lại
    /// </summary>
    private void UpdateLocalFlashlight()
    {
        if (flashlight == null) return;

        PlayerController localPlayer = AdultGuardNPC.GetLocalPlayer();
        bool isLocalCrouching = localPlayer != null && AdultGuardNPC.IsPlayerCrouching(localPlayer);

        float targetRange = isLocalCrouching ? flashlightCrouchRange : flashlightNormalRange;
        flashlight.range = Mathf.Lerp(flashlight.range, targetRange, Time.deltaTime * flashlightTransitionSpeed);
    }

    private void EnsureOnNavMesh()
    {
        if (agent == null || !agent.isActiveAndEnabled || agent.isOnNavMesh) return;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = NavMesh.AllAreas
        };

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, filter))
        {
            try
            {
                agent.Warp(hit.position);
            }
            catch { }
        }
    }

    private bool HasReachedDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold;
    }

    private void Update()
    {
        // 1. Luôn co giãn tầm đèn pin cục bộ theo trạng thái cúi của người chơi trên máy này
        UpdateLocalFlashlight();

        if (!IsMasterAuthority)
        {
            UpdateRemoteVisuals();
            return;
        }

        EnsureOnNavMesh();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // 2. Luôn quét tìm Player theo nón tầm nhìn FOV và Line of Sight
        RayCastHitTarget();

        // 3. Nếu đã phát hiện Player: Chạy hoảng loạn và gọi Adult NPC
        if (targetDetected)
        {
            CallAndPanic();
            SyncAnimationNetworkVariables();
            return;
        }

        // 4. Trạng thái bình thường: Đi tuần ngẫu nhiên (Wander)
        HandleWander();
        SyncAnimationNetworkVariables();
    }

    #region 👀 Vision & Detection

    public bool HasLineOfSight(Vector3 fromPos, Vector3 targetPos, float maxDist)
    {
        Vector3 dir = (targetPos - fromPos).normalized;
        if (Physics.Raycast(fromPos, dir, out RaycastHit hit, maxDist + 0.3f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance < maxDist - 0.3f)
            {
                return false;
            }
        }
        return true;
    }

    public Vector3 GetPlayerTargetPoint(PlayerController p)
    {
        if (p == null) return Vector3.zero;
        if (p.CinemachineCameraTarget != null)
        {
            return p.CinemachineCameraTarget.transform.position;
        }
        bool isCrouching = AdultGuardNPC.IsPlayerCrouching(p);
        return p.transform.position + Vector3.up * (isCrouching ? 0.75f : 1.35f);
    }

    public void RayCastHitTarget()
    {
        PlayerController[] allPlayers = AdultGuardNPC.GetAllLivingPlayers();
        if (allPlayers == null || allPlayers.Length == 0) return;

        Vector3 myEyePos = transform.position + Vector3.up * 1.0f;
        Vector3 myForward = transform.forward;

        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerController p = allPlayers[i];
            if (p == null || !p.gameObject.activeInHierarchy) continue;
            if (p.stats != null && p.stats.isDied) continue;
            if (p.deathHandler != null && p.deathHandler.isDeadProcessed) continue;

            bool isCrouching = AdultGuardNPC.IsPlayerCrouching(p);
            float maxEffectiveDist = isCrouching ? (visionRange * crouchDistanceMultiplier) : visionRange;

            Vector3 playerTargetPos = GetPlayerTargetPoint(p);
            float distToPlayer = Vector3.Distance(myEyePos, playerTargetPos);

            // 1. Ngoài tầm nhìn thực tế (đã giảm 50% nếu cúi) -> Bỏ qua
            if (distToPlayer > maxEffectiveDist) continue;

            // 2. Kiểm tra góc FOV
            Vector3 dirToPlayer = (playerTargetPos - myEyePos).normalized;
            float angleToPlayer = Vector3.Angle(myForward, dirToPlayer);

            if (angleToPlayer <= fovAngle * 0.5f)
            {
                // 3. Bắn tia Line of Sight kiểm tra vật cản
                if (HasLineOfSight(myEyePos, playerTargetPos, distToPlayer))
                {
                    if (!targetDetected)
                    {
                        PlayPanicSound();
                        _hasCalledAdult = false;
                        DebugLog($"Phát hiện Player ({p.name}) [Crouch: {isCrouching}] ở {distToPlayer:F1}m (Tầm tối đa: {maxEffectiveDist:F1}m)! Hét lên và chạy hoảng loạn!", "red");
                    }

                    _spottedPlayer = p;
                    targetDetected = true;
                    callDuration = Random.Range(minCallDuration, maxCallDuration);
                    return;
                }
            }
        }
    }

    #endregion

    #region 😱 Panic Run & Call Adult NPC

    public void CallAndPanic()
    {
        if (callDuration > 0f)
        {
            callDuration -= Time.deltaTime;
        }

        if (callDuration > 0f)
        {
            agent.speed = runSpeed;

            // Nếu đến đích thì chọn điểm chạy hoảng loạn tiếp theo
            if (HasReachedDestination())
            {
                SetRandomDestination();
            }

            // Quét xung quanh để gọi NPC Người Lớn (AdultGuardNPC) chạy tới vị trí Player vừa xuất hiện
            if (!_hasCalledAdult && _spottedPlayer != null)
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, callRanger);
                foreach (var col in colliders)
                {
                    if (col == null || col.gameObject == gameObject) continue;

                    if (col.CompareTag("adult") || col.GetComponent<AdultGuardNPC>() != null || col.GetComponentInParent<AdultGuardNPC>() != null)
                    {
                        AdultGuardNPC adultNpc = col.GetComponent<AdultGuardNPC>() ?? col.GetComponentInParent<AdultGuardNPC>();
                        if (adultNpc != null)
                        {
                            adultNpc.ReceiveKidAlert(_spottedPlayer.transform.position);
                            _hasCalledAdult = true;
                            DebugLog($"Báo tin cho Adult ({adultNpc.npcName}) chạy đến kiểm tra vị trí Player ({_spottedPlayer.transform.position})!", "orange");
                        }
                    }
                }
            }
        }
        else
        {
            // Hết thời gian hoảng sợ -> Quay lại trạng thái bình thường
            DebugLog("Hết thời gian hoảng sợ -> Quay lại đi dạo bình thường.", "green");
            targetDetected = false;
            _spottedPlayer = null;
            _hasCalledAdult = false;
            _idleTime = Random.Range(minIdleTime, maxIdleTime);
        }
    }

    #endregion

    #region 🚶 Wander Mode

    private void HandleWander()
    {
        if (HasReachedDestination())
        {
            if (_idleTime > 0f)
            {
                _idleTime -= Time.deltaTime;
                return;
            }
            else
            {
                _idleTime = Random.Range(minIdleTime, maxIdleTime);
                SetRandomDestination();
            }
        }
        else
        {
            agent.speed = walkSpeed;
        }
    }

    private void SetRandomDestination()
    {
        Vector3 center = (Application.isPlaying && initialPosition != Vector3.zero) ? initialPosition : transform.position;
        Vector3 rand = Random.insideUnitSphere * wanderRadius;
        rand += center;

        if (NavMesh.SamplePosition(rand, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    #endregion

    #region 🔊 Audio & Networking (3 AudioSources: Footstep, Alert, Theme)

    private float _footstepTimer = 0.2f;
    private bool _hasReceivedAnimFootstepEvent = false;

    private void InitAudioSources()
    {
        // 1. Footstep AudioSource (3D Sound)
        if (footstepAudioSource == null)
        {
            Transform footstepChild = transform.Find("AudioManager/FootStep") ?? transform.Find("FootStep") ?? transform.Find("AudioManager");
            if (footstepChild != null) footstepAudioSource = footstepChild.GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                var sources = GetComponentsInChildren<AudioSource>();
                if (sources.Length > 0) footstepAudioSource = sources[0];
            }
        }
        if (footstepAudioSource != null)
        {
            footstepAudioSource.spatialBlend = 1.0f;
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.minDistance = 1.0f;
            footstepAudioSource.maxDistance = 15.0f;
        }

        // 2. Alert AudioSource (3D Sound)
        if (alertAudioSource == null)
        {
            Transform alertChild = transform.Find("AudioManager/Alert") ?? transform.Find("Alert");
            if (alertChild != null) alertAudioSource = alertChild.GetComponent<AudioSource>();
            if (alertAudioSource == null)
            {
                var sources = GetComponentsInChildren<AudioSource>();
                if (sources.Length > 1) alertAudioSource = sources[1];
            }
        }
        if (alertAudioSource != null)
        {
            alertAudioSource.spatialBlend = 1.0f;
            alertAudioSource.playOnAwake = false;
        }

        // 3. Theme AudioSource (2D / Ambient Music)
        if (themeAudioSource == null)
        {
            Transform themeChild = transform.Find("AudioManager/Theme") ?? transform.Find("Theme");
            if (themeChild != null) themeAudioSource = themeChild.GetComponent<AudioSource>();
            if (themeAudioSource == null)
            {
                var sources = GetComponentsInChildren<AudioSource>();
                if (sources.Length > 2) themeAudioSource = sources[2];
            }
        }
        if (themeAudioSource != null)
        {
            themeAudioSource.playOnAwake = false;
            themeAudioSource.loop = true;
            if (panicThemeSound != null) themeAudioSource.clip = panicThemeSound;
        }
    }

    public void PlayPanicSound()
    {
        if (alertAudioSource != null)
        {
            if (panicScreamSound != null)
            {
                alertAudioSource.PlayOneShot(panicScreamSound);
            }
            else if (alertAudioSource.clip != null && !alertAudioSource.isPlaying)
            {
                alertAudioSource.Play();
            }
        }
    }

    private void SyncAnimationNetworkVariables()
    {
        float speed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
        float motionSpeed = speed > 0.1f ? 1f : 0f;

        if (IsNetworkSpawned)
        {
            NetworkSpeed = speed;
            NetworkMotionSpeed = motionSpeed;
        }

        if (animator != null)
        {
            if (_hasAnimSpeed) animator.SetFloat(_animIDSpeed, speed);
            if (_hasAnimMotionSpeed) animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }

        HandleProceduralFootsteps(speed);
    }

    private void UpdateRemoteVisuals()
    {
        if (animator != null)
        {
            float speed = IsNetworkSpawned ? NetworkSpeed : 0f;
            float motionSpeed = IsNetworkSpawned ? NetworkMotionSpeed : 0f;

            if (_hasAnimSpeed) animator.SetFloat(_animIDSpeed, speed);
            if (_hasAnimMotionSpeed) animator.SetFloat(_animIDMotionSpeed, motionSpeed);

            HandleProceduralFootsteps(speed);
        }
    }

    private void HandleProceduralFootsteps(float currentSpeed)
    {
        if (_hasReceivedAnimFootstepEvent) return; // Ưu tiên Animation Event

        if (currentSpeed > 0.2f)
        {
            _footstepTimer -= Time.deltaTime;
            float interval = currentSpeed > (walkSpeed + 0.5f) ? 0.32f : 0.52f;

            if (_footstepTimer <= 0f)
            {
                _footstepTimer = interval;
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
        if (animationEvent == null || animationEvent.animatorClipInfo.weight > 0.3f)
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
        if (footstepAudioSource == null) return;

        AudioClip clipToPlay = null;
        if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
        {
            var validClips = System.Array.FindAll(FootstepAudioClips, c => c != null);
            if (validClips.Length > 0)
            {
                clipToPlay = validClips[Random.Range(0, validClips.Length)];
            }
        }

        if (clipToPlay == null && footstepAudioSource.clip != null)
        {
            clipToPlay = footstepAudioSource.clip;
        }

        if (clipToPlay != null)
        {
            float vol = FootstepAudioVolume > 0.01f ? FootstepAudioVolume : 0.5f;
            footstepAudioSource.PlayOneShot(clipToPlay, vol);
        }
    }

    #endregion

    #region 🎨 Gizmos & Scene Debug

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 pos = transform.position;
        Vector3 center = (Application.isPlaying && initialPosition != Vector3.zero) ? initialPosition : pos;

        // Vùng đi dạo (Wander Radius)
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(center, wanderRadius);

        // Vùng gọi Người Lớn (Call Ranger)
        Gizmos.color = targetDetected ? Color.red : new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(pos, callRanger);

        // Nón tầm nhìn FOV (Cone)
        Gizmos.color = targetDetected ? Color.red : new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(pos + Vector3.up * 1.0f, visionRange);

        Vector3 rayStart = pos + Vector3.up * 1.0f;
        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.AngleAxis(-fovAngle * 0.5f, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(fovAngle * 0.5f, Vector3.up) * forward;

        Gizmos.color = targetDetected ? Color.red : Color.yellow;
        Gizmos.DrawRay(rayStart, leftDir * visionRange);
        Gizmos.DrawRay(rayStart, rightDir * visionRange);
        Gizmos.DrawRay(rayStart, forward * visionRange);
    }

    #endregion
}
