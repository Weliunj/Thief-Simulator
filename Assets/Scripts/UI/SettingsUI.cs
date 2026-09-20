using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("🎯 Sensitivity UI Components")]
    [Tooltip("Slider điều chỉnh độ nhạy (1.0 -> 5.0)")]
    public Slider sensitivitySlider;

    [Tooltip("Text hiển thị giá trị độ nhạy (VD: 1.5, 2.0, ...)")]
    public TextMeshProUGUI sensitivityValueText;

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

        // Cấu hình Slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1.0f;
            sensitivitySlider.maxValue = 10.0f;
            sensitivitySlider.wholeNumbers = false;
            sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);
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

    private void OnEnable()
    {
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
            SettingsManager.Instance.SetSensitivity(currentPendingSensitivity);
            Debug.Log($"[SettingsUI] Đã lưu Sensitivity = {currentPendingSensitivity}");
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
        ClosePanel();

        // Bật lại Panel trước đó (như Main Menu)
        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
        }
    }

    private void PlayClickSound()
    {
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
