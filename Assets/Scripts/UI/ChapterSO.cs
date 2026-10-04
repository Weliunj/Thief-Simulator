using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấu hình thông số chi tiết cho từng cấp độ khó của Chapter
/// </summary>
[System.Serializable]
public class ChapterDifficultySetting
{
    [Header("🎯 Target & Item Spawning")]
    [Range(0.2f, 1f)]
    [Tooltip("Tỉ lệ % giá trị tiền cần gom để qua màn (VD: 0.5 = 50%, 0.75 = 75%, 0.9 = 90%)")]
    public float targetPointPercentage = 0.75f;

    [Range(0.1f, 1f)]
    [Tooltip("Tỉ lệ % số điểm spawn sẽ xuất hiện đồ trong Scene (VD: 1.0 = 100% full bàn, 0.7 = 70%)")]
    public float itemSpawnRatio = 0.8f;

    [Header("⏰ Time Limit")]
    [Tooltip("Thời gian tối đa (giây) cho màn chơi ở độ khó này")]
    public float maxTime = 300f;

    [Header("👥 Active NPCs in Scene")]
    [Tooltip("Danh sách tên hoặc từ khóa của các GameObject NPC trong Scene sẽ được BẬT (SetActive(true)) ở độ khó này. Bỏ trống = Bật tất cả NPC.")]
    public List<string> activeNpcKeywords = new List<string>();

    public ChapterDifficultySetting(float targetPercent, float spawnRatio, float time, params string[] npcKeywords)
    {
        targetPointPercentage = targetPercent;
        itemSpawnRatio = spawnRatio;
        maxTime = time;
        if (npcKeywords != null && npcKeywords.Length > 0)
        {
            activeNpcKeywords = new List<string>(npcKeywords);
        }
    }
}

[CreateAssetMenu(fileName = "NewChapterData", menuName = "Thief Simulator/Chapter Data")]
public class ChapterSO : ScriptableObject
{
    [Header("📌 Chapter Info")]
    public string chapterTitle = "Chapter 1: Small Town";
    public Sprite chapterImage;
    public string sceneName = "Intro";       // Scene Intro Cutscene
    public string gameplaySceneName = "Lv1"; // Scene Gameplay trực tiếp khi Skip Intro

    [Header("📝 Description")]
    [TextArea(3, 6)]
    public string description = "A quiet suburban town filled with valuable loot. Sneak past patrolling adults, pick locked doors, and gather target points before time runs out!";

    [Header("🔒 Lock & Completion Status")]
    public bool isUnlocked = true;
    public bool isCompleted = false;        // Đánh dấu màn chơi đã hoàn thành hay chưa

    [Header("⏰ Default Fallback Time")]
    [Tooltip("Thời gian tối đa (giây) cho màn chơi (nếu không dùng hệ thống độ khó)")]
    public float maxTime = 300f;

    [Header("🎁 Spawnable Items")]
    [Tooltip("Danh sách các Prefab vật phẩm có thể xuất hiện trong Chapter này")]
    public List<GameObject> spawnableItems = new List<GameObject>();

    [Header("🎮 Difficulty Configurations (Easy, Normal, Hard)")]
    public ChapterDifficultySetting easyDifficulty = new ChapterDifficultySetting(0.5f, 1.0f, 360f);
    public ChapterDifficultySetting normalDifficulty = new ChapterDifficultySetting(0.75f, 0.8f, 300f);
    public ChapterDifficultySetting hardDifficulty = new ChapterDifficultySetting(0.9f, 0.6f, 240f);

    /// <summary>
    /// Lấy cấu hình độ khó tương ứng từ GameDifficulty
    /// </summary>
    public ChapterDifficultySetting GetDifficultySetting(GameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case GameDifficulty.Easy:
                return easyDifficulty ?? new ChapterDifficultySetting(0.5f, 1.0f, 360f);
            case GameDifficulty.Hard:
                return hardDifficulty ?? new ChapterDifficultySetting(0.9f, 0.6f, 240f);
            case GameDifficulty.Normal:
            default:
                return normalDifficulty ?? new ChapterDifficultySetting(0.75f, 0.8f, 300f);
        }
    }
}
