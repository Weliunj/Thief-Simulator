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
}
