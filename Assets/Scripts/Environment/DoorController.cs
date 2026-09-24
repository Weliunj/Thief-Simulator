using System.Collections;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Quản lý Cửa thông minh (DoorController) - Cơ chế 1 Collider duy nhất:
/// - Tương tác bẻ khóa (Lockpicking) thông qua IInteractable và Raycast tâm ngắm (khi cửa chưa bẻ khóa).
/// - Sử dụng kiểm tra khoảng cách định kỳ (OverlapSphere) để tự động mở/đóng:
///   + NPC (kid, adult, Npc) đến gần bán kính autoOpenRadius -> Tự động mở cửa.
///   + Player đã bẻ khóa (isUnlocked) đến gần -> Tự động mở cửa.
///   + Khi không còn ai trong bán kính -> Tự động xoay đóng lại.
/// - Cánh cửa chỉ có DUY NHẤT 1 Solid Collider trên model cánh cửa để cản đường và chắn tầm nhìn (Line of Sight) khi đóng.
///   Khi cửa mở, model và collider xoay 90 độ né sang bên để người và tầm nhìn đi qua.
/// </summary>
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("🚪 Display & Prompts (Hiển thị thông tin lên UI HUD)")]
    [Tooltip("Tên cánh cửa hiển thị trên màn hình")]
    public string doorName = "Wooden Locked Door";

    [Tooltip("Hành động trên nút tương tác (VD: Pick Lock, Open, Unlock)")]
    public string actionPrompt = "Pick Lock";

    [Tooltip("Mô tả chi tiết hiển thị trên HUD")]
    [TextArea(2, 4)]
    public string description = "A locked door. Requires lockpicking to open.";

    [Tooltip("Icon đại diện của cánh cửa (nếu có)")]
    public Sprite doorIcon;

    [Header("🚪 Lock & Reward Settings")]
    [Tooltip("Điểm thưởng khi mở khóa thành công")]
    public int rewardPoints = 10;

    [Tooltip("Trạng thái đã bẻ khóa (true = đã mở khóa, false = còn khóa cần bẻ)")]
    public bool isUnlocked = false;

    [Tooltip("Trạng thái cửa đang mở hay đóng")]
    public bool isOpen = false;

    [Header("🚪 Door Model & Open Animation")]
    [Tooltip("Transform cánh cửa con sẽ xoay khi mở (nếu để trống, tự động tìm child đầu tiên hoặc dùng chính object)")]
    public Transform doorChildModel;

    [Tooltip("Góc xoay mở cửa (Local Euler Angles Offset)")]
    public Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);

    [Tooltip("Tốc độ mở / đóng cửa")]
    public float openSpeed = 3.5f;

    [Tooltip("Có tự động mở mượt mà (smooth animation) không")]
    public bool smoothOpen = true;

    [Tooltip("Tự động tắt collider khi cửa mở để Player/NPC đi qua mượt mà, và tự động bật lại khi cửa đóng")]
    public bool disableColliderWhenOpen = true;

    [Header("⭕ Proximity Detection (Quét khoảng cách bằng Code - Không cần Collider Trigger)")]
    [Tooltip("Tự động mở khi Player/NPC đến gần")]
    public bool enableAutoOpen = true;

    [Tooltip("Bán kính phát hiện Player và NPC quanh cửa (mét)")]
    public float autoOpenRadius = 2.5f;

    [Tooltip("Offset tâm vùng quét bán kính so với vị trí Cửa (X, Y, Z)")]
    public Vector3 detectionCenterOffset = new Vector3(0f, 1.0f, 0f);

    [Tooltip("Tần suất quét khoảng cách (giây, càng nhỏ càng nhạy, 0.15s là tối ưu CPU)")]
    public float checkInterval = 0.15f;

    [Tooltip("LayerMask quét các đối tượng Player và NPC quanh cửa")]
    public LayerMask detectionLayerMask = ~0;

    [Header("🔊 3D Spatial Audio (Âm thanh phát tại cửa)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh bấm trúng nấc bẻ khóa (Hit sound)")]
    public AudioClip hitSound;
    [Tooltip("Âm thanh bấm trượt / gãy công cụ (Miss sound)")]
    public AudioClip missSound;
    [Tooltip("Âm thanh mở khóa thành công & cót két mở cửa (Victory sound)")]
    public AudioClip victorySound;
    [Tooltip("Âm thanh khi mở cửa")]
    public AudioClip openDoorSound;
    [Tooltip("Âm thanh khi đóng cửa")]
    public AudioClip closeDoorSound;

    [Header("⚠️ Alarm & NPC Alert Settings")]
    [Tooltip("Bán kính phát hiện và gọi AdultNPC khi bẻ khóa thất bại")]
    public float callRange = 20f;

    private UI_Manager uiManager;
    private Quaternion closedLocalRotation;
    private Coroutine doorCoroutine;
    private float checkTimer = 0f;
    private readonly Collider[] overlapBuffer = new Collider[16];
    private Collider[] doorColliders;

    void Awake()
    {
        InitializeAudio();

        // Tự động tìm child model nếu chưa gán
        if (doorChildModel == null && transform.childCount > 0)
        {
            doorChildModel = transform.GetChild(0);
        }

        if (doorChildModel != null)
        {
            closedLocalRotation = doorChildModel.localRotation;
        }
        else
        {
            closedLocalRotation = transform.localRotation;
        }

        // Mặc định layer quét Player và Npc nếu chưa thiết lập
        if (detectionLayerMask == ~0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int npcLayer = LayerMask.NameToLayer("Npc");
            int defaultLayer = LayerMask.NameToLayer("Default");

            int mask = 0;
            if (playerLayer != -1) mask |= (1 << playerLayer);
            if (npcLayer != -1) mask |= (1 << npcLayer);
            if (defaultLayer != -1) mask |= (1 << defaultLayer);

            detectionLayerMask = (mask != 0) ? mask : ~0;
        }
    }

    void Start()
    {
        uiManager = FindFirstObjectByType<UI_Manager>();
    }

    void Update()
    {
        if (!enableAutoOpen) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            CheckNearbyEntities();
        }
    }

    /// <summary>
    /// Quét các đối tượng trong bán kính autoOpenRadius bằng OverlapSphereNonAlloc (Siêu nhẹ, 0 garbage allocation)
    /// </summary>
    private void CheckNearbyEntities()
    {
        Vector3 centerPos = transform.TransformPoint(detectionCenterOffset);
        int hitCount = Physics.OverlapSphereNonAlloc(centerPos, autoOpenRadius, overlapBuffer, detectionLayerMask, QueryTriggerInteraction.Ignore);

        bool npcNearby = false;
        bool playerNearby = false;
        int npcLayer = LayerMask.NameToLayer("Npc");

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;

            // Bỏ qua chính collider của cánh cửa
            if (col.transform.IsChildOf(transform) || col.transform == transform) continue;

            if (col.CompareTag("Player"))
            {
                playerNearby = true;
            }
            else if (col.CompareTag("kid") || col.CompareTag("adult") || col.CompareTag("Npc") || (npcLayer != -1 && col.gameObject.layer == npcLayer))
            {
                npcNearby = true;
            }
        }

        // Quyết định Mở hay Đóng:
        // - NPC đến gần -> Tự động mở
        // - Player đến gần -> Chỉ tự mở nếu đã bẻ khóa (isUnlocked)
        bool shouldBeOpen = npcNearby || (playerNearby && isUnlocked);

        if (shouldBeOpen && !isOpen)
        {
            SetDoorOpen(true);
        }
        else if (!shouldBeOpen && isOpen)
        {
            SetDoorOpen(false);
        }
    }

    /// <summary>
    /// Đóng/Mở cửa
    /// </summary>
    public void SetDoorOpen(bool open, bool syncNetwork = true)
    {
        if (isOpen == open && doorCoroutine == null) return;

        isOpen = open;

        if (syncNetwork)
        {
            var netDoor = GetComponent<NetworkDoorSync>();
            if (netDoor != null && netDoor.Runner != null && netDoor.Runner.IsRunning)
            {
                netDoor.RpcSyncSetDoorOpen(open);
            }
        }

        if (open)
        {
            if (openDoorSound != null) PlaySound(openDoorSound);
            SetDoorCollidersActive(false); // Tắt collider khi mở để Player/NPC đi qua
        }
        else
        {
            if (closeDoorSound != null) PlaySound(closeDoorSound);
        }

        Transform targetTrans = (doorChildModel != null) ? doorChildModel : transform;

        if (smoothOpen && gameObject.activeInHierarchy)
        {
            if (doorCoroutine != null) StopCoroutine(doorCoroutine);
            doorCoroutine = StartCoroutine(SmoothDoorRoutine(targetTrans, open));
        }
        else
        {
            Quaternion targetRot = open ? closedLocalRotation * Quaternion.Euler(openRotationOffset) : closedLocalRotation;
            targetTrans.localRotation = targetRot;

            if (!open)
            {
                SetDoorCollidersActive(true); // Bật lại collider khi cửa đã đóng
            }
        }
    }

    private IEnumerator SmoothDoorRoutine(Transform doorTrans, bool open)
    {
        Quaternion startRot = doorTrans.localRotation;
        Quaternion targetRot = open ? closedLocalRotation * Quaternion.Euler(openRotationOffset) : closedLocalRotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            doorTrans.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        doorTrans.localRotation = targetRot;
        doorCoroutine = null;

        if (!open)
        {
            SetDoorCollidersActive(true); // Bật lại collider sau khi cửa đã đóng xong
        }
    }

    private void SetDoorCollidersActive(bool active)
    {
        if (!disableColliderWhenOpen) return;

        if (doorColliders == null || doorColliders.Length == 0)
        {
            doorColliders = GetComponentsInChildren<Collider>(true);
        }

        foreach (var col in doorColliders)
        {
            if (col != null)
            {
                col.enabled = active;
            }
        }
    }

    private void InitializeAudio()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Spatial Sound
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 30f;
            audioSource.playOnAwake = false;
        }

        if (audioSource != null && audioSource.outputAudioMixerGroup == null && SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            audioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
        }
    }

    // =========================================================================
    //                        LOCKPICKING & REWARDS
    // =========================================================================

    /// <summary>
    /// Bắt đầu minigame bẻ khóa qua UI_Manager
    /// </summary>
    public void StartLockpicking()
    {
        if (isUnlocked) return;

        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();

        if (uiManager != null)
        {
            uiManager.StartLockpicking(this);
        }
        else
        {
            Debug.LogError("UI_Manager not found in Scene!");
        }
    }

    /// <summary>
    /// Xử lý khi bẻ khóa thành công:
    /// - Đánh dấu isUnlocked = true
    /// - Cộng điểm thưởng
    /// - Phát âm thanh Victory tại cửa
    /// - Tự động mở cửa ngay lập tức
    /// </summary>
    public void OnUnlockSuccess()
    {
        if (isUnlocked) return;
        isUnlocked = true;

        Debug.Log($"<color=green>Successfully unlocked door: {doorName}!</color>");

        // 1. Cộng điểm
        if (uiManager != null && uiManager.playerManager != null)
        {
            uiManager.playerManager.currpoint += rewardPoints;
            Debug.Log($"Added {rewardPoints} points. Current total: {uiManager.playerManager.currpoint}");
        }

        // 2. Phát âm thanh mở khóa
        PlayVictorySound();

        // 3. Tự động mở cửa
        SetDoorOpen(true);

        // 4. Đồng bộ trạng thái mở khóa qua mạng nếu đang trong phòng Fusion
        var netDoor = GetComponent<NetworkDoorSync>();
        if (netDoor != null && netDoor.Runner != null && netDoor.Runner.IsRunning)
        {
            netDoor.RpcSyncUnlockDoor();
        }
    }

    /// <summary>
    /// Xử lý khi bẻ khóa thất bại:
    /// - Phát âm thanh gãy công cụ
    /// - Kêu gọi NPC trong khu vực truy đuổi
    /// </summary>
    public void OnUnlockFailed()
    {
        PlayMissSound();
        AlertNearbyNPCs();
    }

    public void AlertNearbyNPCs()
    {

    }

    // =========================================================================
    //                       AUDIO PLAYBACK API (3D)
    // =========================================================================

    public void PlayHitSound()
    {
        PlaySound(hitSound);
    }

    public void PlayMissSound()
    {
        PlaySound(missSound);
    }

    public void PlayVictorySound()
    {
        PlaySound(victorySound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, callRange);

        // Bán kính tự động mở/đóng cửa hiển thị theo detectionCenterOffset
        Gizmos.color = Color.cyan;
        Vector3 centerPos = transform.TransformPoint(detectionCenterOffset);
        Gizmos.DrawWireSphere(centerPos, autoOpenRadius);
    }

    // =========================================================================
    //                       IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => string.IsNullOrEmpty(doorName) ? gameObject.name : doorName;
    public string GetActionPrompt() => string.IsNullOrEmpty(actionPrompt) ? "Pick Lock" : actionPrompt;
    public int GetPrice() => 0;
    public int GetWeight() => 0;
    public bool IsLootItem() => false;
    public string GetDescription() => string.IsNullOrEmpty(description) ? "A locked door. Requires lockpicking to open." : description;
    public Sprite GetIcon() => doorIcon;
    public ItemRarity GetRarity() => ItemRarity.Common;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        if (isUnlocked)
        {
            failReason = "Already Unlocked";
            return false;
        }
        if (UI_Manager.isSolving)
        {
            failReason = "";
            return false;
        }
        failReason = "";
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (!isUnlocked && !UI_Manager.isSolving)
        {
            StartLockpicking();
        }
    }
}
