using System.Collections;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Quản lý Cửa khóa (DoorController):
/// - Tương tác mở khóa (Lockpicking) thông qua IInteractable và Raycast tâm ngắm
/// - Khi mở khóa thành công: Tắt collider cửa, xoay mở model cánh cửa con (Smooth Open Animation)
/// - Phát âm thanh 3D trực tiếp tại vị trí Cửa: Hit (bấm trúng nấc), Miss (gãy ghim/báo động), Victory (mở khóa)
/// - Báo động gọi NPC truy đuổi khi mở khóa thất bại
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

    [Header("🚪 Door Model & Open Animation")]
    [Tooltip("Transform cánh cửa con sẽ xoay khi mở (nếu để trống, tự động tìm child đầu tiên hoặc dùng chính object)")]
    public Transform doorChildModel;

    [Tooltip("Góc xoay mở cửa (Local Euler Angles Offset)")]
    public Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);

    [Tooltip("Tốc độ mở cửa")]
    public float openSpeed = 3.0f;

    [Tooltip("Có tự động mở mượt mà (smooth animation) không")]
    public bool smoothOpen = true;

    [Header("🔊 3D Spatial Audio (Âm thanh phát tại cửa)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh bấm trúng nấc bẻ khóa (Hit sound)")]
    public AudioClip hitSound;
    [Tooltip("Âm thanh bấm trượt / gãy công cụ (Miss sound)")]
    public AudioClip missSound;
    [Tooltip("Âm thanh mở khóa thành công & cót két mở cửa (Victory sound)")]
    public AudioClip victorySound;

    [Header("⚠️ Alarm & NPC Alert Settings")]
    [Tooltip("Bán kính phát hiện và gọi AdultNPC khi bẻ khóa thất bại")]
    public float callRange = 20f;

    [HideInInspector] public bool isUnlocked = false;
    private UI_Manager uiManager;

    void Awake()
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

        // Tự động tìm child model nếu chưa gán
        if (doorChildModel == null && transform.childCount > 0)
        {
            doorChildModel = transform.GetChild(0);
        }
    }

    void Start()
    {
        uiManager = FindFirstObjectByType<UI_Manager>();
    }

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
    /// - Cộng điểm thưởng
    /// - Phát âm thanh Victory tại cửa
    /// - Tắt các collider để đi qua được
    /// - Mở cánh cửa (xoay model child)
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

        // 2. Phát âm thanh mở cửa
        PlayVictorySound();

        // 3. Tắt Collider chính & con để người chơi và NPC đi xuyên qua
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (c != null) c.enabled = false;
        }

        // 4. Mở cánh cửa (xoay model child)
        OpenDoorModel();
    }

    private void OpenDoorModel()
    {
        Transform targetTrans = (doorChildModel != null) ? doorChildModel : transform;

        if (smoothOpen && gameObject.activeInHierarchy)
        {
            StartCoroutine(SmoothOpenDoorRoutine(targetTrans));
        }
        else
        {
            targetTrans.localRotation = targetTrans.localRotation * Quaternion.Euler(openRotationOffset);
        }
    }

    private IEnumerator SmoothOpenDoorRoutine(Transform doorTrans)
    {
        Quaternion startRot = doorTrans.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(openRotationOffset);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            doorTrans.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        doorTrans.localRotation = targetRot;
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
        Debug.Log($"Trò chơi thất bại! Đang kêu gọi AdultNPC trong phạm vi {callRange}m.");

        Collider[] colliders = Physics.OverlapSphere(transform.position, callRange);
        int adultCount = 0;

        foreach (var col in colliders)
        {
            if (col.CompareTag("adult"))
            {
                AI_Move_NavMesh adultNpc = col.GetComponent<AI_Move_NavMesh>();
                if (adultNpc != null)
                {
                    adultNpc.PlayDetectionSound();
                    adultNpc.HandleChaseMusic(true);
                    adultNpc.targetDetected = true;
                    adultNpc.chaseDuration = Random.Range(
                        adultNpc.chaseDurationPublic.x,
                        adultNpc.chaseDurationPublic.y);

                    adultCount++;
                    Debug.Log($"Kích hoạt chase trên NPC: {col.gameObject.name}");
                }
            }
        }

        if (adultCount == 0)
        {
            Debug.Log("Không tìm thấy AdultNPC nào trong phạm vi.");
        }
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
