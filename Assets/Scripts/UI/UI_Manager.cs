using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Manager : MonoBehaviour
{
    // =========================================================================
    [Header("⚙️ References")]
    public PlayerManager playerManager;
    public static bool isSolving = false;
    
    public GameObject diedPanel;
    public GameObject WinPanel;

    [Header("📱 Main HUD & Mobile Controls")]
    [Tooltip("Panel chứa HUD game chính & các nút điều khiển mobile (tự động tắt khi vào Minigame)")]
    public GameObject mainHUDPanel;

    [Header("⚙️ Settings UI")]
    public GameObject settingPanel;
    [HideInInspector] public bool isPaused = false;

    [Header("🔋 Stamina UI")]
    public TextMeshProUGUI currStamina; 
    public TextMeshProUGUI Stamina; 

    [Header("🏋️ Weight UI")]
    public TextMeshProUGUI currkg;       
    public TextMeshProUGUI kg;          

    [Header("⚠️ Warning Colors")]
    [Tooltip("Normal base color (used when value < threshold)")]
    public Color normalColor = Color.white;
    [Tooltip("Alert color (used at 100%)")]
    public Color alertColor = Color.red;
    [Range(0f, 1f)]
    [Tooltip("Normalized threshold where coloring starts (0.5 = 50%)")]
    public float warnThreshold = 0.5f;

    [Header("🌟 Point UI")]
    public TextMeshProUGUI point;       
    public TextMeshProUGUI targetPoint;
    
    [Header("⏰ Time UI")]
    public TextMeshProUGUI timeText; // Text hiển thị thời gian còn lại
    public AudioSource alarm;
    [Header("🔑 Lockpick Minigame UI")]
    public LockpickMinigame lockpickMinigame;

    // =========================================================================

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isSolving = false;

        if (diedPanel != null) diedPanel.SetActive(false);
        if (WinPanel != null) WinPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);

        if (playerManager == null)
        {
            Debug.LogError("PlayerManager ScriptableObject chưa được gán.");
            enabled = false;
            return;
        }

        // Tự động Reset dữ liệu PlayerManager khi vừa vào Scene mới
        playerManager.isDied = false;
        playerManager.currweight = 0;
        playerManager.currpoint = 0;
        playerManager._stamina = playerManager.MaxStamina;
        playerManager.currentTime = playerManager.MaxTime;

        // Thiết lập giá trị Max/Target Point cố định
        if (Stamina != null) { Stamina.text = $"{playerManager.MaxStamina:F0}"; }
        if (kg != null) { kg.text = $"{playerManager.Maxweight}"; }
        if (targetPoint != null) { targetPoint.text = $"{playerManager.totalpoint}"; }
    }

    void Update()
    {
        if (playerManager == null) return;

        if (playerManager.currentTime <= 0f)
        {
            if (alarm != null && !alarm.isPlaying)
            {
                alarm.loop = true;
                alarm.Play();
            }
        }
        
        // --- CẬP NHẬT THỜI GIAN ---
        if (!playerManager.isDied && playerManager.currpoint < playerManager.totalpoint)
        {
            playerManager.currentTime -= Time.deltaTime;
            
            if (playerManager.currentTime <= 0.3f)
            {
                playerManager.currentTime = 0f;
                Debug.Log("HẾT THỜI GIAN! Game Over (tạm thời chỉ debug)");
            }
        }
        
        // --- CẬP NHẬT UI ĐỘNG ---
        if (currStamina != null) 
        { 
            currStamina.text = $"{playerManager._stamina:F1}"; 
            if (playerManager.MaxStamina > 0f)
            {
                float sNorm = Mathf.Clamp01(playerManager._stamina / playerManager.MaxStamina);
                Color sColor = normalColor;
                if (sNorm <= warnThreshold)
                {
                    float t = Mathf.InverseLerp(warnThreshold, 0f, sNorm);
                    sColor = Color.Lerp(normalColor, alertColor, t);
                }
                currStamina.color = sColor;
            }
        }

        if (currkg != null) 
        { 
            currkg.text = $"{playerManager.currweight}"; 
            if (playerManager.Maxweight > 0)
            {
                float wNorm = Mathf.Clamp01((float)playerManager.currweight / (float)playerManager.Maxweight);
                Color wColor = normalColor;
                if (wNorm >= warnThreshold)
                {
                    float t = Mathf.InverseLerp(warnThreshold, 1f, wNorm);
                    wColor = Color.Lerp(normalColor, alertColor, t);
                }
                currkg.color = wColor;
            }
        }

        if (point != null) { point.text = $"{playerManager.currpoint}"; }
        
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(playerManager.currentTime / 60f);
            int seconds = Mathf.FloorToInt(playerManager.currentTime % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
        
        // --- XỬ LÝ TRẠNG THÁI GAME ---
        if (playerManager.isDied)
        {
            if (isSolving) CancelLockpicking();
            if (diedPanel != null) diedPanel.SetActive(true); 
            if (WinPanel != null) WinPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
        } 
        else if (playerManager.currpoint >= playerManager.totalpoint)
        {
            if (isSolving) CancelLockpicking();
            if (diedPanel != null) diedPanel.SetActive(false); 
            if (WinPanel != null) WinPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
            Debug.Log($"WIN! Đã đạt {playerManager.currpoint}/{playerManager.totalpoint} điểm!");
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
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
        if (settingPanel != null) settingPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (settingPanel != null) settingPanel.SetActive(false);

        if (!isSolving && (playerManager == null || (!playerManager.isDied && playerManager.currpoint < playerManager.totalpoint)))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // =========================================================================
    //                       LOCKPICK MINIGAME API
    // =========================================================================

    public void StartLockpicking(LockpickDoor door)
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
        if (mainHUDPanel != null) mainHUDPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        lockpickMinigame.StartMinigame(
            onSuccess: () =>
            {
                isSolving = false;
                if (mainHUDPanel != null) mainHUDPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (door != null) door.OnUnlockSuccess();
            },
            onFailed: () =>
            {
                isSolving = false;
                if (mainHUDPanel != null) mainHUDPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (door != null) door.OnUnlockFailed();
            }
        );
    }

    public void CancelLockpicking()
    {
        isSolving = false;
        if (mainHUDPanel != null) mainHUDPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        SceneManager.LoadScene("Lv1");
    }
    
    public void Menu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("HomeMenu");
    }
}