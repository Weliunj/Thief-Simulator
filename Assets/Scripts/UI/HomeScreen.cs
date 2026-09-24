using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HomeScreen : MonoBehaviour
{
    [Header("🔊 Audio")]
    public AudioSource[] audioSources; // audioSources[0]: Hover, audioSources[1]: Click

    [Header("🔘 Main Menu Buttons")]
    public Button playButton;
    public Button exitButton;
    public Button optionsButton;
    public Button characterButton;
    public Button multiplayerButton; // Nút Chơi mạng (Multiplayer)
    public Button infoButton;        // Nút mở Hồ sơ / InfoPanel (Avatar hoặc Profile Icon)

    [Header("👤 Player Info Display (Lobby)")]
    public TextMeshProUGUI playerNameText; // Text hiển thị Tên người chơi ngoài HomeScreen
    public TextMeshProUGUI playerCashText; // Text hiển thị Tiền Cash ngoài HomeScreen (nếu có)

    [Header("🧪 Network Test / Simulation")]
    [Tooltip("Tích vào đây để giả lập Mất Mạng (Offline) ngay trong Unity Editor để kiểm tra nút Multiplayer mờ đi")]
    public bool simulateOffline = false;

    [Header("🧍 3D Lobby Player Model")]
    [Tooltip("GameObject chứa 3D Model nhân vật ngoài sảnh Home Menu (chỉ hiện khi ở HomeMenu)")]
    public GameObject lobbyPlayerModel;

    [Header("🚪 Panels Reference")]
    public GameObject authPanel; // Panel Đăng nhập / Đăng ký
    public GameObject mainMenuPanel;
    public GameObject settingPanel;
    public GameObject characterSelectPanel;
    public GameObject infoPanel; // Panel Hồ sơ / Thông tin người chơi (InfoPanel)
    public GameObject multiplayerLobbyPanel; // Panel Sảnh chờ Multiplayer Online (NetworkLobbyHUD)
    public ChapterSelectManager chapterSelectManager;

    private float networkCheckTimer = 0f;

    private void Awake()
    {
        // Đảm bảo trong HomeMenu chỉ có đúng 1 AudioListener duy nhất
        ScenePlayerSpawner.EnsureAudioListener();

        // Luôn mở khóa chuột khi vào màn hình Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tự động đảm bảo EventSystem hoạt động
        if (FindFirstObjectByType<UIEventSystemFixer>() == null)
        {
            gameObject.AddComponent<UIEventSystemFixer>();
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        FirebaseDataService.OnLoggedOutFromAnotherDevice += HandleLoggedOutFromAnotherDevice;
        FirebaseDataService.OnUserProfileLoaded += OnUserProfileChanged;
        FirebaseDataService.OnUserProfileUpdated += OnUserProfileChanged;
        FirebaseAuthService.OnUserSignedIn += OnUserSignedInChanged;
    }

    private void OnDisable()
    {
        FirebaseDataService.OnLoggedOutFromAnotherDevice -= HandleLoggedOutFromAnotherDevice;
        FirebaseDataService.OnUserProfileLoaded -= OnUserProfileChanged;
        FirebaseDataService.OnUserProfileUpdated -= OnUserProfileChanged;
        FirebaseAuthService.OnUserSignedIn -= OnUserSignedInChanged;
    }

    private void HandleLoggedOutFromAnotherDevice()
    {
        Debug.LogWarning("<color=yellow>[HomeScreen] Nhận thông báo đăng nhập trên thiết bị khác -> Chuyển về màn hình AuthHUD.</color>");

        // 1. Đóng toàn bộ các bảng trong sảnh
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (chapterSelectManager != null && chapterSelectManager.chapterSelectPanel != null)
        {
            chapterSelectManager.chapterSelectPanel.SetActive(false);
        }

        // 2. Mở lại AuthHUD
        if (authPanel == null)
        {
            AuthHUD existingHUD = FindFirstObjectByType<AuthHUD>(FindObjectsInactive.Include);
            if (existingHUD != null) authPanel = existingHUD.gameObject;
        }

        if (authPanel != null)
        {
            authPanel.SetActive(true);
            AuthHUD hud = authPanel.GetComponent<AuthHUD>() ?? FindFirstObjectByType<AuthHUD>(FindObjectsInactive.Include);
            if (hud != null)
            {
                hud.ShowLoginPanel(clearStatus: false);
                hud.ShowStatus("This account was logged in on another device. You have been logged out.", true, permanent: true);
                if (hud.loginPasswordInput != null) hud.loginPasswordInput.text = "";
            }
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tự động tìm AudioSources nếu chưa được kéo vào Inspector
        if (audioSources == null || audioSources.Length == 0)
        {
            audioSources = GetComponentsInChildren<AudioSource>(true);
            if (audioSources == null || audioSources.Length == 0)
            {
                audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }
        }

        // Tự động gán Mixer Group (BGM cho nhạc lặp, SFX cho tiếng click)
        if (audioSources != null && SettingsManager.Instance != null)
        {
            foreach (var src in audioSources)
            {
                if (src == null || src.outputAudioMixerGroup != null) continue;

                string objName = src.gameObject.name.ToLower();
                if (src.loop || objName.Contains("bgm") || objName.Contains("music"))
                {
                    src.outputAudioMixerGroup = SettingsManager.Instance.bgmGroup;
                }
                else
                {
                    src.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
                }
            }
        }

        // Tự động tìm AuthPanel nếu chưa kéo
        if (authPanel == null)
        {
            Transform a = transform.Find("AuthPanel") ?? transform.Find("AuthHUD") ?? transform.Find("LoginPanel");
            if (a != null) authPanel = a.gameObject;
        }

        bool isAlreadyLoggedIn = FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn;

        // Khởi động: Nếu chưa đăng nhập và có AuthPanel -> Bật AuthPanel, tắt Main Menu
        if (authPanel != null && !isAlreadyLoggedIn)
        {
            authPanel.SetActive(true);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
        else
        {
            if (authPanel != null) authPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        if (settingPanel != null) settingPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (chapterSelectManager != null && chapterSelectManager.chapterSelectPanel != null)
        {
            chapterSelectManager.chapterSelectPanel.SetActive(false);
        }



        // Tự động tìm các Buttons nếu chưa gán
        if (playButton == null)
        {
            Transform p = transform.Find("PlayButton") ?? transform.Find("PlayBtn");
            if (p != null) playButton = p.GetComponent<Button>();
        }
        if (exitButton == null)
        {
            Transform e = transform.Find("ExitButton") ?? transform.Find("ExitBtn");
            if (e != null) exitButton = e.GetComponent<Button>();
        }
        if (optionsButton == null)
        {
            Transform o = transform.Find("OptionsButton") ?? transform.Find("OptionButton") ?? transform.Find("SettingButton") ?? transform.Find("SettingsBtn");
            if (o != null) optionsButton = o.GetComponent<Button>();
        }
        if (characterButton == null)
        {
            Transform c = transform.Find("CharacterButton") ?? transform.Find("CharacterBtn") ?? transform.Find("CharBtn") ?? transform.Find("SkinBtn");
            if (c != null) characterButton = c.GetComponent<Button>();
        }
        if (multiplayerButton == null)
        {
            Transform m = transform.Find("MultiplayerButton") ?? transform.Find("MultiplayerBtn") ?? transform.Find("OnlineButton") ?? transform.Find("OnlineBtn") ?? transform.Find("CoopBtn");
            if (m != null) multiplayerButton = m.GetComponent<Button>();
        }
        if (infoPanel == null)
        {
            Transform info = transform.Find("InfoPanel") ?? transform.Find("ProfilePanel") ?? transform.Find("UserInfoPanel");
            if (info != null)
            {
                infoPanel = info.gameObject;
                infoPanel.SetActive(false);
            }
        }

        if (infoButton == null)
        {
            Transform ib = transform.Find("InfoButton") ?? transform.Find("InfoBtn") ?? transform.Find("ProfileButton") ?? transform.Find("ProfileBtn") ?? transform.Find("AvatarButton");
            if (ib != null) infoButton = ib.GetComponent<Button>();
        }

        if (playerNameText == null)
        {
            Transform pnt = transform.Find("PlayerNameText") ?? transform.Find("UsernameText") ?? transform.Find("NameText");
            if (pnt != null) playerNameText = pnt.GetComponent<TextMeshProUGUI>();
        }

        if (playerCashText == null)
        {
            Transform pct = transform.Find("PlayerCashText") ?? transform.Find("CashText") ?? transform.Find("GoldText");
            if (pct != null) playerCashText = pct.GetComponent<TextMeshProUGUI>();
        }

        // Đăng ký sự kiện Click cho toàn bộ các nút trên HomeScreen
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(Play_Clicked);
            playButton.onClick.AddListener(Play_Clicked);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Exit_Clicked);
            exitButton.onClick.AddListener(Exit_Clicked);
        }
        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(Options_Clicked);
            optionsButton.onClick.AddListener(Options_Clicked);
        }
        if (characterButton != null)
        {
            characterButton.onClick.RemoveListener(Character_Clicked);
            characterButton.onClick.AddListener(Character_Clicked);
        }
        if (multiplayerButton != null)
        {
            multiplayerButton.onClick.RemoveListener(Multiplayer_Clicked);
            multiplayerButton.onClick.AddListener(Multiplayer_Clicked);
        }
        if (infoButton != null)
        {
            infoButton.onClick.RemoveListener(Info_Clicked);
            infoButton.onClick.AddListener(Info_Clicked);
        }

        UpdatePlayerProfileVisuals();
        UpdateNetworkButtonsVisuals();
        SyncSavedCharacter();
    }

    private void OnUserProfileChanged(UserGameProfile profile)
    {
        UpdatePlayerProfileVisuals();
        SyncSavedCharacter();
    }

    private void OnUserSignedInChanged(Firebase.Auth.FirebaseUser user)
    {
        UpdatePlayerProfileVisuals();
        SyncSavedCharacter();
    }

    /// <summary>
    /// Tự động load và đồng bộ Nhân vật đã lưu (GameSession.SelectedPlayer, Lobby Model)
    /// </summary>
    public void SyncSavedCharacter()
    {
        CharacterSelectionHUD hud = null;
        if (characterSelectPanel != null)
        {
            hud = characterSelectPanel.GetComponent<CharacterSelectionHUD>() ?? characterSelectPanel.GetComponentInChildren<CharacterSelectionHUD>(true);
        }
        if (hud == null)
        {
            hud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        }

        if (hud != null)
        {
            if (hud.lobbyPlayerModel == null && lobbyPlayerModel != null)
            {
                hud.lobbyPlayerModel = lobbyPlayerModel;
            }
            hud.LoadSavedSelection();
            hud.ApplyLobbyModelVisuals();
        }
    }

    /// <summary>
    /// Cập nhật Tên người chơi và Tiền mặt hiển thị trực tiếp ngoài HomeScreen
    /// </summary>
    public void UpdatePlayerProfileVisuals()
    {
        string username = "Thief";
        int cash = 0;

        if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
        {
            username = FirebaseDataService.Instance.CurrentUserProfile.username;
            cash = FirebaseDataService.Instance.CurrentUserProfile.cash;
        }
        else if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.CurrentUser != null)
        {
            username = string.IsNullOrEmpty(FirebaseAuthService.Instance.CurrentUser.DisplayName) ? "Thief" : FirebaseAuthService.Instance.CurrentUser.DisplayName;
        }

        if (playerNameText != null) playerNameText.text = username;
        if (playerCashText != null) playerCashText.text = $"${cash}";
    }

    /// <summary>
    /// Alias để các HUD khác có thể gọi đồng bộ
    /// </summary>
    public void RefreshProfileUI() => UpdatePlayerProfileVisuals();

    private void Update()
    {
        // Phím tắt Test: Bấm 'M' để cộng 1.000$ Cash
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (FirebaseDataService.Instance != null)
            {
                FirebaseDataService.Instance.AddCash(1000);
                UpdatePlayerProfileVisuals();
                Debug.Log("<color=green>[CHEAT TEST] Đã bấm 'M' → Cộng +1,000$ Tiền mặt!</color>");
            }
        }

        // Kiểm tra trạng thái mạng định kỳ mỗi 0.5s để cập nhật độ sáng/mờ của nút Multiplayer
        networkCheckTimer += Time.unscaledDeltaTime;
        if (networkCheckTimer >= 0.5f)
        {
            networkCheckTimer = 0f;
            UpdateNetworkButtonsVisuals();
        }
    }

    /// <summary>
    /// Cập nhật hiển thị (sáng/mờ/khóa) của các nút Online/Multiplayer dựa trên kết nối mạng
    /// </summary>
    public void UpdateNetworkButtonsVisuals()
    {
        bool isOnline = !simulateOffline && FirebaseAuthService.IsNetworkAvailable;

        if (multiplayerButton != null)
        {
            multiplayerButton.interactable = isOnline;

            // Làm mờ Image của Button khi mất mạng
            Image btnImg = multiplayerButton.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = isOnline ? Color.white : new Color(1f, 1f, 1f, 0.38f);
            }

            // Làm mờ Text con của Button
            TMPro.TextMeshProUGUI btnText = multiplayerButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (btnText != null)
            {
                btnText.alpha = isOnline ? 1.0f : 0.4f;
            }
        }
    }

    public void Multiplayer_Clicked()
    {
        PlayClickSound();
        if (!FirebaseAuthService.IsNetworkAvailable || simulateOffline)
        {
            Debug.LogWarning("[HomeScreen] Không thể vào chế độ Multiplayer vì không có kết nối mạng!");
            return;
        }

        Debug.Log("<color=cyan>[HomeScreen] Mở sảnh Multiplayer Online (Task 2.1)!</color>");

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // Ẩn 3D model ngoài sảnh khi mở Lobby
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.SetLobbyModelVisible(false);
        }

        if (multiplayerLobbyPanel == null)
        {
            NetworkLobbyHUD existingLobby = FindFirstObjectByType<NetworkLobbyHUD>(FindObjectsInactive.Include);
            if (existingLobby != null) multiplayerLobbyPanel = existingLobby.gameObject;
        }

        if (multiplayerLobbyPanel != null)
        {
            multiplayerLobbyPanel.SetActive(true);
            NetworkLobbyHUD lobbyHUD = multiplayerLobbyPanel.GetComponent<NetworkLobbyHUD>() ?? multiplayerLobbyPanel.GetComponentInChildren<NetworkLobbyHUD>(true);
            if (lobbyHUD != null)
            {
                lobbyHUD.previousHomePanel = mainMenuPanel;
                lobbyHUD.ShowLobbyMain();
            }
        }
    }

    public void Character_Clicked()
    {
        PlayClickSound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        CharacterSelectionHUD hud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (hud != null)
        {
            hud.SetLobbyModelVisible(false);
        }

        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
            if (hud != null)
            {
                hud.previousPanel = mainMenuPanel;
            }
        }
    }

    public void CloseCharacterSelect()
    {
        PlayClickSound();
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        // Hiện lại 3D model ngoài sảnh chính
        CharacterSelectionHUD hud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (hud != null)
        {
            hud.ApplyLobbyModelVisuals();
        }
    }

    public void Info_Clicked()
    {
        PlayClickSound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // Ẩn 3D model ngoài sảnh khi mở InfoPanel
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.SetLobbyModelVisible(false);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            ProfileHUD pHud = infoPanel.GetComponent<ProfileHUD>() ?? infoPanel.GetComponentInChildren<ProfileHUD>(true);
            if (pHud != null)
            {
                pHud.mainMenuPanel = mainMenuPanel;
                pHud.authPanel = authPanel;
                pHud.RefreshProfileUI();
            }
        }
    }

    public void CloseInfoPanel()
    {
        PlayClickSound();
        if (infoPanel != null) infoPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        // Hiện lại 3D model ngoài sảnh chính
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.ApplyLobbyModelVisuals();
        }

        UpdatePlayerProfileVisuals();
    }

    public void Play_Clicked()
    {
        PlayClickSound();

        // Ẩn 3D model ngoài sảnh khi chuyển sang màn hình Chapter Select
        CharacterSelectionHUD hud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (hud != null)
        {
            hud.SetLobbyModelVisible(false);
        }

        if (chapterSelectManager != null)
        {
            chapterSelectManager.OpenChapterSelect();
        }
        else
        {
            StartCoroutine(LoadSceneAfterDelay(1, 0.5f));
        }
    }

    public void Exit_Clicked()
    {
        PlayClickSound();
        if (FirebaseDataService.Instance != null)
        {
            FirebaseDataService.Instance.SaveLocalSnapshot();
        }
        Debug.Log("Exit Game");
        StartCoroutine(QuitAfterDelay(0.5f));
    }

    public void Options_Clicked()
    {
        PlayClickSound();
        Debug.Log("Options Clicked");
        
        // Đóng Main Menu trước đó và mở Setting
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // Ẩn 3D model ngoài sảnh khi mở Setting để không che khuất UI
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.SetLobbyModelVisible(false);
        }
        
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            SettingsHUD hud = settingPanel.GetComponent<SettingsHUD>() ?? settingPanel.GetComponentInChildren<SettingsHUD>(true);
            if (hud != null)
            {
                hud.previousPanel = mainMenuPanel;
                hud.OpenPanel();
            }
        }
    }

    public void PlayClickSound()
    {
        if (audioSources != null && audioSources.Length > 0)
        {
            // Ưu tiên audioSources[1] (click), fallback về audioSources[0]
            if (audioSources.Length > 1 && audioSources[1] != null)
            {
                audioSources[1].Play();
            }
            else if (audioSources[0] != null)
            {
                audioSources[0].Play();
            }
        }
        else
        {
            var anyAudio = GetComponentInChildren<AudioSource>(true) ?? FindFirstObjectByType<AudioSource>();
            if (anyAudio != null)
            {
                anyAudio.Play();
            }
        }
    }

    private IEnumerator LoadSceneAfterDelay(int sceneIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
