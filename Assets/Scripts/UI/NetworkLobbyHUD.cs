using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Sảnh chờ Mạng (Network Lobby HUD):
/// - Danh sách phòng trực tuyến (Room List View) với tự động làm mới.
/// - Modal Tạo phòng (Create Room) với tùy chọn số lượng người chơi, bản đồ và phòng kín/công khai.
/// - Modal Tham gia bằng mã phòng (Join by Code).
/// - Màn hình phòng chờ (Waiting Room): Hiển thị danh sách thành viên, Sẵn sàng (Ready), Bắt đầu trận (Start Game).
/// </summary>
public class NetworkLobbyHUD : MonoBehaviour
{
    [Header("🚪 Panels Reference")]
    public GameObject lobbyMainPanel;
    public GameObject createRoomModal;
    public GameObject joinCodeModal;
    public GameObject waitingRoomPanel;
    public GameObject loadingSpinner;

    [Header("📋 Lobby Main View")]
    public Transform roomListContent;
    public GameObject roomItemPrefab;
    public TextMeshProUGUI emptyListText;
    public Button refreshButton;
    public Button openCreateModalButton;
    public Button openJoinCodeModalButton;
    public Button quickMatchButton;
    public Button backToHomeButton;

    [Header("➕ Create Room Modal")]
    public TMP_InputField createRoomNameInput;
    public TMP_Dropdown maxPlayersDropdown;
    public TMP_Dropdown difficultyDropdown; // Dropdown chọn Độ khó: 0 = Easy, 1 = Normal, 2 = Hard
    [Tooltip("Toggle cho phép kích hoạt thêm các NPC đặc biệt (Special NPCs)")]
    public Toggle specialNpcsToggle;
    public TMP_Dropdown mapSelectDropdown; // Tùy chọn fallback
    public Button openMapSelectButton;     // Nút mở panel chọn Chapter
    public Image createRoomMapPreviewImage; // Ảnh preview Chapter được chọn
    public TextMeshProUGUI createRoomMapTitleText; // Tiêu đề Chapter được chọn
    public Toggle privateRoomToggle;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public ChapterSelectManager chapterSelectManager;
    public int selectedMapIndex = 0;

    [Header("🔑 Join By Code Modal")]
    public TMP_InputField joinCodeInput;
    public Button confirmJoinCodeButton;
    public Button cancelJoinCodeButton;

    [Header("⏳ Waiting Room View")]
    public TextMeshProUGUI waitingRoomTitleText;
    public TextMeshProUGUI waitingRoomCodeText;
    public Image waitingRoomMapImage;
    public TextMeshProUGUI waitingRoomMapTitleText;
    public Transform playerListContent;
    public GameObject playerSlotPrefab;
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;
    public Button startGameButton;
    public Button leaveRoomButton;

    [Header("🗺️ Chapter & Map Data")]
    [Tooltip("Danh sách các ChapterSO để nạp ảnh bìa, tiêu đề và Scene tương ứng")]
    public List<ChapterSO> chapterList = new List<ChapterSO>();

    [Header("📢 Status Message")]
    public GameObject statusMessagePanel;
    public TextMeshProUGUI statusMessageText;

    [Header("⚙️ Scene Names (Fallback nếu không dùng ChapterSO)")]
    public string[] mapSceneNames = new string[] { "Chapter1", "Chapter2" };

    public GameObject previousHomePanel;

    [Header("🔊 Audio")]
    public AudioSource clickAudioSource;
    public AudioClip clickSoundClip;

    private bool isLocalPlayerReady = false;
    private Dictionary<int, bool> playerReadyStates = new Dictionary<int, bool>();
    private Coroutine statusClearCoroutine;

    private void Awake()
    {
        if (chapterSelectManager == null)
        {
            chapterSelectManager = FindFirstObjectByType<ChapterSelectManager>(FindObjectsInactive.Include);
        }
        if ((chapterList == null || chapterList.Count == 0) && chapterSelectManager != null && chapterSelectManager.chapterList != null && chapterSelectManager.chapterList.Count > 0)
        {
            chapterList = chapterSelectManager.chapterList;
        }

        SetupButtons();
        InitializeMapDropdown();
        EnsureAudioSource();

        if (statusMessagePanel == null && statusMessageText != null)
        {
            Transform parent = statusMessageText.transform.parent;
            if (parent != null && parent != transform && (parent.name.ToLower().Contains("status") || parent.name.ToLower().Contains("panel") || parent.name.ToLower().Contains("msg") || parent.name.ToLower().Contains("message")))
            {
                statusMessagePanel = parent.gameObject;
            }
        }

        if (statusMessagePanel != null)
        {
            statusMessagePanel.SetActive(false);
        }
        else if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(false);
        }
    }

    private void EnsureAudioSource()
    {
        if (clickAudioSource == null)
        {
            clickAudioSource = GetComponentInChildren<AudioSource>(true);
            if (clickAudioSource == null)
            {
                clickAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (clickAudioSource != null && SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            clickAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
        }
    }

    public void PlayClickSound()
    {
        EnsureAudioSource();

        if (clickAudioSource != null)
        {
            if (clickSoundClip != null)
            {
                clickAudioSource.PlayOneShot(clickSoundClip);
            }
            else if (clickAudioSource.clip != null)
            {
                clickAudioSource.PlayOneShot(clickAudioSource.clip);
            }
        }
    }

    private void InitializeMapDropdown()
    {
        if (mapSelectDropdown == null) return;

        mapSelectDropdown.onValueChanged.RemoveAllListeners();
        mapSelectDropdown.onValueChanged.AddListener(OnMapSelectDropdownChanged);

        if (chapterList != null && chapterList.Count > 0)
        {
            mapSelectDropdown.ClearOptions();
            List<string> options = new List<string>();
            foreach (var ch in chapterList)
            {
                if (ch != null) options.Add(ch.chapterTitle);
            }
            if (options.Count > 0) mapSelectDropdown.AddOptions(options);
        }
    }

    public Sprite GetMapSprite(int index)
    {
        if (chapterList != null && index >= 0 && index < chapterList.Count && chapterList[index] != null)
        {
            return chapterList[index].chapterImage;
        }
        return null;
    }

    public string GetMapTitle(int index)
    {
        if (chapterList != null && index >= 0 && index < chapterList.Count && chapterList[index] != null && !string.IsNullOrEmpty(chapterList[index].chapterTitle))
        {
            return chapterList[index].chapterTitle;
        }
        if (mapSceneNames != null && index >= 0 && index < mapSceneNames.Length)
        {
            return mapSceneNames[index];
        }
        return "Chapter 1";
    }

    public string GetMapSceneName(int index)
    {
        if (chapterList != null && index >= 0 && index < chapterList.Count && chapterList[index] != null)
        {
            if (!string.IsNullOrEmpty(chapterList[index].gameplaySceneName))
                return chapterList[index].gameplaySceneName;
            if (!string.IsNullOrEmpty(chapterList[index].sceneName))
                return chapterList[index].sceneName;
        }
        if (mapSceneNames != null && index >= 0 && index < mapSceneNames.Length)
        {
            return mapSceneNames[index];
        }
        return "Chapter1";
    }

    public int GetCurrentMapIndex()
    {
        // 1. Kiểm tra Session Properties "map" của Fusion Runner hiện tại
        if (FusionConnectionManager.Instance != null && FusionConnectionManager.Instance.currentRunner != null && FusionConnectionManager.Instance.currentRunner.IsRunning)
        {
            var session = FusionConnectionManager.Instance.currentRunner.SessionInfo;
            if (session != null && session.IsValid && session.Properties != null && session.Properties.TryGetValue("map", out var mapProp))
            {
                string mapName = mapProp.PropertyValue as string;
                if (!string.IsNullOrEmpty(mapName))
                {
                    if (chapterList != null)
                    {
                        for (int i = 0; i < chapterList.Count; i++)
                        {
                            var ch = chapterList[i];
                            if (ch != null)
                            {
                                if ((!string.IsNullOrEmpty(ch.gameplaySceneName) && ch.gameplaySceneName.Equals(mapName, System.StringComparison.OrdinalIgnoreCase)) ||
                                    (!string.IsNullOrEmpty(ch.sceneName) && ch.sceneName.Equals(mapName, System.StringComparison.OrdinalIgnoreCase)) ||
                                    (!string.IsNullOrEmpty(ch.chapterTitle) && ch.chapterTitle.Equals(mapName, System.StringComparison.OrdinalIgnoreCase)))
                                {
                                    return i;
                                }
                            }
                        }
                    }
                    if (mapSceneNames != null)
                    {
                        for (int i = 0; i < mapSceneNames.Length; i++)
                        {
                            if (mapSceneNames[i].Equals(mapName, System.StringComparison.OrdinalIgnoreCase))
                            {
                                return i;
                            }
                        }
                    }
                }
            }
        }

        // 2. Kiểm tra GameSession.SelectedChapter
        if (GameSession.SelectedChapter != null && chapterList != null)
        {
            int sessionIndex = chapterList.IndexOf(GameSession.SelectedChapter);
            if (sessionIndex >= 0)
            {
                return sessionIndex;
            }
        }

        // 3. Kiểm tra biến selectedMapIndex
        if (chapterList != null && selectedMapIndex >= 0 && selectedMapIndex < chapterList.Count)
        {
            return selectedMapIndex;
        }

        // 4. Kiểm tra Dropdown nếu có
        if (mapSelectDropdown != null && mapSelectDropdown.options != null && mapSelectDropdown.options.Count > 0)
        {
            return mapSelectDropdown.value;
        }

        return 0;
    }

    private void OnMapSelectDropdownChanged(int index)
    {
        selectedMapIndex = index;
        UpdateWaitingRoomMapPreview(index);
    }

    public void UpdateWaitingRoomMapPreview(int index = -1)
    {
        if (index < 0)
        {
            index = GetCurrentMapIndex();
        }
        if (index < 0) index = 0;

        selectedMapIndex = index;

        Sprite sprite = GetMapSprite(index);
        string title = GetMapTitle(index);

        if (waitingRoomMapImage != null)
        {
            waitingRoomMapImage.sprite = sprite;
            waitingRoomMapImage.gameObject.SetActive(sprite != null);
        }

        if (chapterList != null && index >= 0 && index < chapterList.Count)
        {
            GameSession.SelectedChapter = chapterList[index];
            int nextIndex = index + 1;
            GameSession.NextChapter = (nextIndex < chapterList.Count) ? chapterList[nextIndex] : null;
        }

        if (waitingRoomMapTitleText != null)
        {
            waitingRoomMapTitleText.text = title;
        }
    }

    private void OnEnable()
    {
        FusionConnectionManager.OnSessionListUpdatedEvent += UpdateRoomListUI;
        FusionConnectionManager.OnStatusMessageEvent += ShowStatus;
        FusionConnectionManager.OnConnectedToServerEvent += OnConnectedToServer;
        FusionConnectionManager.OnShutdownEvent += OnDisconnected;
        FusionConnectionManager.OnPlayerJoinedEvent += OnPlayerJoinedWaitingRoom;
        FusionConnectionManager.OnPlayerLeftEvent += OnPlayerLeftWaitingRoom;
        FusionConnectionManager.OnPlayerReadyStatusReceived += OnRemotePlayerReadyReceived;

        // Tự động kết nối vào Lobby khi mở màn hình này
        _ = AutoConnectLobby();
    }

    private void OnDisable()
    {
        FusionConnectionManager.OnSessionListUpdatedEvent -= UpdateRoomListUI;
        FusionConnectionManager.OnStatusMessageEvent -= ShowStatus;
        FusionConnectionManager.OnConnectedToServerEvent -= OnConnectedToServer;
        FusionConnectionManager.OnShutdownEvent -= OnDisconnected;
        FusionConnectionManager.OnPlayerJoinedEvent -= OnPlayerJoinedWaitingRoom;
        FusionConnectionManager.OnPlayerLeftEvent -= OnPlayerLeftWaitingRoom;
        FusionConnectionManager.OnPlayerReadyStatusReceived -= OnRemotePlayerReadyReceived;

        // Khôi phục hiển thị 3D Player Model nếu quay về HomeScreen
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "HomeMenu")
        {
            CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
            if (charHud != null)
            {
                charHud.ApplyLobbyModelVisuals();
            }
        }
    }

    private void OnRemotePlayerReadyReceived(int playerId, bool isReady)
    {
        playerReadyStates[playerId] = isReady;
        UpdateWaitingRoomPlayerList();
    }

    private async Task AutoConnectLobby()
    {
        ShowLobbyMain();
        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.JoinLobby();
        }
    }

    private void SetupButtons()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
        if (openCreateModalButton != null) openCreateModalButton.onClick.AddListener(ShowCreateModal);
        if (openJoinCodeModalButton != null) openJoinCodeModalButton.onClick.AddListener(ShowJoinCodeModal);
        if (quickMatchButton != null) quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
        if (backToHomeButton != null) backToHomeButton.onClick.AddListener(CloseLobby);

        if (confirmCreateButton != null) confirmCreateButton.onClick.AddListener(OnConfirmCreateClicked);
        if (cancelCreateButton != null) cancelCreateButton.onClick.AddListener(ShowLobbyMain);
        if (openMapSelectButton != null) openMapSelectButton.onClick.AddListener(OnOpenMapSelectClicked);

        if (confirmJoinCodeButton != null) confirmJoinCodeButton.onClick.AddListener(OnConfirmJoinCodeClicked);
        if (cancelJoinCodeButton != null) cancelJoinCodeButton.onClick.AddListener(ShowLobbyMain);

        if (readyButton != null) readyButton.onClick.AddListener(OnToggleReadyClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveRoomButton != null) leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
    }

    // =========================================================================
    //                        PANEL SWITCHING HELPERS
    // =========================================================================

    public void ShowLobbyMain()
    {
        if (lobbyMainPanel != null) lobbyMainPanel.SetActive(true);
        if (createRoomModal != null) createRoomModal.SetActive(false);
        if (joinCodeModal != null) joinCodeModal.SetActive(false);
        if (waitingRoomPanel != null) waitingRoomPanel.SetActive(false);
        if (loadingSpinner != null) loadingSpinner.SetActive(false);
        if (chapterSelectManager != null && chapterSelectManager.chapterSelectPanel != null)
        {
            chapterSelectManager.chapterSelectPanel.SetActive(false);
        }
    }

    public void ShowCreateModal()
    {
        PlayClickSound();
        if (lobbyMainPanel != null) lobbyMainPanel.SetActive(true);
        if (createRoomModal != null) createRoomModal.SetActive(true);
        if (chapterSelectManager != null && chapterSelectManager.chapterSelectPanel != null)
        {
            chapterSelectManager.chapterSelectPanel.SetActive(false);
        }


        if (createRoomNameInput != null)
        {
            string username = "Player";
            if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
            {
                username = FirebaseDataService.Instance.CurrentUserProfile.username;
            }
            createRoomNameInput.text = $"{username}'s Room";
        }

        UpdateCreateRoomMapPreview(selectedMapIndex);
    }

    public void OnOpenMapSelectClicked()
    {
        PlayClickSound();
        if (chapterSelectManager == null)
        {
            chapterSelectManager = FindFirstObjectByType<ChapterSelectManager>(FindObjectsInactive.Include);
        }

        if (chapterSelectManager != null)
        {
            if (createRoomModal != null) createRoomModal.SetActive(false);
            if (lobbyMainPanel != null) lobbyMainPanel.SetActive(false); // Ẩn toàn bộ LobbyMainPanel
            chapterSelectManager.OpenForRoomCreation(this, selectedMapIndex);
        }
    }


    public void OnChapterSelectedFromPanel(int index)
    {
        selectedMapIndex = index;
        if (mapSelectDropdown != null && mapSelectDropdown.options != null && mapSelectDropdown.options.Count > index)
        {
            mapSelectDropdown.SetValueWithoutNotify(index);
        }
        if (chapterList != null && index >= 0 && index < chapterList.Count)
        {
            GameSession.SelectedChapter = chapterList[index];
            int nextIndex = index + 1;
            GameSession.NextChapter = (nextIndex < chapterList.Count) ? chapterList[nextIndex] : null;
        }
        UpdateCreateRoomMapPreview(selectedMapIndex);
    }

    public void UpdateCreateRoomMapPreview(int index)
    {
        if (index < 0) index = 0;
        Sprite sprite = GetMapSprite(index);
        string title = GetMapTitle(index);

        if (createRoomMapPreviewImage != null)
        {
            createRoomMapPreviewImage.sprite = sprite;
            createRoomMapPreviewImage.enabled = (sprite != null);
        }

        if (createRoomMapTitleText != null)
        {
            createRoomMapTitleText.text = title;
        }
    }

    public void ShowJoinCodeModal()
    {
        PlayClickSound();
        if (joinCodeModal != null) joinCodeModal.SetActive(true);
        if (joinCodeInput != null) joinCodeInput.text = "";
    }

    public void ShowWaitingRoom(string roomCode, string roomDisplayName = "")
    {
        if (lobbyMainPanel != null) lobbyMainPanel.SetActive(false);
        if (createRoomModal != null) createRoomModal.SetActive(false);
        if (joinCodeModal != null) joinCodeModal.SetActive(false);
        if (waitingRoomPanel != null) waitingRoomPanel.SetActive(true);

        string displayTitle = !string.IsNullOrEmpty(roomDisplayName) ? roomDisplayName : roomCode;
        if (string.IsNullOrEmpty(displayTitle)) displayTitle = "Waiting Room";

        if (waitingRoomTitleText != null) waitingRoomTitleText.text = displayTitle;
        if (waitingRoomCodeText != null) waitingRoomCodeText.text = $"ID: {roomCode}";

        UpdateWaitingRoomMapPreview();

        isLocalPlayerReady = false;
        UpdateReadyButtonVisual();
        UpdateWaitingRoomPlayerList();
    }

    public void CloseLobby()
    {
        PlayClickSound();
        _ = FusionConnectionManager.Instance?.LeaveSession();
        gameObject.SetActive(false);
        if (previousHomePanel != null) previousHomePanel.SetActive(true);

        // Khôi phục hiển thị 3D Player Model ngoài sảnh HomeScreen
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.ApplyLobbyModelVisuals();
        }
    }

    // =========================================================================
    //                         ROOM LISTING & UI UPDATES
    // =========================================================================

    private void UpdateRoomListUI(List<SessionInfo> sessionList)
    {
        if (roomListContent == null) return;

        // Xóa các dòng phòng cũ
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        bool hasRooms = sessionList != null && sessionList.Count > 0;
        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(!hasRooms);
        }

        if (hasRooms && roomItemPrefab != null)
        {
            foreach (var session in sessionList)
            {
                if (!session.IsVisible || !session.IsOpen) continue;

                GameObject itemObj = Instantiate(roomItemPrefab, roomListContent);
                NetworkRoomItem roomItem = itemObj.GetComponent<NetworkRoomItem>();
                if (roomItem != null)
                {
                    Sprite mapSprite = GetMapSprite(0);
                    string mapTitle = GetMapTitle(0);

                    if (session.Properties != null && session.Properties.TryGetValue("map", out var mapProp))
                    {
                        string mapName = mapProp.PropertyValue as string;
                        if (!string.IsNullOrEmpty(mapName) && mapSceneNames != null)
                        {
                            for (int i = 0; i < mapSceneNames.Length; i++)
                            {
                                if (mapSceneNames[i].Equals(mapName, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    mapSprite = GetMapSprite(i);
                                    mapTitle = GetMapTitle(i);
                                    break;
                                }
                            }
                        }
                    }

                    roomItem.Setup(session.Name, session.PlayerCount, session.MaxPlayers, OnJoinRoomFromList, mapSprite, mapTitle);
                }
            }
        }
    }

    private void OnJoinRoomFromList(string roomName)
    {
        PlayClickSound();
        _ = JoinRoomAsync(roomName);
    }

    // =========================================================================
    //                            ACTION HANDLERS
    // =========================================================================

    private async void OnRefreshClicked()
    {
        PlayClickSound();
        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.JoinLobby();
        }
    }

    private async void OnConfirmCreateClicked()
    {
        PlayClickSound();
        string roomName = (createRoomNameInput != null) ? createRoomNameInput.text.Trim() : "";
        int maxPlayers = 4;
        if (maxPlayersDropdown != null)
        {
            // Trích xuất số lượng từ nội dung tùy chọn đang chọn trong Dropdown (VD: "1", "2 Players", "3", "4")
            string selectedText = (maxPlayersDropdown.options != null && maxPlayersDropdown.options.Count > maxPlayersDropdown.value)
                ? maxPlayersDropdown.options[maxPlayersDropdown.value].text
                : "";

            var match = System.Text.RegularExpressions.Regex.Match(selectedText, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int parsedNum) && parsedNum > 0)
            {
                maxPlayers = parsedNum;
            }
            else
            {
                // Fallback theo index: Index 0 = 1 player, Index 1 = 2 players, Index 2 = 3 players, Index 3 = 4 players
                maxPlayers = maxPlayersDropdown.value + 1;
            }

            maxPlayers = Mathf.Clamp(maxPlayers, 1, 10);
        }

        bool isPrivate = (privateRoomToggle != null) && privateRoomToggle.isOn;

        int mapIndex = selectedMapIndex;
        if (chapterList != null && (mapIndex < 0 || mapIndex >= chapterList.Count))
        {
            mapIndex = (mapSelectDropdown != null && mapSelectDropdown.options != null && mapSelectDropdown.options.Count > 0) ? mapSelectDropdown.value : 0;
        }

        string selectedMap = GetMapSceneName(mapIndex);

        if (difficultyDropdown != null)
        {
            GameSession.SelectedDifficulty = (GameDifficulty)Mathf.Clamp(difficultyDropdown.value, 0, 2);
        }

        if (specialNpcsToggle != null)
        {
            GameSession.EnableSpecialNPCs = specialNpcsToggle.isOn;
        }

        if (chapterList != null && mapIndex >= 0 && mapIndex < chapterList.Count)
        {
            GameSession.SelectedChapter = chapterList[mapIndex];
            int nextIndex = mapIndex + 1;
            GameSession.NextChapter = (nextIndex < chapterList.Count) ? chapterList[nextIndex] : null;
        }

        if (loadingSpinner != null) loadingSpinner.SetActive(true);

        bool success = await FusionConnectionManager.Instance.CreateSession(roomName, maxPlayers, isPrivate, selectedMap);
        if (loadingSpinner != null) loadingSpinner.SetActive(false);

        if (success)
        {
            string finalCode = FusionConnectionManager.Instance.currentSessionName;
            string displayTitle = !string.IsNullOrEmpty(roomName) ? roomName : finalCode;
            ShowWaitingRoom(finalCode, displayTitle);
        }
    }

    private async void OnConfirmJoinCodeClicked()
    {
        PlayClickSound();
        string code = (joinCodeInput != null) ? joinCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(code))
        {
            ShowStatus("Please enter a room code.", true);
            return;
        }

        await JoinRoomAsync(code);
    }

    private async void OnQuickMatchClicked()
    {
        PlayClickSound();
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        bool success = await FusionConnectionManager.Instance.JoinRandomSession();
        if (loadingSpinner != null) loadingSpinner.SetActive(false);

        if (success)
        {
            string sessionCode = FusionConnectionManager.Instance.currentSessionName;
            ShowWaitingRoom(sessionCode, "Quick Match Room");
        }
    }

    private async Task JoinRoomAsync(string roomName)
    {
        if (loadingSpinner != null) loadingSpinner.SetActive(true);
        bool success = await FusionConnectionManager.Instance.JoinSession(roomName);
        if (loadingSpinner != null) loadingSpinner.SetActive(false);

        if (success)
        {
            ShowWaitingRoom(roomName, roomName);
        }
    }

    private void OnToggleReadyClicked()
    {
        PlayClickSound();
        isLocalPlayerReady = !isLocalPlayerReady;

        // Phát tín hiệu Ready tới tất cả người chơi trong phòng
        FusionConnectionManager.Instance?.SendReadyStatus(isLocalPlayerReady);

        var runner = FusionConnectionManager.Instance?.currentRunner;
        if (runner != null)
        {
            playerReadyStates[runner.LocalPlayer.PlayerId] = isLocalPlayerReady;
        }

        UpdateReadyButtonVisual();
        UpdateWaitingRoomPlayerList();
    }

    private void UpdateReadyButtonVisual()
    {
        if (readyButtonText != null)
        {
            readyButtonText.text = isLocalPlayerReady ? "<color=green>Ready</color>" : "Ready Up";
        }
    }

    private void OnStartGameClicked()
    {
        PlayClickSound();

        // Kiểm tra xem tất cả khách trong phòng đã Ready chưa
        NetworkRunner runner = FusionConnectionManager.Instance?.currentRunner;
        if (runner != null && runner.IsRunning)
        {
            bool isHost = runner.IsServer || runner.IsSharedModeMasterClient;
            if (isHost)
            {
                foreach (var p in runner.ActivePlayers)
                {
                    bool isThisPlayerHost = (p == runner.LocalPlayer) || (p.PlayerId == 1);
                    if (!isThisPlayerHost)
                    {
                        if (!playerReadyStates.TryGetValue(p.PlayerId, out bool r) || !r)
                        {
                            ShowStatus("Cannot start game: All other players must be READY!", true);
                            return;
                        }
                    }
                }
            }
        }

        int mapIndex = GetCurrentMapIndex();
        string sceneToLoad = GetMapSceneName(mapIndex);

        if (chapterList != null && mapIndex >= 0 && mapIndex < chapterList.Count)
        {
            GameSession.SelectedChapter = chapterList[mapIndex];
            int nextIndex = mapIndex + 1;
            GameSession.NextChapter = (nextIndex < chapterList.Count) ? chapterList[nextIndex] : null;
        }

        ShowStatus($"Starting game on '{sceneToLoad}'...", false);
        StartCoroutine(StartGameWithFadeRoutine(sceneToLoad));
    }

    private IEnumerator StartGameWithFadeRoutine(string sceneToLoad)
    {
        ScreenFader.FadeToBlack(0.5f);
        yield return new WaitForSecondsRealtime(0.5f);
        FusionConnectionManager.Instance?.LoadGameplayScene(sceneToLoad);
    }

    private async void OnLeaveRoomClicked()
    {
        PlayClickSound();
        await FusionConnectionManager.Instance?.LeaveSession();
        ShowLobbyMain();
    }

    private void OnPlayerJoinedWaitingRoom(NetworkRunner runner, PlayerRef player)
    {
        // Gửi trạng thái Ready của mình cho người chơi mới vào phòng
        FusionConnectionManager.Instance?.SendReadyStatus(isLocalPlayerReady);
        UpdateWaitingRoomPlayerList();
    }

    private void OnPlayerLeftWaitingRoom(NetworkRunner runner, PlayerRef player)
    {
        playerReadyStates.Remove(player.PlayerId);
        UpdateWaitingRoomPlayerList();
    }

    public void UpdateWaitingRoomPlayerList()
    {
        if (playerListContent == null || playerSlotPrefab == null) return;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        NetworkRunner runner = FusionConnectionManager.Instance?.currentRunner;
        string myName = "Player";
        if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
        {
            myName = FirebaseDataService.Instance.CurrentUserProfile.username;
        }

        if (runner != null && runner.IsRunning)
        {
            bool isHost = runner.IsServer || runner.IsSharedModeMasterClient;
            if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);
            if (readyButton != null) readyButton.gameObject.SetActive(!isHost);

            bool allGuestsReady = true;
            int guestCount = 0;

            int count = 0;
            foreach (var p in runner.ActivePlayers)
            {
                count++;
                GameObject slotObj = Instantiate(playerSlotPrefab, playerListContent);
                NetworkPlayerSlot slot = slotObj.GetComponent<NetworkPlayerSlot>() ?? slotObj.AddComponent<NetworkPlayerSlot>();

                bool isThisPlayerHost = (p == runner.LocalPlayer && isHost) || (p.PlayerId == 1);
                string pName = (p == runner.LocalPlayer) ? $"{myName} (You)" : $"Player {p.PlayerId}";

                bool isReady = false;
                if (isThisPlayerHost)
                {
                    isReady = true;
                }
                else
                {
                    guestCount++;
                    if (p == runner.LocalPlayer)
                    {
                        isReady = isLocalPlayerReady;
                    }
                    else if (playerReadyStates.TryGetValue(p.PlayerId, out bool r))
                    {
                        isReady = r;
                    }

                    if (!isReady)
                    {
                        allGuestsReady = false;
                    }
                }

                slot.Setup(pName, isThisPlayerHost, isReady);
            }

            // Cập nhật trạng thái nút Start Game của Chủ phòng:
            // Chỉ sáng và cho bấm khi tất cả khách trong phòng đã READY!
            if (isHost && startGameButton != null)
            {
                bool canStart = (guestCount == 0) || allGuestsReady;
                startGameButton.interactable = canStart;

                CanvasGroup cg = startGameButton.GetComponent<CanvasGroup>() ?? startGameButton.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = canStart ? 1.0f : 0.45f;
            }

            if (count == 0)
            {
                GameObject slotObj = Instantiate(playerSlotPrefab, playerListContent);
                NetworkPlayerSlot slot = slotObj.GetComponent<NetworkPlayerSlot>() ?? slotObj.AddComponent<NetworkPlayerSlot>();
                slot.Setup($"{myName} (You)", true, true);
            }
        }
        else
        {
            // Offline / Preview fallback
            GameObject slotObj = Instantiate(playerSlotPrefab, playerListContent);
            NetworkPlayerSlot slot = slotObj.GetComponent<NetworkPlayerSlot>() ?? slotObj.AddComponent<NetworkPlayerSlot>();
            slot.Setup($"{myName} (You)", true, true);
        }
    }

    private void OnConnectedToServer(NetworkRunner runner)
    {
        bool isHostOrMaster = runner.IsServer || runner.IsSharedModeMasterClient;
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHostOrMaster);
        }
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(!isHostOrMaster);
        }
        UpdateWaitingRoomPlayerList();
    }

    private void OnDisconnected(NetworkRunner runner, ShutdownReason reason)
    {
        // Bỏ qua hiển thị lỗi nếu đang trong quá trình chuyển tiếp tạo phòng/vào phòng (isConnecting)
        // hoặc nếu shutdown diễn ra bình thường (Ok)
        if (FusionConnectionManager.Instance != null && FusionConnectionManager.Instance.isConnecting)
        {
            return;
        }

        if (reason == ShutdownReason.Ok)
        {
            return;
        }

        ShowLobbyMain();
        ShowStatus($"Disconnected from room: {reason}", true);
    }

    public void ShowStatus(string message, bool isError)
    {
        if (statusMessagePanel != null)
        {
            statusMessagePanel.SetActive(true);
        }

        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(true);
            statusMessageText.text = isError ? $"<color=red>{message}</color>" : $"<color=black>{message}</color>";
        }

        if (statusClearCoroutine != null) StopCoroutine(statusClearCoroutine);
        statusClearCoroutine = StartCoroutine(ClearStatusAfterDelay(2.5f));
    }

    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(false);
        }
        if (statusMessagePanel != null)
        {
            statusMessagePanel.SetActive(false);
        }
    }
}
