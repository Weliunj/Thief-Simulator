using UnityEngine;

/// <summary>
/// ScriptableObject lưu trữ cấu hình tĩnh / chỉ số cơ bản (Base Stats) của Nhân vật / Player:
/// - Profile: Tên, Mô tả, Avatar, ID nhân vật (char_01, char_02...)
/// - Chỉ số cơ bản: baseMoveSpeed, baseSprintSpeed, baseCrouchSpeed, baseJumpHeight
/// - Thể lực cơ bản: baseMaxStamina, staminaDepletionRate, staminaRegenRate, staminaRegenCooldown
/// - Tải trọng cơ bản: baseMaxWeight
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Thief Simulator/Player Data (PlayerSO)", order = 1)]
public class PlayerSO : ScriptableObject
{
    [Header("👤 Character Profile")]
    [Tooltip("Mã định danh nhân vật (VD: char_01, char_02)")]
    public string characterId = "char_01";

    [Tooltip("Tên hiển thị của nhân vật")]
    public string characterName = "Shadow Rookie";

    [Tooltip("Ảnh đại diện / Icon của nhân vật")]
    public Sprite avatar;

    [TextArea(2, 5)]
    [Tooltip("Mô tả tiểu sử hoặc đặc điểm kỹ năng của nhân vật")]
    public string description = "A rookie thief with agile footsteps and balanced stamina.";

    [Header("🏃 Base Movement Stats")]
    [Tooltip("Tốc độ di chuyển cơ bản khi đi bộ")]
    public float baseMoveSpeed = 2.0f;

    [Tooltip("Tốc độ cơ bản khi chạy nhanh (Sprint)")]
    public float baseSprintSpeed = 7.0f;

    [Tooltip("Tốc độ cơ bản khi cúi người (Crouch)")]
    public float baseCrouchSpeed = 1.3f;

    [Tooltip("Độ cao khi nhảy")]
    public float baseJumpHeight = 1.2f;

    [Header("🔋 Base Stamina Settings")]
    [Tooltip("Thể lực tối đa ban đầu")]
    public float baseMaxStamina = 10.0f;

    [Tooltip("Tốc độ giảm Thể lực khi chạy Sprint (/giây)")]
    public float staminaDepletionRate = 3.0f;

    [Tooltip("Tốc độ hồi phục Thể lực (/giây)")]
    public float staminaRegenRate = 2.0f;

    [Tooltip("Thời gian chờ trước khi bắt đầu hồi phục Thể lực (giây)")]
    public float staminaRegenCooldown = 1.0f;

    [Header("🏋️ Base Weight Settings (Kg)")]
    [Tooltip("Sức chứa tải trọng tối đa có thể mang (Kg)")]
    public int baseMaxWeight = 100;
}
