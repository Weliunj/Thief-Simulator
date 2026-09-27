using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Fusion;
using StarterAssets;

[RequireComponent(typeof(NavMeshAgent))]
public class KidRunnerNPC : NetworkBehaviour
{
    public enum MovementMode { Stationary, Patrol, Wander }
    public enum AIState { Idle, Panic, Return }

    [Header("👤 Identity")]
    public string npcName = "Kid";
    public NavMeshAgent agent;
    public Animator animator;

    [Header("🚶 Movement Mode")]
    public MovementMode movementMode = MovementMode.Wander;

    [Header("🏃 Speeds")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float returnSpeed = 2.0f;

    [Header("👀 Vision")]
    public float visionRange = 15f;
    public float raycastAngle = 30f;
    [Range(0.1f, 1f)] public float crouchVisionMultiplier = 0.5f;
    public float eyeHeight = 1.2f;

    [Header("👂 Hearing (Thính giác)")]
    [Tooltip("Bật/tắt thính giác của Kid này (tự động nghe tiếng động cửa/item/bẫy...)")]
    public bool canHear = true;
    [Tooltip("Bật/tắt khả năng nghe tiếng bước chân / tiếng động phát ra từ Player")]
    public bool canHearPlayer = true;
    [Tooltip("Khoảng cách nghe tối đa của Kid này (mét)")]
    public float hearingRange = 15f;

    [Header("🏃 Panic / Flee")]
    [Tooltip("Số giây tiếp tục hoảng loạn sau khi mất dấu player")]
    public float loseSightGrace = 6f;

    [Header("📢 Call Chain")]
    [Tooltip("Bán kính gọi Adult")]
    public float callAdultRadius = 12f;
    [Tooltip("Bán kính gọi Kid khác")]
    public float callKidRadius = 16f;
    [Tooltip("Cooldown giữa các lần gọi Adult (tránh spam)")]
    public float adultCallCooldown = 1.5f;
    [Tooltip("Cooldown chain call giữa các Kid (tránh loop)")]
    public float kidChainCooldown = 6f;

    // ═══════════ MODE 1: STATIONARY ═══════════
    [Header("🧍 Mode 1: Stationary")]
    public Transform stationaryAnchor;
    public float lookAngleSpread = 45f;
    public Vector2 stationaryRestTimeRange = new Vector2(2f, 5f);

    // ═══════════ MODE 2: PATROL ═══════════
    [Header("🚶 Mode 2: Patrol")]
    public Transform[] patrolPoints;
    public Vector2 patrolRestTimeRange = new Vector2(2f, 4f);
    public float patrolLookAngle = 40f;

    // ═══════════ MODE 3: WANDER ═══════════
    [Header("🚶 Mode 3: Wander")]
    public Transform wanderCenterObject;
    public float wanderRadius = 15f;
    public Vector2 wanderRestTimeRange = new Vector2(2f, 5f);
    public float wanderLookAngle = 40f;
    [Range(0f, 1f)] public float wanderLookChance = 0.5f;

    [Header("🔊 Audio")]
    public AudioSource alertSource;
    public AudioClip alertSound;
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    [Range(0, 1)] public float footstepVolume = 0.5f;

    [Header("🗣️ Talk / Voice (Tiếng kêu cứu / hoảng sợ)")]
    [Tooltip("Nguồn phát voice. Trống → dùng alertSource")]
    public AudioSource talkSource;
    [Tooltip("Danh sách câu nói / tiếng kêu cứu random. Rỗng → không phát gì")]
    public AudioClip[] talkClips;
    [Range(0, 1)] public float talkVolume = 1f;
    [Tooltip("Khoảng thời gian cooldown ngẫu nhiên (Min, Max) tính từ lúc câu nói trước KẾT THÚC")]
    public Vector2 talkCooldownRange = new Vector2(2.5f, 5f);

    [Header("🛠️ Debug")]
    public bool debugLog = false;

    [Networked] public NetworkBool NetworkIsPanicking { get; set; }
    [Networked] public float NetworkSpeed { get; set; }

    // ═══════════ RUNTIME ═══════════
    [SerializeField] private AIState _state = AIState.Idle;

    private Vector3 _homePos;
    private Quaternion _homeRot;
    private bool _homeInitialized = false;
    private float _nextTalkTime = 0f;
    private float _talkEndTime = 0f;

    private PlayerController _fleeFrom;
    private Vector3 _fleeFromPos;
    private float _panicTimer;
    private float _lastAdultCallTime = -100f;
    private float _lastChainCallTime = -100f;

    private Coroutine _idleRoutine;
    private int _patrolIndex = 0;
    private int _stationaryAngleIndex = 0;

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
        if (alertSource == null)
        {
            Transform t = transform.Find("AudioManager/Alert") ?? transform.Find("Alert");
            if (t != null) alertSource = t.GetComponent<AudioSource>();
        }
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

        if (!HasAuthority) { RemoteTick(); return; }
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        PlayerController spotted = DetectPlayer();

        if (_state == AIState.Panic)
        {
            PanicTick(spotted);
        }
        else if (spotted != null)
        {
            EnterPanic(spotted, false);
        }
        else
        {
            switch (_state)
            {
                case AIState.Idle: IdleTick(); break;
                case AIState.Return: ReturnTick(); break;
            }
        }

        if (Object != null && Object.IsValid)
        {
            NetworkIsPanicking = (_state == AIState.Panic);
            NetworkSpeed = agent.velocity.magnitude;
        }

        UpdateAnim(agent.velocity.magnitude);

        // Tự động nói / kêu cứu định kỳ ở mọi trạng thái (Idle, Patrol, Wander, Panic, Return...)
        PlayTalkSound();

        if (debugLog && Time.frameCount % 60 == 0)
            Debug.Log($"[{npcName}] State={_state} timer={_panicTimer:F1}");
    }

    void RemoteTick()
    {
        bool panicking = (Object != null && Object.IsValid) && NetworkIsPanicking;
        float spd = (Object != null && Object.IsValid) ? (float)NetworkSpeed : 0f;
        UpdateAnim(spd);
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

    // ═══════════════════════ STATE ═══════════════════════
    void SetState(AIState next)
    {
        if (_state == next) return;
        if (debugLog) Debug.Log($"[{npcName}] {_state} → {next}");

        // ⭐ FIX: nếu đang thoát Idle, luôn reset updateRotation
        if (_state == AIState.Idle)
        {
            if (_idleRoutine != null)
            {
                StopCoroutine(_idleRoutine);
                _idleRoutine = null;
            }
            if (agent != null) agent.updateRotation = true;   // ← THÊM
        }

        _state = next;

        // ⭐ FIX: các state động cần agent tự xoay
        if (next == AIState.Panic || next == AIState.Return)
        {
            if (agent != null) agent.updateRotation = true;   // ← THÊM
        }

        if (next != AIState.Panic && Object != null && Object.IsValid)
            NetworkIsPanicking = false;
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
                    if (pc == null || !pc.gameObject.activeInHierarchy || IsDead(pc)) continue;

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
            if (pc == null || !pc.gameObject.activeInHierarchy || IsDead(pc)) continue;

            float dist = Vector3.Distance(transform.position, pc.transform.position);
            float effectiveRange = IsCrouching(pc) ? (visionRange * crouchVisionMultiplier) : visionRange;
            if (dist > effectiveRange) continue;

            if (_state != AIState.Panic)
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

    // ═══════════════════════ PANIC ═══════════════════════
    void EnterPanic(PlayerController fromPlayer, bool fromChain)
    {
        if (fromPlayer == null) return;
        EnterPanicInternal(fromPlayer, fromPlayer.transform.position, fromChain);
    }

    void EnterPanicFromDoor(Vector3 doorPos, bool fromChain = false)
    {
        EnterPanicInternal(null, doorPos, fromChain);
    }

    void EnterPanicInternal(PlayerController fromPlayer, Vector3 fleeFromPos, bool fromChain)
    {
        if (_state == AIState.Panic) return;

        _fleeFrom = fromPlayer;
        _fleeFromPos = fleeFromPos;
        _panicTimer = loseSightGrace;

        SetState(AIState.Panic);
        PlayAlert();
        PlayTalkSound();
        if (Object != null && Object.IsValid) NetworkIsPanicking = true;

        if (debugLog) Debug.Log($"[{npcName}] PANIC from {fleeFromPos}");

        // Chọn điểm chạy trốn
        PickFleePoint(fleeFromPos);

        // Chain call Kid khác (chỉ khi không phải từ chain, tránh loop)
        if (!fromChain && Time.time - _lastChainCallTime > kidChainCooldown)
        {
            _lastChainCallTime = Time.time;
            ChainCallKids(fromPlayer, fleeFromPos);
        }

        // Gọi Adult
        CallAdults(fromPlayer, fleeFromPos);
    }

    void PanicTick(PlayerController spotted)
    {
        // 1. Quét Player: Nếu nhìn thấy Player trong lúc đang chạy hoảng loạn -> Luôn reset lại bộ đếm loseSightGrace (mặc định 6s)
        if (spotted != null)
        {
            _fleeFrom = spotted;
            _fleeFromPos = spotted.transform.position;
            _panicTimer = loseSightGrace;
        }
        else
        {
            // Nếu không nhìn thấy Player -> Đếm ngược thời gian hoảng loạn
            _panicTimer -= Time.deltaTime;
        }

        // 2. Nếu trong 6s liên tục không nhìn thấy Player -> Bình tĩnh lại và quay về vị trí ban đầu
        if (_panicTimer <= 0f)
        {
            EnterReturn();
            return;
        }

        // 3. Chạy liên tục: Khi chạy đến đích hoặc mất đường đi -> Chọn ngay điểm ngẫu nhiên mới trên map để chạy tiếp
        bool arrived = !agent.pathPending && agent.remainingDistance <= 0.6f;

        if (arrived || !agent.hasPath)
        {
            PickFleePoint(_fleeFromPos);
        }

        if (agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.speed = runSpeed;
        }

        PlayTalkSound();

        if (Time.time - _lastAdultCallTime > adultCallCooldown)
        {
            _lastAdultCallTime = Time.time;
            CallAdults(_fleeFrom, _fleeFromPos);
        }

        // ⭐ Trong lúc chạy hoảng loạn khắp map, nếu chạy ngang qua Kid khác thì cũng la hét báo động
        if (Time.time - _lastChainCallTime > kidChainCooldown)
        {
            _lastChainCallTime = Time.time;
            ChainCallKids(_fleeFrom, _fleeFromPos);
        }
    }

    void PickFleePoint(Vector3 fromPos)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // 1. Thử lấy ngẫu nhiên 1 đỉnh / vị trí trên toàn bộ bề mặt NavMesh của Map
        var triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices != null && triangulation.vertices.Length > 0)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                int randomIndex = Random.Range(0, triangulation.vertices.Length);
                Vector3 randomPoint = triangulation.vertices[randomIndex];

                if (NavMesh.SamplePosition(randomPoint, out var hit, 2f, NavMesh.AllAreas))
                {
                    agent.speed = runSpeed;
                    agent.updateRotation = true;
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        // 2. Fallback: Lấy ngẫu nhiên xung quanh phạm vi rộng của Map
        Vector3 rand = transform.position + Random.insideUnitSphere * 30f;
        rand.y = transform.position.y;
        if (NavMesh.SamplePosition(rand, out var fallbackHit, 15f, NavMesh.AllAreas))
        {
            agent.speed = runSpeed;
            agent.updateRotation = true;
            agent.SetDestination(fallbackHit.position);
        }
    }

    // ═══════════════════════ CALL CHAIN ═══════════════════════
    void CallAdults(PlayerController player, Vector3 fallbackPos)
    {
        if (callAdultRadius <= 0.001f) return;
        var allAdults = FindObjectsByType<AdultGuardNPC>(FindObjectsSortMode.None);
        if (allAdults == null || allAdults.Length == 0) return;

        NetworkId playerId = default;
        bool hasPlayer = false;
        if (player != null)
        {
            var netObj = player.GetComponent<NetworkObject>() ?? player.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.Id.IsValid)
            {
                playerId = netObj.Id;
                hasPlayer = true;
            }
        }

        for (int i = 0; i < allAdults.Length; i++)
        {
            var adult = allAdults[i];
            if (adult == null) continue;

            float dist = Vector3.Distance(transform.position, adult.transform.position);
            if (dist > callAdultRadius) continue;

            if (hasPlayer)
            {
                // Kid thấy player → Adult chase player
                if (adult.HasAuthority)
                {
                    if (debugLog) Debug.Log($"[{npcName}] Gọi {adult.npcName} chase player!");
                    adult.RpcEnterChase(playerId);
                }
                else
                {
                    adult.RpcEnterChase(playerId);
                }
            }
            else
            {
                // Kid thấy cửa break → Adult tới cửa
                if (adult.HasAuthority)
                {
                    if (debugLog) Debug.Log($"[{npcName}] Gọi {adult.npcName} tới cửa {fallbackPos}");
                    adult.EnterDoorInvestigate(fallbackPos);
                }
            }
        }
    }

    void ChainCallKids(PlayerController player, Vector3 fallbackPos)
    {
        if (callKidRadius <= 0.001f) return;
        var allKids = FindObjectsByType<KidRunnerNPC>(FindObjectsSortMode.None);
        if (allKids == null || allKids.Length == 0) return;

        NetworkId playerId = default;
        bool hasPlayer = false;
        if (player != null)
        {
            var netObj = player.GetComponent<NetworkObject>() ?? player.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.Id.IsValid)
            {
                playerId = netObj.Id;
                hasPlayer = true;
            }
        }

        for (int i = 0; i < allKids.Length; i++)
        {
            var kid = allKids[i];
            if (kid == null || kid == this) continue;
            if (kid._state == AIState.Panic) continue;

            float dist = Vector3.Distance(transform.position, kid.transform.position);
            if (dist > callKidRadius) continue;

            if (debugLog) Debug.Log($"[{npcName}] Chain gọi {kid.npcName}");

            // Hỗ trợ cả Fusion Online lẫn Test Offline
            if (kid.Object != null && kid.Object.IsValid)
            {
                kid.RpcEnterPanic(playerId, fallbackPos, hasPlayer);
            }
            else
            {
                if (hasPlayer && player != null)
                    kid.EnterPanic(player, true);
                else
                    kid.EnterPanicFromDoor(fallbackPos, true);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcEnterPanic(NetworkId playerId, Vector3 fallbackPos, NetworkBool hasPlayer)
    {
        if (_state == AIState.Panic) return;

        PlayerController target = null;
        if (hasPlayer && Runner != null && Runner.TryFindObject(playerId, out var netObj))
        {
            target = netObj.GetComponent<PlayerController>() ?? netObj.GetComponentInParent<PlayerController>();
        }

        if (target != null)
            EnterPanic(target, true);            // fromChain = true → không chain tiếp
        else
            EnterPanicFromDoor(fallbackPos, true);
    }

    // ═══════════════════════ RETURN ═══════════════════════
    void EnterReturn()
    {
        _fleeFrom = null;
        SetState(AIState.Return);
        if (agent.isOnNavMesh)
        {
            agent.updateRotation = true;
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

    IEnumerator StationaryRoutine()
    {
        if (agent.isOnNavMesh) agent.ResetPath();
        Quaternion baseRot = stationaryAnchor != null ? stationaryAnchor.rotation : _homeRot;

        while (_state == AIState.Idle)
        {
            float[] offsets = { 0f, -lookAngleSpread, lookAngleSpread };
            float chosen = offsets[_stationaryAngleIndex];
            _stationaryAngleIndex = (_stationaryAngleIndex + 1) % offsets.Length;
            Quaternion targetRot = baseRot * Quaternion.Euler(0f, chosen, 0f);

            yield return RotateToRoutine(targetRot, 1f);
            yield return WaitWhileIdle(Random.Range(stationaryRestTimeRange.x, stationaryRestTimeRange.y));
        }
        _idleRoutine = null;
    }

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
            if (wp == null) { _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length; continue; }

            if (agent.isOnNavMesh)
            {
                agent.speed = walkSpeed;
                agent.updateRotation = true;
                agent.SetDestination(wp.position);
            }

            while (agent.pathPending || agent.remainingDistance > 0.5f)
            {
                if (_state != AIState.Idle) yield break;
                yield return null;
            }

            yield return LookLeftRightRoutine(patrolLookAngle);
            yield return WaitWhileIdle(Random.Range(patrolRestTimeRange.x, patrolRestTimeRange.y));

            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
        }
        _idleRoutine = null;
    }

    IEnumerator WanderRoutine()
    {
        Vector3 center = wanderCenterObject != null ? wanderCenterObject.position : _homePos;

        while (_state == AIState.Idle)
        {
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

            if (Random.value < wanderLookChance)
                yield return LookLeftRightRoutine(wanderLookAngle);
            else
                yield return WaitWhileIdle(Random.Range(wanderRestTimeRange.x, wanderRestTimeRange.y));
        }
        _idleRoutine = null;
    }

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

    // ═══════════════════════ NOISE / DOOR PANIC ALERT ═══════════════════════
    /// <summary>
    /// Kiểm tra xem Kid có nghe thấy tiếng động phát ra tại noisePos không
    /// (Tính toán theo thính lực hearingRange và bộ lọc canHear / canHearPlayer của Kid)
    /// </summary>
    public bool CanHearNoise(Vector3 noisePos, bool isFromPlayer = false)
    {
        if (noisePos == Vector3.zero) return false;
        if (!canHear || hearingRange <= 0.001f) return false;
        if (isFromPlayer && !canHearPlayer) return false;
        if (_state == AIState.Panic) return false;

        float dist = Vector3.Distance(transform.position, noisePos);
        return dist <= hearingRange;
    }

    /// <summary>
    /// Báo động cho tất cả Kid có thính giác nghe được tiếng động (Hỗ trợ cả Online RPC và Offline).
    /// </summary>
    public static void AlertKidsToPosition(Vector3 targetPosition, bool isFromPlayer = false)
    {
        var all = FindObjectsByType<KidRunnerNPC>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var kid = all[i];
            if (kid == null) continue;
            if (!kid.CanHearNoise(targetPosition, isFromPlayer)) continue;

            if (kid.Object != null && kid.Object.IsValid)
                kid.RpcEnterPanic(default, targetPosition, false);
            else
                kid.EnterPanicFromDoor(targetPosition);
        }
    }

    // ═══════════════════════ SYNC AUDIO RPCS ═══════════════════════
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayAlertSound()
    {
        if (alertSource != null && alertSound != null && !alertSource.isPlaying)
            alertSource.PlayOneShot(alertSound);
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
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, callAdultRadius);

        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, callKidRadius);

        // Hearing
        if (canHear && hearingRange > 0f)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f); // Cyan
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }

        Gizmos.color = Color.green;
        Vector3 o = transform.position + Vector3.up * eyeHeight;
        Vector3 f = transform.forward;
        Gizmos.DrawRay(o, f * visionRange);
        Gizmos.DrawRay(o, Quaternion.AngleAxis(-raycastAngle, Vector3.up) * f * visionRange);
        Gizmos.DrawRay(o, Quaternion.AngleAxis(raycastAngle, Vector3.up) * f * visionRange);

        if (movementMode == MovementMode.Wander)
        {
            Vector3 c = wanderCenterObject != null ? wanderCenterObject.position : _homePos;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(c, wanderRadius);
        }

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