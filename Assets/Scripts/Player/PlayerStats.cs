using UnityEngine;

/// <summary>
/// Component quản lý toàn bộ chỉ số động (Runtime State) của Nhân vật trong trận đấu:
/// - Nạp chỉ số cơ bản từ PlayerSO và cập nhật liên tục lúc chơi.
/// - Quản lý Thể lực (Stamina) tiêu hao khi chạy, hồi phục sau cooldown.
/// - Quản lý Tải trọng (Weight) và tự động tính toán giảm tốc độ (Speed Penalty) tuyến tính.
/// - Quản lý tiến trình màn chơi: Điểm số (currpoint/totalpoint), Thời gian đếm ngược (currentTime), Trạng thái sống/chết (isDied).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("👤 Character Base Data Reference")]
    [Tooltip("ScriptableObject chứa cấu hình gốc của nhân vật (nếu để trống, dùng thông số mặc định)")]
    public PlayerSO characterData;

    [Header("🔋 Runtime Stamina")]
    public float currentStamina = 10f;
    public float maxStamina = 10f;
    public float staminaDepletionRate = 3.0f;
    public float staminaRegenRate = 2.0f;
    public float staminaRegenCooldown = 1.0f;
    [HideInInspector] public bool canSprint = true;
    private float staminaRegenTimer = 0f;

    [Header("🏋️ Runtime Weight (Kg)")]
    public int currentWeight = 0;
    public int maxWeight = 100;

    [Header("🏃 Runtime Speeds (Sau khi tính tải trọng)")]
    public float moveSpeed = 2.0f;
    public float sprintSpeed = 7.0f;
    public float crouchSpeed = 1.3f;
    public float jumpHeight = 1.2f;

    [Header("⏰ Chapter / Game Session Stats")]
    public float maxTime = 300f;
    public float currentTime = 300f;
    public int currPoint = 0;
    public int totalPoint = 400;
    public bool isDied = false;

    // =========================================================================
    //               COMPATIBILITY PROPERTIES (Tương thích ngược)
    // =========================================================================
    public float _stamina { get => currentStamina; set => currentStamina = value; }
    public float currstamina { get => currentStamina; set => currentStamina = value; }
    public float MaxStamina { get => maxStamina; set => maxStamina = value; }
    public int currweight { get => currentWeight; set => currentWeight = value; }
    public int Maxweight { get => maxWeight; set => maxWeight = value; }
    public float _MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float _SprintSpeed { get => sprintSpeed; set => sprintSpeed = value; }
    public float MoveSpeed => characterData != null ? characterData.baseMoveSpeed : 2.0f;
    public float SprintSpeed => characterData != null ? characterData.baseSprintSpeed : 7.0f;
    public float CrouchSpeed => characterData != null ? characterData.baseCrouchSpeed : 1.3f;
    public float JumpHeight => characterData != null ? characterData.baseJumpHeight : 1.2f;
    public float StaminaDepletionRate => staminaDepletionRate;
    public float StaminaRegenRate => staminaRegenRate;
    public float StaminaRegenCooldown => staminaRegenCooldown;
    public float MaxTime { get => maxTime; set => maxTime = value; }
    public int currpoint { get => currPoint; set => currPoint = value; }
    public int totalpoint { get => totalPoint; set => totalPoint = value; }

    private void Awake()
    {
        InitializeFromData(characterData);
    }

    /// <summary>
    /// Nạp chỉ số cơ bản từ PlayerSO sang Runtime State
    /// </summary>
    public void InitializeFromData(PlayerSO data)
    {
        characterData = data;

        if (data != null)
        {
            maxStamina = data.baseMaxStamina;
            currentStamina = maxStamina;
            staminaDepletionRate = data.staminaDepletionRate;
            staminaRegenRate = data.staminaRegenRate;
            staminaRegenCooldown = data.staminaRegenCooldown;

            maxWeight = data.baseMaxWeight;
            currentWeight = 0;

            moveSpeed = data.baseMoveSpeed;
            sprintSpeed = data.baseSprintSpeed;
            crouchSpeed = data.baseCrouchSpeed;
            jumpHeight = data.baseJumpHeight;
        }
        else
        {
            currentStamina = maxStamina;
        }

        currentTime = maxTime;
        currPoint = 0;
        isDied = false;
        staminaRegenTimer = 0f;
        canSprint = true;

        CalculateWeightSpeedPenalty();
        ApplyCharacterMeshAndSkin(data);
    }

    /// <summary>
    /// Áp dụng Mesh (Nam/Nữ) và Material/Texture của PlayerSO lên model 3D của Player
    /// </summary>
    public void ApplyCharacterMeshAndSkin(PlayerSO data)
    {
        if (data == null) return;

        bool isMale = GameSession.IsMale;
        Mesh targetMesh = data.GetMesh(isMale);
        if (targetMesh != null)
        {
            ApplyMeshToModel(gameObject, targetMesh);
        }

        ApplySkinToModel(gameObject, data.characterMaterial, data.characterTexture);
    }

    /// <summary>
    /// Tiện ích tĩnh: Áp dụng Mesh lên SkinnedMeshRenderer hoặc MeshFilter của model (tự động ưu tiên object con tên 'Base')
    /// </summary>
    public static void ApplyMeshToModel(GameObject modelRoot, Mesh targetMesh)
    {
        if (modelRoot == null || targetMesh == null) return;

        // Ưu tiên 1: Tìm đối tượng con tên "Base" (tránh thay nhầm Mesh của bục đứng / chỗ đứng)
        Transform baseChild = null;
        foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(t.name, "Base", System.StringComparison.OrdinalIgnoreCase))
            {
                baseChild = t;
                break;
            }
        }

        GameObject targetObj = baseChild != null ? baseChild.gameObject : modelRoot;

        SkinnedMeshRenderer smr = targetObj.GetComponentInChildren<SkinnedMeshRenderer>(true) ?? modelRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null)
        {
            smr.sharedMesh = targetMesh;
            return;
        }

        MeshFilter mf = targetObj.GetComponentInChildren<MeshFilter>(true) ?? modelRoot.GetComponentInChildren<MeshFilter>(true);
        if (mf != null)
        {
            mf.sharedMesh = targetMesh;
        }
    }

    /// <summary>
    /// Tiện ích tĩnh: Áp dụng Material / Texture lên Dummy Preview Model (tự động ưu tiên object con tên 'Base' để không đổi màu chỗ đứng)
    /// </summary>
    public static void ApplySkinToModel(GameObject modelRoot, Material characterMaterial, Texture2D characterTexture = null)
    {
        if (modelRoot == null) return;
        if (characterMaterial == null && characterTexture == null) return;

        // Ưu tiên 1: Tìm đối tượng con tên "Base" (tránh đổi Material của bục đứng / chỗ đứng)
        Transform baseChild = null;
        foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(t.name, "Base", System.StringComparison.OrdinalIgnoreCase))
            {
                baseChild = t;
                break;
            }
        }

        GameObject targetObj = baseChild != null ? baseChild.gameObject : modelRoot;

        Renderer[] renderers = targetObj.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // Bỏ qua nếu là Item đang cầm trong Hotbar, Đèn pin, Particle, hoặc UI
            if (rend.GetComponentInParent<Item>() != null) continue;
            if (rend.GetComponentInParent<FlashlightController>() != null) continue;
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;

            if (characterMaterial != null)
            {
                rend.material = characterMaterial;
            }
            else if (characterTexture != null)
            {
                if (rend.material != null)
                {
                    rend.material.mainTexture = characterTexture;
                    if (rend.material.HasProperty("_BaseMap"))
                    {
                        rend.material.SetTexture("_BaseMap", characterTexture);
                    }
                    if (rend.material.HasProperty("_MainTex"))
                    {
                        rend.material.SetTexture("_MainTex", characterTexture);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Nạp dữ liệu mục tiêu màn chơi từ ChapterSO
    /// </summary>
    public void InitializeChapter(ChapterSO chapter)
    {
        if (chapter != null)
        {
            maxTime = chapter.maxTime;
            totalPoint = chapter.targetPoint;
        }

        currentTime = maxTime;
        currPoint = 0;
        currentWeight = 0;
        currentStamina = maxStamina;
        isDied = false;
        staminaRegenTimer = 0f;

        CalculateWeightSpeedPenalty();
    }

    /// <summary>
    /// Xử lý cập nhật Thể lực (Stamina) tiêu hao và hồi phục mỗi frame
    /// </summary>
    public void HandleStamina(bool isSprinting, bool isMoving, bool isCrouching, float deltaTime)
    {
        canSprint = currentStamina > 0.05f;

        // 1. Chạy nhanh (Sprint): Giảm thể lực
        if (isSprinting && isMoving && !isCrouching)
        {
            if (currentStamina > 0f)
            {
                currentStamina -= staminaDepletionRate * deltaTime;
                staminaRegenTimer = staminaRegenCooldown; // Reset bộ đếm hồi phục
                currentStamina = Mathf.Max(0f, currentStamina);
            }
        }
        // 2. Hồi phục thể lực khi không chạy
        else
        {
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= deltaTime;
            }
            else if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
    }

    /// <summary>
    /// Tính toán giảm tốc độ di chuyển tỉ lệ thuận theo trọng lượng đang mang
    /// </summary>
    public void CalculateWeightSpeedPenalty()
    {
        float baseMove = characterData != null ? characterData.baseMoveSpeed : 2.0f;
        float baseSprint = characterData != null ? characterData.baseSprintSpeed : 7.0f;

        if (maxWeight <= 0) maxWeight = 100;

        float weightRatio = Mathf.Clamp01((float)currentWeight / (float)maxWeight);
        float minMoveSpeed = baseMove * 0.25f;   // Tối thiểu còn 25% tốc độ gốc khi đầy 100% túi
        float minSprintSpeed = baseSprint * 0.25f;

        moveSpeed = Mathf.Lerp(baseMove, minMoveSpeed, weightRatio);
        sprintSpeed = Mathf.Lerp(baseSprint, minSprintSpeed, weightRatio);
    }

    /// <summary>
    /// Kiểm tra xem người chơi có thể mang thêm vật phẩm nặng kg không
    /// </summary>
    public bool CanCarry(int itemKg)
    {
        return (currentWeight + itemKg) <= maxWeight;
    }

    /// <summary>
    /// Thêm trọng lượng khi nhặt item
    /// </summary>
    public void AddWeight(int itemKg)
    {
        currentWeight += itemKg;
        CalculateWeightSpeedPenalty();
    }

    /// <summary>
    /// Giảm trọng lượng khi thả/giao item
    /// </summary>
    public void RemoveWeight(int itemKg)
    {
        currentWeight = Mathf.Max(0, currentWeight - itemKg);
        CalculateWeightSpeedPenalty();
    }

    /// <summary>
    /// Cộng điểm khi giao vật phẩm hoặc mở khóa cửa
    /// </summary>
    public void AddPoints(int points)
    {
        currPoint += points;
    }

    /// <summary>
    /// Đánh dấu nhân vật tử vong
    /// </summary>
    public void Die()
    {
        isDied = true;
    }
}
