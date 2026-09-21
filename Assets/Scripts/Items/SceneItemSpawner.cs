using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Cấu hình tỉ lệ xuất hiện (Drop Weight) cho từng bậc độ hiếm
/// </summary>
[System.Serializable]
public class RarityWeightConfig
{
    public ItemRarity rarity = ItemRarity.Common;
    [Range(0f, 100f)]
    [Tooltip("Trọng số xuất hiện (Weight). Càng cao tỉ lệ ra càng nhiều.")]
    public float weight = 50f;

    public RarityWeightConfig(ItemRarity r, float w)
    {
        rarity = r;
        weight = w;
    }
}

/// <summary>
/// Component quản lý và sinh vật phẩm (Item Spawner) trong Scene dựa trên:
/// - Danh sách các vị trí Spawn Points đặt trong Scene.
/// - Cấu hình danh sách Item và độ hiếm (Rarity) từ ChapterSO / GameSession.
/// </summary>
public class SceneItemSpawner : MonoBehaviour
{
    [Header("📖 Chapter Reference")]
    [Tooltip("Cấu hình Chapter (nếu để trống, sẽ tự động lấy từ GameSession.SelectedChapter khi vào game)")]
    public ChapterSO chapterData;

    [Header("📍 Scene Spawn Points")]
    [Tooltip("Danh sách các Transform điểm spawn đặt trong Scene")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("⚙️ Spawning Settings")]
    [Tooltip("Tự động spawn ngay khi Scene khởi động (Start)")]
    public bool spawnOnStart = true;

    [Tooltip("Ngẫu nhiên xáo trộn thứ tự các điểm spawn")]
    public bool shuffleSpawnPoints = true;

    [Tooltip("Độ lệch vị trí nhỏ khi spawn để tránh chìm vật phẩm vào bề mặt")]
    public Vector3 spawnOffset = new Vector3(0f, 0.05f, 0f);

    [Header("🎲 Default Rarity Weights (Nếu Chapter không cấu hình riêng)")]
    public List<RarityWeightConfig> defaultRarityWeights = new List<RarityWeightConfig>()
    {
        new RarityWeightConfig(ItemRarity.Trash, 20f),
        new RarityWeightConfig(ItemRarity.Common, 45f),
        new RarityWeightConfig(ItemRarity.Uncommon, 20f),
        new RarityWeightConfig(ItemRarity.Rare, 10f),
        new RarityWeightConfig(ItemRarity.Epic, 4f),
        new RarityWeightConfig(ItemRarity.Legendary, 1f),
        new RarityWeightConfig(ItemRarity.Mythic, 0.2f)
    };

    [Header("📦 Spawned Instances")]
    [SerializeField] private List<GameObject> spawnedItems = new List<GameObject>();

    // Bảng phân loại Item theo Rarity để truy xuất nhanh
    private Dictionary<ItemRarity, List<GameObject>> categorizedPrefabs = new Dictionary<ItemRarity, List<GameObject>>();

    private void Awake()
    {
        // Tự động nhận Chapter từ GameSession nếu có
        if (GameSession.SelectedChapter != null)
        {
            chapterData = GameSession.SelectedChapter;
        }

        // Tự động tìm các Transform con nếu danh sách spawnPoints đang trống
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            CollectChildSpawnPoints();
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllItems();
        }
    }

    /// <summary>
    /// Thu thập toàn bộ Transform con làm điểm Spawn (có thể click chuột phải trong Inspector)
    /// </summary>
    [ContextMenu("Auto Collect Child Spawn Points")]
    public void CollectChildSpawnPoints()
    {
        spawnPoints.Clear();
        foreach (Transform child in transform)
        {
            if (child != transform)
            {
                spawnPoints.Add(child);
            }
        }
        Debug.Log($"[SceneItemSpawner] Đã thu thập {spawnPoints.Count} điểm spawn từ các đối tượng con.");
    }

    /// <summary>
    /// Tiến hành Spawn toàn bộ Item vào các vị trí trong Scene dựa theo ChapterSO và Rarity
    /// </summary>
    [ContextMenu("Spawn All Items Now")]
    public void SpawnAllItems()
    {
        ClearSpawnedItems();

        if (chapterData == null)
        {
            Debug.LogWarning("[SceneItemSpawner] Chưa gán ChapterSO và GameSession.SelectedChapter là null!");
            return;
        }

        // 1. Phân loại danh sách Prefabs trong Chapter theo từng độ hiếm (Rarity)
        CategorizePrefabs();

        if (chapterData.spawnableItems == null || chapterData.spawnableItems.Count == 0)
        {
            Debug.LogWarning($"[SceneItemSpawner] Chapter '{chapterData.chapterTitle}' không có Prefab nào trong danh sách spawnableItems.");
            return;
        }

        // 2. Chuẩn bị danh sách vị trí Spawn từ SpawnPoints Transform
        List<Vector3> targetPositions = new List<Vector3>();
        foreach (var pt in spawnPoints)
        {
            if (pt != null) targetPositions.Add(pt.position);
        }

        if (targetPositions.Count == 0)
        {
            Debug.LogWarning("[SceneItemSpawner] Không tìm thấy vị trí Spawn nào trong Scene!");
            return;
        }

        // 3. Xáo trộn ngẫu nhiên thứ tự vị trí nếu được bật
        if (shuffleSpawnPoints)
        {
            targetPositions = targetPositions.OrderBy(x => Random.value).ToList();
        }

        // 4. Tiến hành chọn Item theo Rarity và Instantiate tại tất cả các vị trí
        for (int i = 0; i < targetPositions.Count; i++)
        {
            Vector3 pos = targetPositions[i] + spawnOffset;
            GameObject prefabToSpawn = PickItemPrefabByRarity();

            if (prefabToSpawn != null)
            {
                Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject instance = Instantiate(prefabToSpawn, pos, rot, transform);
                
                // Khởi tạo chỉ số ngẫu nhiên cho Item
                Item itemComp = instance.GetComponent<Item>();
                if (itemComp != null)
                {
                    itemComp.InitializeStats();
                }

                spawnedItems.Add(instance);
            }
        }

        Debug.Log($"[SceneItemSpawner] Đã spawn thành công {spawnedItems.Count} vật phẩm cho '{chapterData.chapterTitle}'!");
    }

    /// <summary>
    /// Phân loại toàn bộ spawnableItems trong ChapterSO theo từng Rarity
    /// </summary>
    private void CategorizePrefabs()
    {
        categorizedPrefabs.Clear();

        // Khởi tạo danh sách cho tất cả các Rarity
        foreach (ItemRarity r in System.Enum.GetValues(typeof(ItemRarity)))
        {
            categorizedPrefabs[r] = new List<GameObject>();
        }

        if (chapterData == null || chapterData.spawnableItems == null) return;

        foreach (var prefab in chapterData.spawnableItems)
        {
            if (prefab == null) continue;

            Item itemComp = prefab.GetComponent<Item>();
            ItemRarity rarity = ItemRarity.Common;

            if (itemComp != null)
            {
                if (itemComp.itemData != null)
                {
                    rarity = itemComp.itemData.rarity;
                }
                else
                {
                    rarity = itemComp.rarity;
                }
            }

            categorizedPrefabs[rarity].Add(prefab);
        }
    }

    /// <summary>
    /// Chọn ngẫu nhiên 1 Prefab dựa trên bảng trọng số xác suất độ hiếm (Weighted Random)
    /// </summary>
    private GameObject PickItemPrefabByRarity()
    {
        if (chapterData == null || chapterData.spawnableItems == null || chapterData.spawnableItems.Count == 0) return null;

        // 1. Tính tổng trọng số
        float totalWeight = 0f;
        foreach (var cfg in defaultRarityWeights)
        {
            // Chỉ tính trọng số của những Rarity thực sự có item trong danh sách
            if (categorizedPrefabs.ContainsKey(cfg.rarity) && categorizedPrefabs[cfg.rarity].Count > 0)
            {
                totalWeight += cfg.weight;
            }
        }

        // Fallback nếu không có trọng số hợp lệ
        if (totalWeight <= 0f)
        {
            return chapterData.spawnableItems[Random.Range(0, chapterData.spawnableItems.Count)];
        }

        // 2. Random giá trị từ 0 đến totalWeight
        float roll = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        ItemRarity selectedRarity = ItemRarity.Common;

        foreach (var cfg in defaultRarityWeights)
        {
            if (categorizedPrefabs.ContainsKey(cfg.rarity) && categorizedPrefabs[cfg.rarity].Count > 0)
            {
                currentSum += cfg.weight;
                if (roll <= currentSum)
                {
                    selectedRarity = cfg.rarity;
                    break;
                }
            }
        }

        // 3. Chọn 1 prefab ngẫu nhiên trong nhóm Rarity đã trúng thưởng
        var candidates = categorizedPrefabs[selectedRarity];
        if (candidates != null && candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        // Fallback lấy ngẫu nhiên 1 item bất kỳ
        return chapterData.spawnableItems[Random.Range(0, chapterData.spawnableItems.Count)];
    }

    /// <summary>
    /// Dọn dẹp toàn bộ vật phẩm đã spawn
    /// </summary>
    [ContextMenu("Clear Spawned Items")]
    public void ClearSpawnedItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(spawnedItems[i]);
                }
                else
                {
                    DestroyImmediate(spawnedItems[i]);
                }
            }
        }
        spawnedItems.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.7f); // Vàng sáng
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Gizmos.DrawSphere(spawnPoints[i].position + spawnOffset, 0.2f);
                    Gizmos.DrawWireCube(spawnPoints[i].position + spawnOffset, Vector3.one * 0.3f);
                }
            }
        }
    }
}
