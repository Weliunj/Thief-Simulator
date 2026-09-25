using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện menu Tạm dừng trong trận đấu (Pause HUD):
/// - Các nút chức năng: Tiếp tục (Resume), Chơi lại (Restart/Replay), Cài đặt (Settings), Về Menu (Home/Menu)
/// - Quản lý mở/đóng lồng bảng Cài đặt (SettingsHUD) khi đang Pause
/// - Tự động liên kết các Button con trong hierarchy nếu chưa gán thủ công
/// </summary>
public class PauseHUD : MonoBehaviour
{
    [Header("🔘 Action Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button menuButton;

    [Header("🔑 Online Room Info")]
    [Tooltip("Text hiển thị ID phòng khi đang chơi Online (Tự ẩn khi chơi Offline)")]
    public TMPro.TextMeshProUGUI roomCodeText;

    [Header("⚙️ Settings HUD Reference")]
    [Tooltip("Component quản lý HUD Settings (nếu có)")]
    public SettingsHUD settingsHUD;
    [Tooltip("GameObject Panel Settings (dùng fallback nếu không có component SettingsHUD)")]
    public GameObject settingPanel;

    [Header("🔊 Audio")]
    public AudioSource clickAudioSource;

    private UI_Manager uiManager;

    private void Awake()
    {
        AutoFindUIElements();
        RegisterEvents();
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UI_Manager>();
    }

    /// <summary>
    /// Tự động tìm kiếm các Button con và panel Settings nếu chưa gán trong Inspector
    /// </summary>
    public void AutoFindUIElements()
    {
        var allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            string bName = btn.gameObject.name.ToLower();

            if (resumeButton == null && (bName.Contains("resume") || bName.Contains("continue") || bName.Contains("play")))
            {
                resumeButton = btn;
            }
            else if (restartButton == null && (bName.Contains("restart") || bName.Contains("replay") || bName.Contains("retry")))
            {
                restartButton = btn;
            }
            else if (settingsButton == null && (bName.Contains("setting") || bName.Contains("option") || bName.Contains("config")))
            {
                settingsButton = btn;
            }
            else if (menuButton == null && (bName.Contains("menu") || bName.Contains("home") || bName.Contains("exit") || bName.Contains("quit")))
            {
                menuButton = btn;
            }
        }

        if (clickAudioSource == null)
        {
            clickAudioSource = GetComponentInChildren<AudioSource>(true);
            if (roomCodeText == null)
            {
                var allTexts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var t in allTexts)
                {
                    string tName = t.gameObject.name.ToLower();
                    if (tName.Contains("room") || tName.Contains("id") || tName.Contains("code"))
                    {
                        roomCodeText = t;
                        break;
                    }
                }
            }

            // Tìm SettingsHUD nếu chưa gán
            if (settingsHUD == null)
            {
                settingsHUD = FindFirstObjectByType<SettingsHUD>(FindObjectsInactive.Include);
            }

            if (settingsHUD != null && settingPanel == null)
            {
                settingPanel = settingsHUD.settingRootObject != null ? settingsHUD.settingRootObject : settingsHUD.gameObject;
            }
        }
    }
    private void RegisterEvents()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(OnResumeClicked);
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
        }
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            UpdateOnlineRoomInfo();
        }
    }

    private void UpdateOnlineRoomInfo()
    {
        bool isOnline = FusionConnectionManager.Instance != null &&
                        FusionConnectionManager.Instance.currentRunner != null &&
                        FusionConnectionManager.Instance.currentRunner.IsRunning;

        if (roomCodeText != null)
        {
            if (isOnline)
            {
                var runner = FusionConnectionManager.Instance.currentRunner;
                string roomName = !string.IsNullOrEmpty(FusionConnectionManager.Instance.currentSessionName)
                    ? FusionConnectionManager.Instance.currentSessionName
                    : (runner.SessionInfo.IsValid ? runner.SessionInfo.Name : "Room");

                int activePlayers = runner.SessionInfo.IsValid ? runner.SessionInfo.PlayerCount : 1;
                int maxPlayers = runner.SessionInfo.IsValid ? runner.SessionInfo.MaxPlayers : 4;

                roomCodeText.gameObject.SetActive(true);
                roomCodeText.text = $"Room: {roomName}   |   Players: {activePlayers}/{maxPlayers}";
            }
            else
            {
                roomCodeText.gameObject.SetActive(false);
            }
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Cập nhật thông tin phòng và số lượng người chơi
        UpdateOnlineRoomInfo();

        bool isOnline = FusionConnectionManager.Instance != null &&
                        FusionConnectionManager.Instance.currentRunner != null &&
                        FusionConnectionManager.Instance.currentRunner.IsRunning;

        // Nếu đang chơi Online: Làm mờ nút Restart (Chơi lại) và không cho bấm vì không thể restart trận đấu nhiều người
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = !isOnline;

            CanvasGroup cg = restartButton.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = restartButton.gameObject.AddComponent<CanvasGroup>();
            }
            cg.alpha = isOnline ? 0.4f : 1f;
            cg.interactable = !isOnline;
            cg.blocksRaycasts = !isOnline;
        }

        // Đảm bảo khi mới mở Pause lên thì bảng setting con đang tắt
        if (settingsHUD != null)
        {
            settingsHUD.ClosePanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        // Tắt luôn setting panel nếu đang mở dở
        if (settingsHUD != null)
        {
            settingsHUD.ClosePanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    public void OnResumeClicked()
    {
        PlayClickSound();
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.ResumeGame();
        }
    }

    public void OnRestartClicked()
    {
        PlayClickSound();
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.Replay();
        }
    }

    public void OnMenuClicked()
    {
        PlayClickSound();
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.Menu();
        }
    }

    public void OnSettingsClicked()
    {
        PlayClickSound();
        OpenSettings();
    }

    /// <summary>
    /// Mở giao diện SettingHUD và tạm ẩn Pause HUD
    /// </summary>
    public void OpenSettings()
    {
        if (settingsHUD != null)
        {
            settingsHUD.previousPanel = gameObject;
            settingsHUD.OpenPanel();
            gameObject.SetActive(false);
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PauseHUD] Không tìm thấy SettingsHUD hoặc settingPanel để mở!");
        }
    }

    /// <summary>
    /// Đóng giao diện SettingHUD và quay lại Pause HUD
    /// </summary>
    public void CloseSettings()
    {
        if (settingsHUD != null)
        {
            settingsHUD.ClosePanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    private void PlayClickSound()
    {
        if (clickAudioSource != null)
        {
            clickAudioSource.Play();
        }
    }
}
