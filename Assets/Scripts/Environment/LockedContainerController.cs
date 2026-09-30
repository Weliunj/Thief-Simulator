using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarterAssets;
using Fusion;

/// <summary>
/// Quản lý các loại tủ / rương / ngăn kéo / két sắt có khóa (Chest, Safe, Cabinet, Drawer):
/// - Yêu cầu bẻ khóa (Lockpick Minigame) để mở.
/// - Đảm bảo chỉ 1 người chơi tương tác bẻ khóa tại 1 thời điểm.
/// - Bẻ khóa sai: Phát tiếng động cảnh báo NPC gần đó chạy lại.
/// - Bẻ khóa đúng: Mở vĩnh viễn (xoay nắp rương/cánh tủ hoặc trượt ngăn kéo) và sinh Item thưởng (Bonus Loot).
/// - Item sinh ra KHÔNG bị tính vào Total Point qua màn.
/// </summary>
public class LockedContainerController : MonoBehaviour, IInteractable, ILockpickable
{
    public enum OpenAnimationType
    {
        Rotate,     // Xoay quanh trục (Rương, Cánh tủ, Cửa két sắt)
        Slide       // Trượt theo hướng (Ngăn kéo bàn, Hộc tủ)
    }

    [Header("📦 Display & Interaction")]
    public string containerName = "Locked Chest";
    public string actionPrompt = "Pick Lock";
    [TextArea(2, 4)]
    public string description = "A locked container. Pick the lock to get bonus loot!";
    public Sprite containerIcon;

    [Header("🔐 Lock Status")]
    public bool isUnlocked = false;
    [HideInInspector] public bool isBeingLockpicked = false;

    [Header("🚪 Open Animation Settings")]
    [Tooltip("Dạng chuyển động khi mở: Xoay nắp (Rotate) hoặc Trượt hộc (Slide)")]
    public OpenAnimationType animationType = OpenAnimationType.Rotate;

    [Tooltip("Transform của nắp rương / cánh cửa tủ / hộc ngăn kéo cần di chuyển")]
    public Transform movingPart;

    [Tooltip("Góc xoay khi mở (áp dụng khi animationType = Rotate, vd: X= -90 hoặc Y= 90)")]
    public Vector3 openRotationOffset = new Vector3(-90f, 0f, 0f);

    [Tooltip("Khoảng cách trượt khi mở (áp dụng khi animationType = Slide, vd: Z= 0.4)")]
    public Vector3 openPositionOffset = new Vector3(0f, 0f, 0.4f);

    public float openSpeed = 2.5f;
    public bool smoothOpen = true;

    [Tooltip("Tắt bớt Collider của nắp sau khi mở để dễ ngắm nhặt đồ bên trong")]
    public bool disableLidColliderWhenOpen = false;

    [Header("📍 Item Spawn Settings")]
    [Tooltip("Danh sách các Prefab Item riêng cho rương/tủ này. Nếu để TRỐNG, sẽ tự động lấy từ danh sách Item của Chapter hiện tại!")]
    public List<GameObject> customContainerItems = new List<GameObject>();

    [Tooltip("Danh sách các vị trí Transform bên trong rương/tủ để spawn item")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Range(0.05f, 1f)]
    [Tooltip("Tỉ lệ % số điểm spawn trong rương sẽ xuất hiện đồ (1 = 100% full mọi điểm)")]
    public float spawnRatio = 1f;

    [Tooltip("Độ lệch vị trí nhỏ khi spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 0.02f, 0f);

    [Tooltip("Cấu hình trọng số Rarity riêng cho rương này (để trống nếu muốn ưu tiên đồ xịn mặc định)")]
    public List<RarityWeightConfig> rarityWeights = new List<RarityWeightConfig>()
    {
        new RarityWeightConfig(ItemRarity.Common, 15f),
        new RarityWeightConfig(ItemRarity.Uncommon, 35f),
        new RarityWeightConfig(ItemRarity.Rare, 30f),
        new RarityWeightConfig(ItemRarity.Epic, 15f),
        new RarityWeightConfig(ItemRarity.Legendary, 5f)
    };

    [Header("🔊 3D Spatial Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip victorySound;
    public AudioClip openSound;

    private Quaternion closedLocalRotation;
    private Vector3 closedLocalPosition;
    private Coroutine animCoroutine;
    private UI_Manager uiManager;
    private bool hasSpawnedItems = false;

    private void Awake()
    {
        InitializeAudio();

        if (movingPart == null && transform.childCount > 0)
        {
            movingPart = transform.Find("Lid") ?? transform.Find("Door") ?? transform.Find("Drawer") ?? transform.GetChild(0);
        }

        if (movingPart != null)
        {
            closedLocalRotation = movingPart.localRotation;
            closedLocalPosition = movingPart.localPosition;
        }

        // Tự tìm các điểm spawn con nếu chưa gán
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Transform spawnRoot = transform.Find("SpawnPoints") ?? transform.Find("Items");
            if (spawnRoot != null)
            {
                foreach (Transform child in spawnRoot)
                {
                    spawnPoints.Add(child);
                }
            }
        }
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UI_Manager>();
    }

    private void InitializeAudio()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.85f;
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 25f;
            audioSource.playOnAwake = false;
        }

        if (audioSource != null && audioSource.outputAudioMixerGroup == null && SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            audioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
        }
    }

    public void PlayLocalSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // =========================================================================
    //                        LOCKPICKING LOGIC
    // =========================================================================

    public void StartLockpicking()
    {
        if (isUnlocked || isBeingLockpicked) return;

        var netSync = GetComponent<NetworkLockedContainerSync>();
        if (netSync != null && netSync.IsNetworkSpawned)
        {
            netSync.RpcRequestSetLockpicking(true);
        }

        isBeingLockpicked = true;

        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null) uiManager.StartLockpicking(this);
    }

    public void OnUnlockSuccess()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        isBeingLockpicked = false;

        Debug.Log($"<color=green>[LockedContainerController] Bẻ khóa thành công '{containerName}'!</color>");

        var netSync = GetComponent<NetworkLockedContainerSync>();
        if (netSync != null && netSync.IsNetworkSpawned)
        {
            netSync.RpcPlayAudio(NetworkLockedContainerSync.AudioType.Victory);
            netSync.RpcRequestUnlock();
        }
        else
        {
            PlayLocalSound(victorySound);
            ApplyOpenVisual(true);
            SpawnLootItems();
        }
    }

    public void OnUnlockFailed()
    {
        Debug.LogWarning($"[LockedContainerController] Bẻ khóa THẤT BẠI '{containerName}' -> Báo động NPC!");
        CancelLockpicking();
        AlertNearbyNPCs();
    }

    public void CancelLockpicking()
    {
        isBeingLockpicked = false;
        var netSync = GetComponent<NetworkLockedContainerSync>();
        if (netSync != null && netSync.IsNetworkSpawned)
        {
            netSync.RpcRequestSetLockpicking(false);
        }
    }

    /// <summary>
    /// Phát tiếng động báo động NPC khi bẻ khóa trượt
    /// </summary>
    public void AlertNearbyNPCs()
    {
        var emitter = GetComponent<NPCAlertEmitter>();
        if (emitter != null)
        {
            emitter.TriggerAlert();
        }
        else
        {
            NPCAlertSystem.EmitNoise(transform.position, alertAdults: true, alertKids: true, isFromPlayer: true);
        }
    }

    public void PlayHitSound()
    {
        var netSync = GetComponent<NetworkLockedContainerSync>();
        if (netSync != null && netSync.IsNetworkSpawned)
        {
            netSync.RpcPlayAudio(NetworkLockedContainerSync.AudioType.Hit);
        }
        else
        {
            PlayLocalSound(hitSound);
        }
    }

    public void PlayMissSound()
    {
        var netSync = GetComponent<NetworkLockedContainerSync>();
        if (netSync != null && netSync.IsNetworkSpawned)
        {
            netSync.RpcPlayAudio(NetworkLockedContainerSync.AudioType.Miss);
        }
        else
        {
            PlayLocalSound(missSound);
        }
    }

    // =========================================================================
    //                        OPEN ANIMATION & LOOT SPAWN
    // =========================================================================

    public void ApplyOpenVisual(bool playSound)
    {
        if (playSound && openSound != null)
        {
            PlayLocalSound(openSound);
        }

        if (movingPart == null) return;

        if (disableLidColliderWhenOpen)
        {
            Collider lidCol = movingPart.GetComponent<Collider>();
            if (lidCol != null) lidCol.enabled = false;
        }

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (smoothOpen && gameObject.activeInHierarchy)
        {
            animCoroutine = StartCoroutine(OpenAnimationRoutine());
        }
        else
        {
            if (animationType == OpenAnimationType.Rotate)
            {
                movingPart.localRotation = closedLocalRotation * Quaternion.Euler(openRotationOffset);
            }
            else
            {
                movingPart.localPosition = closedLocalPosition + openPositionOffset;
            }
        }
    }

    private IEnumerator OpenAnimationRoutine()
    {
        Quaternion targetRot = closedLocalRotation * Quaternion.Euler(openRotationOffset);
        Vector3 targetPos = closedLocalPosition + openPositionOffset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            if (animationType == OpenAnimationType.Rotate)
            {
                movingPart.localRotation = Quaternion.Slerp(movingPart.localRotation, targetRot, t);
            }
            else
            {
                movingPart.localPosition = Vector3.Lerp(movingPart.localPosition, targetPos, t);
            }
            yield return null;
        }

        if (animationType == OpenAnimationType.Rotate)
            movingPart.localRotation = targetRot;
        else
            movingPart.localPosition = targetPos;
    }

    /// <summary>
    /// Sinh các Item thưởng (Bonus Loot) bên trong rương/tủ.
    /// Hoàn toàn độc lập và KHÔNG bị tính vào Total Point của màn chơi.
    /// </summary>
    public void SpawnLootItems()
    {
        if (hasSpawnedItems) return;
        hasSpawnedItems = true;

        if (spawnPoints == null || spawnPoints.Count == 0) return;

        // 1. Xác định nguồn Item Prefabs: Ưu tiên customContainerItems gán trên rương, nếu rỗng thì lấy từ ChapterSO
        List<GameObject> pool = (customContainerItems != null && customContainerItems.Count > 0) 
            ? customContainerItems.Where(x => x != null).ToList() 
            : null;

        if (pool == null || pool.Count == 0)
        {
            ChapterSO chapter = GameSession.SelectedChapter;
            if (chapter == null)
            {
                SceneItemSpawner spawner = FindFirstObjectByType<SceneItemSpawner>();
                if (spawner != null) chapter = spawner.chapterData;
            }
            if (chapter != null && chapter.spawnableItems != null)
            {
                pool = chapter.spawnableItems.Where(x => x != null).ToList();
            }
        }

        if (pool == null || pool.Count == 0) return;

        // 1.1. Khởi tạo Seed ngẫu nhiên đồng bộ (Deterministic) dựa theo Seed của phòng chơi và Vị trí của Rương
        int baseSeed = 12345;
        if (FusionConnectionManager.Instance != null &&
            FusionConnectionManager.Instance.IsInGameplaySession &&
            FusionConnectionManager.Instance.currentRunner != null &&
            FusionConnectionManager.Instance.currentRunner.SessionInfo != null)
        {
            var session = FusionConnectionManager.Instance.currentRunner.SessionInfo;
            if (session.Properties != null && session.Properties.TryGetValue("seed", out var seedProp))
            {
                baseSeed = (int)seedProp;
            }
        }
        int containerPosSeed = Mathf.RoundToInt(transform.position.x * 100f) ^ (Mathf.RoundToInt(transform.position.z * 100f) << 8);
        int containerSeed = baseSeed ^ containerPosSeed ^ name.GetHashCode();

        // Lưu lại state Random cũ để tránh ảnh hưởng logic khác
        Random.State oldState = Random.state;
        Random.InitState(containerSeed);

        // Sắp xếp pool cố định theo tên prefab để tránh thứ tự không đồng nhất giữa các máy
        pool = pool.OrderBy(p => p.name).ToList();

        // 2. Phân loại item theo rarity
        Dictionary<ItemRarity, List<GameObject>> categorized = new Dictionary<ItemRarity, List<GameObject>>();
        foreach (ItemRarity r in System.Enum.GetValues(typeof(ItemRarity)))
        {
            categorized[r] = new List<GameObject>();
        }

        foreach (var p in pool)
        {
            if (p == null) continue;
            Item it = p.GetComponent<Item>();
            ItemRarity r = (it != null && it.itemData != null) ? it.itemData.rarity : (it != null ? it.rarity : ItemRarity.Common);
            categorized[r].Add(p);
        }

        // 3. Lấy danh sách điểm spawn hợp lệ (giữ nguyên thứ tự danh sách điểm spawn để nhất quán trên mọi máy)
        List<Transform> validPoints = spawnPoints.Where(p => p != null).ToList();
        int countToSpawn = Mathf.Clamp(Mathf.RoundToInt(validPoints.Count * Mathf.Clamp01(spawnRatio)), 1, validPoints.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            Transform pt = validPoints[i];
            GameObject prefab = PickPrefab(pool, categorized);
            if (prefab != null)
            {
                Vector3 pos = pt.position + spawnOffset;
                Quaternion rot = pt.rotation * Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f);

                GameObject instance = Instantiate(prefab, pos, rot, transform);
                // Đặt tên định danh duy nhất theo index điểm spawn và tên rương để mọi máy tìm thấy chính xác khi nhặt
                instance.name = $"ContainerItem_{name}_{i}_{prefab.name}";

                Item itemComp = instance.GetComponent<Item>();
                if (itemComp != null)
                {
                    itemComp.InitializeStats();
                }

                // Triệt tiêu vận tốc Rigidbody để item nằm yên trong rương
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
            }
        }

        // Khôi phục lại Random State
        Random.state = oldState;
    }

    private GameObject PickPrefab(List<GameObject> pool, Dictionary<ItemRarity, List<GameObject>> categorized)
    {
        float totalWeight = 0f;
        foreach (var cfg in rarityWeights)
        {
            if (categorized.ContainsKey(cfg.rarity) && categorized[cfg.rarity].Count > 0)
            {
                totalWeight += cfg.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        float roll = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        ItemRarity selectedRarity = ItemRarity.Common;

        foreach (var cfg in rarityWeights)
        {
            if (categorized.ContainsKey(cfg.rarity) && categorized[cfg.rarity].Count > 0)
            {
                currentSum += cfg.weight;
                if (roll <= currentSum)
                {
                    selectedRarity = cfg.rarity;
                    break;
                }
            }
        }

        var candidates = categorized[selectedRarity];
        if (candidates != null && candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        return pool[Random.Range(0, pool.Count)];
    }

    // =========================================================================
    //                        IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => containerName;
    public string GetActionPrompt() => actionPrompt;
    public int GetPrice() => 0;
    public int GetWeight() => 0;
    public bool IsLootItem() => false;
    public string GetDescription() => description;
    public Sprite GetIcon() => containerIcon;
    public ItemRarity GetRarity() => ItemRarity.Rare;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        var netSync = GetComponent<NetworkLockedContainerSync>();
        bool isNetworkActive = netSync != null && netSync.IsNetworkSpawned;

        bool effectiveUnlocked = isNetworkActive ? (bool)netSync.NetworkIsUnlocked : isUnlocked;
        bool effectiveLocking = isNetworkActive ? (bool)netSync.NetworkIsBeingLockpicked : isBeingLockpicked;

        if (effectiveUnlocked)
        {
            failReason = "Already Unlocked";
            return false;
        }
        if (effectiveLocking)
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
        var netSync = GetComponent<NetworkLockedContainerSync>();
        bool isNetworkActive = netSync != null && netSync.IsNetworkSpawned;

        bool effectiveUnlocked = isNetworkActive ? (bool)netSync.NetworkIsUnlocked : isUnlocked;
        bool effectiveLocking = isNetworkActive ? (bool)netSync.NetworkIsBeingLockpicked : isBeingLockpicked;

        if (!effectiveUnlocked && !UI_Manager.isSolving && !effectiveLocking)
        {
            if (isNetworkActive)
            {
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && !netObj.HasStateAuthority)
                {
                    netObj.RequestStateAuthority();
                }
            }

            StartLockpicking();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (spawnPoints != null)
        {
            foreach (var pt in spawnPoints)
            {
                if (pt != null)
                {
                    Gizmos.DrawSphere(pt.position + spawnOffset, 0.08f);
                    Gizmos.DrawWireCube(pt.position + spawnOffset, Vector3.one * 0.15f);
                }
            }
        }
    }
}
