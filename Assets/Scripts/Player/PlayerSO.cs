using System.Collections.Generic;
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

    [Header("👤 Character Avatars (Nam & Nữ)")]
    [Tooltip("Ảnh đại diện / Icon nhân vật Nam (được truyền từ Slot)")]
    public Sprite maleAvatar;

    [Tooltip("Ảnh đại diện / Icon nhân vật Nữ (được truyền từ Slot)")]
    public Sprite femaleAvatar;

    /// <summary>
    /// Lấy ảnh đại diện theo Giới tính (Nam / Nữ)
    /// </summary>
    public Sprite GetAvatar(bool isMale)
    {
        return isMale ? maleAvatar : femaleAvatar;
    }

    [TextArea(2, 5)]
    [Tooltip("Mô tả tiểu sử hoặc đặc điểm kỹ năng của nhân vật")]
    public string description = "A rookie thief with agile footsteps and balanced stamina.";

    [Header("🎭 3D Character Model / Prefab")]
    [Tooltip("Prefab nhân vật 3D tương ứng để Instantiate vào màn chơi (nếu để trống, tự động dùng Prefab mặc định)")]
    public GameObject characterPrefab;

    [Header("🔓 Unlock & Purchase Settings")]
    [Tooltip("Nhân vật này có được mở khóa sẵn miễn phí không? (VD: Nhân vật mặc định = true)")]
    public bool isUnlockedByDefault = false;

    [Tooltip("Giá tiền để mua/mở khóa nhân vật này ($)")]
    public int unlockPrice = 1000;

    [Header("🎭 3D Meshes (Nam & Nữ)")]
    [Tooltip("Mesh 3D cho Nam (được truyền từ Slot khi Save)")]
    public Mesh maleMesh;

    [Tooltip("Mesh 3D cho Nữ (được truyền từ Slot khi Save)")]
    public Mesh femaleMesh;

    /// <summary>
    /// Lấy Mesh 3D theo Giới tính (Nam / Nữ)
    /// </summary>
    public Mesh GetMesh(bool isMale, int variantIndex = 0)
    {
        return isMale ? maleMesh : femaleMesh;
    }

    [Header("🎨 Character Appearance / Skin")]
    [Tooltip("Material đại diện cho trang phục / skin của nhân vật (Ví dụ: palette1.mat, palette2.mat...). Nếu để trống, dùng Material mặc định của Prefab.")]
    public Material characterMaterial;

    [Tooltip("Texture đại diện nếu chỉ muốn thay đổi Texture chính (_BaseMap / _MainTex) mà không cần tạo file Material riêng")]
    public Texture2D characterTexture;

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
