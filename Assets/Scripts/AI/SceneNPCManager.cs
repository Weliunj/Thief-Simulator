using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý bật/tắt (SetActive) các NPC đặt sẵn trong Scene dựa theo ChapterSO và Cấp độ khó (Easy, Normal, Hard):
/// - Thu thập toàn bộ NPC (AdultGuardNPC, KidRunnerNPC) trong Scene.
/// - Đọc cấu hình activeNpcKeywords từ ChapterSO.GetDifficultySetting(GameSession.SelectedDifficulty).
/// - Nếu danh sách activeNpcKeywords trống -> Mặc định BẬT tất cả NPC trong Scene.
/// - Nếu có cấu hình danh sách -> Chỉ BẬT các NPC có tên chứa từ khóa tương ứng, các NPC còn lại sẽ bị TẮT.
/// </summary>
public class SceneNPCManager : MonoBehaviour
{
    [Header("📖 Chapter Reference")]
    [Tooltip("Cấu hình Chapter (nếu để trống, sẽ tự động lấy từ GameSession.SelectedChapter khi vào game)")]
    public ChapterSO chapterData;

    [Header("⚙️ Settings")]
    [Tooltip("Tự động cấu hình NPC ngay khi bắt đầu Scene (Start)")]
    public bool configureOnStart = true;

    [Header("👥 Managed Scene NPCs")]
    [Tooltip("Danh sách NPC chịu sự kiểm soát của độ khó (nếu kéo tay vào đây, chỉ những con trong list này mới bị bật/tắt, những con không nằm trong list sẽ GIỮ NGUYÊN trạng thái). Nếu để trống list này và autoCollectAllSceneNPCs=true thì mới tự quét toàn bộ.")]
    public List<GameObject> sceneNpcObjects = new List<GameObject>();

    [Header("⭐ Special NPCs Toggle Option")]
    [Tooltip("Từ khóa để nhận diện các NPC đặc biệt (VD: 'Special')")]
    public string specialNpcKeyword = "Special";

    [Tooltip("Nếu danh sách sceneNpcObjects để trống, tự động quét toàn bộ NPC trong Scene để quản lý")]
    public bool autoCollectAllSceneNPCs = false;

    private void Awake()
    {
        if (GameSession.SelectedChapter != null)
        {
            chapterData = GameSession.SelectedChapter;
        }

        // Chỉ tự động tìm tất cả NPC nếu sceneNpcObjects trống VÀ được bật autoCollectAllSceneNPCs
        if ((sceneNpcObjects == null || sceneNpcObjects.Count == 0) && autoCollectAllSceneNPCs)
        {
            CollectSceneNPCs();
        }
    }

    private void Start()
    {
        if (configureOnStart)
        {
            ApplyDifficultyToNPCs();
        }
    }

    /// <summary>
    /// Thu thập tất cả GameObject chứa AdultGuardNPC hoặc KidRunnerNPC trong Scene (tự động lấy root container nếu có)
    /// </summary>
    [ContextMenu("Collect Scene NPCs")]
    public void CollectSceneNPCs()
    {
        sceneNpcObjects.Clear();

        var adults = FindObjectsByType<AdultGuardNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var adult in adults)
        {
            if (adult != null)
            {
                // Nếu NPC nằm trong một GameObject cha quản lý cụm (VD: SeekNPC_Easy chứa Seek, PatrolPoints...)
                // thì ưu tiên lấy GameObject cha cao nhất để bật/tắt toàn bộ cụm
                GameObject targetObj = GetNpcRootContainer(adult.gameObject);
                if (!sceneNpcObjects.Contains(targetObj))
                {
                    sceneNpcObjects.Add(targetObj);
                }
            }
        }

        var kids = FindObjectsByType<KidRunnerNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var kid in kids)
        {
            if (kid != null)
            {
                GameObject targetObj = GetNpcRootContainer(kid.gameObject);
                if (!sceneNpcObjects.Contains(targetObj))
                {
                    sceneNpcObjects.Add(targetObj);
                }
            }
        }

        Debug.Log($"<color=cyan>[SceneNPCManager] Đã thu thập {sceneNpcObjects.Count} NPC / Cụm NPC quản lý theo độ khó.</color>");
    }

    /// <summary>
    /// Tìm GameObject gốc chứa cụm NPC (nếu có cha quản lý riêng) hoặc chính nó
    /// </summary>
    private GameObject GetNpcRootContainer(GameObject npcObj)
    {
        if (npcObj == null) return null;
        Transform current = npcObj.transform;

        // Nếu có cha và cha không phải là toàn bộ Map / NPC Manager
        while (current.parent != null)
        {
            Transform p = current.parent;
            string pName = p.name.ToLower();
            // Nếu cha là SceneNPCManager hoặc Map / Environment root thì dừng lại ở current
            if (p.GetComponent<SceneNPCManager>() != null || pName.Contains("environment") || pName.Contains("map") || pName == "npcs")
            {
                break;
            }
            current = p;
        }

        return current.gameObject;
    }

    /// <summary>
    /// Áp dụng bật/tắt NPC dựa theo độ khó đã chọn
    /// </summary>
    [ContextMenu("Apply Difficulty To NPCs")]
    public void ApplyDifficultyToNPCs()
    {
        if (chapterData == null)
        {
            if (GameSession.SelectedChapter != null) chapterData = GameSession.SelectedChapter;
        }

        // Nếu danh sách sceneNpcObjects rỗng và bật autoCollect -> quét tự động
        if ((sceneNpcObjects == null || sceneNpcObjects.Count == 0) && autoCollectAllSceneNPCs)
        {
            CollectSceneNPCs();
        }

        if (sceneNpcObjects == null || sceneNpcObjects.Count == 0)
        {
            Debug.Log("[SceneNPCManager] Không có NPC nào trong danh sách quản lý theo độ khó. Các NPC ngoài danh sách giữ nguyên trạng thái!");
            return;
        }

        // Đồng bộ Độ khó & Tùy chọn Special NPCs từ Fusion Session Properties (nếu đang trong phòng Online)
        if (FusionConnectionManager.Instance != null &&
            FusionConnectionManager.Instance.IsInGameplaySession &&
            FusionConnectionManager.Instance.currentRunner != null &&
            FusionConnectionManager.Instance.currentRunner.SessionInfo != null)
        {
            var session = FusionConnectionManager.Instance.currentRunner.SessionInfo;
            if (session.Properties != null)
            {
                if (session.Properties.TryGetValue("diff", out var diffProp))
                {
                    GameSession.SelectedDifficulty = (GameDifficulty)(int)diffProp;
                }
                if (session.Properties.TryGetValue("spec_npc", out var specProp))
                {
                    GameSession.EnableSpecialNPCs = ((int)specProp == 1);
                }
            }
        }

        GameDifficulty difficulty = GameSession.SelectedDifficulty;
        ChapterDifficultySetting setting = (chapterData != null) ? chapterData.GetDifficultySetting(difficulty) : null;
        bool enableSpecialNPCs = GameSession.EnableSpecialNPCs;
        string lowerSpecialKw = !string.IsNullOrEmpty(specialNpcKeyword) ? specialNpcKeyword.ToLower() : "";

        int activeCount = 0;
        int inactiveCount = 0;

        // Nếu không có ChapterSO hoặc danh sách activeNpcKeywords để trống -> Bật tất cả
        bool enableAll = (setting == null || setting.activeNpcKeywords == null || setting.activeNpcKeywords.Count == 0);

        foreach (var npc in sceneNpcObjects)
        {
            if (npc == null) continue;

            // Lấy tên GameObject cha / Root trong Hierarchy
            string rootName = npc.name.ToLower();

            // Lấy tên các GameObject con (như Seek)
            string childNames = "";
            foreach (Transform child in npc.transform)
            {
                childNames += " " + child.name.ToLower();
            }

            // Lấy tên cấu hình trong Component AdultGuardNPC / KidRunnerNPC (nếu có)
            string componentNpcName = "";
            var adult = npc.GetComponent<AdultGuardNPC>() ?? npc.GetComponentInChildren<AdultGuardNPC>(true);
            if (adult != null && !string.IsNullOrEmpty(adult.npcName)) componentNpcName = adult.npcName.ToLower();

            var kid = npc.GetComponent<KidRunnerNPC>() ?? npc.GetComponentInChildren<KidRunnerNPC>(true);
            if (kid != null && !string.IsNullOrEmpty(kid.npcName)) componentNpcName = kid.npcName.ToLower();

            bool isSpecialNpc = (!string.IsNullOrEmpty(lowerSpecialKw)) &&
                                (rootName.Contains(lowerSpecialKw) || childNames.Contains(lowerSpecialKw) || (!string.IsNullOrEmpty(componentNpcName) && componentNpcName.Contains(lowerSpecialKw)));

            // Kiểm tra NPC có khớp với danh sách từ khóa độ khó (Easy, Normal, Hard) của ChapterSO hay không
            bool matchesDifficulty = enableAll;
            if (!enableAll && setting != null && setting.activeNpcKeywords != null)
            {
                foreach (var kw in setting.activeNpcKeywords)
                {
                    if (!string.IsNullOrEmpty(kw))
                    {
                        string lowerKw = kw.ToLower();
                        if (rootName.Contains(lowerKw) || childNames.Contains(lowerKw) || (!string.IsNullOrEmpty(componentNpcName) && componentNpcName.Contains(lowerKw)))
                        {
                            matchesDifficulty = true;
                            break;
                        }
                    }
                }
            }

            bool shouldActivate = false;

            // 1. Nếu là NPC Đặc Biệt (Special NPC) -> Phải thỏa mãn CẢ 2 điều kiện:
            //    - Toggle enableSpecialNPCs phải BẬT (ON)
            //    - Phải khớp với độ khó hiện tại (VD: Special_Easy, Special_Normal, Special_Hard)
            if (isSpecialNpc)
            {
                shouldActivate = enableSpecialNPCs && matchesDifficulty;
            }
            // 2. Nếu là NPC thông thường -> Chỉ cần khớp độ khó hiện tại
            else
            {
                shouldActivate = matchesDifficulty;
            }

            npc.SetActive(shouldActivate);

            if (shouldActivate) activeCount++;
            else inactiveCount++;
        }

        Debug.Log($"<color=yellow>[SceneNPCManager] Đã cấu hình NPC theo Độ khó [{difficulty}] (Special NPCs: {(enableSpecialNPCs ? "BẬT" : "TẮT")}): Bật {activeCount} NPC, Tắt {inactiveCount} NPC.</color>");
    }
}
