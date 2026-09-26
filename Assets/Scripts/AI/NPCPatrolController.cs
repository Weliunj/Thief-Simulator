using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 3 Chế độ Tuần Tra cho NPC
/// </summary>
public enum NPCPatrolMode
{
    Stationary = 0,    // Mode 1: Đứng im + Quay random 3 góc (Chéo trái, Giữa, Chéo phải)
    Wander = 1,        // Mode 2: Di chuyển tự do (Full map hoặc giới hạn trong Box/Zone)
    WaypointPatrol = 2 // Mode 3: Đi tuần tra qua lại các điểm cố định
}

/// <summary>
/// Quản lý cơ chế Tuần Tra (Patrol Controller) tích hợp 3 chế độ:
/// - Mode 1: Đứng im tại chỗ, quay random 3 góc (Chéo trái - Giữa - Chéo phải), cooldown ngẫu nhiên.
/// - Mode 2: Đi tự do full map hoặc giới hạn trong Box/Zone. Khi đến đích có cơ hội ngó trái/phải rồi đi tiếp hoặc nghỉ tại chỗ.
/// - Mode 3: Đi qua lại các Waypoints, khi đến điểm có cơ hội ngó trái/phải (tỉ lệ thấp hơn Mode 1 & 2).
/// </summary>
[RequireComponent(typeof(BaseNPC))]
public class NPCPatrolController : MonoBehaviour
{
    [Header("🎯 Chế Độ Tuần Tra (Patrol Mode)")]
    public NPCPatrolMode patrolMode = NPCPatrolMode.Wander;

    [Header("📌 Mode 1: Stationary (Đứng im quay 3 góc)")]
    [Tooltip("Góc chéo bên trái so với hướng gốc (độ âm)")]
    public float stationaryLeftAngle = -50f;
    [Tooltip("Góc chéo bên phải so với hướng gốc (độ dương)")]
    public float stationaryRightAngle = 50f;
    [Tooltip("Thời gian chờ tối thiểu tại 1 góc quay")]
    public float stationaryCooldownMin = 2.0f;
    [Tooltip("Thời gian chờ tối đa tại 1 góc quay")]
    public float stationaryCooldownMax = 4.5f;

    [Header("🗺️ Mode 2: Wander (Di chuyển tự do)")]
    [Tooltip("Collider Box giới hạn khu vực đi lại (ví dụ chỉ đi trong nhà). Nếu để trống sẽ đi tự do quanh điểm spawn.")]
    public BoxCollider wanderZoneBoundary;
    [Tooltip("Bán kính đi tự do nếu không dùng BoxCollider")]
    public float wanderRadius = 15f;
    [Tooltip("Tỉ lệ % khi đến đích sẽ ngó nhìn trái/phải (0.0 - 1.0)")]
    [Range(0f, 1f)] public float wanderLookAroundChance = 0.45f;
    [Tooltip("Tỉ lệ % khi đến đích sẽ đứng nghỉ tại chỗ không quay mặt (0.0 - 1.0)")]
    [Range(0f, 1f)] public float wanderRestChance = 0.30f;
    [Tooltip("Thời gian nghỉ tại điểm đích")]
    public float wanderRestDurationMin = 2.0f;
    public float wanderRestDurationMax = 4.0f;

    [Header("🚩 Mode 3: Waypoint Patrol (Đi qua lại các điểm)")]
    public List<Transform> waypoints = new List<Transform>();
    [Tooltip("Tỉ lệ % khi đến mỗi Waypoint sẽ ngó nhìn trái/phải (thấp hơn Wander, mặc định 0.25)")]
    [Range(0f, 1f)] public float waypointLookChance = 0.25f;
    [Tooltip("Thời gian dừng nghỉ tại mỗi Waypoint")]
    public float waypointWaitDuration = 2.0f;

    private BaseNPC _baseNPC;
    private NavMeshAgent _agent;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    private int _currentWaypointIndex = 0;
    private bool _isPatrolCoroutineRunning = false;
    private Coroutine _activePatrolCoroutine;

    private void Awake()
    {
        _baseNPC = GetComponent<BaseNPC>();
        _agent = GetComponent<NavMeshAgent>();
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    /// <summary>
    /// Được gọi từ BaseNPC khi ở trạng thái NPCState.Patrol
    /// </summary>
    public void ExecutePatrolLogic()
    {
        if (!_baseNPC.IsMasterAuthority) return;
        if (_isPatrolCoroutineRunning) return;

        switch (patrolMode)
        {
            case NPCPatrolMode.Stationary:
                _activePatrolCoroutine = StartCoroutine(StationaryPatrolRoutine());
                break;
            case NPCPatrolMode.Wander:
                _activePatrolCoroutine = StartCoroutine(WanderPatrolRoutine());
                break;
            case NPCPatrolMode.WaypointPatrol:
                _activePatrolCoroutine = StartCoroutine(WaypointPatrolRoutine());
                break;
        }
    }

    /// <summary>
    /// Tạm dừng tuần tra khi NPC chuyển sang trạng thái khác (Chase, Investigate...)
    /// </summary>
    public void StopPatrol()
    {
        if (_activePatrolCoroutine != null)
        {
            StopCoroutine(_activePatrolCoroutine);
            _activePatrolCoroutine = null;
        }
        _isPatrolCoroutineRunning = false;
    }

    #region 📌 Mode 1: Stationary Routine

    private IEnumerator StationaryPatrolRoutine()
    {
        _isPatrolCoroutineRunning = true;

        // 1. Nếu NPC đang đứng xa vị trí đứng cố định ban đầu (sau khi rượt đuổi / kiểm tra tiếng động) -> Đi bộ về đúng chỗ cũ
        if (_agent != null && _agent.isOnNavMesh && Vector3.Distance(transform.position, _spawnPosition) > 0.4f)
        {
            _agent.speed = _baseNPC.walkSpeed;
            _agent.SetDestination(_spawnPosition);

            while (_baseNPC.currentState == NPCState.Patrol && (_agent.pathPending || _agent.remainingDistance > 0.35f))
            {
                yield return null;
            }

            if (_agent.isOnNavMesh) _agent.ResetPath();

            // Xoay về lại hướng ban đầu
            float returnTurnTime = 0f;
            while (returnTurnTime < 1.0f && _baseNPC.currentState == NPCState.Patrol)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _spawnRotation, Time.deltaTime * _baseNPC.rotationSpeed * 2f);
                returnTurnTime += Time.deltaTime;
                yield return null;
            }
        }
        else if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }

        // 2. Vòng lặp đứng im và quay ngẫu nhiên 3 góc
        int lastAngleIndex = 1;

        while (_baseNPC.currentState == NPCState.Patrol)
        {
            // Chọn ngẫu nhiên 1 trong 3 góc (khác góc vừa nhìn)
            int targetAngleIndex = Random.Range(0, 3);
            if (targetAngleIndex == lastAngleIndex)
            {
                targetAngleIndex = (targetAngleIndex + 1) % 3;
            }
            lastAngleIndex = targetAngleIndex;

            float angleOffset = 0f;
            if (targetAngleIndex == 0) angleOffset = stationaryLeftAngle;
            else if (targetAngleIndex == 2) angleOffset = stationaryRightAngle;

            Quaternion targetRotation = _spawnRotation * Quaternion.Euler(0f, angleOffset, 0f);

            // Xoay mượt mà về góc đã chọn
            float turnTime = 0f;
            while (turnTime < 1.0f && _baseNPC.currentState == NPCState.Patrol)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _baseNPC.rotationSpeed);
                turnTime += Time.deltaTime;
                yield return null;
            }

            // Cooldown ngẫu nhiên tại góc nhìn
            float waitTime = Random.Range(stationaryCooldownMin, stationaryCooldownMax);
            yield return new WaitForSeconds(waitTime);
        }

        _isPatrolCoroutineRunning = false;
    }

    #endregion

    #region 🗺️ Mode 2: Wander Routine

    private IEnumerator WanderPatrolRoutine()
    {
        _isPatrolCoroutineRunning = true;

        while (_baseNPC.currentState == NPCState.Patrol)
        {
            Vector3 destination;
            if (GetRandomDestination(out destination))
            {
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.speed = _baseNPC.walkSpeed;
                    _agent.SetDestination(destination);
                }

                // Chờ đi tới điểm đích
                while (_baseNPC.currentState == NPCState.Patrol)
                {
                    if (_agent != null && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
                    {
                        break;
                    }
                    yield return null;
                }

                if (_baseNPC.currentState != NPCState.Patrol) break;

                // Khi đến đích -> Cơ hội ngó nhìn trái/phải hoặc nghỉ tại chỗ
                float roll = Random.value;
                if (roll < wanderLookAroundChance)
                {
                    // Ngó trái phải 1 lần rồi đi tiếp
                    yield return StartCoroutine(_baseNPC.LookAroundOnceRoutine(50f, 1.2f));
                }
                else if (roll < wanderLookAroundChance + wanderRestChance)
                {
                    // Đứng nghỉ tại chỗ không quay mặt
                    float restDuration = Random.Range(wanderRestDurationMin, wanderRestDurationMax);
                    yield return new WaitForSeconds(restDuration);
                }
                else
                {
                    // Đi tiếp ngay (nghỉ ngắn 0.5s)
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }

        _isPatrolCoroutineRunning = false;
    }

    private bool GetRandomDestination(out Vector3 result)
    {
        Vector3 targetCenter = _spawnPosition;

        if (wanderZoneBoundary != null)
        {
            // Lấy ngẫu nhiên 1 điểm trong BoxCollider
            Bounds bounds = wanderZoneBoundary.bounds;
            Vector3 randomInside = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (NavMesh.SamplePosition(randomInside, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        else
        {
            // Lấy ngẫu nhiên trong bán kính wanderRadius
            Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPos = targetCenter + new Vector3(randCircle.x, 0f, randCircle.y);

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = _spawnPosition;
        return false;
    }

    #endregion

    #region 🚩 Mode 3: Waypoint Patrol Routine

    private IEnumerator WaypointPatrolRoutine()
    {
        _isPatrolCoroutineRunning = true;

        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning($"[NPCPatrolController] {gameObject.name} đang ở chế độ Waypoint nhưng danh sách waypoints rỗng!");
            yield return new WaitForSeconds(2.0f);
            _isPatrolCoroutineRunning = false;
            yield break;
        }

        while (_baseNPC.currentState == NPCState.Patrol)
        {
            Transform targetWp = waypoints[_currentWaypointIndex];
            if (targetWp != null && _agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = _baseNPC.walkSpeed;
                _agent.SetDestination(targetWp.position);
            }

            // Chờ di chuyển tới Waypoint
            while (_baseNPC.currentState == NPCState.Patrol)
            {
                if (_agent != null && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
                {
                    break;
                }
                yield return null;
            }

            if (_baseNPC.currentState != NPCState.Patrol) break;

            // Khi đến 1 điểm: Cơ hội ngó trái/phải (tỉ lệ thấp hơn)
            if (Random.value < waypointLookChance)
            {
                yield return StartCoroutine(_baseNPC.LookAroundOnceRoutine(45f, 1.0f));
            }
            else
            {
                yield return new WaitForSeconds(waypointWaitDuration);
            }

            // Chuyển sang Waypoint tiếp theo (Loop vòng tròn)
            _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Count;
        }

        _isPatrolCoroutineRunning = false;
    }

    #endregion
}
