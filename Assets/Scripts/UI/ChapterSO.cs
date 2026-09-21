using UnityEngine;

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

    [Header("⏰ Chapter Objectives & Time")]
    [Tooltip("Thời gian tối đa (giây) cho màn chơi")]
    public float maxTime = 300f;
    [Tooltip("Điểm mục tiêu cần đạt để qua màn")]
    public int targetPoint = 400;

    [Header("📍 Item Spawn Settings")]
    [Tooltip("Danh sách các tọa độ (Vector3) xuất hiện Item trong bản đồ của Chapter")]
    public System.Collections.Generic.List<Vector3> itemSpawnPositions = new System.Collections.Generic.List<Vector3>();

    [Header("🎁 Spawnable Items")]
    [Tooltip("Danh sách các Prefab vật phẩm có thể xuất hiện trong Chapter này")]
    public System.Collections.Generic.List<GameObject> spawnableItems = new System.Collections.Generic.List<GameObject>();
}
