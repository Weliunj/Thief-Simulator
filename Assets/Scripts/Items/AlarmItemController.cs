using System.Collections;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Controller cho Đồng hồ báo thức / Điện thoại (Alarm / Distraction Device):
/// - Khi cầm trên tay (Hotbar): Nút Interact hiện "Place Alarm" / "Set Timer".
/// - Khi bấm Interact: Tự động đặt item xuống chân người chơi (tương tự Place của Hotbar, tách khỏi Hotbar).
/// - Sau delayToRing (mặc định 10s), nếu không ai nhặt lên -> Kích hoạt chuông báo thức (Alarm).
/// - Chuông kêu trong ringDuration (mặc định 10s) rồi tự tắt.
/// - Trong suốt thời gian kêu: Mỗi 1 giây phát tín hiệu gọi NPC (NPCAlertSystem.EmitNoise).
/// - Khi ở dưới đất (dù đang đếm ngược, đang kêu hay đã kêu xong): Người chơi có thể nhặt lại bình thường.
/// - Nếu nhặt lên khi đang đếm ngược hoặc đang kêu: Ngay lập tức tắt chuông và reset trạng thái.
/// - Đồng bộ âm thanh và trạng thái Alarm 3D qua mạng Photon Fusion (Shared Mode).
/// </summary>
public class AlarmItemController : MonoBehaviour, IInteractable, IHeldInteractable
{
    [Header("⏰ Timer & Ring Settings")]
    [Tooltip("Thời gian chờ từ lúc đặt xuống đất đến khi bắt đầu kêu (giây)")]
    public float delayToRing = 10f;

    [Tooltip("Thời gian chuông kêu trước khi tự tắt (giây)")]
    public float ringDuration = 10f;

    [Tooltip("Chu kỳ phát tiếng động gọi NPC khi đang kêu (mỗi N giây gọi 1 lần)")]
    public float npcAlertInterval = 1.0f;

    [Header("🔊 Audio Sources & Clips")]
    [Tooltip("AudioSource dành riêng cho tiếng chuông reo báo thức (nếu để trống tự tìm hoặc tạo mới)")]
    public AudioSource alarmAudioSource;
    [Tooltip("Tiếng chuông reo báo thức (lặp lại trong suốt thời gian ringDuration)")]
    public AudioClip alarmSound;

    [Tooltip("AudioSource dành riêng cho tiếng tích tắc đếm ngược (nếu để trống tự tìm hoặc tạo mới)")]
    public AudioSource tickingAudioSource;
    [Tooltip("Tiếng tích tắc đếm ngược chạy liên tục trong suốt thời gian delayToRing (10s) trước khi chuông reo")]
    public AudioClip tickingSound;

    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("📢 NPC Alert Settings")]
    public bool alertAdults = true;
    public bool alertKids = true;

    [Header("🏷️ Display Info")]
    public string deviceName = "Alarm Clock";
    public string actionPrompt = "Set Timer";
    public Sprite customIcon;

    // Trạng thái nội bộ
    private bool _isArmed = false;     // Đang trong quá trình đếm ngược hoặc đang kêu
    private bool _isRinging = false;   // Đang phát chuông
    private Coroutine _alarmCoroutine;
    private Item _itemComp;

    private void Awake()
    {
        _itemComp = GetComponent<Item>();
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        var sources = GetComponents<AudioSource>();

        if (alarmAudioSource == null)
        {
            if (sources.Length > 0) alarmAudioSource = sources[0];
            else alarmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (tickingAudioSource == null)
        {
            if (sources.Length > 1) tickingAudioSource = sources[1];
            else tickingAudioSource = gameObject.AddComponent<AudioSource>();
        }

        Configure3DAudioSource(alarmAudioSource);
        Configure3DAudioSource(tickingAudioSource);
    }

    private void Configure3DAudioSource(AudioSource src)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.spatialBlend = 1.0f; // 3D Spatial Audio
        src.minDistance = 2f;
        src.maxDistance = 25f;
        src.rolloffMode = AudioRolloffMode.Linear;
    }

    // =========================================================================
    //               KHI CẦM TRÊN HOTBAR (IHELDINTERACTABLE)
    // =========================================================================

    public string GetHeldActionPrompt() => actionPrompt;

    public Sprite GetHeldActionIcon() => customIcon != null ? customIcon : (_itemComp != null ? _itemComp.GetIcon() : null);

    public bool CanInteractWhileHeld() => true;

    /// <summary>
    /// Khi bấm nút Interact trên Hotbar: Tự động đặt item xuống chân người chơi và bắt đầu đếm ngược 10s
    /// </summary>
    public void OnHeldInteract(PlayerController player)
    {
        if (player == null) return;

        PlaceAtPlayerFeet(player);
    }

    /// <summary>
    /// Đặt item xuống chân người chơi và kích hoạt hẹn giờ
    /// </summary>
    private void PlaceAtPlayerFeet(PlayerController player)
    {
        // 1. Trừ trọng lượng trong PlayerStats
        if (player.stats != null && _itemComp != null)
        {
            player.stats.RemoveWeight(_itemComp.kg);
        }

        // 2. Xóa khỏi PlayerInventory
        if (player.inventory != null && player.inventory.heldItems != null)
        {
            player.inventory.heldItems.Remove(gameObject);
        }

        // 3. Xóa khỏi UI Hotbar
        if (player.inventory != null && player.inventory.hotbarManager != null)
        {
            var hotbar = player.inventory.hotbarManager;
            if (hotbar.currentSelectedIndex >= 0 && hotbar.currentSelectedIndex < hotbar.slots.Count)
            {
                var slot = hotbar.slots[hotbar.currentSelectedIndex];
                if (slot != null && slot.itemObject == gameObject)
                {
                    slot.ClearSlot();
                }
            }
            hotbar.DeselectAll();
        }

        // 4. Bật lại Colliders và Renderers
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) if (c != null) c.enabled = true;
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) if (r != null) r.enabled = true;

        // Tách khỏi Socket tay cầm
        transform.SetParent(null);

        // 5. Xác định vị trí đặt xuống chân (Bắn Raycast xuống sàn để đặt êm)
        Vector3 placePos = player.transform.position + player.transform.forward * 0.35f + Vector3.up * 0.1f;
        if (Physics.Raycast(player.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 2.5f, ~LayerMask.GetMask("Player", "Npc", "Ignore Raycast")))
        {
            placePos = groundHit.point + Vector3.up * 0.05f;
        }

        Quaternion placeRot = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

        transform.position = placePos;
        transform.rotation = placeRot;
        gameObject.SetActive(true);

        // Reset Rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true; // Cố định vị trí như Place
        }

        // 6. Đồng bộ đặt vật phẩm qua mạng (Shared Mode)
        NetworkItemSync.SyncDropItem(gameObject, placePos, placeRot, Vector3.zero, Vector3.zero, isLadder: false, isLadderPlaced: true);

        // 7. Bắt đầu đếm ngược 10s cục bộ & đồng bộ mạng
        ArmAlarm();

        // Gửi RPC đồng bộ kích hoạt hẹn giờ
        NetworkItemSync.SyncAlarmState(gameObject, isArmed: true, isRinging: false);
    }

    /// <summary>
    /// Bắt đầu đếm ngược 10s rồi kêu 10s
    /// </summary>
    public void ArmAlarm()
    {
        StopAlarmSequence();
        _isArmed = true;
        _isRinging = false;
        _alarmCoroutine = StartCoroutine(AlarmSequenceRoutine());
    }

    private IEnumerator AlarmSequenceRoutine()
    {
        // 1. Chờ 10 giây (Phát tiếng tích tắc đếm ngược nếu có)
        StartTickingAudio();

        yield return new WaitForSeconds(delayToRing);

        // Dừng tiếng tích tắc trước khi chuông reo
        StopTickingAudio();

        // 2. Kích hoạt chuông kêu
        _isRinging = true;
        StartRingingAudio();

        // Đồng bộ chuông kêu qua mạng
        NetworkItemSync.SyncAlarmState(gameObject, isArmed: true, isRinging: true);

        // 3. Chuông kêu trong 10 giây, mỗi 1s phát tiếng gọi NPC
        float elapsed = 0f;
        while (elapsed < ringDuration)
        {
            // Phát tiếng động gọi NPC
            NPCAlertSystem.EmitNoise(transform.position, alertAdults: alertAdults, alertKids: alertKids, isFromPlayer: false);

            yield return new WaitForSeconds(npcAlertInterval);
            elapsed += npcAlertInterval;
        }

        // 4. Hết 10s kêu -> Tự tắt
        StopAudio();
        _isArmed = false;
        _isRinging = false;
        NetworkItemSync.SyncAlarmState(gameObject, isArmed: false, isRinging: false);
    }

    /// <summary>
    /// Phát tiếng tích tắc trong thời gian đếm ngược
    /// </summary>
    public void StartTickingAudio()
    {
        if (tickingAudioSource != null && tickingSound != null)
        {
            tickingAudioSource.clip = tickingSound;
            tickingAudioSource.volume = soundVolume;
            tickingAudioSource.loop = true;
            tickingAudioSource.Play();
        }
    }

    /// <summary>
    /// Dừng tiếng tích tắc đếm ngược
    /// </summary>
    public void StopTickingAudio()
    {
        if (tickingAudioSource != null && tickingAudioSource.isPlaying)
        {
            tickingAudioSource.Stop();
        }
    }

    /// <summary>
    /// Phát âm thanh chuông (lặp lại hoặc 1 clip dài)
    /// </summary>
    public void StartRingingAudio()
    {
        if (alarmAudioSource != null && alarmSound != null)
        {
            alarmAudioSource.clip = alarmSound;
            alarmAudioSource.volume = soundVolume;
            alarmAudioSource.loop = true;
            alarmAudioSource.Play();
        }
    }

    /// <summary>
    /// Dừng âm thanh chuông báo thức
    /// </summary>
    public void StopRingingAudio()
    {
        if (alarmAudioSource != null && alarmAudioSource.isPlaying)
        {
            alarmAudioSource.Stop();
        }
    }

    /// <summary>
    /// Dừng mọi âm thanh đang phát (cả ticking lẫn alarm)
    /// </summary>
    public void StopAudio()
    {
        StopTickingAudio();
        StopRingingAudio();
    }

    /// <summary>
    /// Dừng toàn bộ chuỗi đếm ngược và tắt chuông
    /// </summary>
    public void StopAlarmSequence()
    {
        if (_alarmCoroutine != null)
        {
            StopCoroutine(_alarmCoroutine);
            _alarmCoroutine = null;
        }
        StopAudio();
        _isArmed = false;
        _isRinging = false;
    }

    /// <summary>
    /// Được gọi qua Network RPC để áp dụng trạng thái từ máy khác
    /// </summary>
    public void ApplyNetworkAlarmState(bool isArmed, bool isRinging)
    {
        if (isRinging)
        {
            _isArmed = true;
            _isRinging = true;
            StartRingingAudio();
        }
        else if (isArmed)
        {
            _isArmed = true;
            _isRinging = false;
            StartTickingAudio();
        }
        else
        {
            StopAlarmSequence();
        }
    }

    // =========================================================================
    //               KHI Ở DƯỚI ĐẤT (IINTERACTABLE)
    // =========================================================================

    public string GetInteractableName() => string.IsNullOrEmpty(deviceName) ? (_itemComp != null ? _itemComp.GetInteractableName() : "Alarm Device") : deviceName;

    public string GetActionPrompt() => _isRinging ? "Disarm & Take" : (_isArmed ? "Defuse & Take" : "Take");

    public int GetPrice() => _itemComp != null ? _itemComp.GetPrice() : 50;

    public int GetWeight() => _itemComp != null ? _itemComp.GetWeight() : 1;

    public bool IsLootItem() => true;

    public string GetDescription() => $"Distraction device. When placed, rings after {delayToRing:F0}s for {ringDuration:F0}s to lure NPCs.";

    public Sprite GetIcon() => customIcon != null ? customIcon : (_itemComp != null ? _itemComp.GetIcon() : null);

    public ItemRarity GetRarity() => _itemComp != null ? _itemComp.GetRarity() : ItemRarity.Uncommon;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        failReason = "";
        return true;
    }

    /// <summary>
    /// Khi nhặt lại item: Ngay lập tức tắt chuông, reset hẹn giờ và cho vào túi
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (player == null || player.inventory == null) return;

        // 1. Tắt chuông & reset đếm ngược
        StopAlarmSequence();

        // 2. Đồng bộ tắt chuông qua mạng
        NetworkItemSync.SyncAlarmState(gameObject, isArmed: false, isRinging: false);

        // 3. Nhặt vào balo / hotbar
        player.inventory.TryPickupItem(gameObject, GetWeight());
    }
}
