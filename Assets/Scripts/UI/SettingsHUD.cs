using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsHUD : MonoBehaviour
{
    [Header("🎯 Sensitivity UI Components")]
    [Tooltip("Slider điều chỉnh độ nhạy (1.0 -> 5.0)")]
    public Slider sensitivitySlider;

    [Tooltip("Text hiển thị giá trị độ nhạy (VD: 1.5, 2.0, ...)")]
    public TextMeshProUGUI sensitivityValueText;

    [Header("🔊 Audio Volume UI Components (BGM & SFX)")]
    [Tooltip("Slider điều chỉnh âm lượng nhạc nền BGM (0.0 -> 1.0)")]
    public Slider bgmSlider;
    public TextMeshProUGUI bgmValueText;

    [Tooltip("Slider điều chỉnh âm lượng hiệu ứng âm thanh SFX (0.0 -> 1.0)")]
    public Slider sfxSlider;
    public TextMeshProUGUI sfxValueText;

    [Header("📊 FPS & Graphics UI Components")]
    [Tooltip("Toggle hiển thị FPS trên màn hình")]
    public Toggle showFPSToggle;
    [Tooltip("Dropdown chọn mục tiêu FPS (30, 60, 90, 120)")]
    public TMP_Dropdown fpsDropdown;

    [Header("🔘 Action Buttons")]
    public Button saveButton;
    public TextMeshProUGUI saveButtonText; // Tự động tìm con của SaveBtn nếu để trống
    public Button closeButton;

    [Header("🚪 Panels Reference")]
    [Tooltip("Panel setting cần đóng mở (nếu để trống sẽ tự lấy gameObject này)")]
    public GameObject settingRootObject;
    [Tooltip("Panel trước đó (ví dụ MainMenuPanel) để tự động bật lại khi bấm Close)")]
    public GameObject previousPanel;

    [Header("🔊 Audio")]
    [Tooltip("AudioSource phát tiếng click (nếu để trống sẽ tự tìm)")]
    public AudioSource clickAudioSource;

    private float currentPendingSensitivity = 2.0f;
    private bool currentPendingShowFPS = true;
    private int currentPendingFPS = 60;
    private float currentPendingBGM = 0.8f;
    private float currentPendingSFX = 1.0f;
    private Coroutine saveFeedbackCoroutine;
    private string originalSaveText = "Save";

    private void Awake()
    {
        if (settingRootObject == null)
        {
            settingRootObject = gameObject;
        }

        // Tự động tìm text của nút Save nếu chưa gán
        if (saveButton != null && saveButtonText == null)
        {
            saveButtonText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (saveButtonText != null)
        {
            originalSaveText = saveButtonText.text;
        }

        // Tự động tìm Sliders nếu chưa gán
        AutoFindAudioSliders();

        // Tự động tìm Toggle FPS nếu chưa gán
        if (showFPSToggle == null)
        {
            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var t in toggles)
            {
                if (t.gameObject.name.ToLower().Contains("fps"))
                {
                    showFPSToggle = t;
                    break;
                }
            }
        }

        // Cấu hình Sensitivity Slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1.0f;
            sensitivitySlider.maxValue = 10.0f;
            sensitivitySlider.wholeNumbers = false;
            sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // Cấu hình BGM Slider
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0.0f;
            bgmSlider.maxValue = 1.0f;
            bgmSlider.wholeNumbers = false;
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        // Cấu hình SFX Slider
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0.0f;
            sfxSlider.maxValue = 1.0f;
            sfxSlider.wholeNumbers = false;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        // Cấu hình Show FPS Toggle
        if (showFPSToggle != null)
        {
            showFPSToggle.onValueChanged.AddListener(OnFPSToggleChanged);
        }

        // Cấu hình FPS Dropdown
        if (fpsDropdown != null)
        {
            fpsDropdown.onValueChanged.AddListener(OnFPSDropdownChanged);
        }

        // Đăng ký sự kiện Buttons
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void AutoFindAudioSliders()
    {
        var allSliders = GetComponentsInChildren<Slider>(true);
        foreach (var s in allSliders)
        {
            string sName = s.gameObject.name.ToLower();
            if ((sName.Contains("bgm") || sName.Contains("music")) && bgmSlider == null)
            {
                bgmSlider = s;
                if (bgmValueText == null) bgmValueText = s.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if ((sName.Contains("sfx") || sName.Contains("sound") || sName.Contains("effect")) && sfxSlider == null)
            {
                sfxSlider = s;
                if (sfxValueText == null) sfxValueText = s.GetComponentInChildren<TextMeshProUGUI>();
            }
            else if (sName.Contains("sens") && sensitivitySlider == null)
            {
                sensitivitySlider = s;
            }
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Khi mở bảng Settings lên, load giá trị hiện tại đã lưu
        LoadCurrentSettingsToUI();
        ResetSaveButtonText();
    }

    private void OnDisable()
    {
        if (saveFeedbackCoroutine != null)
        {
            StopCoroutine(saveFeedbackCoroutine);
            saveFeedbackCoroutine = null;
        }
        ResetSaveButtonText();
    }

    private void ResetSaveButtonText()
    {
        if (saveButtonText != null)
        {
            saveButtonText.text = string.IsNullOrEmpty(originalSaveText) ? "Save" : originalSaveText;
        }
    }

    private void LoadCurrentSettingsToUI()
    {
        float savedVal = SettingsManager.Instance != null ? SettingsManager.Instance.Sensitivity : 2.0f;
        savedVal = SnapToStep(savedVal);
        currentPendingSensitivity = savedVal;

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(savedVal);
        }

        UpdateValueDisplay(savedVal);

        if (SettingsManager.Instance != null)
        {
            // 1. Graphics & FPS
            currentPendingShowFPS = SettingsManager.Instance.ShowFPSOnScreen;
            currentPendingFPS = SettingsManager.Instance.TargetFPS;

            if (showFPSToggle != null)
            {
                showFPSToggle.SetIsOnWithoutNotify(currentPendingShowFPS);
            }

            if (fpsDropdown != null)
            {
                // Map FPS (30, 45, 60, 90, 120) sang dropdown index
                int index = currentPendingFPS switch
                {
                    30 => 0,
                    45 => 1,
                    60 => 2,
                    90 => 3,
                    120 => 4,
                    _ => 2
                };
                fpsDropdown.SetValueWithoutNotify(index);
            }

            // 2. Audio (BGM & SFX)
            currentPendingBGM = SettingsManager.Instance.BGMVolume;
            currentPendingSFX = SettingsManager.Instance.SFXVolume;

            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(currentPendingBGM);
            }
            UpdateBGMDisplay(currentPendingBGM);

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(currentPendingSFX);
            }
            UpdateSFXDisplay(currentPendingSFX);
        }
    }

    private void OnBGMSliderChanged(float val)
    {
        currentPendingBGM = Mathf.Clamp01(val);
        UpdateBGMDisplay(currentPendingBGM);

        // Áp dụng thử nghiệm âm lượng thời gian thực (chưa ghi đè file JSON)
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetBGMVolume(currentPendingBGM, false);
        }
    }

    private void OnSFXSliderChanged(float val)
    {
        currentPendingSFX = Mathf.Clamp01(val);
        UpdateSFXDisplay(currentPendingSFX);

        // Áp dụng thử nghiệm âm lượng thời gian thực (chưa ghi đè file JSON)
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSFXVolume(currentPendingSFX, false);
        }
    }

    private void UpdateBGMDisplay(float value)
    {
        if (bgmValueText != null)
        {
            bgmValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void UpdateSFXDisplay(float value)
    {
        if (sfxValueText != null)
        {
            sfxValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void OnFPSToggleChanged(bool isOn)
    {
        currentPendingShowFPS = isOn;
        PlayClickSound();
    }

    private void OnFPSDropdownChanged(int index)
    {
        currentPendingFPS = index switch
        {
            0 => 30,
            1 => 45,
            2 => 60,
            3 => 90,
            4 => 120,
            _ => 60
        };
        PlayClickSound();
    }

    private void OnSliderValueChanged(float rawValue)
    {
        // Làm tròn theo bước 0.5 (1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0)
        float snapped = SnapToStep(rawValue);

        // Cập nhật giá trị hiển thị trên thanh slider nếu chưa khớp bước nhảy
        if (Mathf.Abs(sensitivitySlider.value - snapped) > 0.001f)
        {
            sensitivitySlider.SetValueWithoutNotify(snapped);
        }

        currentPendingSensitivity = snapped;
        UpdateValueDisplay(snapped);
    }

    private float SnapToStep(float value)
    {
        // Bước 0.5: nhân đôi, làm tròn, rồi chia đôi
        float snapped = Mathf.Round(value * 2f) / 2f;
        return Mathf.Clamp(snapped, 1.0f, 10.0f);
    }

    private void UpdateValueDisplay(float value)
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = value.ToString("0.0");
        }
    }

    public void OnSaveClicked()
    {
        PlayClickSound();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetAllSettings(currentPendingSensitivity, currentPendingFPS, currentPendingShowFPS, currentPendingBGM, currentPendingSFX);
            Debug.Log($"[SettingsHUD] Đã lưu Settings: Sensitivity={currentPendingSensitivity}, FPS={currentPendingFPS}, BGM={currentPendingBGM}, SFX={currentPendingSFX}");
        }

        // Hiển thị 'Saved' trong 1 giây rồi quay lại 'Save'
        if (saveFeedbackCoroutine != null)
        {
            StopCoroutine(saveFeedbackCoroutine);
        }
        saveFeedbackCoroutine = StartCoroutine(ShowSavedFeedbackRoutine());
    }

    private IEnumerator ShowSavedFeedbackRoutine()
    {
        if (saveButtonText != null)
        {
            saveButtonText.text = "Saved";
        }

        // Dùng WaitForSecondsRealtime để không bị ảnh hưởng khi game đang pause (timeScale = 0)
        yield return new WaitForSecondsRealtime(1.0f);

        ResetSaveButtonText();
        saveFeedbackCoroutine = null;
    }

    public void OnCloseClicked()
    {
        PlayClickSound();

        // Huỷ bỏ các thay đổi chưa bấm Save (revert lại)
        LoadCurrentSettingsToUI();
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyAudioSettings();
        }
        ClosePanel();

        // Bật lại Panel trước đó (như Main Menu)
        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
        }

        // Hiện lại 3D model ngoài sảnh chính
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.ApplyLobbyModelVisuals();
        }
    }

    private void PlayClickSound()
    {
        if (clickAudioSource == null)
        {
            clickAudioSource = GetComponentInChildren<AudioSource>(true);
            if (clickAudioSource == null)
            {
                clickAudioSource = FindFirstObjectByType<AudioSource>();
            }
        }

        if (clickAudioSource != null)
        {
            clickAudioSource.Play();
        }
    }

    public void OpenPanel()
    {
        if (settingRootObject != null)
        {
            settingRootObject.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (settingRootObject != null)
        {
            settingRootObject.SetActive(false);
        }
    }
}
