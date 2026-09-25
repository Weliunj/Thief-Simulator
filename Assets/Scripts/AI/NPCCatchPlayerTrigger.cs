using UnityEngine;
using StarterAssets;

/// <summary>
/// Script mô phỏng / xử lý khi NPC (hoặc Trigger thử nghiệm) chạm vào Player:
/// - Khi NPC/Trigger tiếp xúc hoặc lại gần Player:
///   1. Player tử vong (isDied = true, chạy animation chết, bật camera lock-on).
///   2. Văng tất cả item đang giữ trong túi/hotbar ra xung quanh kèm lực vật lý (Scatter Force).
///   3. Phát thông báo Status lên màn hình HUD toàn bộ phòng: "[PlayerName] got caught by [NPCName]!".
/// - Có thể gắn lên bất kỳ NPC 3D Model nào, hoặc 1 GameObject có Collider (Is Trigger hoặc Vật lý), hoặc dùng quét khoảng cách.
/// </summary>
public class NPCCatchPlayerTrigger : MonoBehaviour
{
    [Header("👤 NPC Settings")]
    [Tooltip("Tên của NPC hiển thị trong thông báo bị bắt (VD: 'Guard', 'Police', 'Security Dog', ...)")]
    public string npcName = "Guard";

    [Header("🎯 Detection & Catch Options")]
    [Tooltip("Bật tính năng bắt người chơi qua khoảng cách (Distance Check)")]
    public bool catchByDistance = true;

    [Tooltip("Khoảng cách bắt khi lại gần Player (mét)")]
    public float catchDistance = 1.6f;

    [Tooltip("Thời gian chờ giữa các lần bắt")]
    public float catchCooldown = 2.0f;

    [Header("🔊 Audio (Optional)")]
    public AudioClip catchSound;
    public AudioSource audioSource;

    private float _lastCatchTime = -10f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!catchByDistance) return;
        if (Time.time - _lastCatchTime < catchCooldown) return;

        // Quét tìm tất cả Player trong bán kính catchDistance
        Collider[] hits = Physics.OverlapSphere(transform.position, catchDistance, LayerMask.GetMask("Player", "Default"));
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject) continue;

            if (hit.CompareTag("Player") || hit.GetComponentInParent<PlayerController>() != null)
            {
                TryCatchPlayer(hit.gameObject);
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == gameObject) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
        {
            TryCatchPlayer(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == gameObject) return;
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            TryCatchPlayer(collision.gameObject);
        }
    }

    /// <summary>
    /// Xử lý bắt Player
    /// </summary>
    public void TryCatchPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;
        if (Time.time - _lastCatchTime < catchCooldown) return;

        // 1. Tìm các component chính của Player
        PlayerDeathHandler deathHandler = playerObj.GetComponent<PlayerDeathHandler>() ?? playerObj.GetComponentInParent<PlayerDeathHandler>();
        PlayerController playerCtrl = playerObj.GetComponent<PlayerController>() ?? playerObj.GetComponentInParent<PlayerController>();
        PlayerStats stats = playerObj.GetComponent<PlayerStats>() ?? playerObj.GetComponentInParent<PlayerStats>();
        PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>() ?? playerObj.GetComponentInParent<PlayerInventory>();

        // Nếu Player đã chết rồi thì bỏ qua
        if (stats != null && stats.isDied) return;
        if (deathHandler != null && deathHandler.isDeadProcessed) return;

        _lastCatchTime = Time.time;

        // Phát âm thanh bắt (nếu có)
        if (audioSource != null && catchSound != null)
        {
            audioSource.PlayOneShot(catchSound);
        }

        // 2. Kích hoạt xử lý chết
        if (deathHandler != null)
        {
            deathHandler.TriggerDeath(npcName);
        }
        else
        {
            // Fallback thủ công nếu không có component PlayerDeathHandler
            if (stats != null) stats.isDied = true;
            if (inventory != null) inventory.DropAllItemsOnDeath();

            string pName = "Player";
            var netSync = playerObj.GetComponent<NetworkPlayerSync>() ?? playerObj.GetComponentInParent<NetworkPlayerSync>();
            if (netSync != null && !string.IsNullOrEmpty(netSync.NetworkPlayerName.ToString()))
            {
                pName = netSync.NetworkPlayerName.ToString();
            }

            string notice = $"{pName} got caught by {npcName}!";
            if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
            {
                netSync.RpcBroadcastStatusMessage(notice);
            }
            else
            {
                GameStatusHUD.Show(notice);
            }
        }

        Debug.Log($"<color=red>[NPCCatchPlayerTrigger] {npcName} đã bắt được Player ({playerObj.name})!</color>");
    }

    private void OnDrawGizmosSelected()
    {
        if (catchByDistance)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, catchDistance);
        }
    }
}
