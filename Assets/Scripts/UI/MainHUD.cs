using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện HUD chính trong trận đấu (mainHUD):
/// - Hiển thị Stamina, Cân nặng (Kg), Điểm số (Point), và Thời gian đếm ngược (Time_T)
/// - Cảnh báo đổi màu động khi Thể lực cạn hoặc Quá tải trọng lượng
/// - Xử lý nút bấm Tạm dừng (PauseBtn)
/// - Quản lý bật/tắt toàn bộ MainHUD
/// </summary>
public class MainHUD : MonoBehaviour
{
    [Header("🔋 Stamina UI")]
    public TextMeshProUGUI currStamina;
    public TextMeshProUGUI maxStaminaText;

    [Header("🏋️ Weight UI")]
    public TextMeshProUGUI currKg;
    public TextMeshProUGUI maxKgText;

    [Header("🌟 Point UI")]
    public TextMeshProUGUI currPointText;
    public TextMeshProUGUI targetPointText;

    [Header("⏰ Time UI")]
    public TextMeshProUGUI timeText;
    public AudioSource alarmAudio;

    [Header("⏸️ Controls")]
    public Button pauseButton;

    [Header("⚠️ Warning Colors")]
    [Tooltip("Màu cơ bản khi giá trị an toàn")]
    public Color normalColor = Color.white;
    [Tooltip("Màu cảnh báo khi sắp cạn thể lực hoặc quá tải")]
    public Color alertColor = Color.red;
    [Range(0f, 1f)]
    [Tooltip("Ngưỡng bắt đầu đổi màu (0.5 = 50%)")]
    public float warnThreshold = 0.5f;

    private UI_Manager uiManager;

    private void Awake()
    {
        AutoFindUIElements();

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UI_Manager>();
    }

    /// <summary>
    /// Tự động tìm kiếm các Text UI nếu chưa được gán thủ công trên Inspector
    /// </summary>
    public void AutoFindUIElements()
    {
        // Tự động tìm Pause Button
        if (pauseButton == null)
        {
            Transform pBtn = transform.Find("PauseBtn") ?? transform.Find("PauseButton");
            if (pBtn != null) pauseButton = pBtn.GetComponent<Button>();
            if (pauseButton == null) pauseButton = GetComponentInChildren<Button>(true);
        }

        // Tự động tìm Alarm Audio
        if (alarmAudio == null)
        {
            alarmAudio = GetComponentInChildren<AudioSource>(true);
        }

        // Tự động tìm TextMeshProUGUI trong các nhánh con
        if (currStamina == null || currKg == null || currPointText == null || timeText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                string objName = t.gameObject.name.ToLower();
                string parentName = t.transform.parent != null ? t.transform.parent.name.ToLower() : "";

                // Stamina
                if (parentName.Contains("stamina") || objName.Contains("stamina"))
                {
                    if (objName.Contains("curr") || objName == "stamina" || objName == "text" || objName == "value")
                    {
                        if (currStamina == null) currStamina = t;
                    }
                    else if (objName.Contains("max") || objName.Contains("total"))
                    {
                        if (maxStaminaText == null) maxStaminaText = t;
                    }
                }
                // Kg / Weight
                else if (parentName.Contains("kg") || objName.Contains("kg") || parentName.Contains("weight"))
                {
                    if (objName.Contains("curr") || objName == "kg" || objName == "text" || objName == "value")
                    {
                        if (currKg == null) currKg = t;
                    }
                    else if (objName.Contains("max") || objName.Contains("total"))
                    {
                        if (maxKgText == null) maxKgText = t;
                    }
                }
                // Point
                else if (parentName.Contains("point") || objName.Contains("point"))
                {
                    if (objName.Contains("target") || objName.Contains("total") || objName.Contains("max"))
                    {
                        if (targetPointText == null) targetPointText = t;
                    }
                    else
                    {
                        if (currPointText == null) currPointText = t;
                    }
                }
                // Time
                else if (parentName.Contains("time") || objName.Contains("time"))
                {
                    if (timeText == null) timeText = t;
                }
            }
        }
    }

    /// <summary>
    /// Khởi tạo các giá trị Max (Target) ban đầu từ PlayerStats
    /// </summary>
    public void InitializeMaxValues(PlayerStats ps)
    {
        if (ps == null) return;

        if (maxStaminaText != null) maxStaminaText.text = $"{ps.MaxStamina:F0}";
        if (maxKgText != null) maxKgText.text = $"{ps.Maxweight}";
        if (targetPointText != null) targetPointText.text = $"{ps.totalpoint}";
    }

    /// <summary>
    /// Khởi tạo các giá trị Max (Target) ban đầu từ PlayerSO (fallback)
    /// </summary>
    public void InitializeMaxValues(PlayerSO pm)
    {
        if (pm == null) return;

        if (maxStaminaText != null) maxStaminaText.text = $"{pm.baseMaxStamina:F0}";
        if (maxKgText != null) maxKgText.text = $"{pm.baseMaxWeight}";
    }

    /// <summary>
    /// Cập nhật thông số HUD động mỗi frame từ PlayerStats
    /// </summary>
    public void UpdateHUD(PlayerStats ps)
    {
        if (ps == null) return;

        // 1. Cập nhật Thể lực (Stamina)
        if (currStamina != null)
        {
            currStamina.text = $"{ps.currentStamina:F1}";
            if (ps.maxStamina > 0f)
            {
                float sNorm = Mathf.Clamp01(ps.currentStamina / ps.maxStamina);
                Color sColor = normalColor;
                if (sNorm <= warnThreshold)
                {
                    float t = Mathf.InverseLerp(warnThreshold, 0f, sNorm);
                    sColor = Color.Lerp(normalColor, alertColor, t);
                }
                currStamina.color = sColor;
            }
        }

        // 2. Cập nhật Cân nặng (Kg)
        if (currKg != null)
        {
            currKg.text = $"{ps.currentWeight}";
            if (ps.maxWeight > 0)
            {
                float wNorm = Mathf.Clamp01((float)ps.currentWeight / (float)ps.maxWeight);
                Color wColor = normalColor;
                if (wNorm >= warnThreshold)
                {
                    float t = Mathf.InverseLerp(warnThreshold, 1f, wNorm);
                    wColor = Color.Lerp(normalColor, alertColor, t);
                }
                currKg.color = wColor;
            }
        }

        // 3. Cập nhật Điểm số (Point)
        if (currPointText != null)
        {
            currPointText.text = $"{ps.currPoint}";
        }

        // 4. Cập nhật Thời gian (Time)
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(ps.currentTime / 60f);
            int seconds = Mathf.FloorToInt(ps.currentTime % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }

        // 5. Cảnh báo hết giờ
        if (ps.currentTime <= 0f)
        {
            if (alarmAudio != null && !alarmAudio.isPlaying)
            {
                alarmAudio.loop = true;
                alarmAudio.Play();
            }
        }
    }

    /// <summary>
    /// Cập nhật thông số HUD động mỗi frame từ PlayerSO (fallback)
    /// </summary>
    public void UpdateHUD(PlayerSO pm)
    {
        if (pm == null) return;

        if (currStamina != null) currStamina.text = $"{pm.baseMaxStamina:F1}";
        if (currKg != null) currKg.text = "0";
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnPauseButtonClicked()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.PauseGame();
        }
    }
}
