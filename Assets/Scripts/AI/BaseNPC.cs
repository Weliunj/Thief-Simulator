using System.Collections;
using System.Collections.Generic;
using Fusion;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public enum NPCMovementState
{
    Stationary = 0, // Đứng yên quét 3 góc [-lookAngleSpread, 0, lookAngleSpread]
    Wander = 1,     // Đi dạo quanh Obj tâm, đến nơi 50% nghỉ hoặc ngó trái phải
    Patrol = 2      // Tuần tra qua các điểm Waypoints, đến nơi ngó trái phải
}

[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseNPC : NetworkBehaviour
{
    [Header("👤 NPC Identity")]
    public string npcName = "NPC";
    public NavMeshAgent agent;
    public Animator animator;
    public Light flashlight;

    [Header("🔊 Base Audio")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepAudioClips;
    [Range(0, 1)] public float footstepAudioVolume = 0.5f;
    public AudioSource alertAudioSource;
    public AudioClip alertSound;

    [Header("🏃 Movement Speeds")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float returnSpeed = 2.0f;
    public float stoppingDistanceThreshold = 0.5f;

    [Header("🚶 3 Chế Độ Di Chuyển")]
    public NPCMovementState currentMovementState = NPCMovementState.Stationary;

    [Header("Chế độ 1: Đứng Im (Stationary)")]
    public Transform stationaryAnchor;
    public float lookAngleSpread = 45f;
    public Vector2 stationaryRestTimeRange = new Vector2(2f, 5f);
    public float stationaryTurnSpeed = 3.0f;
    protected float stationaryTimer = 0f;
    protected Quaternion targetStationaryRot;

    [Header("Chế độ 2: Đi Quanh Tâm (Wander)")]
    public Transform wanderCenterObject;
    public float wanderRadius = 15f;
    public Vector2 wanderRestTimeRange = new Vector2(2f, 5f);
    public float wanderLookAngle = 40f;
    protected bool isWanderInspecting = false;

    [Header("Chế độ 3: Tuần Tra (Patrol)")]
    public Transform[] patrolPoints;
    public Vector2 patrolRestTimeRange = new Vector2(2f, 4f);
    public float patrolLookAngle = 40f;
    protected int currentPatrolIndex = 0;
    protected bool isPatrolInspecting = false;

    [Header("👀 Detection (Raycast 5 hướng + CompareTag)")]
    public float visionRange = 15f;
    public float raycastAngle = 30f;
    [Range(0.1f, 1f)] public float crouchVisionMultiplier = 0.5f;

    [Header("🔦 Flashlight Adaptation")]
    public float flashlightNormalRange = 24f;
    public float flashlightTransitionSpeed = 8f;

    [Header("💥 Catch Zone")]
    public float catchDistance = 1.6f;
    public Vector3 catchOffset = new Vector3(0f, 1.0f, 0.4f);

    [Header("🛠️ Debug & Gizmos")]
    public bool showGizmos = true;
    public bool debugOnlyCurrentMode = true;

    [Networked] public float NetworkSpeed { get; set; } = 0f;
    [Networked] public float NetworkMotionSpeed { get; set; } = 1f;

    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public Quaternion initialRotation;
    [HideInInspector] public bool isReturningOrigin = false;

    protected Coroutine inspectionRoutine;
    private int _animIDSpeed;
    private int _animIDMotionSpeed;
    private bool _hasAnimSpeed;
    private bool _hasAnimMotionSpeed;

    public bool IsNetworkSpawned => Object != null && Object.IsValid && Runner != null && Runner.IsRunning;
    public bool IsMasterAuthority => !IsNetworkSpawned || Runner.IsServer || Runner.IsSharedModeMasterClient || Object.HasStateAuthority;

    protected virtual void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (flashlight == null) flashlight = GetComponentInChildren<Light>();

        if (flashlight != null) flashlightNormalRange = flashlight.range;

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

        UpdateOriginAnchor();
        InitBaseAudio();

        if (agent != null)
        {
            agent.speed = walkSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 18f;
            agent.updateRotation = true;
        }

        ResetModeState();
    }

    public void UpdateOriginAnchor()
    {
        if (stationaryAnchor != null)
        {
            initialPosition = stationaryAnchor.position;
            initialRotation = stationaryAnchor.rotation;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
    }

    public virtual void ResetModeState()
    {
        StopInspection();
        stationaryTimer = Random.Range(stationaryRestTimeRange.x, stationaryRestTimeRange.y);
        PickRandomStationaryAngle();
        isWanderInspecting = false;
        isPatrolInspecting = false;

        if (currentMovementState == NPCMovementState.Wander)
        {
            SetRandomWanderDestination();
        }
        else if (currentMovementState == NPCMovementState.Patrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            if (agent != null && agent.isOnNavMesh && patrolPoints[currentPatrolIndex] != null)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }

    protected void StopInspection()
    {
        if (inspectionRoutine != null)
        {
            StopCoroutine(inspectionRoutine);
            inspectionRoutine = null;
        }
    }

    protected virtual void Update()
    {
        UpdateLocalFlashlight();

        if (!IsMasterAuthority)
        {
            UpdateRemoteVisuals();
            return;
        }

        EnsureOnNavMesh();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        ExecuteSubclassAI();
        SyncAnimationNetworkVariables();
    }

    protected abstract void ExecuteSubclassAI();

    #region 🔦 Flashlight Local Adaptation
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

    private void UpdateLocalFlashlight()
    {
        if (flashlight == null) return;
        PlayerController local = GetLocalPlayer();
        bool isCrouched = local != null && IsPlayerCrouching(local);

        float targetRange = isCrouched ? (flashlightNormalRange * 0.5f) : flashlightNormalRange;
        flashlight.range = Mathf.Lerp(flashlight.range, targetRange, Time.deltaTime * flashlightTransitionSpeed);
    }
    #endregion

    #region 👀 Detection (Raycast 5 hướng + CompareTag - Giống Code Cũ)
    public static bool IsPlayerCrouching(PlayerController p)
    {
        if (p == null) return false;
        if (p.Crouching) return true;
        var netSync = p.GetComponent<NetworkPlayerSync>();
        if (netSync != null && netSync.Object != null && netSync.Object.IsValid)
        {
            try { if (netSync.NetworkCrouch || netSync.NetworkIsCrouching) return true; } catch { }
        }
        return false;
    }

    /// <summary>
    /// Phát hiện Player bằng Raycast 5 hướng (center, left, right, up, down) + CompareTag("Player")
    /// Hoạt động giống code cũ (KidOld.txt, AudultOld.txt) - ổn định, không cần cấu hình LayerMask
    /// </summary>
    public PlayerController DetectPlayerMultiRays()
    {
        Vector3 rayStart = transform.position + Vector3.up * 1.2f;
        Vector3 forward = transform.forward;

        Vector3[] directions = new Vector3[]
        {
            forward,
            Quaternion.AngleAxis(-raycastAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(raycastAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(raycastAngle, transform.right) * forward,
            Quaternion.AngleAxis(-raycastAngle, transform.right) * forward
        };

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(rayStart, dir, out RaycastHit hit, visionRange))
            {
                Debug.DrawLine(rayStart, hit.point, Color.red);
                if (hit.collider.CompareTag("Player"))
                {
                    PlayerController pc = hit.collider.GetComponent<PlayerController>()
                        ?? hit.collider.GetComponentInParent<PlayerController>();
                    if (pc == null || !pc.gameObject.activeInHierarchy) continue;
                    if (pc.stats != null && pc.stats.isDied) continue;
                    if (pc.deathHandler != null && pc.deathHandler.isDeadProcessed) continue;

                    // Crouch check: giảm tầm nhìn khi player đang cúi (giống code cũ)
                    bool isCrouched = IsPlayerCrouching(pc);
                    float effectiveRange = isCrouched ? visionRange * crouchVisionMultiplier : visionRange;
                    if (hit.distance > effectiveRange) continue;

                    return pc;
                }
            }
            else
            {
                Debug.DrawLine(rayStart, rayStart + dir * visionRange, Color.green);
            }
        }

        return null;
    }
    #endregion

    #region 🚶 3 Patrol Modes Execution
    protected void HandleStationaryMode()
    {
        if (Vector3.Distance(transform.position, initialPosition) > 0.5f)
        {
            agent.updateRotation = true;
            agent.speed = returnSpeed;
            agent.SetDestination(initialPosition);
            return;
        }

        if (agent.hasPath) agent.ResetPath();
        agent.updateRotation = false;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetStationaryRot, Time.deltaTime * stationaryTurnSpeed);
        stationaryTimer -= Time.deltaTime;

        if (stationaryTimer <= 0f)
        {
            stationaryTimer = Random.Range(stationaryRestTimeRange.x, stationaryRestTimeRange.y);
            PickRandomStationaryAngle();
        }
    }

    private void PickRandomStationaryAngle()
    {
        float[] angles = { 0f, -lookAngleSpread, lookAngleSpread };
        float chosen = angles[Random.Range(0, angles.Length)];
        Quaternion baseRot = stationaryAnchor != null ? stationaryAnchor.rotation : initialRotation;
        targetStationaryRot = baseRot * Quaternion.Euler(0, chosen, 0);
    }

    protected void HandleWanderMode()
    {
        if (isWanderInspecting) return;

        if (HasReachedDestination())
        {
            isWanderInspecting = true;
            inspectionRoutine = StartCoroutine(WanderArrivedRoutine());
        }
        else
        {
            agent.updateRotation = true;
            agent.speed = walkSpeed;
        }
    }

    private IEnumerator WanderArrivedRoutine()
    {
        if (Random.value > 0.5f)
        {
            yield return LookLeftRightSequence(wanderLookAngle);
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(wanderRestTimeRange.x, wanderRestTimeRange.y));
        }

        SetRandomWanderDestination();
        isWanderInspecting = false;
        inspectionRoutine = null;
    }

    protected void SetRandomWanderDestination()
    {
        Vector3 center = wanderCenterObject != null ? wanderCenterObject.position : initialPosition;
        Vector3 rand = center + (Random.insideUnitSphere * wanderRadius);
        rand.y = center.y;

        if (NavMesh.SamplePosition(rand, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            if (agent.isOnNavMesh)
            {
                agent.updateRotation = true;
                agent.speed = walkSpeed;
                agent.SetDestination(hit.position);
            }
        }
    }

    protected void HandlePatrolMode()
    {
        if (isPatrolInspecting) return;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentMovementState = NPCMovementState.Stationary;
            return;
        }

        if (HasReachedDestination())
        {
            isPatrolInspecting = true;
            inspectionRoutine = StartCoroutine(PatrolArrivedRoutine());
        }
        else
        {
            agent.updateRotation = true;
            agent.speed = walkSpeed;
        }
    }

    private IEnumerator PatrolArrivedRoutine()
    {
        yield return LookLeftRightSequence(patrolLookAngle);
        yield return new WaitForSeconds(Random.Range(patrolRestTimeRange.x, patrolRestTimeRange.y));

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        if (patrolPoints[currentPatrolIndex] != null && agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.speed = walkSpeed;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }

        isPatrolInspecting = false;
        inspectionRoutine = null;
    }

    protected IEnumerator LookLeftRightSequence(float angle)
    {
        agent.updateRotation = false;
        Quaternion original = transform.rotation;

        yield return RotateToAngle(original * Quaternion.Euler(0, -angle, 0), 1.0f);
        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));

        yield return RotateToAngle(original * Quaternion.Euler(0, angle, 0), 1.0f);
        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));

        yield return RotateToAngle(original, 0.8f);
        agent.updateRotation = true;
    }

    private IEnumerator RotateToAngle(Quaternion targetRot, float duration)
    {
        Quaternion start = transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(start, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRot;
    }

    protected void HandleReturnToOrigin()
    {
        agent.speed = returnSpeed;
        agent.updateRotation = true;

        if (agent.isOnNavMesh && (!agent.hasPath || Vector3.Distance(agent.destination, initialPosition) > 0.5f))
        {
            agent.SetDestination(initialPosition);
        }

        if (HasReachedDestination())
        {
            agent.updateRotation = false;
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * 5f);

            if (Quaternion.Angle(transform.rotation, initialRotation) < 5f)
            {
                transform.rotation = initialRotation;
                isReturningOrigin = false;
                if (agent.isOnNavMesh) agent.ResetPath();
                ResetModeState();
            }
        }
    }

    public bool HasReachedDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold;
    }

    public void EnsureOnNavMesh()
    {
        if (agent == null || !agent.isActiveAndEnabled || agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            try { agent.Warp(hit.position); } catch { }
        }
    }
    #endregion

    #region 🔊 Audio & Sync
    private void InitBaseAudio()
    {
        if (footstepAudioSource == null) footstepAudioSource = GetComponentInChildren<AudioSource>();
        if (alertAudioSource == null)
        {
            AudioSource[] sources = GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != footstepAudioSource)
                {
                    alertAudioSource = sources[i];
                    break;
                }
            }
            if (alertAudioSource == null) alertAudioSource = footstepAudioSource;
        }

        if (footstepAudioSource != null) footstepAudioSource.spatialBlend = 1.0f;
        if (alertAudioSource != null) alertAudioSource.spatialBlend = 1.0f;
    }

    public void PlayAlertSound()
    {
        AudioSource src = alertAudioSource != null ? alertAudioSource : footstepAudioSource;
        if (src != null && alertSound != null && !src.isPlaying)
        {
            src.PlayOneShot(alertSound);
        }
    }

    // Nhận sự kiện từ Animation Event (hỗ trợ cả có param và không param)
    public void OnFootstep(AnimationEvent animationEvent) { PlayFootstepSound(); }
    public void OnFootstep() { PlayFootstepSound(); }

    public void PlayFootstepSound()
    {
        if (footstepAudioSource == null || footstepAudioClips == null || footstepAudioClips.Length == 0) return;
        var valid = System.Array.FindAll(footstepAudioClips, c => c != null);
        if (valid.Length == 0) return;
        AudioClip clip = valid[Random.Range(0, valid.Length)];
        float vol = footstepAudioVolume > 0.01f ? footstepAudioVolume : 0.5f;
        footstepAudioSource.PlayOneShot(clip, vol);
    }

    protected void SyncAnimationNetworkVariables()
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

    private void UpdateRemoteVisuals()
    {
        if (animator != null)
        {
            float speed = IsNetworkSpawned ? NetworkSpeed : 0f;
            float motionSpeed = IsNetworkSpawned ? NetworkMotionSpeed : 0f;
            if (_hasAnimSpeed) animator.SetFloat(_animIDSpeed, speed);
            if (_hasAnimMotionSpeed) animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }
    }
    #endregion

    #region 🎨 Gizmos
    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 pos = transform.position;
        Vector3 rayStart = pos + Vector3.up * 1.2f;

        // 1. Catch Zone
        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(catchOrigin, catchDistance);

        // 2. Tia Raycast Detection (5 hướng giống code cũ)
        Vector3 forward = transform.forward;
        Vector3[] detDirs = new Vector3[]
        {
            forward,
            Quaternion.AngleAxis(-raycastAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(raycastAngle, Vector3.up) * forward,
            Quaternion.AngleAxis(raycastAngle, transform.right) * forward,
            Quaternion.AngleAxis(-raycastAngle, transform.right) * forward
        };

        Gizmos.color = Color.green;
        foreach (var dir in detDirs)
        {
            Gizmos.DrawRay(rayStart, dir * visionRange);
        }

        // Vòng tròn tầm nhìn khi player cúi
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rayStart, visionRange * crouchVisionMultiplier);

        // 4. Gizmo theo Mode
        if (!debugOnlyCurrentMode || currentMovementState == NPCMovementState.Stationary)
        {
            if (stationaryAnchor != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(stationaryAnchor.position, 0.4f);
                Gizmos.DrawLine(pos, stationaryAnchor.position);
            }
        }

        if (!debugOnlyCurrentMode || currentMovementState == NPCMovementState.Wander)
        {
            Vector3 center = wanderCenterObject != null ? wanderCenterObject.position : initialPosition;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }

        if (!debugOnlyCurrentMode || currentMovementState == NPCMovementState.Patrol)
        {
            Gizmos.color = Color.magenta;
            if (patrolPoints != null)
            {
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                        if (i + 1 < patrolPoints.Length && patrolPoints[i + 1] != null)
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
        }
    }
    #endregion
}