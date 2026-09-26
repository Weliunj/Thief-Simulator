using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC Người Lớn / Bảo Vệ (Adult / Guard NPC):
/// 1. Khi nhìn thấy Player hoặc nhận tin báo: Chuyển sang Chase truy đuổi.
/// 2. Đổi mục tiêu thông minh: Ưu tiên đổi sang Player gần hơn nếu phát hiện trên đường đuổi.
/// 3. Kéo đồng đội cùng đuổi (Proximity Chain Chase): Quét xung quanh, lôi kéo các Adult NPC khác cùng tham gia truy đuổi.
/// 4. Bắt Player khi tiếp cận gần (Catch Player -> Die + Scatter Drop Items).
/// 5. Khi mất dấu: Đi tới vị trí nhìn thấy lần cuối, ngó trái/phải kiểm tra rồi quay về tuần tra.
/// </summary>
public class AdultGuardNPC : BaseNPC
{
    [Header("🎯 Chase & Target Settings")]
    public PlayerController currentTarget;
    [Tooltip("Khoảng cách gần hơn tối thiểu để đổi mục tiêu sang Player khác")]
    public float targetSwitchDistanceThreshold = 2.5f;

    [Tooltip("Thời gian mất dấu Player trước khi chuyển sang tìm kiếm tại vị trí cuối cùng")]
    public float lostTargetDuration = 3.5f;

    [Tooltip("Khoảng cách bắt Player (bán kính)")]
    public float catchDistance = 1.5f;

    [Tooltip("Độ lệch tâm điểm bắt Player (X, Y, Z) so với vị trí NPC (VD: nâng lên ngang ngực hoặc đẩy ra trước)")]
    public Vector3 catchOffset = new Vector3(0f, 1.0f, 0.4f);

    [Header("📢 Proximity Chain Chase (Kéo Đồng Đội)")]
    [Tooltip("Bán kính quét để kéo các NPC Người lớn khác cùng đuổi")]
    public float alertOtherNpcRadius = 8.0f;
    [Tooltip("Tần suất quét tìm đồng đội (giây)")]
    public float alertScanInterval = 1.0f;
    public LayerMask npcLayer;

    [Header("💥 Catch Trigger Reference")]
    public NPCCatchPlayerTrigger catchTrigger;

    private float _lostTargetTimer = 0f;
    private float _lastAlertScanTime = 0f;
    private Vector3 _lastKnownPosition;
    private bool _isInvestigatingLostTarget = false;

    protected override void Awake()
    {
        base.Awake();
        if (catchTrigger == null) catchTrigger = GetComponent<NPCCatchPlayerTrigger>() ?? GetComponentInChildren<NPCCatchPlayerTrigger>();
        if (npcLayer == 0) npcLayer = LayerMask.GetMask("Npc", "Default");
    }

    public override void OnPlayerSpotted(PlayerController spottedPlayer)
    {
        if (spottedPlayer == null) return;

        // Nếu đang không Chase hoặc phát hiện mục tiêu mới
        if (currentState != NPCState.Chase)
        {
            StartChasePlayer(spottedPlayer);
        }
        else
        {
            // Đang Chase: Kiểm tra đổi mục tiêu nếu spottedPlayer gần hơn
            if (senses != null && senses.TryGetCloserTarget(currentTarget, targetSwitchDistanceThreshold, out PlayerController closerPlayer))
            {
                currentTarget = closerPlayer;
                _lastKnownPosition = closerPlayer.transform.position;
                _lostTargetTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Kích hoạt trạng thái Chase nhắm vào mục tiêu chỉ định
    /// </summary>
    public void StartChasePlayer(PlayerController target)
    {
        if (target == null) return;
        if (target.stats != null && target.stats.isDied) return;

        currentTarget = target;
        _lastKnownPosition = target.transform.position;
        _lostTargetTimer = lostTargetDuration; // Bắt đầu với timer đầy (ví dụ 5s)
        _isInvestigatingLostTarget = false;

        SetState(NPCState.Chase);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            agent.SetDestination(target.transform.position);
        }

        PlayAudio(alertSound);
    }

    protected override void UpdateChaseState()
    {
        // 1. Nếu không có mục tiêu hoặc mục tiêu đã chết
        if (currentTarget == null || (currentTarget.stats != null && currentTarget.stats.isDied))
        {
            // Thử tìm Player còn sống khác trong tầm nhìn
            if (senses != null && senses.visiblePlayers.Count > 0)
            {
                StartChasePlayer(senses.visiblePlayers[0]);
                return;
            }

            // Nếu không còn ai -> Quay về tuần tra (về đúng điểm Stand nếu ở Stationary mode)
            currentTarget = null;
            SetState(NPCState.Patrol);
            return;
        }

        // 2. Kiểm tra bắt Player khi lại gần (tính từ tâm catchOffset)
        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        Vector3 playerCenter = (currentTarget.CinemachineCameraTarget != null) ? currentTarget.CinemachineCameraTarget.transform.position : currentTarget.transform.position + Vector3.up * 1.0f;
        float distToTarget = Vector3.Distance(catchOrigin, playerCenter);
        if (distToTarget <= catchDistance)
        {
            CatchTargetPlayer(currentTarget);
            return;
        }

        // 3. Kiểm tra Raycast chiếu thẳng tới mục tiêu (không bị cản bởi tường/cửa/chướng ngại vật)
        bool isTargetVisible = false;
        if (senses != null)
        {
            Vector3 myEye = senses.EyePosition;
            Vector3 targetPt = senses.GetPlayerTargetPoint(currentTarget);
            float dist = Vector3.Distance(myEye, targetPt);

            // Raycast chiếu thẳng tới player: nếu không bị tường/vật thể che thì vẫn nhìn thấy
            if (senses.HasLineOfSight(myEye, targetPt, dist))
            {
                isTargetVisible = true;
                _lastKnownPosition = currentTarget.transform.position;
                _lostTargetTimer = lostTargetDuration; // Giữ timer luôn đầy (ví dụ 5s) khi đường nhìn không bị che

                // Kiểm tra đổi sang mục tiêu khác gần hơn nếu có
                if (senses.TryGetCloserTarget(currentTarget, targetSwitchDistanceThreshold, out PlayerController closerPlayer))
                {
                    currentTarget = closerPlayer;
                    _lastKnownPosition = closerPlayer.transform.position;
                }
            }
        }

        // 4. Di chuyển đuổi theo mục tiêu
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            if (isTargetVisible)
            {
                // Đuổi theo liên tục vị trí thực tế của Player
                agent.SetDestination(currentTarget.transform.position);
            }
            else
            {
                // Raycast bị che khuất (tường/vật cản chặn đường nhìn) -> Đếm ngược timer về 0 và chạy tới điểm nhìn thấy cuối
                _lostTargetTimer -= Time.deltaTime;
                agent.SetDestination(_lastKnownPosition);

                if (_lostTargetTimer <= 0f)
                {
                    // Hết thời gian mất dấu -> Bỏ player, quay về tuần tra
                    currentTarget = null;
                    SetState(NPCState.Patrol);
                    return;
                }
            }
        }

        // 5. Quét kéo các NPC Người Lớn khác xung quanh cùng tham gia đuổi bắt
        if (Time.time - _lastAlertScanTime >= alertScanInterval)
        {
            _lastAlertScanTime = Time.time;
            AlertNearbyAdultNPCs();
        }
    }

    /// <summary>
    /// Kéo các NPC Người Lớn khác trong bán kính alertOtherNpcRadius cùng Chase
    /// </summary>
    private void AlertNearbyAdultNPCs()
    {
        if (currentTarget == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, alertOtherNpcRadius, npcLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || hits[i].gameObject == gameObject) continue;

            AdultGuardNPC otherAdult = hits[i].GetComponent<AdultGuardNPC>() ?? hits[i].GetComponentInParent<AdultGuardNPC>();
            if (otherAdult != null && otherAdult != this && otherAdult.currentState != NPCState.Chase && otherAdult.currentState != NPCState.Stunned)
            {
                otherAdult.StartChasePlayer(currentTarget);
            }
        }
    }

    /// <summary>
    /// Bắt Player
    /// </summary>
    private void CatchTargetPlayer(PlayerController target)
    {
        if (target == null) return;

        if (catchTrigger != null)
        {
            catchTrigger.TryCatchPlayer(target.gameObject);
        }
        else
        {
            // Fallback nếu không có catchTrigger
            if (target.deathHandler != null)
            {
                target.deathHandler.TriggerDeath(npcName);
            }
        }

        PlayAudio(catchSound);
        currentTarget = null;
        SetState(NPCState.Patrol);
    }

    /// <summary>
    /// Khi mất dấu: Đi tới vị trí cuối cùng, ngó trái phải rồi quay lại tuần tra
    /// </summary>
    private IEnumerator InvestigateLostTargetRoutine()
    {
        _isInvestigatingLostTarget = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = investigateSpeed;
            agent.SetDestination(_lastKnownPosition);
        }

        // Chờ đến gần vị trí cuối cùng
        float timeout = 4.0f;
        while (timeout > 0f)
        {
            timeout -= Time.deltaTime;
            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.4f)
            {
                break;
            }
            yield return null;
        }

        if (agent != null && agent.isOnNavMesh) agent.ResetPath();

        // Ngó trái phải tìm kiếm
        yield return StartCoroutine(LookAroundOnceRoutine(60f, 1.2f));

        _isInvestigatingLostTarget = false;
        currentTarget = null;
        SetState(NPCState.Patrol);
    }

    private void OnDrawGizmosSelected()
    {
        // Bán kính kéo đồng đội
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, alertOtherNpcRadius);

        // Bán kính bắt trộm (tính từ tâm catchOffset)
        Gizmos.color = Color.red;
        Vector3 catchOrigin = transform.TransformPoint(catchOffset);
        Gizmos.DrawWireSphere(catchOrigin, catchDistance);
    }
}
