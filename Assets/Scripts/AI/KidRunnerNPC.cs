using System.Collections;
using Fusion;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class KidRunnerNPC : BaseNPC
{
    [Header("📢 Kid Panic & Anti-Loop Chain Call")]
    public Vector2 panicDurationRange = new Vector2(5f, 9f);
    public float kidCallRadius = 16f;
    public float callCooldown = 6f;
    public LayerMask adultNpcLayer;
    public LayerMask kidNpcLayer;

    // Biến mạng đồng bộ trạng thái gọi
    [Networked] public NetworkBool NetworkIsCalling { get; set; } = false;

    // Biến local dùng khi chơi Offline hoặc trước khi Spawned()
    private bool _localIsCalling = false;

    // Thuộc tính an toàn: Tự kiểm tra IsNetworkSpawned để tránh lỗi InvalidOperationException
    public bool SafeIsCalling
    {
        get => IsNetworkSpawned ? (bool)NetworkIsCalling : _localIsCalling;
        set
        {
            if (IsNetworkSpawned) NetworkIsCalling = value;
            _localIsCalling = value;
        }
    }

    private Coroutine _panicRoutine;
    private Coroutine _cooldownRoutine;
    private Vector3 _lastKnownPlayerPos;
    private PlayerController _spottedPlayer;
    // _hasPlayedAlertSound được reset khi PanicRoutine kết thúc (không reset mọi frame)
    private bool _hasPlayedAlertSound = false;

    protected override void ExecuteSubclassAI()
    {
        // 1. Đang hoảng loạn -> gọi Adult liên tục
        if (_panicRoutine != null)
        {
            CallNearbyAdultsContinuous();
            return;
        }

        // 2. Quét 9 hướng tìm Player
        PlayerController spotted = DetectPlayerMultiRays();
        if (spotted != null)
        {
            _spottedPlayer = spotted;
            _lastKnownPlayerPos = spotted.transform.position;
            TriggerPanicRun(true);
            return;
        }

        // 3. Bình thường
        switch (currentMovementState)
        {
            case NPCMovementState.Stationary: HandleStationaryMode(); break;
            case NPCMovementState.Wander: HandleWanderMode(); break;
            case NPCMovementState.Patrol: HandlePatrolMode(); break;
        }
    }

    public void TriggerPanicRun(bool initiateCallChain)
    {
        StopInspection();
        if (_panicRoutine != null) StopCoroutine(_panicRoutine);
        _panicRoutine = StartCoroutine(PanicRoutine(initiateCallChain));
    }

    private IEnumerator PanicRoutine(bool initiateCallChain)
    {
        // Phát 1 lần duy nhất khi bắt đầu hoảng loạn
        if (!_hasPlayedAlertSound)
        {
            PlayAlertSound();
            _hasPlayedAlertSound = true;
        }

        float duration = Random.Range(panicDurationRange.x, panicDurationRange.y);
        float elapsed = 0f;

        agent.speed = runSpeed;
        agent.updateRotation = true;
        PickRandomNavMeshPoint();

        if (initiateCallChain && !SafeIsCalling)
        {
            ExecuteCallChain();
        }

        while (elapsed < duration)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.5f || !agent.hasPath)
            {
                PickRandomNavMeshPoint();
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.speed = walkSpeed;
        _hasPlayedAlertSound = false; // reset sau khi hết panic để lần sau gặp Player lại kêu được
        _spottedPlayer = null; // Clear khi hết panic
        _panicRoutine = null;
    }

    // Cơ chế quét OverlapSphere liên tục gọi Adult giống như file Kid.txt cũ
    // Nếu có Player → Adult sẽ StartChase trực tiếp (giống code cũ set targetDetected = true)
    // Nếu chỉ có vị trí (door broken) → Adult sẽ investigate
    private void CallNearbyAdultsContinuous()
    {
        Collider[] adultHits = Physics.OverlapSphere(transform.position, kidCallRadius, adultNpcLayer);
        foreach (var col in adultHits)
        {
            var adult = col.GetComponent<AdultGuardNPC>() ?? col.GetComponentInParent<AdultGuardNPC>();
            if (adult != null && !adult.targetDetected)
            {
                if (_spottedPlayer != null && _spottedPlayer.gameObject.activeInHierarchy)
                    adult.StartChase(_spottedPlayer);
                else
                    adult.ReceiveKidAlert(_lastKnownPlayerPos);
            }
        }
    }

    private void ExecuteCallChain()
    {
        SafeIsCalling = true;

        // Lan truyền sang Kid khác (Tránh lặp vô hạn)
        Collider[] kidHits = Physics.OverlapSphere(transform.position, kidCallRadius, kidNpcLayer);
        foreach (var col in kidHits)
        {
            if (col.gameObject == gameObject) continue;
            var otherKid = col.GetComponent<KidRunnerNPC>() ?? col.GetComponentInParent<KidRunnerNPC>();
            if (otherKid != null && !otherKid.SafeIsCalling)
            {
                otherKid._spottedPlayer = this._spottedPlayer;
                otherKid._lastKnownPlayerPos = this._lastKnownPlayerPos;
                otherKid.TriggerPanicRun(true);
            }
        }

        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = StartCoroutine(CallCooldownRoutine());
    }

    private IEnumerator CallCooldownRoutine()
    {
        yield return new WaitForSeconds(callCooldown);
        SafeIsCalling = false;
        _cooldownRoutine = null;
    }

    // Khi cửa bị phá/fail minigame
    public void OnDoorBrokenPanic(Vector3 doorPosition)
    {
        if (!IsMasterAuthority) return;
        _spottedPlayer = null; // Không có player spotted, chỉ có vị trí cửa
        _lastKnownPlayerPos = doorPosition;
        TriggerPanicRun(true);
    }

    private void PickRandomNavMeshPoint()
    {
        Vector3 rand = Random.insideUnitSphere * 14f + transform.position;
        if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 14f, NavMesh.AllAreas))
        {
            if (agent.isOnNavMesh) agent.SetDestination(hit.position);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, kidCallRadius);
    }
}