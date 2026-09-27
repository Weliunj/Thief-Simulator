using UnityEngine;

public static class NPCAlertSystem
{
    /// <summary>
    /// Phát tín hiệu tiếng động — NPC tự quyết định có nghe hay không dựa trên hearingRange của nó.
    /// </summary>
    public static void EmitNoise(Vector3 position, bool alertAdults = true, bool alertKids = false, bool isFromPlayer = false)
    {
        if (alertAdults) AdultGuardNPC.AlertAdultsToPosition(position, isFromPlayer);
        if (alertKids) KidRunnerNPC.AlertKidsToPosition(position, isFromPlayer);
    }

    public static void AlertAdults(Vector3 position, bool isFromPlayer = false) => EmitNoise(position, true, false, isFromPlayer);
    public static void AlertKids(Vector3 position, bool isFromPlayer = false) => EmitNoise(position, false, true, isFromPlayer);
}

public class NPCAlertEmitter : MonoBehaviour
{
    [Header("📢 Cấu hình Báo động")]
    [Tooltip("Báo động cho Adult NPC tới kiểm tra vị trí tiếng động")]
    public bool alertAdults = true;

    [Tooltip("Báo động cho Kid NPC hoảng loạn chạy trốn")]
    public bool alertKids = false;

    [Tooltip("Đánh dấu tiếng động này phát ra từ Player (để NPC kiểm tra biến canHearPlayer). Tự động bật nếu gắn trên Player.")]
    public bool isPlayerNoise = false;

    [Tooltip("Bán kính vùng nghi ngờ quanh vị trí phát tiếng động. " +
         "NPC sẽ đi tới vị trí random trong vùng này thay vì đúng tâm. " +
         "0 = đi đúng tâm (chính xác 100%).")]
    public float noiseRadius = 0f;

    [Header("⏱️ Cooldown")]
    public float alertCooldown = 0.5f;

    [Header("💥 Tự động kích hoạt khi va chạm")]
    public bool triggerOnCollision = false;
    public float minImpactVelocity = 2.5f;
    [Tooltip("Khoảng thời gian chờ sau khi spawn (giây). Bỏ qua mọi va chạm rơi tự do trong thời gian này để tránh tiếng ồn khi load scene.")]
    public float spawnGracePeriod = 2.0f;
    public LayerMask impactLayers = ~0;

    [Header("🔊 Âm thanh kèm theo (Tùy chọn)")]
    public AudioSource audioSource;
    public AudioClip soundEffect;
    [Range(0f, 1f)] public float soundVolume = 1f;

    private float _lastAlertTime = -100f;
    private float _spawnTime = 0f;
    public bool isEmitting => Time.time - _lastAlertTime < 0.5f;

    private void Awake()
    {
        _spawnTime = Time.time;

        // Tự động nhận diện nếu script này gắn trên Player
        if (GetComponent<StarterAssets.PlayerController>() != null ||
            GetComponentInParent<StarterAssets.PlayerController>() != null ||
            CompareTag("Player"))
        {
            isPlayerNoise = true;
        }
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
    }

    public void TriggerAlert() => TriggerAlert(transform.position);

    public void TriggerAlert(Vector3 customPosition)
    {
        if (Time.time - _lastAlertTime < alertCooldown) return;
        _lastAlertTime = Time.time;

        if (audioSource != null && soundEffect != null)
            audioSource.PlayOneShot(soundEffect, soundVolume);

        // ⭐ Random vị trí trong vùng noiseRadius
        Vector3 targetPos = GetRandomNoisePosition(customPosition);

        NPCAlertSystem.EmitNoise(targetPos, alertAdults, alertKids, isPlayerNoise);
    }

    /// <summary>
    /// Lấy vị trí random trong bán kính noiseRadius quanh vị trí gốc.
    /// Sample trên NavMesh để đảm bảo NPC có thể đi tới.
    /// </summary>
    private Vector3 GetRandomNoisePosition(Vector3 origin)
    {
        if (noiseRadius <= 0.01f) return origin;

        // Random hướng + khoảng cách trong vòng tròn
        Vector2 randCircle = Random.insideUnitCircle * noiseRadius;
        Vector3 candidate = origin + new Vector3(randCircle.x, 0f, randCircle.y);

        // Sample NavMesh — nếu không có NavMesh gần đó thì giữ vị trí gốc
        if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, noiseRadius + 1f, UnityEngine.AI.NavMesh.AllAreas))
            return hit.position;

        // Fallback: sample vị trí gốc
        if (UnityEngine.AI.NavMesh.SamplePosition(origin, out var originHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            return originHit.position;

        return origin;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!triggerOnCollision) return;
        if (((1 << collision.gameObject.layer) & impactLayers.value) == 0) return;
        if (collision.relativeVelocity.magnitude >= minImpactVelocity)
            TriggerAlert();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isEmitting ? new Color(1f, 0.2f, 0.2f, 0.5f) : new Color(1f, 0.8f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}