using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện HUD chính trong trận đấu (mainHUD):
/// - Hiển thị Stamina (HiệnTại/TốiĐa), Cân nặng (HiệnTại/TốiĐa Kg), Điểm số (HiệnTại/TốiĐa), và Thời gian đếm ngược
/// - Cảnh báo đổi màu động khi Thể lực cạn hoặc Quá tải trọng lượng
/// - Xử lý nút bấm Tạm dừng (PauseBtn)
/// - Quản lý bật/tắt toàn bộ MainHUD
/// </summary>
public class MainHUD : MonoBehaviour
{
    [Header("🔋 Stamina UI (HiệnTại/TốiĐa)")]
    [Tooltip("Text hiển thị thể lực (VD: 10/10, 7/10)")]
    public TextMeshProUGUI currStamina;

    [Header("🏋️ Weight UI (HiệnTại/TốiĐa Kg)")]
    [Tooltip("Text hiển thị tải trọng (VD: 0/100Kg, 25/100Kg)")]
    public TextMeshProUGUI currKg;

    [Header("🌟 Point UI (Đã chuyển sang hiển thị tại EscapeZone)")]
    [Tooltip("Text hiển thị điểm số (Đã ẩn theo yêu cầu thiết kế mới)")]
    public TextMeshProUGUI currPointText;

    [Header("🚪 Escape UI & Audio")]
    [Tooltip("Nút Tẩu Thoát / Về Sảnh (Chỉ hiện khi đứng trong EscapeZone và đã đủ chỉ tiêu)")]
    public Button escapeButton;
    [Tooltip("AudioSource phát tiếng xe khởi động (phát local không sync)")]
    public AudioSource escapeAudioSource;
    [Tooltip("AudioClip tiếng khởi động xe khi bấm Escape")]
    public AudioClip escapeEngineClip;
    [Tooltip("Thời gian mờ dần sang màn hình đen (giây)")]
    public float escapeFadeDuration = 0.8f;
    [Tooltip("Thời gian giữ màn hình đen (5 giây) trước khi chuyển Scene về sảnh")]
    public float escapeBlackHoldDuration = 5.0f;

    [Header("⏰ Time UI")]
    [Tooltip("Text hiển thị thời gian đếm ngược (VD: 04:59)")]
    public TextMeshProUGUI timeText;
    public AudioSource alarmAudio;

    [Header("🏳️ Give Up UI (Khi hết giờ / Chết)")]
    [Tooltip("Nút Give Up / Thoát về sảnh")]
    public Button giveUpButton;
    [Tooltip("Panel thông báo thất bại / Hết giờ (tùy chọn)")]
    public GameObject gameOverPanel;
    [Tooltip("Text mô tả lý do Game Over")]
    public TextMeshProUGUI gameOverReasonText;

    [Header("⏸️ Controls & Audio")]
    public Button pauseButton;
    public AudioSource clickAudioSource;
    public AudioClip clickAudioClip;

    [Header("⚠️ Warning Colors")]
    [Tooltip("Màu cơ bản khi giá trị an toàn")]
    public Color normalColor = Color.white;
    [Tooltip("Màu cảnh báo khi sắp cạn thể lực hoặc quá tải")]
    public Color alertColor = Color.red;
    [Range(0f, 1f)]
    [Tooltip("Ngưỡng bắt đầu đổi màu cảnh báo (0.5 = 50%)")]
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

        if (escapeButton != null)
        {
            escapeButton.onClick.RemoveListener(OnEscapeButtonClicked);
            escapeButton.onClick.AddListener(OnEscapeButtonClicked);
            escapeButton.gameObject.SetActive(false);
        }

        if (giveUpButton != null)
        {
            giveUpButton.onClick.RemoveListener(OnGiveUpButtonClicked);
            giveUpButton.onClick.AddListener(OnGiveUpButtonClicked);
            giveUpButton.gameObject.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UI_Manager>();

        // Ẩn Point Text trên Main HUD (Điểm số giờ hiển thị trực quan 3D tại EscapeZone)
        if (currPointText != null)
        {
            currPointText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Bật hoặc ẩn nút Escape trên MainHUD
    /// </summary>
    public void SetEscapeButtonActive(bool active)
    {
        if (escapeButton != null && escapeButton.gameObject.activeSelf != active)
        {
            escapeButton.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// Hiển thị giao diện Give Up / Hết giờ (Chỉ trên máy Local)
    /// </summary>
    public void ShowGiveUpUI(string reason = "Time's Up!")
    {
        if (giveUpButton != null)
        {
            giveUpButton.gameObject.SetActive(true);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverReasonText != null)
        {
            gameOverReasonText.text = reason;
        }

        // Đảm bảo mở khóa con trỏ chuột để người chơi bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGiveUpButtonClicked()
    {
        PlayClickSound();
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.Menu();
        }
    }

    /// <summary>
    /// Tự động tìm kiếm các UI Elements nếu chưa được gán thủ công trên Inspector
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

        // Tự động tìm Escape Button
        if (escapeButton == null)
        {
            Transform eBtn = transform.Find("EscapeBtn") ?? transform.Find("EscapeButton") ?? transform.Find("ExitBtn") ?? transform.Find("LeaveBtn");
            if (eBtn != null) escapeButton = eBtn.GetComponent<Button>();

            if (escapeButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    string bName = b.gameObject.name.ToLower();
                    if (bName.Contains("escape") || bName.Contains("exit") || bName.Contains("leave"))
                    {
                        escapeButton = b;
                        break;
                    }
                }
            }
        }

        // Tự động tìm Give Up Button
        if (giveUpButton == null)
        {
            Transform gBtn = transform.Find("GiveUpBtn") ?? transform.Find("GiveUpButton") ?? transform.Find("SurrenderBtn");
            if (gBtn != null) giveUpButton = gBtn.GetComponent<Button>();

            if (giveUpButton == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    string bName = b.gameObject.name.ToLower();
                    if (bName.Contains("giveup") || bName.Contains("surrender"))
                    {
                        giveUpButton = b;
                        break;
                    }
                }
            }
        }

        // Tự động tìm Alarm Audio
        if (alarmAudio == null)
        {
            alarmAudio = GetComponentInChildren<AudioSource>(true);
        }

        // Tự động tìm TextMeshProUGUI trong các nhánh con
        if (currStamina == null || currKg == null || timeText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                string objName = t.gameObject.name.ToLower();
                string parentName = t.transform.parent != null ? t.transform.parent.name.ToLower() : "";

                // Stamina
                if (currStamina == null && (parentName.Contains("stamina") || objName.Contains("stamina")))
                {
                    currStamina = t;
                }
                // Kg / Weight
                else if (currKg == null && (parentName.Contains("kg") || objName.Contains("kg") || parentName.Contains("weight")))
                {
                    currKg = t;
                }
                // Time
                else if (timeText == null && (parentName.Contains("time") || objName.Contains("time")))
                {
                    timeText = t;
                }
            }
        }
    }

    /// <summary>
    /// Khởi tạo các giá trị ban đầu từ PlayerStats
    /// </summary>
    public void InitializeMaxValues(PlayerStats ps)
    {
        UpdateHUD(ps);
    }

    /// <summary>
    /// Khởi tạo các giá trị ban đầu từ PlayerSO (fallback)
    /// </summary>
    public void InitializeMaxValues(PlayerSO pm)
    {
        UpdateHUD(pm);
    }

    /// <summary>
    /// Cập nhật thông số HUD động mỗi frame từ PlayerStats
    /// </summary>
    public void UpdateHUD(PlayerStats ps)
    {
        if (ps == null) return;

        // 1. Cập nhật Thể lực (Stamina: HiệnTại/TốiĐa)
        if (currStamina != null)
        {
            currStamina.text = $"{ps.currentStamina:F0}/{ps.maxStamina:F0}";

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

        // 2. Cập nhật Cân nặng (Kg: HiệnTại/TốiĐaKg)
        if (currKg != null)
        {
            currKg.text = $"{ps.currentWeight}/{ps.maxWeight}Kg";

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

        // 3. Cập nhật Điểm số (Point: HiệnTại/TốiĐa)
        if (currPointText != null)
        {
            currPointText.text = $"{ps.currPoint}/{ps.totalpoint}";
        }

        // 4. Cập nhật Thời gian (Time: MM:SS)
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
    /// Cập nhật thông số HUD động từ PlayerSO (fallback)
    /// </summary>
    public void UpdateHUD(PlayerSO pm)
    {
        if (pm == null) return;

        if (currStamina != null) currStamina.text = $"{pm.baseMaxStamina:F0}/{pm.baseMaxStamina:F0}";
        if (currKg != null) currKg.text = $"0/{pm.baseMaxWeight}Kg";
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
        PlayClickSound();

        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.PauseGame();
        }
    }

    private bool isEscaping = false;

    private void OnEscapeButtonClicked()
    {
        if (isEscaping) return;
        isEscaping = true;

        // 1. Tắt nút Escape và khóa tương tác để tránh bấm đè nhiều lần
        SetEscapeButtonActive(false);

        // 2. Phát âm thanh tiếng xe khởi động (phát Local, không sync)
        PlayEscapeEngineSound();

        // 3. Khởi động tiến trình chuyển cảnh: Màn hình đen -> Giữ 5 giây -> Về Sảnh
        StartCoroutine(EscapeSequenceRoutine());
    }

    private void PlayEscapeEngineSound()
    {
        if (escapeEngineClip == null)
        {
            PlayClickSound();
            return;
        }

        if (escapeAudioSource != null)
        {
            escapeAudioSource.PlayOneShot(escapeEngineClip);
        }
        else
        {
            // Fallback tìm hoặc tạo AudioSource local
            AudioSource src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 0f; // 2D Sound (Local)
            src.PlayOneShot(escapeEngineClip);
        }
    }

    private System.Collections.IEnumerator EscapeSequenceRoutine()
    {
        // Làm mờ dần sang màn hình đen
        bool fadeDone = false;
        ScreenFader.FadeToBlack(escapeFadeDuration, () => fadeDone = true);

        while (!fadeDone)
        {
            yield return null;
        }

        // Đợi đủ 5 giây trong màn hình đen
        yield return new WaitForSecondsRealtime(escapeBlackHoldDuration);

        // Chuyển Scene về sảnh
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.EscapeToHome();
        }
    }

    private void PlayClickSound()
    {
        if (clickAudioSource != null)
        {
            if (clickAudioClip != null)
            {
                clickAudioSource.PlayOneShot(clickAudioClip);
            }
            else
            {
                clickAudioSource.Play();
            }
        }
        else
        {
            // Tự động tìm AudioSource bất kỳ trên Canvas UI để phát
            AudioSource anyAudio = GetComponentInParent<AudioSource>() ?? FindFirstObjectByType<AudioSource>();
            if (anyAudio != null)
            {
                if (clickAudioClip != null) anyAudio.PlayOneShot(clickAudioClip);
                else anyAudio.Play();
            }
        }
    }
}
