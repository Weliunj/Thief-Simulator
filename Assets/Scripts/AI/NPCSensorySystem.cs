using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Hệ thống Đa Giác Quan (Sensory System) cho NPC:
/// 1. Thị giác (FOV Vision): Nón quét tầm nhìn phía trước, giảm tầm phát hiện khi Player cúi (Crouch), kiểm tra Line of Sight qua Raycast.
/// 2. Thính giác (Footstep & Sound Hearing): Lắng nghe tiếng bước chân (Sprint > Walk > Crouch = 0) hoặc âm thanh ngoại cảnh.
/// 3. Ưu tiên Thính giác và cung cấp danh sách người chơi nhìn thấy để đổi mục tiêu thông minh.
/// </summary>
public class NPCSensorySystem : MonoBehaviour
{
    [Header("👁️ Thị Giác (Vision FOV Settings)")]
    [Tooltip("Góc quét hình nón phía trước mặt (độ)")]
    public float fovAngle = 100f;

    [Tooltip("Tầm nhìn xa tối đa (mét)")]
    public float viewDistance = 16f;

    [Tooltip("Hệ số giảm tầm nhìn khi Player đang cúi người (Crouching)")]
    [Range(0.1f, 1f)]
    public float crouchDistanceMultiplier = 0.5f;

    [Tooltip("Vị trí mắt của NPC (nếu để trống sẽ tự lấy đỉnh đầu hoặc offset từ transform)")]
    public Transform eyeTransform;

    [Tooltip("Độ cao của mắt nếu không gán eyeTransform")]
    public float eyeHeightOffset = 1.6f;

    [Tooltip("Các Layer cản tầm nhìn của NPC (Tường, Đồ đạc, Mặt đất)")]
    public LayerMask obstacleMask;

    [Tooltip("Layer của Player")]
    public LayerMask playerLayer;

    [Header("👂 Thính Giác (Hearing Settings)")]
    [Tooltip("Bán kính nghe tiếng bước chân khi Player Chạy Nhanh (Sprint)")]
    public float sprintNoiseRadius = 12f;

    [Tooltip("Bán kính nghe tiếng bước chân khi Player Đi Bộ (Walk)")]
    public float walkNoiseRadius = 4.5f;

    [Tooltip("Bán kính nghe tiếng bước chân khi Player Cúi (Crouch) = 0 (Hoàn toàn im lặng)")]
    public float crouchNoiseRadius = 0f;

    [Tooltip("Thời gian giữa các lần quét giác quan (giây). Đặt 0.1s - 0.2s giúp giảm 90% Raycast & CPU cycles mà không gây trễ.")]
    public float scanInterval = 0.15f;

    [Header("🎯 Cache & Detected Targets")]
    public PlayerController currentPrimaryTarget;
    public List<PlayerController> visiblePlayers = new List<PlayerController>();

    private float _lastScanTime = -10f;
    private PlayerController _cachedBestTarget;
    private bool _cachedHeardNoise;
    private Vector3 _cachedNoisePos;
    private bool _cachedHasVisibleTarget;

    private static PlayerController[] _allPlayersCache;
    private static float _lastPlayerCacheTime = -10f;

    public Vector3 EyePosition => eyeTransform != null ? eyeTransform.position : transform.position + Vector3.up * eyeHeightOffset;
    public Vector3 ForwardDirection => eyeTransform != null ? eyeTransform.forward : transform.forward;

    private void Awake()
    {
        if (obstacleMask == 0)
        {
            obstacleMask = LayerMask.GetMask("Default", "Ground", "Obj");
        }
        if (playerLayer == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả Player còn sống trong Scene (cache 0.5s để tối ưu hiệu năng)
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
    /// Quét toàn bộ giác quan (Thính giác và Thị giác). Có bộ đệm cache theo scanInterval để tối ưu hiệu năng CPU.
    /// </summary>
    public bool ScanSensors(out PlayerController bestVisibleTarget, out bool heardNoise, out Vector3 noisePos, bool forceImmediate = false)
    {
        // 1. Kiểm tra bộ đệm Cache (Throttle) nếu chưa tới thời gian quét tiếp theo
        if (!forceImmediate && Time.time - _lastScanTime < scanInterval)
        {
            bestVisibleTarget = _cachedBestTarget;
            heardNoise = _cachedHeardNoise;
            noisePos = _cachedNoisePos;
            return _cachedHasVisibleTarget;
        }

        _lastScanTime = Time.time;
        bestVisibleTarget = null;
        heardNoise = false;
        noisePos = Vector3.zero;

        visiblePlayers.Clear();

        PlayerController[] players = GetAllLivingPlayers();
        if (players == null || players.Length == 0)
        {
            _cachedBestTarget = null;
            _cachedHeardNoise = false;
            _cachedNoisePos = Vector3.zero;
            _cachedHasVisibleTarget = false;
            return false;
        }

        Vector3 myEyePos = EyePosition;
        Vector3 myForward = ForwardDirection;
        float closestVisibleDist = float.MaxValue;

        // 1. Quét kiểm tra Thính giác & Thị giác trên từng Player
        for (int i = 0; i < players.Length; i++)
        {
            PlayerController p = players[i];
            if (p == null || !p.gameObject.activeInHierarchy) continue;

            // Bỏ qua người chơi đã chết
            if (p.stats != null && p.stats.isDied) continue;
            if (p.deathHandler != null && p.deathHandler.isDeadProcessed) continue;

            Vector3 playerCenter = GetPlayerTargetPoint(p);
            float distToPlayer = Vector3.Distance(myEyePos, playerCenter);

            // --- KIỂM TRA THÍNH GIÁC (Hearing) ---
            float noiseRadius = GetPlayerNoiseRadius(p);
            if (noiseRadius > 0.01f && distToPlayer <= noiseRadius)
            {
                heardNoise = true;
                noisePos = p.transform.position;
            }

            // --- KIỂM TRA THỊ GIÁC (FOV Vision) ---
            float maxEffectiveViewDist = viewDistance;
            if (p.Crouching)
            {
                maxEffectiveViewDist *= crouchDistanceMultiplier;
            }

            if (distToPlayer <= maxEffectiveViewDist)
            {
                Vector3 dirToPlayer = (playerCenter - myEyePos).normalized;
                float angle = Vector3.Angle(myForward, dirToPlayer);

                if (angle <= fovAngle * 0.5f)
                {
                    // Bắn tia Line of Sight kiểm tra vật cản
                    if (HasLineOfSight(myEyePos, playerCenter, distToPlayer))
                    {
                        visiblePlayers.Add(p);

                        if (distToPlayer < closestVisibleDist)
                        {
                            closestVisibleDist = distToPlayer;
                            bestVisibleTarget = p;
                        }
                    }
                }
            }
        }

        currentPrimaryTarget = bestVisibleTarget;
        _cachedBestTarget = bestVisibleTarget;
        _cachedHeardNoise = heardNoise;
        _cachedNoisePos = noisePos;
        _cachedHasVisibleTarget = visiblePlayers.Count > 0;

        return _cachedHasVisibleTarget;
    }

    /// <summary>
    /// Phát ra một nguồn âm thanh tại vị trí xác định (tiếng bẻ khóa hỏng, rơi đồ, cửa va đập...).
    /// Tất cả NPC trong bán kính radius sẽ nhận được âm thanh và kích hoạt cơ chế điều tra tiếng động.
    /// </summary>
    public static void EmitNoiseAtPosition(Vector3 noisePosition, float radius)
    {
        BaseNPC[] allNpcs = FindObjectsByType<BaseNPC>(FindObjectsSortMode.None);
        if (allNpcs == null || allNpcs.Length == 0) return;

        for (int i = 0; i < allNpcs.Length; i++)
        {
            BaseNPC npc = allNpcs[i];
            if (npc == null || !npc.gameObject.activeInHierarchy || !npc.IsMasterAuthority) continue;

            float dist = Vector3.Distance(npc.transform.position, noisePosition);
            if (dist <= radius)
            {
                npc.TriggerNoiseInvestigation(noisePosition);
            }
        }
    }

    /// <summary>
    /// Kiểm tra tia nhìn thẳng Line of Sight có bị vật cản (tường, đồ đạc) che khuất không
    /// </summary>
    public bool HasLineOfSight(Vector3 fromPos, Vector3 targetPos, float maxDist)
    {
        Vector3 dir = (targetPos - fromPos).normalized;
        if (Physics.Raycast(fromPos, dir, out RaycastHit hit, maxDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Bị vật cản chặn trước khi tới đích
            if (hit.distance < maxDist - 0.3f)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Tính bán kính tiếng ồn bước chân của Player
    /// </summary>
    public float GetPlayerNoiseRadius(PlayerController p)
    {
        if (p == null) return 0f;

        // Cúi người (Crouch) = 0m (hoàn toàn im lặng)
        if (p.Crouching) return crouchNoiseRadius;

        // Kiểm tra xem Player có đang di chuyển không
        bool isMoving = false;
        if (p._input != null && p._input.move.sqrMagnitude > 0.01f)
        {
            isMoving = true;
        }
        else
        {
            var cc = p.GetComponent<CharacterController>();
            if (cc != null && cc.velocity.sqrMagnitude > 0.1f) isMoving = true;
        }

        if (!isMoving) return 0f;

        // Đang chạy Sprint
        if (p._input != null && p._input.sprint && p.stats != null && p.stats.canSprint)
        {
            return sprintNoiseRadius;
        }

        // Đi bộ bình thường (Walk)
        return walkNoiseRadius;
    }

    /// <summary>
    /// Lấy điểm ngắm trên thân Player (ngực / đầu) để bắn tia Line of Sight chính xác
    /// </summary>
    public Vector3 GetPlayerTargetPoint(PlayerController p)
    {
        if (p == null) return Vector3.zero;

        if (p.CinemachineCameraTarget != null)
        {
            return p.CinemachineCameraTarget.transform.position;
        }

        return p.transform.position + Vector3.up * (p.Crouching ? 0.75f : 1.35f);
    }

    /// <summary>
    /// Kiểm tra nếu có Player nhìn thấy nào gần hơn mục tiêu hiện tại một khoảng threshold (để đổi mục tiêu)
    /// </summary>
    public bool TryGetCloserTarget(PlayerController currentTarget, float switchThreshold, out PlayerController newTarget)
    {
        newTarget = null;
        if (currentTarget == null)
        {
            if (visiblePlayers.Count > 0)
            {
                newTarget = visiblePlayers[0];
                return true;
            }
            return false;
        }

        float currentDist = Vector3.Distance(transform.position, currentTarget.transform.position);
        float bestDist = currentDist;

        for (int i = 0; i < visiblePlayers.Count; i++)
        {
            PlayerController p = visiblePlayers[i];
            if (p == null || p == currentTarget) continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < bestDist - switchThreshold)
            {
                bestDist = dist;
                newTarget = p;
            }
        }

        return newTarget != null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = EyePosition;
        Vector3 fwd = ForwardDirection;

        // Vẽ nón tầm nhìn FOV
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(eyePos, viewDistance);

        Quaternion leftRayRotation = Quaternion.AngleAxis(-fovAngle * 0.5f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fovAngle * 0.5f, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * fwd;
        Vector3 rightRayDirection = rightRayRotation * fwd;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(eyePos, leftRayDirection * viewDistance);
        Gizmos.DrawRay(eyePos, rightRayDirection * viewDistance);

        // Vẽ bán kính nghe tiếng bước chân
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, sprintNoiseRadius);
    }
}
