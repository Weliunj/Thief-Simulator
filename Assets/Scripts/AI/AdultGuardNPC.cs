using System.Collections;
using UnityEngine;
using StarterAssets;

public class AdultGuardNPC : BaseNPC
{
    public static bool isChaseMusicPlaying = false;

    [Header("🔊 Theme Audio")]
    public AudioSource themeAudioSource;
    public AudioClip chaseThemeSound;

    [Header("🎯 Adult Chase Settings")]
    public Vector2 chaseDurationRange = new Vector2(5f, 10f);
    public float callAdultRadius = 10f;
    public LayerMask adultNpcLayer;
    public float maxChaseRadius = 35f;
    public float catchCooldown = 2.0f;
    private float _lastCatchTime = -10f;

    [Header("🚪 Door Investigation")]
    public Vector2 doorCooldownRange = new Vector2(3f, 6f);
    public float doorInvestigateRadius = 2.5f;

    [HideInInspector] public bool targetDetected = false;
    private float _chaseTimer = 0f;
    private PlayerController _lockedTargetPlayer;
    private bool _isInvestigatingDoor = false;
    private Vector3 _doorInvestigateTarget;
    private bool _hasPlayedAlertSound = false; // Cờ chặn âm thanh kêu liên tục

    protected override void Awake()
    {
        base.Awake();
        if (themeAudioSource == null)
        {
            Transform t = transform.Find("AudioManager/Theme") ?? transform.Find("Theme");
            if (t != null) themeAudioSource = t.GetComponent<AudioSource>();
        }
        if (themeAudioSource != null)
        {
            themeAudioSource.playOnAwake = false;
            themeAudioSource.loop = true;
            if (chaseThemeSound != null) themeAudioSource.clip = chaseThemeSound;
        }
    }

    protected override void ExecuteSubclassAI()
    {
        PlayerController spotted = DetectPlayerMultiRays();

        // 1. Đang đuổi: cập nhật mục tiêu nếu vẫn thấy, tiếp tục đuổi
        if (targetDetected)
        {
            if (spotted != null)
            {
                // Vẫn thấy Player -> giữ mục tiêu và duy trì timer
                _lockedTargetPlayer = spotted;
                _chaseTimer = Mathf.Max(_chaseTimer, 3.0f);
            }
            ExecuteChase();
            return;
        }

        // 2. Lần đầu nhìn thấy Player -> bắt đầu đuổi
        if (spotted != null)
        {
            StartChase(spotted);
            return;
        }

        // 3. Không thấy Player và không đuổi -> các nhiệm vụ bình thường
        if (_isInvestigatingDoor)
        {
            HandleInvestigateDoor();
            return;
        }

        if (isReturningOrigin)
        {
            HandleReturnToOrigin();
            return;
        }

        switch (currentMovementState)
        {
            case NPCMovementState.Stationary: HandleStationaryMode(); break;
            case NPCMovementState.Wander: HandleWanderMode(); break;
            case NPCMovementState.Patrol: HandlePatrolMode(); break;
        }
    }

    public void StartChase(PlayerController target)
    {
        if (target == null || targetDetected) return; // Tránh gọi lại khi đã đuổi rồi
        StopInspection();

        _lockedTargetPlayer = target;
        _chaseTimer = Random.Range(chaseDurationRange.x, chaseDurationRange.y);
        targetDetected = true;
        isReturningOrigin = false;
        _isInvestigatingDoor = false;
        _hasPlayedAlertSound = false; // reset để phát ngay

        // Phát 1 lần duy nhất
        PlayAlertSound();
        _hasPlayedAlertSound = true;

        HandleChaseMusic(true);

        if (agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.stoppingDistance = 0f;
            agent.SetDestination(target.transform.position);
        }

        CallNearbyAdults(target);
    }

    private void CallNearbyAdults(PlayerController target)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, callAdultRadius, adultNpcLayer);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            var adult = col.GetComponent<AdultGuardNPC>() ?? col.GetComponentInParent<AdultGuardNPC>();
            if (adult != null && !adult.targetDetected)
            {
                adult.StartChase(target);
            }
        }
    }

    private void ExecuteChase()
    {
        // Kiểm tra mục tiêu hợp lệ
        if (_lockedTargetPlayer == null || !_lockedTargetPlayer.gameObject.activeInHierarchy ||
            (_lockedTargetPlayer.stats != null && _lockedTargetPlayer.stats.isDied))
        {
            StopChaseAndReturn();
            return;
        }

        // Đếm ngược thời gian Chase
        _chaseTimer -= Time.deltaTime;
        float distFromStart = Vector3.Distance(transform.position, initialPosition);

        if (_chaseTimer <= 0f || distFromStart > maxChaseRadius)
        {
            StopChaseAndReturn();
            return;
        }

        // Đuổi theo Player liên tục - Catch xử lý qua OnTriggerEnter
        if (agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            agent.stoppingDistance = 0f; // Không để NavMesh tự dừng trước khi chạm player
            agent.updateRotation = true;
            agent.SetDestination(_lockedTargetPlayer.transform.position);
        }
    }

    // ⭐ Cách catch chính: OnTriggerEnter - đơn giản và đáng tin cậy như code cũ
    private void OnTriggerEnter(Collider other)
    {
        if (!targetDetected || _lockedTargetPlayer == null) return;
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>()
            ?? other.GetComponentInParent<PlayerController>();
        if (pc != null && pc == _lockedTargetPlayer)
        {
            ExecuteCatchPlayer(pc);
        }
    }

    private void StopChaseAndReturn()
    {
        targetDetected = false;
        _lockedTargetPlayer = null;
        _hasPlayedAlertSound = false;
        isReturningOrigin = true;
        HandleChaseMusic(false);

        if (agent.isOnNavMesh)
        {
            agent.stoppingDistance = stoppingDistanceThreshold; // Khôi phục
            agent.speed = returnSpeed;
            agent.updateRotation = true;
            agent.SetDestination(initialPosition);
        }
    }

    private void ExecuteCatchPlayer(PlayerController target)
    {
        if (target == null || Time.time - _lastCatchTime < catchCooldown) return;
        _lastCatchTime = Time.time;

        var deathHandler = target.deathHandler ?? target.GetComponent<PlayerDeathHandler>() ?? target.GetComponentInParent<PlayerDeathHandler>();
        if (deathHandler != null)
        {
            deathHandler.TriggerDeath(npcName);
        }
        else
        {
            if (target.stats != null) target.stats.isDied = true;
            if (target.inventory != null) target.inventory.DropAllItemsOnDeath();
            GameStatusHUD.Show($"Player was caught by {npcName}!");
        }
        StopChaseAndReturn();
    }

    // Nhận cảnh báo từ Cửa bị bẻ khóa hỏng
    public static void AlertDoorTampered(Vector3 doorPosition, float radius)
    {
        AdultGuardNPC[] all = FindObjectsByType<AdultGuardNPC>(FindObjectsSortMode.None);
        foreach (var adult in all)
        {
            if (adult == null || !adult.IsMasterAuthority || adult.targetDetected) continue;
            if (Vector3.Distance(adult.transform.position, doorPosition) <= radius)
            {
                adult.TriggerInvestigateDoor(doorPosition);
            }
        }
    }

    public void ReceiveKidAlert(Vector3 playerLastKnownPos)
    {
        if (targetDetected) return; // Đang đuổi thì không đi kiểm tra
        TriggerInvestigateDoor(playerLastKnownPos);
    }

    public void TriggerInvestigateDoor(Vector3 doorPos)
    {
        if (targetDetected) return;

        PlayAlertSound();
        _doorInvestigateTarget = doorPos;
        _isInvestigatingDoor = true;
        isReturningOrigin = false;
        StopInspection();

        if (agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.speed = runSpeed;
            agent.SetDestination(doorPos);
        }
    }

    private void HandleInvestigateDoor()
    {
        if (HasReachedDestination())
        {
            _isInvestigatingDoor = false;
            inspectionRoutine = StartCoroutine(DoorInvestigationRoutine());
        }
    }

    private IEnumerator DoorInvestigationRoutine()
    {
        if (agent.isOnNavMesh) agent.ResetPath();

        Vector2 circleOffset = Random.insideUnitCircle * doorInvestigateRadius;
        Vector3 nearbyDoorPos = _doorInvestigateTarget + new Vector3(circleOffset.x, 0, circleOffset.y);
        if (UnityEngine.AI.NavMesh.SamplePosition(nearbyDoorPos, out UnityEngine.AI.NavMeshHit hit, doorInvestigateRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            while (agent.pathPending || agent.remainingDistance > 0.3f) yield return null;
        }

        yield return LookLeftRightSequence(45f);
        yield return new WaitForSeconds(Random.Range(doorCooldownRange.x, doorCooldownRange.y));

        isReturningOrigin = true;
        inspectionRoutine = null;
    }

    public void HandleChaseMusic(bool play)
    {
        if (themeAudioSource == null) return;
        if (play)
        {
            if (!isChaseMusicPlaying)
            {
                if (chaseThemeSound != null && themeAudioSource.clip != chaseThemeSound)
                    themeAudioSource.clip = chaseThemeSound;
                themeAudioSource.Play();
                isChaseMusicPlaying = true;
            }
        }
        else
        {
            themeAudioSource.Stop();
            isChaseMusicPlaying = false;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, callAdultRadius);
    }
}