using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC Trẻ Con / Người Báo Tin (Kid / Runner NPC):
/// 1. Khi nhìn thấy Player: Không bắt Player mà sợ hãi chạy hoảng loạn (Panic Run) quanh map trong thời gian cấu hình.
/// 2. Trong lúc chạy hoảng loạn:
///    - Nếu quét trúng NPC Người Lớn (AdultGuardNPC) -> Báo tin khiến NPC Người Lớn lập tức Chase Player đứa trẻ đã nhìn thấy.
///    - Nếu trúng NPC Trẻ Con khác -> Cả hai tiếp tục chạy hoảng loạn.
/// 3. Hết thời gian hoảng loạn -> Ngó trái/phải kiểm tra rồi quay lại tuần tra bình thường.
/// </summary>
public class KidRunnerNPC : BaseNPC
{
    [Header("😱 Panic & Flee Settings")]
    [Tooltip("Thời gian chạy hoảng loạn khi thấy Player (giây)")]
    public float panicDuration = 10.0f;

    [Tooltip("Khoảng cách chuyển điểm chạy tiếp theo khi đang hoảng loạn")]
    public float panicWanderRadius = 18.0f;

    [Tooltip("Bán kính quét va chạm để báo tin cho NPC Người Lớn")]
    public float alertContactRadius = 3.0f;

    [Tooltip("Tần suất quét tìm NPC Người Lớn khi đang chạy (giây)")]
    public float contactScanInterval = 0.3f;

    [Header("🔊 Panic Sounds")]
    public AudioClip panicScreamSound;

    [Header("🎯 Target Information")]
    public PlayerController spottedPlayerTarget;
    public LayerMask npcLayer;

    private float _panicTimer = 0f;
    private float _lastContactScanTime = 0f;
    private bool _hasAlertedAdult = false;

    protected override void Awake()
    {
        base.Awake();
        if (npcLayer == 0) npcLayer = LayerMask.GetMask("Npc", "Default");
    }

    public override void OnPlayerSpotted(PlayerController spottedPlayer)
    {
        if (spottedPlayer == null) return;
        spottedPlayerTarget = spottedPlayer;

        if (currentState != NPCState.PanicRun)
        {
            StartPanicRun();
        }
    }

    /// <summary>
    /// Kích hoạt trạng thái Chạy Hoảng Loạn
    /// </summary>
    public void StartPanicRun()
    {
        SetState(NPCState.PanicRun);
        _panicTimer = panicDuration;
        _hasAlertedAdult = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            PickNextPanicDestination();
        }

        PlayAudio(panicScreamSound != null ? panicScreamSound : alertSound);
    }

    protected override void UpdatePanicRunState()
    {
        _panicTimer -= Time.deltaTime;

        // 1. Nếu hết thời gian hoảng loạn -> Dừng chạy và quay lại tuần tra
        if (_panicTimer <= 0f)
        {
            StartCoroutine(EndPanicRoutine());
            return;
        }

        // 2. Di chuyển liên tục đổi hướng hoảng loạn
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.8f)
            {
                PickNextPanicDestination();
            }
        }

        // 3. Quét tìm NPC Người Lớn để báo tin
        if (Time.time - _lastContactScanTime >= contactScanInterval)
        {
            _lastContactScanTime = Time.time;
            ScanAndAlertAdultNPC();
        }
    }

    /// <summary>
    /// Quét va chạm / phạm vi xung quanh để báo tin cho NPC Người Lớn
    /// </summary>
    private void ScanAndAlertAdultNPC()
    {
        if (spottedPlayerTarget == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, alertContactRadius, npcLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || hits[i].gameObject == gameObject) continue;

            // Kiểm tra nếu là NPC Người Lớn (AdultGuardNPC)
            AdultGuardNPC adult = hits[i].GetComponent<AdultGuardNPC>() ?? hits[i].GetComponentInParent<AdultGuardNPC>();
            if (adult != null)
            {
                adult.StartChasePlayer(spottedPlayerTarget);
                _hasAlertedAdult = true;
                Debug.Log($"<color=orange>[KidRunnerNPC] {gameObject.name} đã báo tin cho Người Lớn ({adult.name}) đi bắt trộm ({spottedPlayerTarget.name})!</color>");
            }

            // Nếu là NPC Trẻ Con khác: Cả hai vẫn tiếp tục chạy hoảng loạn (không làm dừng lại)
        }
    }

    /// <summary>
    /// Chọn điểm đến ngẫu nhiên trên NavMesh để chạy tiếp
    /// </summary>
    private void PickNextPanicDestination()
    {
        Vector2 randCircle = Random.insideUnitCircle * panicWanderRadius;
        Vector3 targetPos = transform.position + new Vector3(randCircle.x, 0f, randCircle.y);

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, panicWanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// Khi hết thời gian hoảng sợ: Đứng thở, ngó nghiêng rồi quay lại tuần tra
    /// </summary>
    private IEnumerator EndPanicRoutine()
    {
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();

        // Ngó trái phải kiểm tra an toàn
        yield return StartCoroutine(LookAroundOnceRoutine(50f, 1.0f));

        spottedPlayerTarget = null;
        _hasAlertedAdult = false;
        SetState(NPCState.Patrol);
    }

    private void OnDrawGizmosSelected()
    {
        // Bán kính quét chạm để báo tin
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, alertContactRadius);
    }
}
