using System.Collections;
using Fusion;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Trạng thái hoạt động của NPC
/// </summary>
public enum NPCState
{
    Idle = 0,
    Patrol = 1,
    InvestigateNoise = 2,
    Chase = 3,
    PanicRun = 4,
    Stunned = 5
}

/// <summary>
/// Cơ chế phản ứng khi nghe thấy tiếng động
/// </summary>
public enum NoiseReactionType
{
    TurnToNoiseOnly = 0,   // Cơ chế 1: Đứng tại chỗ quay thẳng mặt về hướng tiếng động
    MoveToLastNoisePos = 1, // Cơ chế 2: Di chuyển đến gần vị trí tiếng động lần cuối
    RandomChoice = 2       // Ngẫu nhiên chọn 1 trong 2 cơ chế trên
}

/// <summary>
/// Lớp cơ sở (Base Class) cho toàn bộ NPC trong game:
/// - Quản lý phân quyền Photon Fusion (Chỉ MasterClient tính toán NavMesh & Sensory Logic).
/// - Đồng bộ vị trí, góc quay (NetworkTransform) và Animation State qua Fusion.
/// - Hệ thống Giác quan (NPCSensorySystem) ưu tiên phản xạ thính giác với 2 cơ chế.
/// - Cung cấp State Machine chung để các lớp con (AdultGuardNPC, KidRunnerNPC...) kế thừa.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BaseNPC : NetworkBehaviour
{
    [Header("👤 NPC General Info")]
    public string npcName = "NPC";
    public NPCState currentState = NPCState.Idle;

    [Header("🏃 Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.5f;
    public float investigateSpeed = 2.5f;
    public float rotationSpeed = 6.0f;

    [Header("👂 Noise Investigation Settings")]
    [Tooltip("Cơ chế phản ứng khi nghe thấy tiếng động")]
    public NoiseReactionType noiseReaction = NoiseReactionType.RandomChoice;

    [Tooltip("Thời gian đứng soi / quan sát vị trí tiếng động trước khi ngó trái phải")]
    public float noiseInvestigateDuration = 2.0f;

    [Tooltip("Góc ngó nghiêng khi kết thúc kiểm tra âm thanh (độ)")]
    public float investigateLookAngle = 45.0f;

    [Header("🔊 Audio & SFX")]
    [Tooltip("AudioSource phát các hiệu ứng âm thanh 1 lần (tiếng nói, tiếng còi, tiếng nghi ngờ...)")]
    public AudioSource audioSource;

    [Tooltip("AudioSource riêng chuyên phát nhạc nền rượt đuổi (Chase Theme)")]
    public AudioSource chaseAudioSource;

    [Tooltip("1. Tiếng hô / câu thoại phát hiện trộm (VD: 'Hey you, stop!', tiếng còi) - Phát 1 lần ngay khi thấy")]
    public AudioClip alertSound;

    [Tooltip("2. Nhạc nền rượt đuổi (Chase BGM) - Phát lặp lại (Loop) trong suốt quá trình đuổi, tự tắt khi mất dấu")]
    public AudioClip chaseThemeClip;

    [Tooltip("Tiếng nghi ngờ khi nghe thấy tiếng động bước chân / bẻ khóa")]
    public AudioClip suspectSound;

    [Tooltip("Tiếng khi bắt được Player")]
    public AudioClip catchSound;

    [Header("👣 Footstep Audio (Tiếng bước chân)")]
    public AudioClip[] footstepAudioClips;
    [Tooltip("AudioSource 3D phát tiếng bước chân (tự tạo nếu để trống)")]
    public AudioSource footstepAudioSource;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    [Header("🧩 Component References")]
    public NavMeshAgent agent;
    public Animator animator;
    public NPCSensorySystem senses;
    public NPCPatrolController patrolController;

    [Header("🌐 Network Synced Properties")]
    [Networked, OnChangedRender(nameof(OnNetworkStateChanged))]
    public int NetworkStateInt { get; set; } = 0;

    [Networked] public float NetworkSpeed { get; set; } = 0f;
    [Networked] public float NetworkMotionSpeed { get; set; } = 1f;

    // Animation Hashes (Chỉ dùng Speed & MotionSpeed để điều khiển Idle, Walk, Run)
    protected int _animIDSpeed;
    protected int _animIDMotionSpeed;

    protected bool _hasAnimSpeed = false;
    protected bool _hasAnimMotionSpeed = false;

    // Runtime variables
    protected Coroutine _lookAroundCoroutine;
    protected Coroutine _noiseInvestigationCoroutine;
    protected Vector3 _lastHeardNoisePos;
    protected float _stateTimer = 0f;

    /// <summary>
    /// Kiểm tra xem đối tượng đã được Spawn qua mạng Photon Fusion chưa
    /// </summary>
    public bool IsNetworkSpawned => Object != null && Object.IsValid && Runner != null && Runner.IsRunning;

    /// <summary>
    /// Kiểm tra quyền Master Authority (chỉ máy Host/MasterClient mới tính toán AI)
    /// </summary>
    public bool IsMasterAuthority
    {
        get
        {
            if (!IsNetworkSpawned) return true;
            return Runner.IsServer || Runner.IsSharedModeMasterClient || Object.HasStateAuthority;
        }
    }

    protected virtual void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (senses == null) senses = GetComponent<NPCSensorySystem>() ?? GetComponentInChildren<NPCSensorySystem>();
        if (patrolController == null) patrolController = GetComponent<NPCPatrolController>();
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();

        InitializeFootstepAudio();
        AssignAnimationIDs();
        ConfigurePhysicsAvoidance();
    }

    private void InitializeFootstepAudio()
    {
        if (footstepAudioSource == null)
        {
            Transform footstepChild = transform.Find("AudioManager/FootStep") ?? transform.Find("FootStep") ?? transform.Find("AudioManager");
            if (footstepChild != null)
            {
                footstepAudioSource = footstepChild.GetComponent<AudioSource>();
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
            footstepAudioSource.maxDistance = 18.0f;
            footstepAudioSource.rolloffMode = AudioRolloffMode.Linear;

            if (SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
            {
                footstepAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
            }
        }
    }

    protected virtual void Start()
    {
        ConfigureAuthorityState();
    }

    public override void Spawned()
    {
        ConfigureAuthorityState();
    }

    private void AssignAnimationIDs()
    {
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
    }

    private void ConfigurePhysicsAvoidance()
    {
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoBraking = true;
            agent.angularSpeed = 720f; // Xoay người nhanh nhạy, không bị đơ/chậm khi đổi hướng
            agent.acceleration = 18f; // Tăng tốc tức thì khi di chuyển
            agent.updateRotation = true;
        }

        // Đảm bảo Rigidbody trên NPC là Kinematic
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Phân quyền chạy Logic: Chỉ MasterClient bật NavMeshAgent, các client khác nhận vị trí qua NetworkTransform
    /// </summary>
    public void ConfigureAuthorityState()
    {
        if (agent != null)
        {
            agent.enabled = IsMasterAuthority;
        }

        if (IsMasterAuthority)
        {
            SetState(NPCState.Patrol);
        }
    }

    protected virtual void Update()
    {
        if (!IsMasterAuthority)
        {
            // Remote Clients: Cập nhật Animation từ biến Networked
            UpdateRemoteAnimator();
            HandleProceduralFootsteps();
            return;
        }

        // Master Client: Chạy AI Logic Loop
        UpdateMasterAILogic();
        AssistFastRotation();
        SyncAnimationNetworkVariables();
        HandleProceduralFootsteps();
    }

    /// <summary>
    /// Hỗ trợ NPC xoay người tức thì theo hướng di chuyển mong muốn (desiredVelocity)
    /// </summary>
    protected void AssistFastRotation()
    {
        if (agent != null && agent.isOnNavMesh && agent.desiredVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 lookDir = agent.desiredVelocity;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * 40f * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Vòng lặp Logic AI chính trên máy Master Client
    /// </summary>
    protected virtual void UpdateMasterAILogic()
    {
        if (currentState == NPCState.Stunned) return;

        // 1. Quét giác quan (Sensors)
        bool hasVisibleTarget = false;
        PlayerController bestVisibleTarget = null;
        bool heardNoise = false;
        Vector3 noisePos = Vector3.zero;

        if (senses != null)
        {
            hasVisibleTarget = senses.ScanSensors(out bestVisibleTarget, out heardNoise, out noisePos);
        }

        // 2. Xử lý THỊ GIÁC: Nếu nhìn thấy Player -> Chuyển sang hành vi tương ứng (Chase hoặc Panic)
        if (hasVisibleTarget && bestVisibleTarget != null)
        {
            OnPlayerSpotted(bestVisibleTarget);
            return;
        }

        // 3. Xử lý THÍNH GIÁC (Ưu tiên kiểm tra tiếng ồn lên đầu các mode khi chưa nhìn thấy ai)
        if (heardNoise && currentState != NPCState.Chase && currentState != NPCState.PanicRun)
        {
            TriggerNoiseInvestigation(noisePos);
            return;
        }

        // 4. Xử lý theo từng State hiện tại
        switch (currentState)
        {
            case NPCState.Idle:
                UpdateIdleState();
                break;
            case NPCState.Patrol:
                UpdatePatrolState();
                break;
            case NPCState.InvestigateNoise:
                UpdateInvestigateNoiseState();
                break;
            case NPCState.Chase:
                UpdateChaseState();
                break;
            case NPCState.PanicRun:
                UpdatePanicRunState();
                break;
        }
    }

    #region 👂 Xử Lý Thính Giác & Điều Tra Tiếng Động (Noise Reaction)

    /// <summary>
    /// Kích hoạt cơ chế phản ứng khi nghe thấy tiếng động
    /// </summary>
    public virtual void TriggerNoiseInvestigation(Vector3 noisePosition)
    {
        if (currentState == NPCState.Chase || currentState == NPCState.PanicRun || currentState == NPCState.Stunned)
        {
            return;
        }

        _lastHeardNoisePos = noisePosition;

        if (_noiseInvestigationCoroutine != null)
        {
            StopCoroutine(_noiseInvestigationCoroutine);
        }
        if (_lookAroundCoroutine != null)
        {
            StopCoroutine(_lookAroundCoroutine);
        }

        SetState(NPCState.InvestigateNoise);

        NoiseReactionType reaction = noiseReaction;
        if (reaction == NoiseReactionType.RandomChoice)
        {
            reaction = (Random.value > 0.5f) ? NoiseReactionType.MoveToLastNoisePos : NoiseReactionType.TurnToNoiseOnly;
        }

        _noiseInvestigationCoroutine = StartCoroutine(NoiseInvestigationRoutine(noisePosition, reaction));
    }

    protected virtual IEnumerator NoiseInvestigationRoutine(Vector3 noisePosition, NoiseReactionType reaction)
    {
        // Phát âm thanh nghi ngờ (nếu có)
        PlayAudio(suspectSound);

        if (reaction == NoiseReactionType.TurnToNoiseOnly)
        {
            // === CƠ CHẾ 1: Đứng tại chỗ quay thẳng mặt về hướng tiếng động ===
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            Vector3 lookDir = (noisePosition - transform.position);
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                float turnElapsed = 0f;
                while (turnElapsed < 0.6f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                    turnElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Đứng soi / quan sát trong khoảng noiseInvestigateDuration
            yield return new WaitForSeconds(noiseInvestigateDuration);

            // Trước khi kết thúc: Quay ngó trái / phải 1 lần để kiểm tra
            yield return StartCoroutine(LookAroundOnceRoutine(investigateLookAngle, 1.2f));
        }
        else
        {
            // === CƠ CHẾ 2: Di chuyển đến gần vị trí tiếng động lần cuối ===
            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = investigateSpeed;
                // Tạo độ lệch ngẫu nhiên 1.5 - 2.5m quanh vị trí tiếng động để không đè lên NPC khác
                Vector2 circleOffset = Random.insideUnitCircle * Random.Range(1.2f, 2.5f);
                Vector3 targetPoint = noisePosition + new Vector3(circleOffset.x, 0f, circleOffset.y);

                if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    agent.SetDestination(noisePosition);
                }
            }

            // Chờ NPC đi tới gần điểm đến
            float timeout = 7.0f;
            while (timeout > 0f)
            {
                timeout -= Time.deltaTime;
                if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f)
                {
                    break;
                }
                yield return null;
            }

            // Đứng soi tại hiện trường
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            yield return new WaitForSeconds(noiseInvestigateDuration);

            // Trước khi kết thúc: Quay ngó trái / phải 1 lần để kiểm tra
            yield return StartCoroutine(LookAroundOnceRoutine(investigateLookAngle, 1.2f));
        }

        // Kết thúc kiểm tra âm thanh -> Quay lại trạng thái Tuần tra
        _noiseInvestigationCoroutine = null;
        SetState(NPCState.Patrol);
    }

    /// <summary>
    /// Coroutine hỗ trợ xoay ngó nghiêng Trái -> Phải -> Về giữa 1 lần
    /// </summary>
    public IEnumerator LookAroundOnceRoutine(float angleDegrees, float durationPerSide)
    {
        Quaternion originalRot = transform.rotation;
        Quaternion leftRot = originalRot * Quaternion.Euler(0f, -angleDegrees, 0f);
        Quaternion rightRot = originalRot * Quaternion.Euler(0f, angleDegrees, 0f);

        // 1. Xoay sang Trái
        float t = 0f;
        while (t < durationPerSide)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, leftRot, Time.deltaTime * rotationSpeed);
            t += Time.deltaTime;
            yield return null;
        }

        // 2. Xoay sang Phải
        t = 0f;
        while (t < durationPerSide)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rightRot, Time.deltaTime * rotationSpeed);
            t += Time.deltaTime;
            yield return null;
        }

        // 3. Xoay về lại Giữa
        t = 0f;
        while (t < 0.6f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRot, Time.deltaTime * rotationSpeed);
            t += Time.deltaTime;
            yield return null;
        }
    }

    #endregion

    #region 🔄 State Management & Virtual Handlers

    public virtual void SetState(NPCState newState)
    {
        if (currentState == newState) return;

        if (patrolController != null && currentState == NPCState.Patrol && newState != NPCState.Patrol)
        {
            patrolController.StopPatrol();
        }

        currentState = newState;
        if (IsNetworkSpawned)
        {
            NetworkStateInt = (int)newState;
        }
        _stateTimer = 0f;

        PlayChaseTheme(newState == NPCState.Chase);
    }

    protected virtual void UpdateIdleState() { }
    protected virtual void UpdatePatrolState()
    {
        if (patrolController != null)
        {
            patrolController.ExecutePatrolLogic();
        }
    }
    protected virtual void UpdateInvestigateNoiseState() { }
    protected virtual void UpdateChaseState() { }
    protected virtual void UpdatePanicRunState() { }

    /// <summary>
    /// Được gọi khi hệ thống Thị giác phát hiện Player trong tầm mắt
    /// </summary>
    public virtual void OnPlayerSpotted(PlayerController spottedPlayer) { }

    #endregion

    #region 🌐 Sync Animation & Remote Rendering

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
    }

    private void UpdateRemoteAnimator()
    {
        if (animator != null)
        {
            float speed = IsNetworkSpawned ? NetworkSpeed : 0f;
            float motionSpeed = IsNetworkSpawned ? NetworkMotionSpeed : 0f;

            if (_hasAnimSpeed) animator.SetFloat(_animIDSpeed, speed);
            if (_hasAnimMotionSpeed) animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }
    }

    private void OnNetworkStateChanged()
    {
        currentState = (NPCState)NetworkStateInt;
        PlayChaseTheme(currentState == NPCState.Chase);
    }

    public void PlayAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Bật/Tắt nhạc nền rượt đuổi (Chase Theme) phát lặp trong suốt quá trình Chase, tự tắt khi mất dấu
    /// </summary>
    public void PlayChaseTheme(bool play)
    {
        if (chaseThemeClip == null) return;

        if (chaseAudioSource == null)
        {
            chaseAudioSource = gameObject.AddComponent<AudioSource>();
            chaseAudioSource.spatialBlend = 0.85f; // 3D Audio dồn dập khi ở gần
            chaseAudioSource.minDistance = 2.0f;
            chaseAudioSource.maxDistance = 25.0f;
            chaseAudioSource.loop = true; // Phát lặp trong suốt lúc rượt đuổi
            chaseAudioSource.playOnAwake = false;

            if (SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
            {
                chaseAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
            }
        }

        if (play)
        {
            if (!chaseAudioSource.isPlaying || chaseAudioSource.clip != chaseThemeClip)
            {
                chaseAudioSource.clip = chaseThemeClip;
                chaseAudioSource.loop = true; // Lặp lại cho đến khi mất dấu
                chaseAudioSource.Play();
            }
        }
        else
        {
            if (chaseAudioSource.isPlaying)
            {
                chaseAudioSource.Stop();
            }
        }
    }

    #region 👣 Footstep Playback Handlers

    private float _footstepTimer = 0.2f;
    private bool _hasReceivedAnimFootstepEvent = false;

    /// <summary>
    /// Phát tiếng bước chân theo nhịp di chuyển tự động (hoạt động cả khi animation không có Animation Event)
    /// </summary>
    private void HandleProceduralFootsteps()
    {
        if (_hasReceivedAnimFootstepEvent) return;

        float currentSpeed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : NetworkSpeed;
        if (currentSpeed > 0.3f)
        {
            _footstepTimer -= Time.deltaTime;
            // Tốc độ chạy nhanh -> nhịp bước nhanh hơn
            float stepInterval = currentSpeed > (walkSpeed + 0.5f) ? 0.32f : 0.48f;

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

    /// <summary>
    /// Được gọi từ Animation Event trên Animation Clip của NPC (nếu có)
    /// </summary>
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

        if (footstepAudioClips != null && footstepAudioClips.Length > 0)
        {
            var validClips = System.Array.FindAll(footstepAudioClips, c => c != null);
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
            float vol = footstepVolume > 0.01f ? footstepVolume : 0.5f;

            if (footstepAudioSource != null)
            {
                footstepAudioSource.PlayOneShot(clipToPlay, vol);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clipToPlay, transform.position, vol);
            }
        }
    }

    #endregion

    #endregion
}
