using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dữ liệu lưu trữ thông tin Nhân vật dưới dạng file JSON
/// </summary>
[System.Serializable]
public class CharacterSaveData
{
    public string selectedCharacterId = "char_01";
    public int selectedCharacterIndex = 0;
    public bool isMale = true;
    public List<string> unlockedCharacterIds = new List<string>() { "char_01" };
}

/// <summary>
/// Quản lý Bảng Chọn Nhân Vật (Character Selection HUD):
/// - Quản lý InfoPanel (Name, Desc, MoveSpeed, RunSpeed, Stamina, CarryWeight kèm Slider & Value).
/// - Quản lý Tab Giới Tính (MaleBtn, FemaleBtn) đổi màu nút và chuyển đổi ContentMale / ContentFemale.
/// - Quản lý các Slot nhân vật trong ScrollView (đổi màu Vàng khi chọn, cập nhật InfoPanel).
/// - Lưu toàn bộ nhân vật đã mở khóa, nhân vật đang chọn, giới tính vào JSON (character_save.json).
/// - Tự động tải và gán trang phục cho 3D Model ngoài sảnh ngay khi vào game.
/// - Tùy chọn ẩn model trong lúc mở bảng chọn để phục vụ sảnh Online sau này.
/// </summary>
public class CharacterSelectionHUD : MonoBehaviour
{
    [Header("📚 Character Data (PlayerSO)")]
    [Tooltip("Danh sách các cấu hình nhân vật (VD: 3 PlayerSO cho Normal, Fat, Strong)")]
    public List<PlayerSO> characterList = new List<PlayerSO>();

    [Header("🚪 Panel Navigation")]
    [Tooltip("Panel trước đó cần bật lại khi bấm nút Close (Ví dụ: MainMenuPanel)")]
    public GameObject previousPanel;

    [Header("🧍 3D Lobby Player Model")]
    [Tooltip("GameObject chứa 3D Model nhân vật ngoài sảnh Home Menu")]
    public GameObject lobbyPlayerModel;
    [Tooltip("SkinnedMeshRenderer của model ngoài sảnh để đổi Mesh/Skin (nếu có)")]
    public SkinnedMeshRenderer lobbySkinnedMesh;

    [Header("🔊 Audio / SFX Settings")]
    [Tooltip("AudioSource phát tiếng click chung cho toàn bộ nút và slot (tự động kết nối SFX Group)")]
    public AudioSource clickAudioSource;

    [Header("📊 Info Panel Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;

    [Space(5)]
    public TextMeshProUGUI moveSpeedValueText;
    public Slider moveSpeedSlider;
    public float maxMoveSpeedSlider = 10.0f;

    [Space(5)]
    public TextMeshProUGUI runSpeedValueText;
    public Slider runSpeedSlider;
    public float maxRunSpeedSlider = 15.0f;

    [Space(5)]
    public TextMeshProUGUI staminaValueText;
    public Slider staminaSlider;
    public float maxStaminaSlider = 20.0f;

    [Space(5)]
    public TextMeshProUGUI carryWeightValueText;
    public Slider carryWeightSlider;
    public float maxCarryWeightSlider = 100.0f;

    [Header("🔘 Bottom Panel Elements")]
    public Button maleBtn;
    public Image maleBtnChildImage; // Image con của MaleBtn đổi màu
    public Button femaleBtn;
    public Image femaleBtnChildImage; // Image con của FemaleBtn đổi màu

    public Button skinBtn;
    public Button saveBtn;
    public TextMeshProUGUI saveBtnText;
    public Button closeBtn;

    [Header("🎨 Tab Button Colors")]
    public Color maleActiveColor = new Color(0.12f, 0.35f, 0.85f, 1f);   // Xanh dương đậm
    public Color femaleActiveColor = new Color(0.85f, 0.15f, 0.55f, 1f); // Hồng đậm
    public Color tabInactiveColor = new Color(0.25f, 0.28f, 0.35f, 1f);  // Xám tối / Không chọn

    [Header("📜 Scroll View Settings")]
    [Tooltip("Component ScrollRect chính")]
    public ScrollRect characterScrollRect;

    [Tooltip("Content duy nhất chứa danh sách các Slot nhân vật")]
    public GameObject content;

    [Header("🔲 Slot Colors")]
    public Color slotSelectedColor = new Color(1f, 0.84f, 0.0f, 1f); // Màu Vàng rực rỡ khi được chọn
    public Color slotDefaultColor = new Color(0.3f, 0.3f, 0.35f, 1f); // Màu bình thường

    [Header("💾 Runtime State")]
    [SerializeField] private bool isMaleSelected = true;
    [SerializeField] private int selectedCharacterIndex = 0;

    [Header("🗂️ Character Slots (Mỗi slot chứa Mesh & Avatar)")]
    public List<CharacterSlotItem> slotItems = new List<CharacterSlotItem>();
    public List<Button> slotButtons = new List<Button>();
    private Coroutine saveFeedbackCoroutine;

    public static CharacterSaveData CurrentSaveData { get; private set; } = new CharacterSaveData();

    /// <summary>
    /// Đường dẫn file save theo UID của user hiện tại (delegate sang FirebaseDataService)
    /// Fallback về legacy path nếu DataService chưa sẵn sàng
    /// </summary>
    private static string SaveFilePath
    {
        get
        {
            if (FirebaseDataService.Instance != null && !string.IsNullOrEmpty(FirebaseDataService.Instance.ActiveUserId))
            {
                string uid = FirebaseDataService.Instance.ActiveUserId;
                string safeId = uid;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    safeId = safeId.Replace(c, '_');
                }
                return Path.Combine(Application.persistentDataPath, $"character_save_{safeId}.json");
            }
            // Fallback: legacy path
            return Path.Combine(Application.persistentDataPath, "character_save.json");
        }
    }

    private const string PREF_KEY_CHAR_INDEX = "SelectedCharIndex";
    private const string PREF_KEY_GENDER = "SelectedGender"; // 0 = Male, 1 = Female

    private void Awake()
    {
        SetupAudioSource();
        AutoBindHierarchy();
        LoadSavedSelection();
        ApplyLobbyModelVisuals();

        FirebaseDataService.OnUserProfileLoaded += OnFirebaseProfileLoaded;
    }

    private void OnDestroy()
    {
        FirebaseDataService.OnUserProfileLoaded -= OnFirebaseProfileLoaded;
    }

    private void OnFirebaseProfileLoaded(UserGameProfile profile)
    {
        if (profile == null) return;
        CurrentSaveData = new CharacterSaveData(); // Xóa sạch dữ liệu cache cũ trước khi nạp profile mới
        LoadSavedSelection();
        ApplyLobbyModelVisuals();
        if (gameObject.activeInHierarchy)
        {
            RefreshUI();
        }
    }

    private void Start()
    {
        ApplyLobbyModelVisuals();
    }

    /// <summary>
    /// Thiết lập AudioSource và gán AudioMixerGroup (SFX) từ SettingsManager
    /// </summary>
    private void SetupAudioSource()
    {
        if (clickAudioSource == null)
        {
            clickAudioSource = GetComponent<AudioSource>();
            if (clickAudioSource == null)
            {
                clickAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (clickAudioSource != null)
        {
            clickAudioSource.playOnAwake = false;
            clickAudioSource.spatialBlend = 0f; // 2D Audio cho UI

            if (SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
            {
                clickAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
            }
        }
    }

    private void OnEnable()
    {
        LoadSavedSelection();
        SetupButtonListeners();
        RefreshUI();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();

        // Không tự động hiện lại model ở đây - trách nhiệm thuộc về HomeScreen (caller)
        // Chỉ ẩn model để tránh phantom model khi panel bị disable
    }

    /// <summary>
    /// Bật hoặc ẩn 3D Player Model ngoài sảnh HomeScreen
    /// </summary>
    public void SetLobbyModelVisible(bool visible)
    {
        // Chỉ cho phép hiển thị nếu đang ở Scene HomeMenu
        bool isHomeMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "HomeMenu";
        if (!isHomeMenuScene)
        {
            if (lobbyPlayerModel != null)
            {
                lobbyPlayerModel.SetActive(false);
            }
            return;
        }

        if (lobbyPlayerModel == null)
        {
            FindLobbyModelInScene();
        }

        if (lobbyPlayerModel != null)
        {
            lobbyPlayerModel.SetActive(visible);
        }
    }

    /// <summary>
    /// Tự động tìm kiếm 3D Model nhân vật trong Scene HomeMenu nếu chưa gán
    /// </summary>
    public void FindLobbyModelInScene()
    {
        if (lobbyPlayerModel != null) return;

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.name != "HomeMenu" || !activeScene.isLoaded) return;

        HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
        if (home != null && home.lobbyPlayerModel != null)
        {
            lobbyPlayerModel = home.lobbyPlayerModel;
            if (lobbySkinnedMesh == null && lobbyPlayerModel != null)
            {
                lobbySkinnedMesh = lobbyPlayerModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
            }
            return;
        }

        string[] possibleNames = new string[] { "LobbyPlayerModel", "PlayerModel", "LobbyModel", "LobbyCharacter", "CharacterModel", "Player_Dummy", "PlayerPreview" };
        GameObject[] roots = activeScene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (string name in possibleNames)
            {
                if (root.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    lobbyPlayerModel = root;
                    break;
                }
            }
            if (lobbyPlayerModel != null) break;

            foreach (string name in possibleNames)
            {
                Transform found = root.transform.Find(name);
                if (found != null)
                {
                    lobbyPlayerModel = found.gameObject;
                    break;
                }
            }
            if (lobbyPlayerModel != null) break;
        }

        if (lobbyPlayerModel != null && lobbySkinnedMesh == null)
        {
            lobbySkinnedMesh = lobbyPlayerModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }
    }

    /// <summary>
    /// Áp dụng ngoại hình nhân vật đã chọn (Mesh, Skin/Material) lên 3D Model ngoài sảnh HomeMenu
    /// </summary>
    public void ApplyLobbyModelVisuals()
    {
        bool isHomeMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "HomeMenu";
        if (!isHomeMenuScene)
        {
            SetLobbyModelVisible(false);
            return;
        }

        FindLobbyModelInScene();
        if (lobbyPlayerModel == null) return;

        // Chỉ hiển thị Model nếu đã đăng nhập và đang ở màn hình chính MainMenuPanel (không mở các modal khác)
        bool shouldShow = true;

        if (FirebaseAuthService.Instance == null || !FirebaseAuthService.Instance.IsLoggedIn)
        {
            shouldShow = false;
        }

        AuthHUD auth = FindFirstObjectByType<AuthHUD>(FindObjectsInactive.Include);
        if (auth != null && auth.gameObject.activeInHierarchy && (auth.authRootPanel == null || auth.authRootPanel.activeInHierarchy))
        {
            shouldShow = false;
        }

        HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
        if (home != null)
        {
            if ((home.characterSelectPanel != null && home.characterSelectPanel.activeSelf) ||
                (home.multiplayerLobbyPanel != null && home.multiplayerLobbyPanel.activeSelf) ||
                (home.settingPanel != null && home.settingPanel.activeSelf) ||
                (home.infoPanel != null && home.infoPanel.activeSelf) ||
                (home.authPanel != null && home.authPanel.activeSelf) ||
                (home.chapterSelectManager != null && home.chapterSelectManager.chapterSelectPanel != null && home.chapterSelectManager.chapterSelectPanel.activeSelf))
            {
                shouldShow = false;
            }
        }
        else if (gameObject.activeInHierarchy)
        {
            shouldShow = false;
        }

        lobbyPlayerModel.SetActive(shouldShow);

        // Áp dụng Mesh & Skin lên Model ngoài sảnh theo PlayerSO đã chọn
        if (characterList != null && characterList.Count > selectedCharacterIndex && selectedCharacterIndex >= 0)
        {
            PlayerSO so = characterList[selectedCharacterIndex];
            if (so != null)
            {
                if (lobbySkinnedMesh == null)
                {
                    lobbySkinnedMesh = lobbyPlayerModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
                }

                if (lobbySkinnedMesh != null)
                {
                    Mesh mesh = so.GetMesh(isMaleSelected);
                    if (mesh != null) lobbySkinnedMesh.sharedMesh = mesh;

                    if (so.characterMaterial != null)
                    {
                        lobbySkinnedMesh.material = so.characterMaterial;
                    }
                    else if (so.characterTexture != null && lobbySkinnedMesh.material != null)
                    {
                        lobbySkinnedMesh.material.mainTexture = so.characterTexture;
                    }
                }
                else
                {
                    PlayerStats.ApplyMeshToModel(lobbyPlayerModel, so.GetMesh(isMaleSelected));
                    PlayerStats.ApplySkinToModel(lobbyPlayerModel, so.characterMaterial, so.characterTexture);
                }
            }
        }
    }

    /// <summary>
    /// Tự động dò tìm các đối tượng con theo đúng cấu trúc Hierarchy nếu chưa được gán
    /// </summary>
    [ContextMenu("Auto Bind Hierarchy")]
    public void AutoBindHierarchy()
    {
        // 1. Dò tìm InfoPanel
        Transform infoPanel = transform.Find("InfoPanel");
        if (infoPanel != null)
        {
            if (nameText == null) nameText = infoPanel.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (descText == null) descText = infoPanel.Find("DescText")?.GetComponent<TextMeshProUGUI>();

            Transform moveSpeed = infoPanel.Find("MoveSpeed");
            if (moveSpeed != null)
            {
                if (moveSpeedValueText == null) moveSpeedValueText = moveSpeed.Find("Value")?.GetComponent<TextMeshProUGUI>();
                if (moveSpeedSlider == null) moveSpeedSlider = moveSpeed.Find("Slider")?.GetComponent<Slider>();
            }

            Transform runSpeed = infoPanel.Find("RunSpeed");
            if (runSpeed != null)
            {
                if (runSpeedValueText == null) runSpeedValueText = runSpeed.Find("Value")?.GetComponent<TextMeshProUGUI>();
                if (runSpeedSlider == null) runSpeedSlider = runSpeed.Find("Slider")?.GetComponent<Slider>();
            }

            Transform stamina = infoPanel.Find("Stamina");
            if (stamina != null)
            {
                if (staminaValueText == null) staminaValueText = stamina.Find("Value")?.GetComponent<TextMeshProUGUI>();
                if (staminaSlider == null) staminaSlider = stamina.Find("Slider")?.GetComponent<Slider>();
            }

            Transform carryWeight = infoPanel.Find("CarryWeight");
            if (carryWeight != null)
            {
                if (carryWeightValueText == null) carryWeightValueText = carryWeight.Find("Value")?.GetComponent<TextMeshProUGUI>();
                if (carryWeightSlider == null) carryWeightSlider = carryWeight.Find("Slider")?.GetComponent<Slider>();
            }
        }

        // 2. Dò tìm BottomPanel
        Transform bottomPanel = transform.Find("BottomPanel");
        if (bottomPanel != null)
        {
            if (maleBtn == null) maleBtn = bottomPanel.Find("MaleBtn")?.GetComponent<Button>();
            if (maleBtn != null && maleBtnChildImage == null)
            {
                Transform btnChild = maleBtn.transform.Find("Button");
                if (btnChild != null) maleBtnChildImage = btnChild.GetComponent<Image>();
                else maleBtnChildImage = maleBtn.GetComponent<Image>();
            }

            if (femaleBtn == null) femaleBtn = bottomPanel.Find("FemaleBtn")?.GetComponent<Button>();
            if (femaleBtn != null && femaleBtnChildImage == null)
            {
                Transform btnChild = femaleBtn.transform.Find("Button");
                if (btnChild != null) femaleBtnChildImage = btnChild.GetComponent<Image>();
                else femaleBtnChildImage = femaleBtn.GetComponent<Image>();
            }

            if (skinBtn == null) skinBtn = bottomPanel.Find("SkinBtn")?.GetComponent<Button>();
            if (saveBtn == null) saveBtn = bottomPanel.Find("SaveBtn")?.GetComponent<Button>();
            if (saveBtn != null && saveBtnText == null) saveBtnText = saveBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (closeBtn == null) closeBtn = bottomPanel.Find("CloseBtn")?.GetComponent<Button>();
        }

        // 3. Dò tìm Scroll View Content & Slots
        Transform scrollView = transform.Find("Scroll View") ?? transform.Find("ScrollView");
        if (scrollView != null)
        {
            if (characterScrollRect == null) characterScrollRect = scrollView.GetComponent<ScrollRect>();

            Transform viewport = scrollView.Find("Viewport") ?? scrollView;
            if (viewport != null)
            {
                if (content == null) content = viewport.Find("Content")?.gameObject ?? viewport.Find("ContentMale")?.gameObject;
            }
        }

        CollectSlotButtons();
    }

    /// <summary>
    /// Thu thập danh sách các nút Slot và CharacterSlotItem bên trong Content
    /// </summary>
    private void CollectSlotButtons()
    {
        slotButtons.Clear();
        slotItems.Clear();

        if (content != null)
        {
            int index = 0;
            foreach (Transform child in content.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    slotButtons.Add(btn);
                }

                CharacterSlotItem item = child.GetComponent<CharacterSlotItem>();
                if (item == null)
                {
                    item = child.gameObject.AddComponent<CharacterSlotItem>();
                }

                item.AutoBindReferences();

                // Tự động liên kết PlayerSO từ characterList nếu slot chưa được gán
                if (item.characterSO == null && characterList != null && index < characterList.Count)
                {
                    item.characterSO = characterList[index];
                }

                slotItems.Add(item);
                index++;
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem nhân vật đã được mở khóa chưa (theo UID hiện tại)
    /// </summary>
    public bool IsCharacterUnlocked(PlayerSO so)
    {
        if (so == null) return false;
        if (so.isUnlockedByDefault || so.unlockPrice <= 0) return true;

        if (CurrentSaveData != null && CurrentSaveData.unlockedCharacterIds != null && CurrentSaveData.unlockedCharacterIds.Contains(so.characterId))
        {
            return true;
        }

        if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null && FirebaseDataService.Instance.CurrentUserProfile.unlockedCharacterIds != null)
        {
            return FirebaseDataService.Instance.CurrentUserProfile.unlockedCharacterIds.Contains(so.characterId);
        }

        return false;
    }

    /// <summary>
    /// Mở khóa nhân vật và lưu vào JSON + Firebase của User hiện tại
    /// </summary>
    public void UnlockCharacter(PlayerSO so)
    {
        if (so == null) return;

        if (CurrentSaveData.unlockedCharacterIds == null)
        {
            CurrentSaveData.unlockedCharacterIds = new List<string>();
        }

        if (!CurrentSaveData.unlockedCharacterIds.Contains(so.characterId))
        {
            CurrentSaveData.unlockedCharacterIds.Add(so.characterId);
        }

        SaveCharacterDataToJSON();

        // Đồng bộ mở khóa nhân vật lên Firebase Cloud
        if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
        {
            FirebaseDataService.Instance.UnlockCharacter(so.characterId);
        }

        Debug.Log($"<color=cyan>[CharacterSelectionHUD] Đã mở khóa thành công nhân vật: {so.characterName} (ID: {so.characterId})</color>");
    }

    /// <summary>
    /// Reset toàn bộ trạng thái mở khóa nhân vật về mặc định để test lại
    /// </summary>
    [ContextMenu("Reset All Character Unlocks (For Testing)")]
    public void ResetAllCharacterUnlocks()
    {
        CurrentSaveData = new CharacterSaveData();
        CurrentSaveData.unlockedCharacterIds = new List<string>();

        if (characterList != null)
        {
            for (int i = 0; i < characterList.Count; i++)
            {
                var so = characterList[i];
                if (so != null && (so.isUnlockedByDefault || so.unlockPrice <= 0))
                {
                    CurrentSaveData.unlockedCharacterIds.Add(so.characterId);
                }

                if (so != null && !string.IsNullOrEmpty(so.characterId))
                {
                    PlayerPrefs.DeleteKey("CharUnlocked_" + so.characterId);
                }
            }
        }

        CurrentSaveData.selectedCharacterIndex = 0;
        CurrentSaveData.selectedCharacterId = (characterList != null && characterList.Count > 0) ? characterList[0].characterId : "char_01";
        CurrentSaveData.isMale = true;

        SaveCharacterDataToJSON();
        PlayerPrefs.Save();

        Debug.Log("<color=yellow>[CharacterSelectionHUD] Đã reset JSON & PlayerPrefs! Chỉ nhân vật mặc định (NormalHuman) được mở.</color>");
        ApplyLobbyModelVisuals();
        RefreshUI();
    }

    /// <summary>
    /// Đăng ký sự kiện Click cho các nút UI
    /// </summary>
    private void SetupButtonListeners()
    {
        RemoveButtonListeners();

        if (maleBtn != null) maleBtn.onClick.AddListener(OnMaleTabClicked);
        if (femaleBtn != null) femaleBtn.onClick.AddListener(OnFemaleTabClicked);
        if (saveBtn != null) saveBtn.onClick.AddListener(OnSaveButtonClicked);
        if (closeBtn != null) closeBtn.onClick.AddListener(OnCloseButtonClicked);

        // Gắn sự kiện cho các Slot
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));

            // Đăng ký riêng cho nút con Button bên trong LockImg -> Image để mở khóa
            Transform lockImg = FindChildRecursive(slotButtons[i].transform, "LockImg");
            if (lockImg != null)
            {
                Button lockBtn = FindChildRecursive(lockImg, "Image")?.GetComponent<Button>() ?? lockImg.GetComponent<Button>();
                if (lockBtn != null && lockBtn != slotButtons[i])
                {
                    lockBtn.onClick.RemoveAllListeners();
                    lockBtn.onClick.AddListener(() => OnUnlockButtonClicked(index));
                }
            }
        }
    }

    /// <summary>
    /// Hủy đăng ký sự kiện tránh trùng lặp
    /// </summary>
    private void RemoveButtonListeners()
    {
        if (maleBtn != null) maleBtn.onClick.RemoveListener(OnMaleTabClicked);
        if (femaleBtn != null) femaleBtn.onClick.RemoveListener(OnFemaleTabClicked);
        if (saveBtn != null) saveBtn.onClick.RemoveListener(OnSaveButtonClicked);
        if (closeBtn != null) closeBtn.onClick.RemoveListener(OnCloseButtonClicked);

        foreach (var btn in slotButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                Transform lockImg = FindChildRecursive(btn.transform, "LockImg");
                if (lockImg != null)
                {
                    Button lockBtn = FindChildRecursive(lockImg, "Image")?.GetComponent<Button>() ?? lockImg.GetComponent<Button>();
                    if (lockBtn != null && lockBtn != btn) lockBtn.onClick.RemoveAllListeners();
                }
            }
        }
    }

    /// <summary>
    /// Đọc dữ liệu nhân vật đã lưu từ file JSON (character_save.json) và đồng bộ với Firebase Cloud & GameSession
    /// </summary>
    public void LoadSavedSelection()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                CurrentSaveData = JsonUtility.FromJson<CharacterSaveData>(json);
                if (CurrentSaveData == null) CurrentSaveData = new CharacterSaveData();
            }
            else
            {
                CurrentSaveData = new CharacterSaveData();
                SaveCharacterDataToJSON();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterSelectionHUD] Lỗi khi đọc file JSON ({SaveFilePath}): {ex.Message}");
            CurrentSaveData = new CharacterSaveData();
        }

        // Ưu tiên nạp từ Firebase Cloud nếu đã có phiên đăng nhập
        if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null && !string.IsNullOrEmpty(FirebaseDataService.Instance.CurrentUserProfile.uid))
        {
            var fbProfile = FirebaseDataService.Instance.CurrentUserProfile;
            CurrentSaveData.selectedCharacterIndex = fbProfile.selectedCharacterIndex;
            CurrentSaveData.selectedCharacterId = fbProfile.selectedCharacterId;
            CurrentSaveData.isMale = fbProfile.isMale;

            if (fbProfile.unlockedCharacterIds != null)
            {
                if (CurrentSaveData.unlockedCharacterIds == null) CurrentSaveData.unlockedCharacterIds = new List<string>();
                foreach (var id in fbProfile.unlockedCharacterIds)
                {
                    if (!CurrentSaveData.unlockedCharacterIds.Contains(id))
                    {
                        CurrentSaveData.unlockedCharacterIds.Add(id);
                    }
                }
            }
        }

        // Tự động đảm bảo tất cả nhân vật mở khóa mặc định (isUnlockedByDefault) đều có trong danh sách
        if (characterList != null)
        {
            if (CurrentSaveData.unlockedCharacterIds == null) CurrentSaveData.unlockedCharacterIds = new List<string>();
            foreach (var so in characterList)
            {
                if (so != null && (so.isUnlockedByDefault || so.unlockPrice <= 0))
                {
                    if (!CurrentSaveData.unlockedCharacterIds.Contains(so.characterId))
                    {
                        CurrentSaveData.unlockedCharacterIds.Add(so.characterId);
                    }
                }
            }
        }

        selectedCharacterIndex = CurrentSaveData.selectedCharacterIndex;
        isMaleSelected = CurrentSaveData.isMale;

        if (selectedCharacterIndex < 0 || (characterList != null && selectedCharacterIndex >= characterList.Count))
        {
            selectedCharacterIndex = 0;
            CurrentSaveData.selectedCharacterIndex = 0;
        }

        // Tự động đồng bộ Mesh & Avatar từ các Slot sang PlayerSO tương ứng
        if (slotItems != null)
        {
            foreach (var slot in slotItems)
            {
                if (slot != null)
                {
                    slot.ApplyDataToPlayerSO();
                }
            }
        }

        // Đồng bộ vào GameSession & PlayerPrefs cho các Scene Gameplay
        if (characterList != null && characterList.Count > selectedCharacterIndex && selectedCharacterIndex >= 0)
        {
            GameSession.SelectedPlayer = characterList[selectedCharacterIndex];
            GameSession.IsMale = isMaleSelected;
            PlayerPrefs.SetInt(PREF_KEY_CHAR_INDEX, selectedCharacterIndex);
            PlayerPrefs.SetInt(PREF_KEY_GENDER, isMaleSelected ? 0 : 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Lưu trạng thái hiện tại vào file JSON (character_save.json) và đồng bộ lên Firebase Cloud
    /// </summary>
    public void SaveCharacterDataToJSON()
    {
        try
        {
            CurrentSaveData.selectedCharacterIndex = selectedCharacterIndex;
            CurrentSaveData.isMale = isMaleSelected;
            if (characterList != null && characterList.Count > selectedCharacterIndex && selectedCharacterIndex >= 0)
            {
                CurrentSaveData.selectedCharacterId = characterList[selectedCharacterIndex].characterId;
            }

            string json = JsonUtility.ToJson(CurrentSaveData, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"<color=cyan>[CharacterSelectionHUD] Đã lưu thông tin nhân vật vào JSON ({SaveFilePath}):\n{json}</color>");

            // Đồng bộ trực tiếp lên Firebase Realtime Database
            if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
            {
                string charId = !string.IsNullOrEmpty(CurrentSaveData.selectedCharacterId) ? CurrentSaveData.selectedCharacterId : "char_01";
                FirebaseDataService.Instance.SetSelectedCharacter(charId, selectedCharacterIndex, isMaleSelected);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterSelectionHUD] Lỗi khi ghi file JSON ({SaveFilePath}): {ex.Message}");
        }
    }

    // =========================================================================
    //                        XỬ LÝ TAB & SLOT CLICK
    // =========================================================================

    public void OnMaleTabClicked()
    {
        isMaleSelected = true;
        PlayClickSound();
        RefreshUI();
    }

    public void OnFemaleTabClicked()
    {
        isMaleSelected = false;
        PlayClickSound();
        RefreshUI();
    }

    public void OnSlotClicked(int index)
    {
        if (index >= 0 && index < characterList.Count)
        {
            selectedCharacterIndex = index;
            PlayClickSound();
            RefreshUI();
        }
    }

    /// <summary>
    /// Bấm trực tiếp vào nút ổ khóa Image trong LockImg để mở khóa
    /// </summary>
    public async void OnUnlockButtonClicked(int index)
    {
        if (index >= 0 && index < characterList.Count)
        {
            PlayerSO so = characterList[index];
            if (!IsCharacterUnlocked(so))
            {
                // Kiểm tra số tiền hiện tại của người chơi
                int currentCash = FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null
                    ? FirebaseDataService.Instance.CurrentUserProfile.cash
                    : PlayerPrefs.GetInt("PlayerCash", 0);

                if (so.unlockPrice > 0 && currentCash < so.unlockPrice)
                {
                    Debug.LogWarning($"<color=red>[CharacterSelectionHUD] Không đủ tiền mua {so.characterName}! Cần ${so.unlockPrice}, hiện có ${currentCash}</color>");
                    PlayClickSound();
                    return;
                }

                // Trừ tiền khi mua
                if (so.unlockPrice > 0)
                {
                    if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
                    {
                        bool spendOk = await FirebaseDataService.Instance.TrySpendCash(so.unlockPrice);
                        if (!spendOk)
                        {
                            Debug.LogWarning("[CharacterSelectionHUD] Trừ tiền thất bại!");
                            return;
                        }
                    }
                    else
                    {
                        PlayerPrefs.SetInt("PlayerCash", currentCash - so.unlockPrice);
                        PlayerPrefs.Save();
                    }
                }

                UnlockCharacter(so);

                // Cập nhật lại UI tiền mặt ngoài HomeScreen nếu có
                HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
                if (home != null) home.RefreshProfileUI();
            }

            selectedCharacterIndex = index;
            PlayClickSound();
            RefreshUI();
        }
    }

    public void OnSaveButtonClicked()
    {
        PlayerSO selectedSO = (characterList != null && characterList.Count > selectedCharacterIndex) ? characterList[selectedCharacterIndex] : null;

        // Nếu ô đang chọn bị khóa -> Lưu nhân vật mặc định ở Slot 1 (index 0) và giữ nguyên chữ "Save"
        if (selectedSO != null && !IsCharacterUnlocked(selectedSO))
        {
            int defaultIndex = 0;
            selectedCharacterIndex = defaultIndex;
            SaveCharacterDataToJSON();

            PlayerPrefs.SetInt(PREF_KEY_CHAR_INDEX, defaultIndex);
            PlayerPrefs.SetInt(PREF_KEY_GENDER, isMaleSelected ? 0 : 1);
            PlayerPrefs.Save();

            if (characterList != null && characterList.Count > defaultIndex)
            {
                GameSession.SelectedPlayer = characterList[defaultIndex];
                GameSession.IsMale = isMaleSelected;
            }

            PlayClickSound();
            Debug.Log($"<color=yellow>[CharacterSelectionHUD] Nhân vật {selectedSO.characterName} đang khóa. Đã lưu nhân vật mặc định (Slot 1): {GameSession.SelectedPlayer?.characterName} | Giới tính: {(isMaleSelected ? "Male" : "Female")}</color>");

            // Chữ Save vẫn giữ nguyên là "Save"
            if (saveBtnText != null)
            {
                saveBtnText.text = "Save";
            }
            return;
        }

        // Nếu ô đang chọn ĐÃ mở khóa -> Truyền Mesh & Avatar từ Slot sang PlayerSO rồi Lưu
        if (slotItems != null && slotItems.Count > selectedCharacterIndex && slotItems[selectedCharacterIndex] != null)
        {
            slotItems[selectedCharacterIndex].ApplyDataToPlayerSO();
        }

        SaveCharacterDataToJSON();

        PlayerPrefs.SetInt(PREF_KEY_CHAR_INDEX, selectedCharacterIndex);
        PlayerPrefs.SetInt(PREF_KEY_GENDER, isMaleSelected ? 0 : 1);
        PlayerPrefs.Save();

        if (characterList != null && characterList.Count > selectedCharacterIndex)
        {
            GameSession.SelectedPlayer = characterList[selectedCharacterIndex];
            GameSession.IsMale = isMaleSelected;
        }

        PlayClickSound();
        Debug.Log($"<color=green>[CharacterSelectionHUD] Đã lưu nhân vật vào JSON: {GameSession.SelectedPlayer?.characterName} | Giới tính: {(isMaleSelected ? "Male" : "Female")}</color>");

        if (saveFeedbackCoroutine != null) StopCoroutine(saveFeedbackCoroutine);
        saveFeedbackCoroutine = StartCoroutine(ShowSavedFeedbackRoutine());
    }

    public void OnCloseButtonClicked()
    {
        PlayClickSound();
        gameObject.SetActive(false);

        // Khôi phục model ngoài sảnh
        ApplyLobbyModelVisuals();

        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
            return;
        }

        HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
        if (home != null && home.mainMenuPanel != null)
        {
            home.mainMenuPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Phát âm thanh click chung cho toàn bộ nút và slot từ AudioSource
    /// </summary>
    private void PlayClickSound()
    {
        if (clickAudioSource != null)
        {
            if (clickAudioSource.clip != null)
            {
                clickAudioSource.PlayOneShot(clickAudioSource.clip);
            }
            else
            {
                clickAudioSource.Play();
            }
        }
    }

    private IEnumerator ShowSavedFeedbackRoutine()
    {
        if (saveBtnText != null)
        {
            saveBtnText.text = "Saved";
            yield return new WaitForSecondsRealtime(1.5f);
            saveBtnText.text = "Save";
        }
    }

    // =========================================================================
    //                        CẬP NHẬT HIỂN THỊ UI & STATS
    // =========================================================================

    public void RefreshUI()
    {
        // 1. Cập nhật Tab Giới tính & Đổi màu Button con
        if (maleBtnChildImage != null)
        {
            maleBtnChildImage.color = isMaleSelected ? maleActiveColor : tabInactiveColor;
        }

        if (femaleBtnChildImage != null)
        {
            femaleBtnChildImage.color = !isMaleSelected ? femaleActiveColor : tabInactiveColor;
        }

        // 2. Cập nhật Icon/Avatar, trạng thái Khóa (LockImg), giá tiền và màu các Slot
        for (int i = 0; i < slotButtons.Count; i++)
        {
            if (slotButtons[i] == null) continue;

            bool isSelected = (i == selectedCharacterIndex);
            bool isUnlocked = (i < characterList.Count) && IsCharacterUnlocked(characterList[i]);

            // Nếu có component CharacterSlotItem trên Slot -> Cập nhật trực tiếp qua component
            if (i < slotItems.Count && slotItems[i] != null)
            {
                slotItems[i].UpdateSlotUI(isMaleSelected, isSelected, isUnlocked, slotSelectedColor, slotDefaultColor);
            }
            else
            {
                // Fallback nếu chưa gắn component
                Image slotImg = slotButtons[i].GetComponent<Image>();
                if (slotImg != null) slotImg.color = isSelected ? slotSelectedColor : slotDefaultColor;

                if (i < characterList.Count && characterList[i] != null)
                {
                    PlayerSO so = characterList[i];
                    Sprite activeAvatar = so.GetAvatar(isMaleSelected);

                    Transform iconTrans = FindChildRecursive(slotButtons[i].transform, "Icon") 
                        ?? FindChildRecursive(slotButtons[i].transform, "Avatar") 
                        ?? FindChildRecursive(slotButtons[i].transform, "Image");

                    if (iconTrans != null)
                    {
                        Image iconImg = iconTrans.GetComponent<Image>();
                        if (iconImg != null && activeAvatar != null) iconImg.sprite = activeAvatar;
                    }

                    Transform lockImg = FindChildRecursive(slotButtons[i].transform, "LockImg");
                    if (lockImg != null)
                    {
                        lockImg.gameObject.SetActive(!isUnlocked);
                        if (!isUnlocked)
                        {
                            Transform priceTrans = FindChildRecursive(lockImg, "Price");
                            if (priceTrans != null)
                            {
                                TextMeshProUGUI priceTmp = priceTrans.GetComponent<TextMeshProUGUI>();
                                if (priceTmp != null) priceTmp.text = $"${so.unlockPrice}";
                            }
                        }
                    }
                }
            }
        }

        // 3. Cập nhật InfoPanel theo PlayerSO được chọn
        if (characterList != null && characterList.Count > selectedCharacterIndex && selectedCharacterIndex >= 0)
        {
            PlayerSO activeSO = characterList[selectedCharacterIndex];
            UpdateInfoPanel(activeSO);
        }
    }

    /// <summary>
    /// Hàm đệ quy tìm kiếm GameObject con theo tên (không phân biệt hoa/thường)
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null) return null;
        Transform direct = parent.Find(targetName);
        if (direct != null) return direct;

        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private void UpdateInfoPanel(PlayerSO so)
    {
        if (so == null) return;

        if (nameText != null) nameText.text = so.characterName;
        if (descText != null) descText.text = so.description;

        // Move Speed
        if (moveSpeedValueText != null) moveSpeedValueText.text = $"{so.baseMoveSpeed:F1}";
        if (moveSpeedSlider != null)
        {
            moveSpeedSlider.maxValue = maxMoveSpeedSlider;
            moveSpeedSlider.value = so.baseMoveSpeed;
        }

        // Run Speed (Sprint)
        if (runSpeedValueText != null) runSpeedValueText.text = $"{so.baseSprintSpeed:F1}";
        if (runSpeedSlider != null)
        {
            runSpeedSlider.maxValue = maxRunSpeedSlider;
            runSpeedSlider.value = so.baseSprintSpeed;
        }

        // Stamina
        if (staminaValueText != null) staminaValueText.text = $"{so.baseMaxStamina:F0}";
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStaminaSlider;
            staminaSlider.value = so.baseMaxStamina;
        }

        // Carry Weight
        if (carryWeightValueText != null) carryWeightValueText.text = $"{so.baseMaxWeight} kg";
        if (carryWeightSlider != null)
        {
            carryWeightSlider.maxValue = maxCarryWeightSlider;
            carryWeightSlider.value = so.baseMaxWeight;
        }
    }


}
