using System.Collections;
using System.Collections.Generic;
using Fusion;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 3 Chế độ Di chuyển / Tuần tra của AdultGuardNPC (tương thích 100% từ OLDkidadult.cs):
/// </summary>
public enum AdultMovementState
{
    RandomMove = 0, // Đi tự do ngẫu nhiên trong bán kính targetRadius quanh vị trí ban đầu
    Stationary = 1, // Đứng yên quét 3 góc [-45, 0, 45], sau khi Chase tự đi bộ về đúng chỗ cũ
    Patrol = 2      // Đi tuần tra qua các điểm Waypoints
}

/// <summary>
/// AI Người Lớn / Bảo Vệ (Adult Guard NPC) hoàn chỉnh:
/// - Kế thừa NetworkBehaviour để đồng bộ mạng Photon Fusion (Shared Mode).
/// - 3 Chế độ Tuần tra (RandomMove, Stationary, Patrol).
/// - Hệ thống 5 Raycast đa hướng (Forward, Left, Right, Up, Down), giảm tầm khi Player cúi, tự co giãn tầm đèn pin.
/// - Rượt đuổi (Chase), giới hạn maxChaseRadius, bắt Player (Catch) với thông báo tên, rơi đồ, đồng bộ hồi sinh.
/// - Nhạc Chase và âm thanh phát hiện (phát 1 lần khi bắt đầu đuổi).
/// - Tự động chạy tới kiểm tra cửa khi Player bẻ khóa thất bại (Door Tampered) rồi quay về điểm đứng cũ.
/// - Toàn bộ biến hardcode đều được mở thành tham số cấu hình trong Inspector.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class AdultGuardNPC : NetworkBehaviour
{
    // =========================================================================
    // BIẾN STATIC TOÀN CỤC CHO NHẠC CHASE (Đảm bảo chỉ 1 Audio phát cùng lúc)
    public static bool isChaseMusicPlaying = false;
    // =========================================================================

    [Header("👤 NPC Identity & Name")]
    [Tooltip("Tên của NPC hiển thị trong thông báo khi bắt được trộm")]
    public string npcName = "Guard";

    [Header("🤖 AI Components")]
    public NavMeshAgent agent;
    public Animator animator;
    public Light flashlight;

    [Header("🔊 1. Footstep Audio (Giống Player)")]
    [Tooltip("AudioSource phát tiếng bước chân 3D")]
    public AudioSource footstepAudioSource;
    [Tooltip("Danh sách clip âm thanh bước chân ngẫu nhiên")]
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("🔊 2. Alert & Detection Audio")]
    [Tooltip("AudioSource phát âm thanh phát hiện trộm (phát 1 lần khi nhìn thấy Player)")]
    public AudioSource alertAudioSource;
    public AudioClip alertSound;

    [Header("🔊 3. Theme & Chase Music")]
    [Tooltip("AudioSource phát nhạc nền khi rượt đuổi")]
    public AudioSource themeAudioSource;
    public AudioClip chaseThemeSound;

    [Header("🏃 Movement Speeds")]
    [Tooltip("Tốc độ đi bộ bình thường khi tuần tra")]
    public float walkSpeed = 1.5f;
    [Tooltip("Tốc độ chạy nhanh khi rượt đuổi hoặc chạy tới cửa bị phá")]
    public float runSpeed = 4.0f;
    [Tooltip("Tốc độ đi bộ khi quay trở về vị trí đứng ban đầu")]
    public float returnSpeed = 2.0f;
    [Tooltip("Ngưỡng khoảng cách nhận diện đã đến gần điểm đích")]
    public float stoppingDistanceThreshold = 0.5f;

    [Header("🚶 Movement & Patrol States (3 Chế độ Tuần tra)")]
    public AdultMovementState currentMovementState = AdultMovementState.RandomMove;

    [Tooltip("Bán kính tìm điểm đến ngẫu nhiên quanh vị trí gốc khi đi dạo (RandomMove)")]
    public float wanderRadius = 20f;

    [Tooltip("Thời gian đứng nghỉ tối thiểu khi đến đích (giây)")]
    public float minIdleTime = 2.0f;
    [Tooltip("Thời gian đứng nghỉ tối đa khi đến đích (giây)")]
    public float maxIdleTime = 5.0f;
    private float _idleTime = 0f;

    [Header("🔎 Stationary Scan Settings")]
    [Tooltip("Thời gian dừng nghỉ tại mỗi góc quét trước khi đổi góc mới (giây)")]
    public float scanDuration = 1.0f;
    [Tooltip("Tốc độ xoay người khi đổi góc quét")]
    public float scanRotationSpeed = 3.0f;
    [Tooltip("Các góc quét ngó nghiêng lệch so với hướng gốc (độ)")]
    public float[] scanAngles = { -45f, 0f, 45f };
    private float _scanTimer = 0f;
    private Quaternion _targetScanRotation;

    [Header("📍 Patrol Points (Dành cho chế độ Patrol)")]
    public Transform[] patrolPoints;
    private int _currentPatrolIndex = 0;

    [Header("👀 Detection & Vision Settings")]
    [Tooltip("Góc nón tầm nhìn quét phía trước mặt (độ)")]
    public float fovAngle = 100f;
    [Tooltip("Khoảng cách quét tối đa khi Player đứng/đi bình thường (mét)")]
    public float visionRange = 16f;
    [Tooltip("Hệ số giảm tầm nhìn khi Player Cúi (Crouch) - 0.5 = giảm 50% tầm nhìn (còn 8m)")]
    [Range(0.1f, 1f)]
    public float crouchDistanceMultiplier = 0.5f;
    [Tooltip("LayerMask quét vật cản che tầm nhìn (Tường, Cửa, Đồ vật)")]
    public LayerMask obstacleMask;

    [Header("🔦 Flashlight (Đèn pin NPC)")]
    [Tooltip("Tầm chiếu sáng của đèn pin khi Player trên máy này Đứng")]
    public float flashlightNormalRange = 27f;
    [Tooltip("Tầm chiếu sáng của đèn pin khi Player trên máy này Cúi (Crouch)")]
    public float flashlightCrouchRange = 8f;

    [Header("🎯 Target & Chase Settings")]
    [Tooltip("Thời gian rượt đuổi tối thiểu khi phát hiện (giây)")]
    public float minChaseDuration = 5.0f;
    [Tooltip("Thời gian rượt đuổi tối đa khi phát hiện (giây)")]
    public float maxChaseDuration = 10.0f;
    [HideInInspector] public bool targetDetected = false;
    public float chaseDuration = 0f;
    [Tooltip("Bán kính rượt đuổi tối đa từ vị trí gốc trước khi từ bỏ và quay về")]
    public float maxChaseRadius = 30f;
    [Tooltip("Khoảng cách gần hơn tối thiểu để đổi mục tiêu sang Player khác")]
    public float targetSwitchDistanceThreshold = 2.5f;

    [Header("🛠️ Debug Settings")]
    [Tooltip("Hiển thị thông tin log chi tiết trong Console")]
    public bool showDebugLogs = true;
    [Tooltip("Vẽ Gizmos trực quan trong Scene View")]
    public bool showGizmos = true;

    [Header("💥 Catch Player Settings")]
    [Tooltip("Bán kính bắt Player khi lại gần (mét)")]
    public float catchDistance = 1.6f;
    [Tooltip("Độ lệch tâm điểm bắt Player (X, Y, Z) so với NPC")]
    public Vector3 catchOffset = new Vector3(0f, 1.0f, 0.4f);
    [Tooltip("Thời gian chờ giữa các lần bắt")]
    public float catchCooldown = 2.0f;
    private float _lastCatchTime = -10f;

    [Header("🚪 Door & Suspicious Investigation")]
    [Tooltip("Thời gian đứng quan sát tại khu vực cửa bị phá hoặc nơi Kid báo trước khi quay về")]
    public float doorInvestigateDuration = 2.5f;
    private bool _isInvestigatingDoor = false;
    private Vector3 _doorInvestigateTarget;
    private bool _isInvestigatingSuspicious = false;
    private Vector3 _suspiciousInvestigateTarget;

    [Header("🏡 Stand / Return Position Settings")]
    [Tooltip("Điểm đứng cố định trong Scene (nếu gán, NPC sẽ luôn quay về đúng vị trí và góc quay của Transform này)")]
    public Transform customStaySpot;
    [Tooltip("Tốc độ xoay người căn chỉnh lại hướng gốc khi về đến nơi")]
    public float returnRotationSpeed = 5f;
    [Tooltip("Ngưỡng góc sai số chấp nhận được khi căn lại hướng")]
    public float returnAngleThreshold = 3.0f;
    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public Quaternion initialRotation;
    [HideInInspector] public bool isReturningToStayArea = false;

    [Header("🌐 Network Synced Properties")]
    [Networked] public float NetworkSpeed { get; set; } = 0f;
    [Networked] public float NetworkMotionSpeed { get; set; } = 1f;

    // Animation Hashes (Chỉ dùng Speed & MotionSpeed điều khiển Idle, Walk, Run)
    private int _animIDSpeed;
    private int _animIDMotionSpeed;
    private bool _hasAnimSpeed = false;
    private bool _hasAnimMotionSpeed = false;

    private PlayerController _lockedTargetPlayer;
    private bool _isWalkSoundPlaying = false;
    private int _scanAngleIndex = 0;
    private static readonly float[] _scanCycleAngles = { -45f, 0f, 45f, 0f };

    public bool IsNetworkSpawned => Object != null && Object.IsValid && Runner != null && Runner.IsRunning;
    public bool IsMasterAuthority => !IsNetworkSpawned || Runner.IsServer || Runner.IsSharedModeMasterClient || Object.HasStateAuthority;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (flashlight == null) flashlight = GetComponentInChildren<Light>();
        if (flashlight != null) flashlight.range = flashlightNormalRange;

        // Đảm bảo NPC có Collider để tương tác cửa
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
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

        if (customStaySpot != null)
        {
            initialPosition = customStaySpot.position;
            initialRotation = customStaySpot.rotation;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        InitAudioSources();

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 18f;
            agent.updateRotation = true;
        }

        InitializeMovementState();
    }

    private void DebugLog(string message, string color = "cyan")
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color={color}>[AdultGuardNPC - {npcName}]</color> {message}");
        }
    }

    public void InitializeMovementState()
    {
        _idleTime = Random.Range(minIdleTime, maxIdleTime);
        _targetScanRotation = initialRotation;
        SetRandomScanRotation();

        if (currentMovementState == AdultMovementState.RandomMove)
        {
            SetRandomDestination();
        }
        else if (currentMovementState == AdultMovementState.Patrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            if (agent != null && agent.isOnNavMesh && patrolPoints[_currentPatrolIndex] != null)
            {
                agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
            }
        }

        DebugLog($"Khởi tạo trạng thái: {currentMovementState} | Vị trí gốc: {initialPosition}", "yellow");
    }

    private static PlayerController _cachedLocalPlayer;

    /// <summary>
    /// Lấy người chơi cục bộ trên máy này (Local Player)
    /// </summary>
    public static PlayerController GetLocalPlayer()
    {
        if (_cachedLocalPlayer == null || !_cachedLocalPlayer.gameObject.activeInHierarchy)
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                var netSync = players[i].GetComponent<NetworkPlayerSync>();
                if (netSync != null && netSync.Object != null && netSync.Object.IsValid && netSync.HasInputAuthority)
                {
                    _cachedLocalPlayer = players[i];
                    break;
                }
                else if (players[i].isActiveAndEnabled)
                {
                    _cachedLocalPlayer = players[i];
                }
            }
        }
        return _cachedLocalPlayer;
    }

    /// <summary>
    /// Điều chỉnh tầm đèn pin cục bộ trên từng máy: Máy nào cúi thì đèn pin trên màn hình máy đó co lại
    /// </summary>
    private void UpdateLocalFlashlight()
    {
        if (flashlight == null) return;

        PlayerController localPlayer = GetLocalPlayer();
        bool isLocalCrouching = localPlayer != null && IsPlayerCrouching(localPlayer);

        float targetRange = isLocalCrouching ? flashlightCrouchRange : flashlightNormalRange;
        flashlight.range = Mathf.Lerp(flashlight.range, targetRange, Time.deltaTime * 8f);
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
        // 1. Luôn co giãn đèn pin cục bộ theo trạng thái cúi của người chơi trên máy này
        UpdateLocalFlashlight();

        if (!IsMasterAuthority)
        {
            UpdateRemoteVisuals();
            return;
        }

        EnsureOnNavMesh();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // 2. Quét tìm Player theo nón tầm nhìn FOV và Line of Sight
        RayCastHitTarget();

        // 3. Ưu tiên cao nhất: CHASE Player (khi tự mình nhìn thấy mục tiêu)
        if (targetDetected)
        {
            isReturningToStayArea = false;
            _isInvestigatingDoor = false;
            _isInvestigatingSuspicious = false;
            Chase();
            SyncAnimationNetworkVariables();
            return;
        }

        // 4. Ưu tiên 2: Chạy tới kiểm tra cửa bị phá
        if (_isInvestigatingDoor)
        {
            HandleInvestigateDoor();
            SyncAnimationNetworkVariables();
            return;
        }

        // 5. Ưu tiên 3: Chạy tới kiểm tra vị trí tình nghi do Kid báo
        if (_isInvestigatingSuspicious)
        {
            HandleInvestigateSuspiciousLocation();
            SyncAnimationNetworkVariables();
            return;
        }

        // 6. Ưu tiên 4: Quay trở về điểm đứng gốc
        if (isReturningToStayArea)
        {
            ReturnToStayArea();
            SyncAnimationNetworkVariables();
            return;
        }

        // 6. Chạy 3 chế độ tuần tra thông thường
        switch (currentMovementState)
        {
            case AdultMovementState.RandomMove:
                HandleRandomMove();
                break;
            case AdultMovementState.Stationary:
                HandleStationary();
                break;
            case AdultMovementState.Patrol:
                HandlePatrol();
                break;
        }

        SyncAnimationNetworkVariables();
    }

    #region 👀 FOV & Line-of-Sight Detection (NPCSensorySystem chuẩn)

    private static PlayerController[] _allPlayersCache;
    private static float _lastPlayerCacheTime = -10f;

    /// <summary>
    /// Lấy danh sách tất cả Player còn sống trong Scene (cache 0.5s)
    /// </summary>
    public static PlayerController[] GetAllLivingPlayers()
    {
        if (Time.time - _lastPlayerCacheTime > 0.5f || _allPlayersCache == null)
        {
            _allPlayersCache = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            _lastPlayerCacheTime = Time.time;
        }
        return _allPlayersCache;
    }

    /// <summary>
    /// Kiểm tra trạng thái cúi của người chơi (hỗ trợ cả Offline, Local và Remote Client qua mạng)
    /// </summary>
    public static bool IsPlayerCrouching(PlayerController p)
    {
        if (p == null) return false;
        if (p.Crouching) return true;
        
        var netSync = p.GetComponent<NetworkPlayerSync>();
        if (netSync != null && netSync.Object != null && netSync.Object.IsValid)
        {
            try
            {
                if (netSync.NetworkCrouch || netSync.NetworkIsCrouching) return true;
            }
            catch { }
        }
        return false;
    }

    /// <summary>
    /// Kiểm tra tia nhìn thẳng Line of Sight có bị vật cản (tường, cửa, tủ) che khuất không
    /// </summary>
    public bool HasLineOfSight(Vector3 fromPos, Vector3 targetPos, float maxDist)
    {
        Vector3 dir = (targetPos - fromPos).normalized;
        if (Physics.Raycast(fromPos, dir, out RaycastHit hit, maxDist + 0.3f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance < maxDist - 0.3f)
            {
                return false; // Bị vật cản chặn
            }
        }
        return true;
    }

    /// <summary>
    /// Lấy điểm ngắm trên thân Player (ngực / đầu)
    /// </summary>
    public Vector3 GetPlayerTargetPoint(PlayerController p)
    {
        if (p == null) return Vector3.zero;
        if (p.CinemachineCameraTarget != null)
        {
            return p.CinemachineCameraTarget.transform.position;
        }
        bool isCrouching = IsPlayerCrouching(p);
        return p.transform.position + Vector3.up * (isCrouching ? 0.75f : 1.35f);
    }

    public void RayCastHitTarget()
    {
        PlayerController[] allPlayers = GetAllLivingPlayers();
        if (allPlayers == null || allPlayers.Length == 0) return;

        Vector3 myEyePos = transform.position + Vector3.up * 1.5f;
        Vector3 myForward = transform.forward;

        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerController p = allPlayers[i];
            if (p == null || !p.gameObject.activeInHierarchy) continue;
            if (p.stats != null && p.stats.isDied) continue;
            if (p.deathHandler != null && p.deathHandler.isDeadProcessed) continue;

            bool isCrouching = IsPlayerCrouching(p);
            float maxEffectiveDist = isCrouching ? (visionRange * crouchDistanceMultiplier) : visionRange;

            Vector3 playerTargetPos = GetPlayerTargetPoint(p);
            float distToPlayer = Vector3.Distance(myEyePos, playerTargetPos);

            // 1. Ngoài tầm nhìn thực tế (đã giảm 50% nếu cúi) -> Bỏ qua
            if (distToPlayer > maxEffectiveDist) continue;

            // 2. Kiểm tra góc nón tầm nhìn FOV
            Vector3 dirToPlayer = (playerTargetPos - myEyePos).normalized;
            float angleToPlayer = Vector3.Angle(myForward, dirToPlayer);

            if (angleToPlayer <= fovAngle * 0.5f)
            {
                // 3. Bắn tia kiểm tra vật cản (Line of Sight)
                if (HasLineOfSight(myEyePos, playerTargetPos, distToPlayer))
                {
                    if (!targetDetected)
                    {
                        PlayDetectionSound();
                        HandleChaseMusic(true);
                        DebugLog($"Phát hiện Player ({p.name}) [Crouch: {isCrouching}] ở {distToPlayer:F1}m (Tầm tối đa: {maxEffectiveDist:F1}m)! Bắt đầu đuổi {minChaseDuration:F0}-{maxChaseDuration:F0}s.", "red");
                    }

                    _lockedTargetPlayer = p;
                    chaseDuration = Random.Range(minChaseDuration, maxChaseDuration);
                    targetDetected = true;
                    return;
                }
            }
        }

        // Đếm ngược timer Chase nếu mất dấu
        if (targetDetected && chaseDuration > 0f)
        {
            chaseDuration -= Time.deltaTime;
        }

        if (targetDetected && chaseDuration <= 0f)
        {
            DebugLog("Hết thời gian rượt đuổi / Mất dấu mục tiêu -> Đi bộ quay về điểm gốc.", "orange");
            targetDetected = false;
            _lockedTargetPlayer = null;
            isReturningToStayArea = true;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(initialPosition);
                agent.speed = returnSpeed;
            }
            HandleChaseMusic(false);
        }
    }

    #endregion

    #region 🏃 Chase & Catch Player

    public void StartChasePlayer(PlayerController target)
    {
        if (target == null || (target.stats != null && target.stats.isDied)) return;

        _lockedTargetPlayer = target;
        chaseDuration = Random.Range(minChaseDuration, maxChaseDuration);

        if (!targetDetected)
        {
            PlayDetectionSound();
            HandleChaseMusic(true);
            DebugLog($"Nhận lệnh Chase từ Kid/Hệ thống -> Đuổi mục tiêu ({target.name})!", "red");
        }

        targetDetected = true;
        isReturningToStayArea = false;
        _isInvestigatingDoor = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            agent.SetDestination(target.transform.position);
        }
    }

    public void Chase()
    {
        if (_lockedTargetPlayer == null || (_lockedTargetPlayer.stats != null && _lockedTargetPlayer.stats.isDied))
        {
            targetDetected = false;
            _lockedTargetPlayer = null;
            isReturningToStayArea = true;
            HandleChaseMusic(false);
            return;
        }

        // 1. Kiểm tra giới hạn Max Chase Radius
        float distanceToInitial = Vector3.Distance(transform.position, initialPosition);
        if (distanceToInitial > maxChaseRadius || chaseDuration <= 0f)
        {
            targetDetected = false;
            _lockedTargetPlayer = null;
            isReturningToStayArea = true;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(initialPosition);
                agent.speed = returnSpeed;
            }
            HandleChaseMusic(false);
            return;
        }

        // 2. Di chuyển đuổi theo Player
        agent.speed = runSpeed;
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(_lockedTargetPlayer.transform.position);
        }

        // 3. Kiểm tra bắt Player (Catch)
        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        Vector3 playerPos = _lockedTargetPlayer.transform.position + Vector3.up * 1.0f;
        float distToPlayer = Vector3.Distance(catchOrigin, playerPos);

        if (distToPlayer <= catchDistance)
        {
            ExecuteCatchPlayer(_lockedTargetPlayer);
            targetDetected = false;
            _lockedTargetPlayer = null;
            isReturningToStayArea = true;
            HandleChaseMusic(false);
        }
    }

    private void ExecuteCatchPlayer(PlayerController target)
    {
        if (target == null) return;
        if (Time.time - _lastCatchTime < catchCooldown) return;
        _lastCatchTime = Time.time;

        // Xử lý tử vong chuyên nghiệp
        if (target.deathHandler != null)
        {
            target.deathHandler.TriggerDeath(npcName);
        }
        else
        {
            if (target.stats != null) target.stats.isDied = true;
            if (target.inventory != null) target.inventory.DropAllItemsOnDeath();
            GameStatusHUD.Show($"Player got caught by {npcName}!");
        }

        Debug.Log($"<color=red>[AdultGuardNPC] {npcName} đã bắt được Player ({target.name})!</color>");
    }

    #endregion

    #region 🚪 Door & Suspicious Location Investigation (Cửa gãy & Kid báo tin)

    public static void AlertDoorTampered(Vector3 doorPosition, float radius)
    {
        AdultGuardNPC[] allAdults = FindObjectsByType<AdultGuardNPC>(FindObjectsSortMode.None);
        if (allAdults == null || allAdults.Length == 0) return;

        foreach (var adult in allAdults)
        {
            if (adult == null || !adult.gameObject.activeInHierarchy || !adult.IsMasterAuthority) continue;
            if (adult.targetDetected) continue; // Đang đuổi thì không đi soi cửa

            float dist = Vector3.Distance(adult.transform.position, doorPosition);
            if (dist <= radius)
            {
                adult.TriggerInvestigateDoor(doorPosition);
            }
        }
    }

    public void TriggerInvestigateDoor(Vector3 doorPosition)
    {
        if (targetDetected) return;

        PlayDetectionSound(); // Phát 1 lần alertSound khi nghe tiếng cửa gãy / bẻ khóa hỏng

        _doorInvestigateTarget = doorPosition;
        _isInvestigatingDoor = true;
        _isInvestigatingSuspicious = false;
        isReturningToStayArea = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            agent.SetDestination(doorPosition);
        }
    }

    private void HandleInvestigateDoor()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.speed = runSpeed;

        if (HasReachedDestination())
        {
            StartCoroutine(FinishDoorInvestigationRoutine());
            _isInvestigatingDoor = false;
        }
    }

    private IEnumerator FinishDoorInvestigationRoutine()
    {
        if (agent.isOnNavMesh) agent.ResetPath();

        // Đứng quan sát tại cửa và ngó nghiêng
        yield return new WaitForSeconds(doorInvestigateDuration);

        // Quay lại điểm đứng ban đầu
        isReturningToStayArea = true;
    }

    /// <summary>
    /// Nhận tin báo từ Kid: Phát âm thanh cảnh báo 1 lần, lấy vị trí hiện tại của Player lúc Kid gọi, chạy tới đó kiểm tra rồi quay về (chứ không tự động rượt đuổi Player liên tục)
    /// </summary>
    public void ReceiveKidAlert(Vector3 playerLastKnownPos)
    {
        if (targetDetected) return; // Nếu đang tự mình nhìn thấy trộm thì không bị phân tâm

        PlayDetectionSound(); // Phát 1 lần alertSound khi nghe Kid gọi báo trộm

        _suspiciousInvestigateTarget = playerLastKnownPos;
        _isInvestigatingSuspicious = true;
        _isInvestigatingDoor = false;
        isReturningToStayArea = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            agent.SetDestination(playerLastKnownPos);
        }

        DebugLog($"[Kid Alert] Nghe Kid gọi! Chạy đến vị trí trộm xuất hiện ({playerLastKnownPos}) để kiểm tra...", "yellow");
    }

    private void HandleInvestigateSuspiciousLocation()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.speed = runSpeed;

        if (HasReachedDestination())
        {
            StartCoroutine(FinishSuspiciousInvestigationRoutine());
            _isInvestigatingSuspicious = false;
        }
    }

    private IEnumerator FinishSuspiciousInvestigationRoutine()
    {
        if (agent.isOnNavMesh) agent.ResetPath();

        // Đứng quan sát và ngó nghiêng tại vị trí tình nghi trong vài giây
        yield return new WaitForSeconds(doorInvestigateDuration);

        // Không thấy gì -> Quay về điểm đứng gốc
        isReturningToStayArea = true;
        DebugLog("[Kid Alert] Đã kiểm tra xong vị trí khả nghi -> Đi bộ quay về điểm gốc.", "green");
    }

    #endregion

    #region 🚶 3 Patrol Modes (RandomMove, Stationary, Patrol)

    private void HandleRandomMove()
    {
        if (HasReachedDestination())
        {
            if (_idleTime > 0f)
            {
                _idleTime -= Time.deltaTime;
                _scanTimer -= Time.deltaTime;

                transform.rotation = Quaternion.Slerp(transform.rotation, _targetScanRotation, Time.deltaTime * scanRotationSpeed);
                if (_scanTimer <= 0f) SetRandomScanRotation();
                return;
            }
            else
            {
                _idleTime = Random.Range(minIdleTime, maxIdleTime);
                SetRandomDestination();
                SetRandomScanRotation();
            }
        }
        else
        {
            agent.speed = walkSpeed;
        }
    }

    private void HandleStationary()
    {
        if (agent.hasPath) agent.ResetPath();
        if (agent.updateRotation) agent.updateRotation = false; // Tắt updateRotation để script tự do xoay ngó nghiêng

        transform.rotation = Quaternion.Slerp(transform.rotation, _targetScanRotation, Time.deltaTime * scanRotationSpeed);
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            SetNextScanRotation();
        }
    }

    private void HandlePatrol()
    {
        if (agent != null && !agent.updateRotation) agent.updateRotation = true;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentMovementState = AdultMovementState.Stationary;
            return;
        }

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
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
                if (patrolPoints[_currentPatrolIndex] != null)
                {
                    agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
                }
            }
        }
        else
        {
            agent.speed = walkSpeed;
        }
    }

    public void ReturnToStayArea()
    {
        agent.speed = returnSpeed;
        if (agent.isOnNavMesh && (!agent.hasPath || Vector3.Distance(agent.destination, initialPosition) > 0.5f))
        {
            agent.updateRotation = true;
            agent.SetDestination(initialPosition);
        }

        if (HasReachedDestination())
        {
            agent.updateRotation = false;
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * returnRotationSpeed);

            if (Quaternion.Angle(transform.rotation, initialRotation) < returnAngleThreshold)
            {
                transform.rotation = initialRotation; // Khóa chuẩn góc ban đầu
                isReturningToStayArea = false;
                if (agent.isOnNavMesh) agent.ResetPath();
                InitializeMovementState();
                DebugLog("Đã quay về đúng vị trí và góc quay gốc thành công!", "green");
            }
        }
    }

    private void SetNextScanRotation()
    {
        _scanTimer = scanDuration;
        _scanAngleIndex = (_scanAngleIndex + 1) % _scanCycleAngles.Length;
        float currentAngleOffset = _scanCycleAngles[_scanAngleIndex];

        Quaternion initialY = Quaternion.Euler(0f, initialRotation.eulerAngles.y, 0f);
        _targetScanRotation = initialY * Quaternion.Euler(0f, currentAngleOffset, 0f);
        DebugLog($"[Stand Mode] Ngó nghiêng sang hướng {currentAngleOffset:F0}°", "cyan");
    }

    private void SetRandomScanRotation()
    {
        _scanTimer = scanDuration;
        float randomAngle = Random.Range(-60f, 60f);
        _targetScanRotation = transform.rotation * Quaternion.Euler(0f, randomAngle, 0f);
    }

    private void SetRandomDestination()
    {
        Vector3 rand = Random.insideUnitSphere * wanderRadius;
        rand += initialPosition;

        if (NavMesh.SamplePosition(rand, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            if (agent.isOnNavMesh)
            {
                agent.speed = walkSpeed;
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
            if (chaseThemeSound != null) themeAudioSource.clip = chaseThemeSound;
        }
    }

    public void PlayDetectionSound()
    {
        if (alertAudioSource != null)
        {
            if (alertSound != null)
            {
                alertAudioSource.PlayOneShot(alertSound);
            }
            else if (alertAudioSource.clip != null && !alertAudioSource.isPlaying)
            {
                alertAudioSource.Play();
            }
        }
    }

    public void HandleChaseMusic(bool shouldPlay)
    {
        if (themeAudioSource == null) return;

        if (shouldPlay)
        {
            if (!isChaseMusicPlaying)
            {
                themeAudioSource.loop = true;
                if (chaseThemeSound != null && themeAudioSource.clip != chaseThemeSound)
                {
                    themeAudioSource.clip = chaseThemeSound;
                }
                if (!themeAudioSource.isPlaying) themeAudioSource.Play();
                isChaseMusicPlaying = true;
            }
        }
        else
        {
            themeAudioSource.Stop();
            isChaseMusicPlaying = false;
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
        if (_hasReceivedAnimFootstepEvent) return; // Nếu animator có animation event thì ưu tiên animation event

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

        // Vùng tối đa rượt đuổi
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
        Gizmos.DrawWireSphere(center, maxChaseRadius);
        Gizmos.DrawSphere(center, 0.2f);

        // Vùng đi dạo ngẫu nhiên (Wander)
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(center, wanderRadius);

        // Nón tầm nhìn FOV (Cone)
        Gizmos.color = targetDetected ? Color.red : new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(pos + Vector3.up * 1.5f, visionRange);

        Vector3 rayStart = pos + Vector3.up * 1.5f;
        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.AngleAxis(-fovAngle * 0.5f, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(fovAngle * 0.5f, Vector3.up) * forward;

        Gizmos.color = targetDetected ? Color.red : Color.yellow;
        Gizmos.DrawRay(rayStart, leftDir * visionRange);
        Gizmos.DrawRay(rayStart, rightDir * visionRange);
        Gizmos.DrawRay(rayStart, forward * visionRange);

        // Vẽ đường nối mục tiêu khi Chase
        if (targetDetected && _lockedTargetPlayer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, _lockedTargetPlayer.transform.position);
            Gizmos.DrawWireSphere(_lockedTargetPlayer.transform.position, 0.5f);
        }
    }

    #endregion
}
