using System.Collections;
using StarterAssets;
using UnityEngine;
using Fusion;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("🚪 Display & Prompts")]
    public string doorName = "Wooden Locked Door";
    public string actionPrompt = "Pick Lock";
    [TextArea(2, 4)]
    public string description = "A locked door. Requires lockpicking to open.";
    public Sprite doorIcon;

    [Header("🚪 Lock & Reward Settings")]
    public int rewardPoints = 10;
    public bool isUnlocked = false;
    public bool isOpen = false;

    [Header("🚪 Door Model & Open Animation")]
    public Transform doorChildModel;
    public Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
    public float openSpeed = 3.5f;
    public bool smoothOpen = true;
    public bool disableColliderWhenOpen = true;

    [Header("⭕ Proximity Detection")]
    public bool enableAutoOpen = true;
    public float autoOpenRadius = 1.2f;
    public float autoCloseRadius = 2.2f;
    public float closeDelayDuration = 1f;
    private float closeTimer = 0f;

    [HideInInspector] public bool isBeingLockpicked = false;
    public Vector3 detectionCenterOffset = new Vector3(0f, 1.0f, 0f);
    public float checkInterval = 0.15f;
    public LayerMask detectionLayerMask = ~0;

    [Header("🔊 3D Spatial Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip victorySound;
    public AudioClip openDoorSound;
    public AudioClip closeDoorSound;

    [Header("⚠️ Alarm & NPC Alert Settings")]
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

        if (doorChildModel == null && transform.childCount > 0)
        {
            doorChildModel = transform.GetChild(0);
        }

        closedLocalRotation = (doorChildModel != null) ? doorChildModel.localRotation : transform.localRotation;

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
    /// Quét các đối tượng quanh cửa. 
    /// Tự động chiếm StateAuthority khi đứng gần để Client không bị Host ép đóng cửa từ xa.
    /// </summary>
    private bool isLocalPlayerInsideZone = false;

    private void CheckNearbyEntities()
    {
        var netObj = GetComponent<NetworkObject>();
        var netSync = GetComponent<NetworkDoorSync>();

        // 1. Quét cục bộ: Xem nhân vật trên MÁY NÀY có đang ở gần cửa không
        Vector3 centerPos = transform.position + detectionCenterOffset;
        float currentRadius = isOpen ? autoCloseRadius : autoOpenRadius;

        // Quét tìm Player
        int hitCount = Physics.OverlapSphereNonAlloc(centerPos, currentRadius, overlapBuffer, detectionLayerMask, QueryTriggerInteraction.Ignore);
        bool amINearby = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null || col.transform.IsChildOf(transform) || col.transform == transform) continue;

            // Tìm thấy tag Player hoặc NPC
            if (col.CompareTag("Player") || col.CompareTag("kid") || col.CompareTag("adult") || col.CompareTag("Npc"))
            {
                amINearby = true;
                break;
            }
        }

        // 2. Nếu trạng thái vào/ra của máy này thay đổi -> Gửi báo hiệu lên Host
        if (amINearby != isLocalPlayerInsideZone)
        {
            isLocalPlayerInsideZone = amINearby;
            if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
            {
                netSync.RpcUpdatePlayerPresence(netSync.Runner.LocalPlayer, isLocalPlayerInsideZone);
            }
        }

        // 3. Logic Đóng/Mở CHỈ chạy trên máy nắm StateAuthority (Host)
        if (netObj != null && netObj.HasStateAuthority)
        {
            bool effectiveUnlocked = (netSync != null) ? (bool)netSync.NetworkIsUnlocked : isUnlocked;
            bool someoneIsInside = (netSync != null && netSync.PlayersNearbyCount > 0) || amINearby;

            if (someoneIsInside && effectiveUnlocked)
            {
                closeTimer = closeDelayDuration;
                if (!isOpen)
                {
                    RequestSetDoorOpen(true);
                }
            }
            else if (isOpen)
            {
                closeTimer -= checkInterval;
                if (closeTimer <= 0f)
                {
                    RequestSetDoorOpen(false);
                }
            }
        }
    }

    public void RequestSetDoorOpen(bool open)
    {
        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            // Bắn RPC tới máy đang nắm StateAuthority để thay đổi biến [Networked]
            netSync.RpcRequestSetDoorOpen(open);
        }
        else
        {
            ApplyDoorVisual(open, true);
        }
    }

    private bool IsAnyNpcInHits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i] == null) continue;
            if (overlapBuffer[i].CompareTag("Npc")) return true;
        }
        return false;
    }

    /// <summary>
    /// Thực hiện thay đổi hình ảnh và âm thanh của cửa (được gọi từ OnChangedRender của NetworkDoorSync)
    /// </summary>
    public void ApplyDoorVisual(bool open, bool playSound)
    {
        isOpen = open;

        if (playSound)
        {
            if (open && openDoorSound != null) PlayLocalSound(openDoorSound);
            else if (!open && closeDoorSound != null) PlayLocalSound(closeDoorSound);
        }

        SetDoorCollidersActive(!open);

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
            if (col != null) col.enabled = active;
        }
    }

    private void InitializeAudio()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.8f;
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 25f;
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

    public void StartLockpicking()
    {
        if (isUnlocked || isBeingLockpicked) return;

        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcRequestSetLockpicking(true);
        }

        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null) uiManager.StartLockpicking(this);
    }

    public void OnUnlockSuccess()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        isBeingLockpicked = false;

        if (uiManager != null && uiManager.playerManager != null)
        {
            uiManager.playerManager.currpoint += rewardPoints;
        }

        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcPlayDoorAudio(NetworkDoorSync.DoorAudioType.Victory);
            netSync.RpcRequestUnlockDoor();
        }
        else
        {
            PlayLocalSound(victorySound);
            ApplyDoorVisual(true, true);
        }
    }

    public void OnUnlockFailed()
    {
        CancelLockpicking();
        AlertNearbyNPCs();
    }

    public void AlertNearbyNPCs() { }

    public void CancelLockpicking()
    {
        isBeingLockpicked = false;
        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcRequestSetLockpicking(false);
        }
    }

    // Đồng bộ phát âm thanh qua NetworkDoorSync
    public void PlayHitSound()
    {
        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcPlayDoorAudio(NetworkDoorSync.DoorAudioType.Hit);
        }
        else
        {
            PlayLocalSound(hitSound);
        }
    }

    public void PlayMissSound()
    {
        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcPlayDoorAudio(NetworkDoorSync.DoorAudioType.Miss);
        }
        else
        {
            PlayLocalSound(missSound);
        }
    }

    public void PlayVictorySound()
    {
        var netSync = GetComponent<NetworkDoorSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcPlayDoorAudio(NetworkDoorSync.DoorAudioType.Victory);
        }
        else
        {
            PlayLocalSound(victorySound);
        }
    }

    public void PlayLocalSound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) InitializeAudio();

        if (audioSource != null) audioSource.PlayOneShot(clip, 1.0f);
        else AudioSource.PlayClipAtPoint(clip, transform.position, 1.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, callRange);
        Gizmos.color = Color.cyan;
        Vector3 centerPos = transform.TransformPoint(detectionCenterOffset);
        Gizmos.DrawWireSphere(centerPos, autoOpenRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(centerPos, autoCloseRadius);
    }

    public string GetInteractableName() => string.IsNullOrEmpty(doorName) ? gameObject.name : doorName;
    public string GetActionPrompt() => string.IsNullOrEmpty(actionPrompt) ? "Pick Lock" : actionPrompt;
    public int GetPrice() => 0;
    public int GetWeight() => 0;
    public bool IsLootItem() => false;
    public string GetDescription() => description;
    public Sprite GetIcon() => doorIcon;
    public ItemRarity GetRarity() => ItemRarity.Common;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        var netSync = GetComponent<NetworkDoorSync>();
        bool networkLocking = netSync != null && netSync.NetworkIsBeingLockpicked;
        bool networkUnlocked = netSync != null && netSync.NetworkIsUnlocked;

        if (isUnlocked || networkUnlocked)
        {
            failReason = "Already Unlocked";
            return false;
        }
        if (isBeingLockpicked || networkLocking)
        {
            failReason = "Someone is picking this lock...";
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
        var netSync = GetComponent<NetworkDoorSync>();
        bool networkLocking = netSync != null && netSync.NetworkIsBeingLockpicked;
        bool networkUnlocked = netSync != null && netSync.NetworkIsUnlocked;

        if (!isUnlocked && !networkUnlocked && !UI_Manager.isSolving && !isBeingLockpicked && !networkLocking)
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && !netObj.HasStateAuthority)
            {
                netObj.RequestStateAuthority();
            }

            StartLockpicking();
        }
    }
}