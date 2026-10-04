using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Fusion;
using StarterAssets;

[RequireComponent(typeof(NavMeshAgent))]
public class AdultGuardNPC : NetworkBehaviour
{
    public enum MovementMode { Stationary, Patrol, Wander }
    public enum AIState { Idle, Chase, Return, DoorInvestigate }

    [Header("👤 Identity")]
    public string npcName = "Adult Guard";
    public NavMeshAgent agent;
    public Animator animator;

    [Header("🚶 Movement Mode")]
    public MovementMode movementMode = MovementMode.Stationary;

    [Header("🏃 Speeds")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float returnSpeed = 2.0f;

    [Header("👀 Vision & Flashlight")]
    public float visionRange = 15f;
    public float raycastAngle = 30f;
    [Range(0.1f, 1f)] public float crouchVisionMultiplier = 0.5f;
    public float eyeHeight = 1.2f;
    public Light flashlight;
    public float flashlightNormalRange = 27f;
    public float flashlightTransitionSpeed = 8f;

    [Header("👂 Hearing (Thính giác)")]
    [Tooltip("Bật/tắt thính giác của NPC này (tự động nghe tiếng động cửa/item/bẫy...)")]
    public bool canHear = true;
    [Tooltip("Bật/tắt khả năng nghe tiếng bước chân / tiếng động phát ra từ Player")]
    public bool canHearPlayer = true;
    [Tooltip("Khoảng cách nghe tối đa của NPC này (mét)")]
    public float hearingRange = 20f;

    [Header("🎯 Chase & Catch")]
    [Tooltip("Số giây mất dấu Player thì bỏ chase (reset mỗi frame khi còn thấy)")]
    public float loseSightGrace = 5f;
    public float catchDistance = 1.6f;
    public Vector3 catchOffset = new Vector3(0f, 1.0f, 0.4f);
    public float catchCooldown = 1.5f;
    [Tooltip("Tên Trigger hoặc State Animation khi bắt Player (mặc định 'catch' hoặc 'attack')")]
    public string catchAnimTrigger = "catch";
    [Tooltip("Thời gian NPC tạm dừng di chuyển để diễn animation bắt (giây)")]
    public float catchFreezeDuration = 1.2f;
    public float callAdultRadius = 12f;
    public LayerMask adultNpcLayer;

    // ═══════════ MODE 1: STATIONARY ═══════════
    [Header("🧍 Mode 1: Stationary")]
    [Tooltip("Anchor chứa pos + rot gốc. Trống → dùng vị trí spawn")]
    public Transform stationaryAnchor;
    [Tooltip("Góc quay trái/phải so với hướng anchor (0 = chỉ đứng yên quay theo anchor)")]
    public float lookAngleSpread = 45f;
    [Tooltip("Thời gian giữ mỗi hướng (giây)")]
    public Vector2 stationaryRestTimeRange = new Vector2(2f, 5f);
    public float stationaryTurnSpeed = 3.0f;

    // ═══════════ MODE 2: PATROL ═══════════
    [Header("🚶 Mode 2: Patrol")]
    public Transform[] patrolPoints;
    [Tooltip("Thời gian rest tại mỗi waypoint")]
    public Vector2 patrolRestTimeRange = new Vector2(2f, 4f);
    [Tooltip("Góc ngó trái/phải khi tới waypoint")]
    public float patrolLookAngle = 40f;

    // ═══════════ MODE 3: WANDER ═══════════
    [Header("🚶 Mode 3: Wander")]
    [Tooltip("Object làm tâm wander. Trống → dùng vị trí spawn")]
    public Transform wanderCenterObject;
    public float wanderRadius = 15f;
    public Vector2 wanderRestTimeRange = new Vector2(2f, 5f);
    public float wanderLookAngle = 40f;
    [Range(0f, 1f)] public float wanderLookChance = 0.5f;

    // ═══════════ DOOR INVESTIGATION ═══════════
    [Header("🚪 Door Investigation")]
    [Tooltip("Bán kính đi quanh cửa khi kiểm tra")]
    public float doorInspectRadius = 2.5f;
    [Tooltip("Số bước đi quanh cửa (mỗi bước là 1 góc)")]
    public int doorInspectSteps = 6;
    [Tooltip("Thời gian pause và quay đầu tại mỗi bước")]
    public Vector2 doorLookPauseRange = new Vector2(1.0f, 2.5f);

    [Header("🔊 Audio")]
    public AudioSource alertSource;
    public AudioClip alertSound;
    [Tooltip("Âm thanh phát ra khi tóm được Player (Kill / Catch Audio)")]
    public AudioClip catchSound;
    [Range(0, 1)] public float catchVolume = 1f;
    public AudioSource themeSource;
    public AudioClip chaseTheme;
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    [Range(0, 1)] public float footstepVolume = 0.5f;

    [Header("🗣️ Talk / Voice")]
    [Tooltip("Nguồn phát voice. Trống → dùng alertSource")]
    public AudioSource talkSource;
    [Tooltip("Danh sách câu nói random. Rỗng → không phát gì")]
    public AudioClip[] talkClips;
    [Range(0, 1)] public float talkVolume = 1f;
    [Tooltip("Khoảng thời gian cooldown ngẫu nhiên (Min, Max) tính từ lúc câu nói trước KẾT THÚC")]
    public Vector2 talkCooldownRange = new Vector2(3f, 6f);

    [Header("🛠️ Debug")]
    public bool debugLog = false;

    [Networked] public NetworkBool NetworkIsChasing { get; set; }
    [Networked] public float NetworkSpeed { get; set; }

    // ═══════════ RUNTIME ═══════════
    [SerializeField] private AIState _state = AIState.Idle;

    private static int _musicRefCount = 0;
    private static AudioSource _activeMusicSrc = null;
    private bool _musicOn = false;

    private Vector3 _homePos;
    private Quaternion _homeRot;
    private bool _homeInitialized = false;
    private float _nextTalkTime = 0f;
    private float _talkEndTime = 0f;


    private PlayerController _target;
    private float _chaseTimer;
    private float _lastCatchTime = -10f;
    private float _catchFreezeUntil = 0f;

    private Coroutine _idleRoutine;
    private Coroutine _doorRoutine;
    private int _patrolIndex = 0;
    private int _stationaryAngleIndex = 0;
    private Vector3 _doorInvestigatePos;

    private int _animSpeedHash;
    private bool _hasSpeedParam;
    private string _lastAnimTag = "";

    public bool HasAuthority => !Object || !Object.IsValid || Object.HasStateAuthority;

    // ═══════════════════════ LIFECYCLE ═══════════════════════
    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        _animSpeedHash = Animator.StringToHash("Speed");
        if (animator != null)
            foreach (var p in animator.parameters)
                if (p.nameHash == _animSpeedHash) { _hasSpeedParam = true; break; }

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 18f;
            agent.updateRotation = true;
            agent.stoppingDistance = 0.3f;
        }

        if (flashlight == null) flashlight = GetComponentInChildren<Light>();
        if (flashlight != null && flashlight.range > 0.1f) flashlightNormalRange = flashlight.range;

        InitAudio();
    }

    public override void Spawned()
    {
        base.Spawned();
        CacheHome();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(transform.position);
    }

    void CacheHome()
    {
        if (_homeInitialized) return;
        if (stationaryAnchor != null)
        {
            _homePos = stationaryAnchor.position;
            _homeRot = stationaryAnchor.rotation;
        }
        else
        {
            _homePos = transform.position;
            _homeRot = transform.rotation;
        }
        _homeInitialized = true;
    }

    void InitAudio()
    {
        if (themeSource == null)
        {
            Transform t = transform.Find("AudioManager/Theme") ?? transform.Find("Theme");
            if (t != null) themeSource = t.GetComponent<AudioSource>();
        }
        if (themeSource == null)
        {
            var go = new GameObject("AutoThemeSource");
            go.transform.SetParent(transform, false);
            themeSource = go.AddComponent<AudioSource>();
        }
        themeSource.playOnAwake = false;
        themeSource.loop = true;
        themeSource.spatialBlend = 0f;
        if (chaseTheme != null) themeSource.clip = chaseTheme;
        if (alertSource != null) alertSource.spatialBlend = 1f;

        if (footstepSource == null)
        {
            Transform t = transform.Find("AudioManager/FootStep") ?? transform.Find("FootStep");
            if (t != null) footstepSource = t.GetComponent<AudioSource>();
            if (footstepSource == null) footstepSource = GetComponentInChildren<AudioSource>();
        }
        if (footstepSource != null) footstepSource.spatialBlend = 1f;

        if (talkSource == null) talkSource = alertSource;
        if (talkSource != null) talkSource.spatialBlend = 1f;
    }

    // ═══════════════════════ MAIN UPDATE ═══════════════════════
    void Update()
    {
        if (!_homeInitialized) CacheHome();

        UpdateFlashlight();

        if (!HasAuthority) { RemoteTick(); return; }
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // ─── Tạm dừng di chuyển khi đang diễn Animation Catch ───
        if (Time.time < _catchFreezeUntil)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            if (Object != null && Object.IsValid)
            {
                NetworkIsChasing = (_state == AIState.Chase);
                NetworkSpeed = 0f;
            }
            UpdateAnim(0f);
            return;
        }

        // ─── 0. Always check Instant Catch Zone (Bất kể trạng thái nào nếu Player bước vào vùng catchDistance và không nấp) ───
        CheckInstantCatchZone();

        // ─── 1. Detection ───
        PlayerController spotted = DetectPlayer();

        // ─── 2. State priority: Chase > DoorInvestigate > Return > Idle ───
        if (_state == AIState.Chase)
        {
            ChaseTick(spotted);
        }
        else if (spotted != null)
        {
            EnterChase(spotted); // mọi state đều có thể bị interrupt bởi Player
        }
        else
        {
            switch (_state)
            {
                case AIState.Idle: IdleTick(); break;
                case AIState.Return: ReturnTick(); break;
                case AIState.DoorInvestigate: /* routine tự chạy */ break;
            }
        }

        // ─── 3. Sync network ───
        if (Object != null && Object.IsValid)
        {
            NetworkIsChasing = (_state == AIState.Chase);
            NetworkSpeed = agent.velocity.magnitude;
        }

        UpdateAnim(agent.velocity.magnitude);

        // Tự động nói chuyện định kỳ ở mọi trạng thái (Idle, Patrol, Wander, Chase, Return...)
        PlayTalkSound();

        if (debugLog && Time.frameCount % 60 == 0)
            Debug.Log($"[{npcName}] State={_state} timer={_chaseTimer:F1} " +
                      $"distHome={Vector3.Distance(transform.position, _homePos):F1}");
    }

    void RemoteTick()
    {
        UpdateFlashlight();
        bool chasing = (Object != null && Object.IsValid) && NetworkIsChasing;
        SetMusic(chasing);
        float spd = (Object != null && Object.IsValid) ? (float)NetworkSpeed : 0f;
        UpdateAnim(spd);
    }

    private static PlayerController _cachedLocalPlayer;
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

    void UpdateFlashlight()
    {
        if (flashlight == null) return;
        PlayerController local = GetLocalPlayer();
        bool isCrouched = local != null && IsCrouching(local);

        float targetRange = isCrouched ? (flashlightNormalRange * 0.5f) : flashlightNormalRange;
        flashlight.range = Mathf.Lerp(flashlight.range, targetRange, Time.deltaTime * flashlightTransitionSpeed);
    }

    void UpdateAnim(float speed)
    {
        if (animator == null) return;
        if (_hasSpeedParam) animator.SetFloat(_animSpeedHash, speed);

        string tag = speed < 0.2f ? "idle" : (speed > 2.5f ? "run" : "walk");
        if (tag != _lastAnimTag)
        {
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == tag)
                {
                    animator.SetTrigger(tag);
                    break;
                }
            }
            _lastAnimTag = tag;
        }
    }

    // ═══════════════════════ STATE TRANSITIONS ═══════════════════════
    void SetState(AIState next)
    {
        if (_state == next) return;
        if (debugLog) Debug.Log($"[{npcName}] {_state} → {next}");

        if (_state == AIState.Idle && _idleRoutine != null)
        {
            StopCoroutine(_idleRoutine);
            _idleRoutine = null;
            if (agent != null) agent.updateRotation = true;   // ← THÊM
        }
        if (_state == AIState.DoorInvestigate && _doorRoutine != null)
        {
            StopCoroutine(_doorRoutine);
            _doorRoutine = null;
            if (agent != null) agent.updateRotation = true;   // ← THÊM
        }

        _state = next;

        if (next == AIState.Chase || next == AIState.Return || next == AIState.DoorInvestigate)
        {
            if (agent != null) agent.updateRotation = true;   // ← THÊM
        }

        if (next != AIState.Chase)
        {
            SetMusic(false);
            if (Object != null && Object.IsValid) NetworkIsChasing = false;
        }
    }

    // ═══════════════════════ DETECT ═══════════════════════
    PlayerController DetectPlayer()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        PlayerController p = RaycastDetect(origin);
        if (p != null) return p;
        return ProximityDetect();
    }

    PlayerController RaycastDetect(Vector3 origin)
    {
        Vector3 fwd = transform.forward;
        Vector3[] dirs =
        {
            fwd,
            Quaternion.AngleAxis(-raycastAngle, Vector3.up) * fwd,
            Quaternion.AngleAxis( raycastAngle, Vector3.up) * fwd,
            Quaternion.AngleAxis( raycastAngle * 0.7f, transform.right) * fwd,
            Quaternion.AngleAxis(-raycastAngle * 0.7f, transform.right) * fwd,
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            var hits = Physics.RaycastAll(origin, dirs[i], visionRange, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int h = 0; h < hits.Length; h++)
            {
                var col = hits[h].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                if (col.CompareTag("Player"))
                {
                    var pc = col.GetComponent<PlayerController>()
                          ?? col.GetComponentInParent<PlayerController>();
                    if (pc == null || !pc.gameObject.activeInHierarchy || IsDead(pc) || pc.isHiding) continue;

                    if (IsCrouching(pc) && hits[h].distance > visionRange * crouchVisionMultiplier)
                        continue;

                    Debug.DrawLine(origin, hits[h].point, Color.red);
                    return pc;
                }
                else
                {
                    Debug.DrawLine(origin, hits[h].point, Color.yellow);
                    break;
                }
            }
            Debug.DrawLine(origin, origin + dirs[i] * visionRange, Color.green);
        }
        return null;
    }

    PlayerController ProximityDetect()
    {
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            var pc = all[i];
            if (pc == null || !pc.gameObject.activeInHierarchy || IsDead(pc) || pc.isHiding) continue;

            // ⭐ THÊM dòng này — tính khoảng cách trước
            float dist = Vector3.Distance(transform.position, pc.transform.position);

            float effectiveRange = IsCrouching(pc) ? (visionRange * crouchVisionMultiplier) : visionRange;
            if (dist > effectiveRange) continue;

            if (_state != AIState.Chase)
            {
                Vector3 toPlayer = pc.transform.position - transform.position;
                if (Vector3.Angle(transform.forward, toPlayer) > raycastAngle * 1.5f) continue;
            }

            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPos = pc.transform.position + Vector3.up;
            Vector3 dir = (targetPos - origin).normalized;

            var hits = Physics.RaycastAll(origin, dir, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool visible = false;
            for (int h = 0; h < hits.Length; h++)
            {
                var col = hits[h].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                if (col.CompareTag("Player")) { visible = true; break; }
                break;
            }

            if (visible && dist < bestDist) { bestDist = dist; best = pc; }
        }
        return best;
    }

    static bool IsCrouching(PlayerController p)
    {
        if (p == null) return false;
        if (p.Crouching) return true;
        var ns = p.GetComponent<NetworkPlayerSync>();
        if (ns != null && ns.Object != null && ns.Object.IsValid)
        {
            try { return ns.NetworkCrouch; } catch { }
        }
        return false;
    }

    static bool IsDead(PlayerController p)
    {
        if (p == null) return true;
        if (p.deathHandler != null && p.deathHandler.isDeadProcessed) return true;
        if (p.stats != null && p.stats.isDied) return true;
        return false;
    }

    // ═══════════════════════ CHASE ═══════════════════════
    void EnterChase(PlayerController target)
    {
        if (target == null) return;

        _target = target;
        _chaseTimer = loseSightGrace;

        SetState(AIState.Chase);

        PlayAlert();
        PlayTalkSound();
        SetMusic(true);
        if (Object != null && Object.IsValid) NetworkIsChasing = true;

        if (agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.SetDestination(target.transform.position);
        }

        CallNearbyAdults(target);
    }
    // ═══════════════════════ SYNC AUDIO & ANIMATION RPCS ═══════════════════════
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayCatchAnim()
    {
        PlayCatchAnimLocal();
    }

    void PlayCatchAnimLocal()
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(catchAnimTrigger))
        {
            foreach (var p in animator.parameters)
            {
                if (p.name == catchAnimTrigger)
                {
                    if (p.type == AnimatorControllerParameterType.Trigger)
                        animator.SetTrigger(catchAnimTrigger);
                    else if (p.type == AnimatorControllerParameterType.Bool)
                        animator.SetBool(catchAnimTrigger, true);
                    return;
                }
            }
            // Dự phòng: nếu không tìm thấy parameter, thử Play State trực tiếp
            try { animator.Play(catchAnimTrigger); } catch { }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayAlertSound()
    {
        if (alertSource != null && alertSound != null && !alertSource.isPlaying)
            alertSource.PlayOneShot(alertSound);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayCatchSound()
    {
        PlayCatchSoundLocal();
    }

    void PlayCatchSoundLocal()
    {
        if (catchSound == null) return;
        AudioSource src = alertSource != null ? alertSource : talkSource;
        if (src != null)
        {
            float vol = catchVolume > 0.01f ? catchVolume : 1f;
            src.PlayOneShot(catchSound, vol);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayTalkSound(int clipIndex)
    {
        if (talkClips == null || clipIndex < 0 || clipIndex >= talkClips.Length) return;
        var clip = talkClips[clipIndex];
        if (clip == null) return;
        AudioSource src = talkSource != null ? talkSource : alertSource;
        if (src == null) return;
        src.PlayOneShot(clip, talkVolume);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcInvestigatePosition(Vector3 targetPosition)
    {
        EnterDoorInvestigate(targetPosition);
    }

    void ChaseTick(PlayerController spotted)
    {
        if (spotted != null)
        {
            agent.updateRotation = true;
            _target = spotted;
            _chaseTimer = loseSightGrace;    // ← reset về grace mỗi frame còn thấy
        }
        else
        {
            _chaseTimer -= Time.deltaTime;   // ← mất dấu → đếm ngược
        }

        // Target invalid → bỏ chase
        if (_target == null || !_target.gameObject.activeInHierarchy || IsDead(_target))
        {
            EnterReturn();
            return;
        }

        // Timer hết → bỏ chase
        if (_chaseTimer <= 0f)
        {
            EnterReturn();
            return;
        }

        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        Vector3 targetCenter = _target.transform.position + Vector3.up * 0.9f;
        float distTarget = Vector3.Distance(catchOrigin, targetCenter);
        if (!_target.isHiding && (distTarget <= catchDistance || Vector3.Distance(transform.position, _target.transform.position) <= catchDistance))
        {
            TryCatch(_target);
            return;
        }

        if (agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            agent.SetDestination(_target.transform.position);
        }

        PlayTalkSound();

        if (Time.frameCount % 30 == 0 && _target != null)
        {
            CallNearbyAdults(_target);
        }
    }
    // ═══════════════════════ RETURN ═══════════════════════
    void EnterReturn()
    {
        _target = null;
        SetState(AIState.Return);
        if (agent.isOnNavMesh)
        {
            agent.speed = returnSpeed;
            agent.SetDestination(_homePos);
        }
    }

    void ReturnTick()
    {
        if (agent.isOnNavMesh)
        {
            agent.speed = returnSpeed;
            if (!agent.hasPath || Vector3.Distance(agent.destination, _homePos) > 0.5f)
                agent.SetDestination(_homePos);
        }

        if (Vector3.Distance(transform.position, _homePos) <= 0.6f)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            SetState(AIState.Idle);
            _patrolIndex = 0;
            _stationaryAngleIndex = 0;
        }
    }

    // ═══════════════════════ IDLE — MOVEMENT MODES ═══════════════════════
    void IdleTick()
    {
        if (!agent.isOnNavMesh) return;

        if (_idleRoutine == null)
        {
            switch (movementMode)
            {
                case MovementMode.Stationary: _idleRoutine = StartCoroutine(StationaryRoutine()); break;
                case MovementMode.Patrol: _idleRoutine = StartCoroutine(PatrolRoutine()); break;
                case MovementMode.Wander: _idleRoutine = StartCoroutine(WanderRoutine()); break;
            }
        }
    }

    // ─── MODE 1: STATIONARY ───
    // Đứng yên tại anchor, xoay giữa 3 hướng [-lookAngleSpread, 0, +lookAngleSpread] so với anchor
    IEnumerator StationaryRoutine()
    {
        if (agent.isOnNavMesh) agent.ResetPath();

        Quaternion baseRot = stationaryAnchor != null ? stationaryAnchor.rotation : _homeRot;

        while (_state == AIState.Idle)
        {
            // Chọn góc tiếp theo: -spread / 0 / +spread
            float[] offsets = { 0f, -lookAngleSpread, lookAngleSpread };
            float chosen = offsets[_stationaryAngleIndex];
            _stationaryAngleIndex = (_stationaryAngleIndex + 1) % offsets.Length;

            Quaternion targetRot = baseRot * Quaternion.Euler(0f, chosen, 0f);

            // Xoay mượt về hướng đó
            float elapsed = 0f;
            float turnDuration = 1f;
            Quaternion startRot = transform.rotation;
            while (elapsed < turnDuration)
            {
                if (_state != AIState.Idle) yield break;
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / turnDuration);
                yield return null;
            }
            transform.rotation = targetRot;

            // Nghỉ ngẫu nhiên
            float rest = Random.Range(stationaryRestTimeRange.x, stationaryRestTimeRange.y);
            float waitTimer = 0f;
            while (waitTimer < rest)
            {
                if (_state != AIState.Idle) yield break;
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        _idleRoutine = null;
    }

    // ─── MODE 2: PATROL ───
    // Đi tuần tự các waypoint, dừng nghỉ + ngó trái/phải tại mỗi điểm
    IEnumerator PatrolRoutine()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            movementMode = MovementMode.Stationary;
            _idleRoutine = null;
            yield break;
        }

        while (_state == AIState.Idle)
        {
            Transform wp = patrolPoints[_patrolIndex];
            if (wp == null)
            {
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                continue;
            }

            // Di chuyển tới waypoint
            if (agent.isOnNavMesh)
            {
                agent.speed = walkSpeed;
                agent.updateRotation = true;
                agent.SetDestination(wp.position);
            }

            // Đợi đến nơi
            while (agent.pathPending || agent.remainingDistance > 0.5f)
            {
                if (_state != AIState.Idle) yield break;
                yield return null;
            }

            // Ngó trái/phải
            yield return LookLeftRightRoutine(patrolLookAngle);

            // Nghỉ
            yield return WaitWhileIdle(Random.Range(patrolRestTimeRange.x, patrolRestTimeRange.y));

            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
        }
        _idleRoutine = null;
    }

    // ─── MODE 3: WANDER ───
    // Đi tới điểm random quanh tâm wander, đến nơi thì 50% ngó trái/phải hoặc nghỉ
    IEnumerator WanderRoutine()
    {
        Vector3 center = wanderCenterObject != null ? wanderCenterObject.position : _homePos;

        while (_state == AIState.Idle)
        {
            // Chọn điểm random trên NavMesh
            Vector3 rand = center + Random.insideUnitSphere * wanderRadius;
            rand.y = center.y;

            if (NavMesh.SamplePosition(rand, out var hit, wanderRadius, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh)
                {
                    agent.speed = walkSpeed;
                    agent.updateRotation = true;
                    agent.SetDestination(hit.position);
                }

                while (agent.pathPending || agent.remainingDistance > 0.5f)
                {
                    if (_state != AIState.Idle) yield break;
                    yield return null;
                }
            }

            // Tới nơi: 50% ngó trái/phải, 50% nghỉ
            if (Random.value < wanderLookChance)
                yield return LookLeftRightRoutine(wanderLookAngle);
            else
                yield return WaitWhileIdle(Random.Range(wanderRestTimeRange.x, wanderRestTimeRange.y));
        }
        _idleRoutine = null;
    }

    // ─── HELPERS ───
    IEnumerator LookLeftRightRoutine(float angle)
    {
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.updateRotation = false;

        Quaternion original = transform.rotation;

        yield return RotateToRoutine(original * Quaternion.Euler(0, -angle, 0), 1.0f);
        yield return WaitWhileIdle(Random.Range(0.8f, 1.5f));
        yield return RotateToRoutine(original * Quaternion.Euler(0, angle, 0), 1.0f);
        yield return WaitWhileIdle(Random.Range(0.8f, 1.5f));
        yield return RotateToRoutine(original, 0.8f);

        agent.updateRotation = true;
    }

    IEnumerator RotateToRoutine(Quaternion targetRot, float duration)
    {
        Quaternion start = transform.rotation;
        float t = 0f;
        while (t < 1f)
        {
            if (_state != AIState.Idle) yield break;
            t += Time.deltaTime / duration;
            transform.rotation = Quaternion.Slerp(start, targetRot, t);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    IEnumerator WaitWhileIdle(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (_state != AIState.Idle) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    // ═══════════════════════ INVESTIGATION ALERT ═══════════════════════

    /// <summary>
    /// Kiểm tra xem NPC có nghe thấy tiếng động phát ra tại noisePos không
    /// (Tính toán theo thính lực hearingRange và bộ lọc canHear / canHearPlayer của NPC)
    /// </summary>
    public bool CanHearNoise(Vector3 noisePos, bool isFromPlayer = false)
    {
        if (noisePos == Vector3.zero) return false;
        if (!canHear || hearingRange <= 0.001f) return false;
        if (isFromPlayer && !canHearPlayer) return false;
        if (_state == AIState.Chase) return false;

        float dist = Vector3.Distance(transform.position, noisePos);
        return dist <= hearingRange;
    }

    /// <summary>
    /// Báo động cho tất cả Adult có thính giác nghe được tiếng động tới điều tra vị trí (Hỗ trợ cả Online RPC và Offline).
    /// </summary>
    public static void AlertAdultsToPosition(Vector3 targetPosition, bool isFromPlayer = false)
    {
        var all = FindObjectsByType<AdultGuardNPC>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var adult = all[i];
            if (adult == null) continue;
            if (!adult.CanHearNoise(targetPosition, isFromPlayer)) continue;

            if (adult.Object != null && adult.Object.IsValid)
                adult.RpcInvestigatePosition(targetPosition);
            else
                adult.EnterDoorInvestigate(targetPosition);
        }
    }

    public void EnterDoorInvestigate(Vector3 doorPos)
    {
        if (_state == AIState.DoorInvestigate && Vector3.Distance(_doorInvestigatePos, doorPos) < 1.5f) return;

        // Dừng routine cũ để lập tức phản ứng và chạy tới vị trí phát tiếng động mới nhất
        if (_idleRoutine != null) { StopCoroutine(_idleRoutine); _idleRoutine = null; }
        if (_doorRoutine != null) { StopCoroutine(_doorRoutine); _doorRoutine = null; }

        _doorInvestigatePos = doorPos;
        SetState(AIState.DoorInvestigate);
        PlayAlert();
        PlayTalkSound();

        if (debugLog) Debug.Log($"[{npcName}] Door investigate @ {doorPos}");

        _doorRoutine = StartCoroutine(DoorInvestigateRoutine(doorPos));
    }

    IEnumerator DoorInvestigateRoutine(Vector3 doorPos)
    {
        // 1. Chạy tới điểm phát tiếng động (Tìm NavMesh chính xác theo độ cao tầng, không nhảy lên trần/tầng 2)
        if (agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.speed = runSpeed;

            Vector3 dest = doorPos;
            if (NavMesh.SamplePosition(doorPos, out var doorHit, 1.5f, NavMesh.AllAreas))
            {
                if (Mathf.Abs(doorHit.position.y - doorPos.y) <= 1.8f)
                {
                    dest = doorHit.position;
                }
            }

            agent.SetDestination(dest);

            while (agent.pathPending || agent.remainingDistance > 0.5f)
            {
                if (_state != AIState.DoorInvestigate) { _doorRoutine = null; yield break; }
                yield return null;
            }
        }

        // 2. Đi quanh điểm phát tiếng động theo vòng tròn đúng tầng
        if (agent.isOnNavMesh) agent.speed = walkSpeed;

        int steps = Mathf.Max(3, doorInspectSteps);
        float startAngle = Random.Range(0f, 360f);
        float angleStep = 360f / steps;

        for (int i = 0; i < steps; i++)
        {
            if (_state != AIState.DoorInvestigate) { _doorRoutine = null; yield break; }

            float angleDeg = startAngle + angleStep * i;
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * doorInspectRadius;
            Vector3 target = doorPos + offset;

            // Di chuyển tới điểm xung quanh (giới hạn độ cao tầng không quá 1.8m)
            if (NavMesh.SamplePosition(target, out var point, 1.0f, NavMesh.AllAreas))
            {
                if (Mathf.Abs(point.position.y - doorPos.y) <= 1.8f)
                {
                    agent.SetDestination(point.position);
                    while (agent.pathPending || agent.remainingDistance > 0.3f)
                    {
                        if (_state != AIState.DoorInvestigate) { _doorRoutine = null; yield break; }
                        yield return null;
                    }
                }
            }

            // Quay mặt về phía cửa
            Vector3 lookDir = doorPos - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                Quaternion startRot = transform.rotation;
                float t = 0f;
                while (t < 1f)
                {
                    if (_state != AIState.DoorInvestigate) { _doorRoutine = null; yield break; }
                    t += Time.deltaTime * 3f;
                    transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
                transform.rotation = targetRot;
            }

            // Pause + ngó trái/phải nhẹ để "quan sát"
            float pause = Random.Range(doorLookPauseRange.x, doorLookPauseRange.y);
            float tPause = 0f;
            while (tPause < pause)
            {
                if (_state != AIState.DoorInvestigate) { _doorRoutine = null; yield break; }
                tPause += Time.deltaTime;
                yield return null;
            }
        }

        // 3. Xong → về nhà
        if (_state == AIState.DoorInvestigate)
            EnterReturn();

        _doorRoutine = null;
    }

    // ═══════════════════════ CATCH ═══════════════════════
    void CheckInstantCatchZone()
    {
        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var pc = all[i];
            if (pc == null || !pc.gameObject.activeInHierarchy || pc.isHiding || IsDead(pc)) continue;

            Vector3 targetCenter = pc.transform.position + Vector3.up * 0.9f;
            float distOrigin = Vector3.Distance(catchOrigin, targetCenter);
            float distBase = Vector3.Distance(transform.position, pc.transform.position);

            if (distOrigin <= catchDistance || distBase <= catchDistance)
            {
                TryCatch(pc);
                break;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return;

        var pc = other.GetComponent<PlayerController>()
              ?? other.GetComponentInParent<PlayerController>();
        if (pc != null && !pc.isHiding)
        {
            TryCatch(pc);
        }
    }

    void TryCatch(PlayerController target)
    {
        if (target == null || target.isHiding) return;
        if (Time.time - _lastCatchTime < catchCooldown) return;
        if (IsDead(target)) return;

        var dh = target.deathHandler
              ?? target.GetComponent<PlayerDeathHandler>()
              ?? target.GetComponentInParent<PlayerDeathHandler>();

        if (dh != null)
        {
            if (dh.IsInvulnerable || dh.isDeadProcessed) return;
            _lastCatchTime = Time.time;
            dh.TriggerDeath(npcName);
        }
        else
        {
            _lastCatchTime = Time.time;
            if (target.stats != null) target.stats.isDied = true;
        }

        // ⭐ Tạm dừng di chuyển và quay mặt về phía target để diễn Animation Catch
        if (catchFreezeDuration > 0f)
        {
            _catchFreezeUntil = Time.time + catchFreezeDuration;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            Vector3 lookDir = target.transform.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // ⭐ Phát Animation Catch & Âm thanh Catch / Kill (Đồng bộ qua Fusion RPC)
        if (Object != null && Object.IsValid)
        {
            if (HasAuthority)
            {
                RpcPlayCatchAnim();
                if (catchSound != null) RpcPlayCatchSound();
            }
        }
        else
        {
            PlayCatchAnimLocal();
            PlayCatchSoundLocal();
        }

        PlayTalkSound();
        EnterReturn();
    }

    // ═══════════════════════ CALL ADULTS ═══════════════════════
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcEnterChase(NetworkId targetPlayerId)
    {
        if (_state == AIState.Chase) return;
        if (Runner != null && Runner.TryFindObject(targetPlayerId, out var netObj))
        {
            var pc = netObj.GetComponent<PlayerController>() ?? netObj.GetComponentInParent<PlayerController>();
            if (pc != null && pc.gameObject.activeInHierarchy && !IsDead(pc))
            {
                EnterChase(pc);
            }
        }
    }

    void CallNearbyAdults(PlayerController target)
    {
        if (target == null || callAdultRadius <= 0.001f) return;

        var allAdults = FindObjectsByType<AdultGuardNPC>(FindObjectsSortMode.None);
        for (int i = 0; i < allAdults.Length; i++)
        {
            var other = allAdults[i];
            if (other == null || other == this) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist <= callAdultRadius)
            {
                if (other._state != AIState.Chase)
                {
                    if (other.HasAuthority)
                    {
                        other.EnterChase(target);
                    }
                    else if (other.Object != null && other.Object.IsValid)
                    {
                        var targetNetObj = target.GetComponent<NetworkObject>() ?? target.GetComponentInParent<NetworkObject>();
                        if (targetNetObj != null)
                        {
                            other.RpcEnterChase(targetNetObj.Id);
                        }
                    }
                }
            }
        }
    }

    // ═══════════════════════ AUDIO ═══════════════════════
    void PlayAlert()
    {
        if (Object != null && Object.IsValid)
        {
            // Online: authority gửi RPC, mọi client (kể cả authority) đều phát
            if (HasAuthority) RpcPlayAlertSound();
        }
        else
        {
            // Offline fallback
            if (alertSource != null && alertSound != null && !alertSource.isPlaying)
                alertSource.PlayOneShot(alertSound);
        }
    }

    void SetMusic(bool play)
    {
        if (play == _musicOn) return;
        _musicOn = play;

        if (play)
        {
            _musicRefCount++;
            if (_musicRefCount == 1) PlayMusicHere();
        }
        else
        {
            _musicRefCount = Mathf.Max(0, _musicRefCount - 1);
            if (_activeMusicSrc == themeSource)
            {
                StopMusicHere();
                if (_musicRefCount > 0) TryTransferMusic();
            }
        }
    }

    void PlayTalkSound()
    {
        if (talkClips == null || talkClips.Length == 0) return;
        if (Time.time < _talkEndTime) return;
        if (Time.time < _nextTalkTime) return;

        // Chọn clip hợp lệ
        var validIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < talkClips.Length; i++)
            if (talkClips[i] != null) validIndices.Add(i);
        if (validIndices.Count == 0) return;

        int clipIndex = validIndices[Random.Range(0, validIndices.Count)];
        AudioClip clip = talkClips[clipIndex];

        // Cập nhật cooldown NGAY (chỉ authority chạy tới đây vì Update() chỉ gọi khi HasAuthority)
        _talkEndTime = Time.time + clip.length;
        float randomCooldown = Random.Range(talkCooldownRange.x, talkCooldownRange.y);
        _nextTalkTime = _talkEndTime + randomCooldown;

        // Phát cho tất cả client
        if (Object != null && Object.IsValid)
            RpcPlayTalkSound(clipIndex);
        else
            PlayTalkLocal(clipIndex);
    }

    void PlayTalkLocal(int clipIndex)
    {
        if (talkClips == null || clipIndex < 0 || clipIndex >= talkClips.Length) return;
        var clip = talkClips[clipIndex];
        if (clip == null) return;
        AudioSource src = talkSource != null ? talkSource : alertSource;
        if (src == null) return;
        src.PlayOneShot(clip, talkVolume);
    }
    void PlayMusicHere()
    {
        if (themeSource == null) return;
        if (chaseTheme != null) themeSource.clip = chaseTheme;
        themeSource.loop = true;
        if (!themeSource.isPlaying) themeSource.Play();
        _activeMusicSrc = themeSource;
    }

    void StopMusicHere()
    {
        if (themeSource != null) themeSource.Stop();
        _activeMusicSrc = null;
    }

    void TryTransferMusic()
    {
        var all = FindObjectsByType<AdultGuardNPC>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var a = all[i];
            if (a == null || a == this) continue;
            if (a._musicOn && a.themeSource != null) { a.PlayMusicHere(); return; }
        }
    }

    // ═══════════════════════ ANIMATION EVENTS ═══════════════════════
    public void OnFootstep(AnimationEvent animationEvent) { PlayFootstep(); }
    public void OnFootstep() { PlayFootstep(); }
    public void OnLand(AnimationEvent animationEvent) { }
    public void OnLand() { }

    public void PlayFootstep()
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;
        var valid = System.Array.FindAll(footstepClips, c => c != null);
        if (valid.Length == 0) return;
        AudioClip clip = valid[Random.Range(0, valid.Length)];
        float vol = footstepVolume > 0.01f ? footstepVolume : 0.5f;
        footstepSource.PlayOneShot(clip, vol);
    }

    // ═══════════════════════ GIZMOS ═══════════════════════
    void OnDrawGizmosSelected()
    {
        // Chase + catch
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, callAdultRadius);
        Gizmos.color = Color.red;
        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        Gizmos.DrawWireSphere(catchOrigin, catchDistance);

        // Hearing
        if (canHear && hearingRange > 0f)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f); // Cyan
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }

        // Vision
        Gizmos.color = Color.green;
        Vector3 o = transform.position + Vector3.up * eyeHeight;
        Vector3 f = transform.forward;
        Gizmos.DrawRay(o, f * visionRange);
        Gizmos.DrawRay(o, Quaternion.AngleAxis(-raycastAngle, Vector3.up) * f * visionRange);
        Gizmos.DrawRay(o, Quaternion.AngleAxis(raycastAngle, Vector3.up) * f * visionRange);

        // Stationary
        if (movementMode == MovementMode.Stationary && stationaryAnchor != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(stationaryAnchor.position, 0.4f);
            Gizmos.DrawLine(transform.position, stationaryAnchor.position);
        }

        // Wander
        if (movementMode == MovementMode.Wander)
        {
            Vector3 c = wanderCenterObject != null ? wanderCenterObject.position : _homePos;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(c, wanderRadius);
        }

        // Patrol
        if (movementMode == MovementMode.Patrol && patrolPoints != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                if (i + 1 < patrolPoints.Length && patrolPoints[i + 1] != null)
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
        }

    }
}