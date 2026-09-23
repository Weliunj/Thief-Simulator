using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Manager : MonoBehaviour
{
    // =========================================================================
    [Header("⚙️ References")]
    [Tooltip("Component quản lý toàn bộ chỉ số runtime đang diễn ra (Stamina, Weight, Time, Point...)")]
    public PlayerStats playerStats;
    [Tooltip("ScriptableObject dữ liệu gốc của nhân vật (fallback)")]
    public PlayerSO playerData;

    /// <summary>
    /// Thuộc tính tương thích ngược cho các đoạn mã cũ
    /// </summary>
    public PlayerStats playerManager => playerStats;

    public static bool isSolving = false;

    public GameObject diedPanel;
    public GameObject WinPanel;

    [Header("📱 Main HUD")]
    [Tooltip("Component quản lý HUD chính trong trận (Kg, Stamina, Point, Time, Alarm, PauseBtn)")]
    public MainHUD mainHUD;
    [Tooltip("Fallback GameObject Panel nếu không gán MainHUD component")]
    public GameObject mainHUDPanel;

    [Header("⏸️ Pause & Settings UI")]
    [Tooltip("Component quản lý giao diện Tạm dừng (Resume, Restart, Settings, Menu)")]
    public PauseHUD pauseHUD;
    [Tooltip("Component quản lý bảng Cài đặt (Sensitivity, Save, Close)")]
    public SettingsHUD settingsHUD;
    [Tooltip("Fallback GameObject Panel nếu không dùng component PauseHUD/SettingsHUD")]
    public GameObject settingPanel;
    [HideInInspector] public bool isPaused = false;

    [Header("📖 Chapter Data")]
    public ChapterSO currentChapter;
    public ChapterSO nextChapter;

    [Header("🔑 Lockpick Minigame UI")]
    public LockpickMinigame lockpickMinigame;

    // =========================================================================

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isSolving = false;

        // Tự động tìm MainHUD nếu chưa gán
        if (mainHUD == null)
        {
            mainHUD = GetComponentInChildren<MainHUD>(true);
            if (mainHUD == null)
            {
                mainHUD = FindFirstObjectByType<MainHUD>(FindObjectsInactive.Include);
            }
        }

        // Tự động tìm PauseHUD nếu chưa gán
        if (pauseHUD == null)
        {
            pauseHUD = GetComponentInChildren<PauseHUD>(true);
            if (pauseHUD == null)
            {
                pauseHUD = FindFirstObjectByType<PauseHUD>(FindObjectsInactive.Include);
            }
        }

        // Tự động tìm SettingsHUD nếu chưa gán
        if (settingsHUD == null)
        {
            settingsHUD = GetComponentInChildren<SettingsHUD>(true);
            if (settingsHUD == null)
            {
                settingsHUD = FindFirstObjectByType<SettingsHUD>(FindObjectsInactive.Include);
            }
        }

        SetMainHUDActive(true);
        if (pauseHUD != null) pauseHUD.Hide();
        if (settingsHUD != null) settingsHUD.ClosePanel();
        else if (settingPanel != null) settingPanel.SetActive(false);

        if (diedPanel != null) diedPanel.SetActive(false);
        if (WinPanel != null) WinPanel.SetActive(false);

        if (lockpickMinigame == null)
        {
            lockpickMinigame = FindFirstObjectByType<LockpickMinigame>(FindObjectsInactive.Include);
        }
        if (lockpickMinigame != null)
        {
            lockpickMinigame.CloseMinigame();
        }

        // Nhận dữ liệu Chapter và Character từ GameSession nếu được mở từ Menu
        if (GameSession.SelectedChapter != null)
        {
            currentChapter = GameSession.SelectedChapter;
        }
        if (GameSession.NextChapter != null)
        {
            nextChapter = GameSession.NextChapter;
        }
        if (GameSession.SelectedPlayer != null)
        {
            playerData = GameSession.SelectedPlayer;
        }

        // Tự động tìm PlayerStats trong scene nếu chưa được gán
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats == null)
        {
            Debug.LogWarning("Không tìm thấy PlayerStats trong Scene.");
        }
        else
        {
            // Khởi tạo thông số từ ChapterSO nếu có, hoặc reset mặc định
            if (currentChapter != null)
            {
                playerStats.InitializeChapter(currentChapter);
            }
            else
            {
                playerStats.isDied = false;
                playerStats.currweight = 0;
                playerStats.currpoint = 0;
                playerStats.currentStamina = playerStats.MaxStamina;
                playerStats.currentTime = playerStats.MaxTime;
            }

            // Khởi tạo thông số hiển thị ban đầu trên HUD
            if (mainHUD != null)
            {
                mainHUD.InitializeMaxValues(playerStats);
            }
        }
    }

    void Update()
    {
        if (playerStats == null) return;

        // --- CẬP NHẬT THỜI GIAN ĐẾM NGƯỢC ---
        if (!playerStats.isDied && playerStats.currpoint < playerStats.totalpoint)
        {
            playerStats.currentTime -= Time.deltaTime;

            if (playerStats.currentTime <= 0.3f)
            {
                playerStats.currentTime = 0f;
                Debug.Log("HẾT THỜI GIAN! Game Over (tạm thời chỉ debug)");
            }
        }

        // --- CẬP NHẬT GIAO DIỆN MAIN HUD ---
        if (mainHUD != null)
        {
            mainHUD.UpdateHUD(playerStats);
        }

        // --- XỬ LÝ TRẠNG THÁI GAME ---
        UpdateGameState();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    private bool isWinHandled = false;
    private bool isDiedHandled = false;

    void UpdateGameState()
    {
        if (playerStats == null) return;

        if (playerStats.isDied)
        {
            if (!isDiedHandled)
            {
                isDiedHandled = true;
                if (isSolving) CancelLockpicking();
                if (pauseHUD != null) pauseHUD.Hide();
                if (settingsHUD != null) settingsHUD.ClosePanel();
                else if (settingPanel != null) settingPanel.SetActive(false);
                SetMainHUDActive(false);
                if (WinPanel != null) WinPanel.SetActive(false);
                if (diedPanel != null) diedPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else if (playerStats.totalpoint > 0 && playerStats.currpoint >= playerStats.totalpoint)
        {
            if (!isWinHandled)
            {
                isWinHandled = true;
                if (isSolving) CancelLockpicking();
                if (pauseHUD != null) pauseHUD.Hide();
                if (settingsHUD != null) settingsHUD.ClosePanel();
                else if (settingPanel != null) settingPanel.SetActive(false);
                SetMainHUDActive(false);
                if (diedPanel != null) diedPanel.SetActive(false);
                if (WinPanel != null) WinPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Tự động tích hoàn thành Chapter hiện tại và mở khóa Chapter tiếp theo
                if (currentChapter != null)
                {
                    currentChapter.isCompleted = true;
                }
                if (nextChapter != null)
                {
                    nextChapter.isUnlocked = true;
                }

                Debug.Log($"WIN! Đã hoàn thành '{currentChapter?.chapterTitle ?? "Chapter"}' - Đạt {playerStats.currpoint}/{playerStats.totalpoint} điểm!");
            }
        }
    }

    // =========================================================================
    //                       SETTINGS & PAUSE LOGIC
    // =========================================================================

    public void ToggleSettings()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetMainHUDActive(false);

        if (pauseHUD != null)
        {
            pauseHUD.Show();
        }
        else if (settingsHUD != null)
        {
            settingsHUD.OpenPanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseHUD != null)
        {
            pauseHUD.Hide();
        }
        if (settingsHUD != null)
        {
            settingsHUD.ClosePanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        // Chỉ bật lại Main HUD nếu không ở trong Minigame và chưa Win / Lose
        if (!isSolving && !isDiedHandled && !isWinHandled)
        {
            SetMainHUDActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Mở bảng Setting từ script bên ngoài hoặc nút bấm
    /// </summary>
    public void OpenSettings()
    {
        if (pauseHUD != null)
        {
            pauseHUD.OpenSettings();
        }
        else if (settingsHUD != null)
        {
            settingsHUD.OpenPanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Đóng bảng Setting
    /// </summary>
    public void CloseSettings()
    {
        if (pauseHUD != null)
        {
            pauseHUD.CloseSettings();
        }
        else if (settingsHUD != null)
        {
            settingsHUD.ClosePanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Helper đồng bộ bật/tắt MainHUD
    /// </summary>
    public void SetMainHUDActive(bool active)
    {
        if (mainHUD != null)
        {
            if (active) mainHUD.Show();
            else mainHUD.Hide();
        }
        else if (mainHUDPanel != null)
        {
            mainHUDPanel.SetActive(active);
        }
    }

    // =========================================================================
    //                       LOCKPICK MINIGAME API
    // =========================================================================

    public void StartLockpicking(DoorController door)
    {
        if (lockpickMinigame == null)
        {
            lockpickMinigame = FindFirstObjectByType<LockpickMinigame>(FindObjectsInactive.Include);
        }

        if (lockpickMinigame == null)
        {
            Debug.LogError("LockpickMinigame chưa được gán hoặc không tìm thấy trong Canvas UI!");
            return;
        }

        isSolving = true;
        SetMainHUDActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        lockpickMinigame.StartMinigame(
            onSuccess: () =>
            {
                isSolving = false;
                if (!isPaused && !isDiedHandled && !isWinHandled)
                {
                    SetMainHUDActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if (door != null) door.OnUnlockSuccess();
            },
            onFailed: () =>
            {
                isSolving = false;
                if (!isPaused && !isDiedHandled && !isWinHandled)
                {
                    SetMainHUDActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if (door != null) door.OnUnlockFailed();
            },
            door: door
        );
    }

    public void CancelLockpicking()
    {
        isSolving = false;
        if (!isPaused && !isDiedHandled && !isWinHandled)
        {
            SetMainHUDActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (lockpickMinigame != null)
        {
            lockpickMinigame.CloseMinigame();
        }
    }

    // =========================================================================
    //                            LOGIC UI CƠ BẢN
    // =========================================================================

    public void Replay()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (playerManager != null)
        {
            playerManager.isDied = false;
            playerManager.currweight = 0;
            playerManager.currpoint = 0;
            playerManager.currentTime = playerManager.MaxTime;
            playerManager._stamina = playerManager.MaxStamina;
        }

        // Tải lại đúng Scene hiện tại của màn chơi đang chơi
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Menu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameSession.ClearSession();

        HotbarManager hotbar = FindFirstObjectByType<HotbarManager>(FindObjectsInactive.Include);
        if (hotbar != null)
        {
            hotbar.ClearAllSlots();
        }

        SceneManager.LoadScene("HomeMenu");
    }
}