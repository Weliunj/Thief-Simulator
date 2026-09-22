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

    [Tooltip("Ảnh đại diện / Icon của nhân vật")]
    public Sprite avatar;

    [TextArea(2, 5)]
    [Tooltip("Mô tả tiểu sử hoặc đặc điểm kỹ năng của nhân vật")]
    public string description = "A rookie thief with agile footsteps and balanced stamina.";

    [Header("🎭 3D Character Model / Prefab")]
    [Tooltip("Prefab nhân vật 3D tương ứng để Instantiate vào màn chơi (nếu để trống, tự động dùng Prefab mặc định)")]
    public GameObject characterPrefab;

    [Tooltip("Mesh 3D chính cho phiên bản Nam (Male)")]
    public Mesh maleMesh;

    [Tooltip("Mesh 3D chính cho phiên bản Nữ (Female)")]
    public Mesh femaleMesh;

    [Header("🎭 3D Mesh Variants (Kiểu 1, Kiểu 2, Kiểu 3...)")]
    [Tooltip("Danh sách các biến thể Mesh 3D cho Nam (VD: normal-man-1, normal-man-2, normal-man-3)")]
    public List<Mesh> maleMeshes = new List<Mesh>();

    [Tooltip("Danh sách các biến thể Mesh 3D cho Nữ (VD: normal-woman-1, normal-woman-2, normal-woman-3)")]
    public List<Mesh> femaleMeshes = new List<Mesh>();

    /// <summary>
    /// Lấy Mesh 3D theo Giới tính và Chỉ số Biến thể (Variant Index)
    /// </summary>
    public Mesh GetMesh(bool isMale, int variantIndex = 0)
    {
        if (isMale)
        {
            if (maleMeshes != null && maleMeshes.Count > variantIndex && maleMeshes[variantIndex] != null)
                return maleMeshes[variantIndex];
            if (maleMesh != null) return maleMesh;
            if (maleMeshes != null && maleMeshes.Count > 0 && maleMeshes[0] != null)
                return maleMeshes[0];
        }
        else
        {
            if (femaleMeshes != null && femaleMeshes.Count > variantIndex && femaleMeshes[variantIndex] != null)
                return femaleMeshes[variantIndex];
            if (femaleMesh != null) return femaleMesh;
            if (femaleMeshes != null && femaleMeshes.Count > 0 && femaleMeshes[0] != null)
                return femaleMeshes[0];
        }
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
