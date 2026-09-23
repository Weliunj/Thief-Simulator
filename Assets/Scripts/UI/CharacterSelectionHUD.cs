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

    [Header("🎯 3D Preview Settings")]
    [Tooltip("GameObject cha chứa toàn bộ mô hình đứng preview và chỗ đứng (bục đứng) trong Scene HomeMenu")]
    public GameObject previewModelRoot;

    [Tooltip("GameObject đối tượng con chứa Mesh nhân vật (Mặc định tự động tìm con tên 'Base' bên trong previewModelRoot)")]
    public GameObject previewCharacterTarget;

    [Tooltip("Tùy chọn: Tự động ẩn 3D Model ngoài sảnh khi mở bảng Chọn Nhân Vật (để không bị che khuất giao diện UI)")]
    public bool hidePreviewModelWhenPanelOpens = true;

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
    [Tooltip("Component ScrollRect chính (tự động đổi ô Content khi bấm Male/Female)")]
    public ScrollRect characterScrollRect;

    [Tooltip("Content danh sách nhân vật Nam")]
    public GameObject contentMale;

    [Tooltip("Content danh sách nhân vật Nữ")]
    public GameObject contentFemale;

    [Header("🔲 Slot Colors")]
    public Color slotSelectedColor = new Color(1f, 0.84f, 0.0f, 1f); // Màu Vàng rực rỡ khi được chọn
    public Color slotDefaultColor = new Color(0.3f, 0.3f, 0.35f, 1f); // Màu bình thường

    [Header("💾 Runtime State")]
    [SerializeField] private bool isMaleSelected = true;
    [SerializeField] private int selectedCharacterIndex = 0;

    private List<Button> maleSlotButtons = new List<Button>();
    private List<Button> femaleSlotButtons = new List<Button>();
    private Coroutine saveFeedbackCoroutine;

    public static CharacterSaveData CurrentSaveData { get; private set; } = new CharacterSaveData();
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "character_save.json");

    private const string PREF_KEY_CHAR_INDEX = "SelectedCharIndex";
    private const string PREF_KEY_GENDER = "SelectedGender"; // 0 = Male, 1 = Female

    private void Awake()
    {
        SetupAudioSource();
        AutoBindHierarchy();
        LoadSavedSelection();
        ApplyLobbyModelVisuals();
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

        // Luôn ẩn 3D model ngoài sảnh khi đang mở bảng chọn nhân vật (tránh che khuất UI)
        if (previewModelRoot != null)
        {
            previewModelRoot.SetActive(false);
        }

        SetupButtonListeners();
        RefreshUI();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();

        // Khi đóng bảng chọn nhân vật, khôi phục lại hiển thị model ngoài sảnh theo nhân vật đã lưu
        ApplyLobbyModelVisuals();
    }

    /// <summary>
    /// Nạp dữ liệu đã lưu từ JSON và áp dụng trang phục cho 3D Model ngoài sảnh ngay khi vào game
    /// </summary>
    public void ApplyLobbyModelVisuals()
    {
        LoadSavedSelection();

        if (previewModelRoot != null)
        {
            previewModelRoot.SetActive(true);

            PlayerSO activeSO = null;
            if (characterList != null && characterList.Count > selectedCharacterIndex && selectedCharacterIndex >= 0)
            {
                activeSO = characterList[selectedCharacterIndex];
            }
            else if (characterList != null && characterList.Count > 0)
            {
                activeSO = characterList[0];
            }

            if (activeSO != null)
            {
                GameObject characterTarget = GetPreviewCharacterTarget();
                if (characterTarget != null)
                {
                    Mesh targetMesh = activeSO.GetMesh(isMaleSelected);
                    if (targetMesh != null)
                    {
                        PlayerStats.ApplyMeshToModel(characterTarget, targetMesh);
                    }
                    PlayerStats.ApplySkinToModel(characterTarget, activeSO.characterMaterial, activeSO.characterTexture);
                }
            }
        }
    }

    /// <summary>
    /// Lấy GameObject con chứa Mesh và Renderer của Nhân vật (ưu tiên tìm con tên 'Base' để không sửa nhầm chỗ đứng)
    /// </summary>
    public GameObject GetPreviewCharacterTarget()
    {
        if (previewCharacterTarget != null) return previewCharacterTarget;
        if (previewModelRoot == null) return null;

        // 1. Ưu tiên tìm đối tượng con tên "Base" (không phân biệt hoa/thường)
        Transform baseTrans = FindChildRecursive(previewModelRoot.transform, "Base");
        if (baseTrans != null) return baseTrans.gameObject;

        // 2. Tìm đối tượng có SkinnedMeshRenderer (mesh body nhân vật)
        SkinnedMeshRenderer smr = previewModelRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null) return smr.gameObject;

        return previewModelRoot;
    }

    /// <summary>
    /// Bật hoặc ẩn 3D Model ngoài sảnh (chỉ hiện trên sảnh chính MainMenuPanel / Sảnh Online)
    /// </summary>
    public void SetLobbyModelVisible(bool visible)
    {
        if (previewModelRoot != null)
        {
            previewModelRoot.SetActive(visible);
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

        // 3. Dò tìm Scroll View Contents & Slots
        Transform scrollView = transform.Find("Scroll View") ?? transform.Find("ScrollView");
        if (scrollView != null)
        {
            if (characterScrollRect == null) characterScrollRect = scrollView.GetComponent<ScrollRect>();

            Transform viewport = scrollView.Find("Viewport") ?? scrollView;
            if (viewport != null)
            {
                if (contentMale == null) contentMale = viewport.Find("ContentMale")?.gameObject;
                if (contentFemale == null) contentFemale = viewport.Find("ContentFemale")?.gameObject;
            }
        }

        CollectSlotButtons();
    }

    /// <summary>
    /// Thu thập danh sách các nút Slot bên trong ContentMale và ContentFemale
    /// </summary>
    private void CollectSlotButtons()
    {
        maleSlotButtons.Clear();
        if (contentMale != null)
        {
            foreach (Transform child in contentMale.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    maleSlotButtons.Add(btn);
                }
            }
        }

        femaleSlotButtons.Clear();
        if (contentFemale != null)
        {
            foreach (Transform child in femaleFemaleTransform())
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    femaleSlotButtons.Add(btn);
                }
            }
        }
    }

    private Transform femaleFemaleTransform()
    {
        return contentFemale != null ? contentFemale.transform : transform;
    }

    /// <summary>
    /// Kiểm tra xem nhân vật đã được mở khóa chưa (mặc định = true, hoặc đã lưu trong JSON / PlayerPrefs)
    /// </summary>
    public bool IsCharacterUnlocked(PlayerSO so)
    {
        if (so == null) return false;
        if (so.isUnlockedByDefault || so.unlockPrice <= 0) return true;

        if (CurrentSaveData != null && CurrentSaveData.unlockedCharacterIds != null && CurrentSaveData.unlockedCharacterIds.Contains(so.characterId))
        {
            return true;
        }

        return PlayerPrefs.GetInt("CharUnlocked_" + so.characterId, 0) == 1;
    }

    /// <summary>
    /// Mở khóa nhân vật và lưu vào JSON + PlayerPrefs
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

        PlayerPrefs.SetInt("CharUnlocked_" + so.characterId, 1);
        PlayerPrefs.Save();
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

        // Gắn sự kiện cho các Slot Male
        for (int i = 0; i < maleSlotButtons.Count; i++)
        {
            int index = i;
            maleSlotButtons[i].onClick.AddListener(() => OnSlotClicked(index));

            // Đăng ký riêng cho nút con Button bên trong LockImg -> Image để mở khóa
            Transform lockImg = FindChildRecursive(maleSlotButtons[i].transform, "LockImg");
            if (lockImg != null)
            {
                Button lockBtn = FindChildRecursive(lockImg, "Image")?.GetComponent<Button>() ?? lockImg.GetComponent<Button>();
                if (lockBtn != null && lockBtn != maleSlotButtons[i])
                {
                    lockBtn.onClick.RemoveAllListeners();
                    lockBtn.onClick.AddListener(() => OnUnlockButtonClicked(index));
                }
            }
        }

        // Gắn sự kiện cho các Slot Female
        for (int i = 0; i < femaleSlotButtons.Count; i++)
        {
            int index = i;
            femaleSlotButtons[i].onClick.AddListener(() => OnSlotClicked(index));

            // Đăng ký riêng cho nút con Button bên trong LockImg -> Image để mở khóa
            Transform lockImg = FindChildRecursive(femaleSlotButtons[i].transform, "LockImg");
            if (lockImg != null)
            {
                Button lockBtn = FindChildRecursive(lockImg, "Image")?.GetComponent<Button>() ?? lockImg.GetComponent<Button>();
                if (lockBtn != null && lockBtn != femaleSlotButtons[i])
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

        foreach (var btn in maleSlotButtons)
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
        foreach (var btn in femaleSlotButtons)
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
    /// Đọc dữ liệu nhân vật đã lưu từ file JSON (character_save.json) và đồng bộ với GameSession / PlayerPrefs
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

        // Đồng bộ vào GameSession & PlayerPrefs cho các Scene Gameplay
        if (characterList != null && characterList.Count > selectedCharacterIndex)
        {
            GameSession.SelectedPlayer = characterList[selectedCharacterIndex];
            GameSession.IsMale = isMaleSelected;
            PlayerPrefs.SetInt(PREF_KEY_CHAR_INDEX, selectedCharacterIndex);
            PlayerPrefs.SetInt(PREF_KEY_GENDER, isMaleSelected ? 0 : 1);
        }
    }

    /// <summary>
    /// Lưu trạng thái hiện tại vào file JSON (character_save.json)
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
    public void OnUnlockButtonClicked(int index)
    {
        if (index >= 0 && index < characterList.Count)
        {
            PlayerSO so = characterList[index];
            if (!IsCharacterUnlocked(so))
            {
                UnlockCharacter(so);
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

        // Nếu ô đang chọn ĐÃ mở khóa -> Lưu vào JSON + PlayerPrefs và hiển thị "Saved" chữ trắng
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
        if (contentMale != null) contentMale.SetActive(isMaleSelected);
        if (contentFemale != null) contentFemale.SetActive(!isMaleSelected);

        // Tự động hoán đổi Content của ScrollRect sang Content của Giới tính đang chọn
        if (characterScrollRect != null)
        {
            GameObject activeContent = isMaleSelected ? contentMale : contentFemale;
            if (activeContent != null)
            {
                RectTransform rt = activeContent.GetComponent<RectTransform>();
                if (rt != null && characterScrollRect.content != rt)
                {
                    characterScrollRect.content = rt;
                }
            }
        }

        if (maleBtnChildImage != null)
        {
            maleBtnChildImage.color = isMaleSelected ? maleActiveColor : tabInactiveColor;
        }

        if (femaleBtnChildImage != null)
        {
            femaleBtnChildImage.color = !isMaleSelected ? femaleActiveColor : tabInactiveColor;
        }

        // 2. Cập nhật trạng thái Khóa (LockImg) và màu các Slot
        UpdateSlotsLockVisuals(maleSlotButtons);
        UpdateSlotsLockVisuals(femaleSlotButtons);

        List<Button> activeSlotButtons = isMaleSelected ? maleSlotButtons : femaleSlotButtons;
        for (int i = 0; i < activeSlotButtons.Count; i++)
        {
            if (activeSlotButtons[i] == null) continue;

            Image slotImg = activeSlotButtons[i].GetComponent<Image>();
            if (slotImg != null)
            {
                bool isSelected = (i == selectedCharacterIndex);
                slotImg.color = isSelected ? slotSelectedColor : slotDefaultColor;
            }
        }

        // 3. Cập nhật InfoPanel theo PlayerSO được chọn
        if (characterList != null && characterList.Count > selectedCharacterIndex && selectedCharacterIndex >= 0)
        {
            PlayerSO activeSO = characterList[selectedCharacterIndex];
            UpdateInfoPanel(activeSO);
            UpdatePreviewModel(activeSO);
        }
    }

    /// <summary>
    /// Cập nhật hiển thị UI Khóa (LockImg) và giá tiền cho từng Slot
    /// </summary>
    private void UpdateSlotsLockVisuals(List<Button> slotButtons)
    {
        for (int i = 0; i < slotButtons.Count; i++)
        {
            if (slotButtons[i] == null) continue;

            // Tìm GameObject cha LockImg bằng hàm đệ quy an toàn
            Transform lockImg = FindChildRecursive(slotButtons[i].transform, "LockImg");

            if (lockImg != null)
            {
                bool isUnlocked = (i < characterList.Count) && IsCharacterUnlocked(characterList[i]);
                lockImg.gameObject.SetActive(!isUnlocked);

                // Nếu đang khóa và có Text/TextMeshPro Price con, cập nhật số tiền
                if (!isUnlocked && i < characterList.Count && characterList[i] != null)
                {
                    Transform priceTrans = FindChildRecursive(lockImg, "Price");
                    if (priceTrans != null)
                    {
                        TextMeshProUGUI priceTmp = priceTrans.GetComponent<TextMeshProUGUI>();
                        if (priceTmp != null)
                        {
                            priceTmp.text = $"${characterList[i].unlockPrice}";
                        }
                        else
                        {
                            Text priceLegacy = priceTrans.GetComponent<Text>();
                            if (priceLegacy != null)
                            {
                                priceLegacy.text = $"${characterList[i].unlockPrice}";
                            }
                        }
                    }
                }
            }
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

    private void UpdatePreviewModel(PlayerSO so)
    {
        if (previewModelRoot == null || so == null) return;

        GameObject characterTarget = GetPreviewCharacterTarget();
        if (characterTarget == null) return;

        Mesh targetMesh = so.GetMesh(isMaleSelected);
        if (targetMesh != null)
        {
            PlayerStats.ApplyMeshToModel(characterTarget, targetMesh);
        }

        PlayerStats.ApplySkinToModel(characterTarget, so.characterMaterial, so.characterTexture);
    }
}
