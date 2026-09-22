using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Bảng Chọn Nhân Vật (Character Selection HUD):
/// - Quản lý InfoPanel (Name, Desc, MoveSpeed, RunSpeed, Stamina, CarryWeight kèm Slider & Value).
/// - Quản lý Tab Giới Tính (MaleBtn, FemaleBtn) đổi màu nút và chuyển đổi ContentMale / ContentFemale.
/// - Quản lý các Slot nhân vật trong ScrollView (đổi màu Vàng khi chọn, cập nhật InfoPanel).
/// - Lưu lựa chọn (SaveBtn) vào PlayerPrefs & GameSession, đổi text thành 'Saved!'.
/// - Tự động liên kết tham chiếu (Auto-Bind) theo đúng cấu trúc cây Hierarchy.
/// </summary>
public class CharacterSelectionHUD : MonoBehaviour
{
    [Header("📚 Character Data (PlayerSO)")]
    [Tooltip("Danh sách các cấu hình nhân vật (VD: 3 PlayerSO cho Normal, Fat, Strong)")]
    public List<PlayerSO> characterList = new List<PlayerSO>();

    [Header("🚪 Panel Navigation")]
    [Tooltip("Panel trước đó cần bật lại khi bấm nút Close (Ví dụ: MainMenuPanel)")]
    public GameObject previousPanel;

    [Header("🎯 3D Preview in Scene (Optional)")]
    [Tooltip("GameObject Model nhân vật 3D đứng preview trong Scene HomeMenu (nếu có)")]
    public GameObject previewModelRoot;

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

    private const string PREF_KEY_CHAR_INDEX = "SelectedCharIndex";
    private const string PREF_KEY_GENDER = "SelectedGender"; // 0 = Male, 1 = Female

    private void Awake()
    {
        SetupAudioSource();
        AutoBindHierarchy();
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
        }

        // Gắn sự kiện cho các Slot Female
        for (int i = 0; i < femaleSlotButtons.Count; i++)
        {
            int index = i;
            femaleSlotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
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
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
        foreach (var btn in femaleSlotButtons)
        {
            if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// Đọc dữ liệu đã lưu từ PlayerPrefs
    /// </summary>
    private void LoadSavedSelection()
    {
        selectedCharacterIndex = PlayerPrefs.GetInt(PREF_KEY_CHAR_INDEX, 0);
        int genderCode = PlayerPrefs.GetInt(PREF_KEY_GENDER, 0);
        isMaleSelected = (genderCode == 0);

        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characterList.Count)
        {
            selectedCharacterIndex = 0;
        }

        // Đồng bộ vào GameSession
        if (characterList.Count > selectedCharacterIndex)
        {
            GameSession.SelectedPlayer = characterList[selectedCharacterIndex];
            GameSession.IsMale = isMaleSelected;
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

    public void OnSaveButtonClicked()
    {
        PlayerPrefs.SetInt(PREF_KEY_CHAR_INDEX, selectedCharacterIndex);
        PlayerPrefs.SetInt(PREF_KEY_GENDER, isMaleSelected ? 0 : 1);
        PlayerPrefs.Save();

        if (characterList.Count > selectedCharacterIndex)
        {
            GameSession.SelectedPlayer = characterList[selectedCharacterIndex];
            GameSession.IsMale = isMaleSelected;
        }

        PlayClickSound();
        Debug.Log($"<color=green>[CharacterSelectionHUD] Đã lưu nhân vật: {GameSession.SelectedPlayer?.characterName} | Giới tính: {(isMaleSelected ? "Male" : "Female")}</color>");

        if (saveFeedbackCoroutine != null) StopCoroutine(saveFeedbackCoroutine);
        saveFeedbackCoroutine = StartCoroutine(ShowSavedFeedbackRoutine());
    }

    public void OnCloseButtonClicked()
    {
        PlayClickSound();
        gameObject.SetActive(false);

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
            string originalText = saveBtnText.text;
            saveBtnText.text = "<color=#22C55E>Saved!</color>";
            yield return new WaitForSecondsRealtime(1.5f);
            saveBtnText.text = originalText;
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

        // 2. Cập nhật màu các Slot (Slot được chọn đổi màu Vàng)
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

        Mesh targetMesh = so.GetMesh(isMaleSelected);
        if (targetMesh != null)
        {
            PlayerStats.ApplyMeshToModel(previewModelRoot, targetMesh);
        }

        PlayerStats.ApplySkinToModel(previewModelRoot, so.characterMaterial, so.characterTexture);
    }
}
