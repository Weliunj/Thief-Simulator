using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChapterSelectManager : MonoBehaviour
{
    [Header("📦 Chapter Data (ScriptableObjects)")]
    public List<ChapterSO> chapterList = new List<ChapterSO>();

    [Header("🖼️ Current Chapter UI (Ở giữa)")]
    public Image chapterPreviewImage;        // Ảnh Chapter chính ở giữa
    public TextMeshProUGUI chapterTitleText; // Tiêu đề Chapter
    public TextMeshProUGUI chapterDescriptionText; // Mô tả Chapter
    public GameObject lockOverlay;           // GameObject biểu tượng Ổ Khóa (hiện khi locked)

    [Header("🖼️ Side Preview Cards (Ảnh Chapter Trước & Sau)")]
    public Image prevChapterImage; // Ảnh của Chapter phía trước (bên trái)
    public Image nextChapterImage; // Ảnh của Chapter kế tiếp (bên phải)

    [Header("◀️ ▶️ Navigation & Action Buttons")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button startChapterButton;
    public Button backButton;

    [Header("🚪 Panels Reference")]
    public GameObject chapterSelectPanel; // Panel chọn Chapter này
    public GameObject mainMenuPanel;      // Panel Main Menu chính (để hiện lại khi bấm Back)

    [Header("👤 Default Player Data (Tạm thời hardcode khi chưa có UI chọn nhân vật)")]
    public PlayerSO defaultPlayerData;

    [Header("🔊 Audio")]
    public AudioSource audioSource;

    private int currentChapterIndex = 0;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (FindFirstObjectByType<UIEventSystemFixer>() == null)
        {
            gameObject.AddComponent<UIEventSystemFixer>();
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (leftArrowButton != null) leftArrowButton.onClick.AddListener(PreviousChapter);
        if (rightArrowButton != null) rightArrowButton.onClick.AddListener(NextChapter);
        if (startChapterButton != null) startChapterButton.onClick.AddListener(PlayCurrentChapter);
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);

        UpdateChapterUI();
    }

    public void OpenChapterSelect()
    {
        PlayClickSound();
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // Ẩn 3D model ngoài sảnh khi vào màn hình chọn Chapter
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.SetLobbyModelVisible(false);
        }

        UpdateChapterUI();
    }

    public void OnBackButtonClicked()
    {
        PlayClickSound();
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        // Hiện lại 3D model ngoài sảnh chính
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.ApplyLobbyModelVisuals();
        }
    }

    public void NextChapter()
    {
        if (chapterList == null || chapterList.Count == 0) return;
        PlayClickSound();
        currentChapterIndex = (currentChapterIndex + 1) % chapterList.Count;
        UpdateChapterUI();
    }

    public void PreviousChapter()
    {
        if (chapterList == null || chapterList.Count == 0) return;
        PlayClickSound();
        currentChapterIndex = (currentChapterIndex - 1 + chapterList.Count) % chapterList.Count;
        UpdateChapterUI();
    }

    public void PlayCurrentChapter()
    {
        if (chapterList == null || chapterList.Count == 0) return;

        ChapterSO currentChapter = chapterList[currentChapterIndex];

        if (!currentChapter.isUnlocked)
        {
            Debug.LogWarning($"🔒 Chapter '{currentChapter.chapterTitle}' chưa được mở khóa!");
            return;
        }

        PlayClickSound();

        // Lưu thông tin phiên chơi vào GameSession
        GameSession.SelectedChapter = currentChapter;
        int nextIndex = currentChapterIndex + 1;
        GameSession.NextChapter = (nextIndex < chapterList.Count) ? chapterList[nextIndex] : null;

        // Đồng bộ nhân vật đã lưu vào GameSession
        if (GameSession.SelectedPlayer == null)
        {
            CharacterSelectionHUD hud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
            if (hud != null)
            {
                hud.LoadSavedSelection();
            }
        }

        if (GameSession.SelectedPlayer == null && defaultPlayerData != null)
        {
            GameSession.SelectedPlayer = defaultPlayerData;
        }

        if (!string.IsNullOrEmpty(currentChapter.sceneName))
        {
            StartCoroutine(LoadSceneByNameAfterDelay(currentChapter.sceneName, 0.4f));
        }
    }

    private void UpdateChapterUI()
    {
        if (chapterList == null || chapterList.Count == 0) return;

        if (currentChapterIndex >= chapterList.Count) currentChapterIndex = 0;
        ChapterSO current = chapterList[currentChapterIndex];

        // 1. Cập nhật Chapter chính ở giữa
        if (chapterPreviewImage != null && current.chapterImage != null)
        {
            chapterPreviewImage.sprite = current.chapterImage;
            chapterPreviewImage.enabled = true;
        }

        if (chapterTitleText != null) chapterTitleText.text = current.chapterTitle;
        if (chapterDescriptionText != null) chapterDescriptionText.text = current.description;

        // 2. Cập nhật Trạng thái Ổ Khóa
        if (lockOverlay != null) lockOverlay.SetActive(!current.isUnlocked);
        if (startChapterButton != null) startChapterButton.interactable = current.isUnlocked;

        // 3. Cập nhật Ảnh Preview của Chapter Trước & Sau
        int prevIndex = (currentChapterIndex - 1 + chapterList.Count) % chapterList.Count;
        int nextIndex = (currentChapterIndex + 1) % chapterList.Count;

        if (prevChapterImage != null && chapterList[prevIndex].chapterImage != null)
        {
            prevChapterImage.sprite = chapterList[prevIndex].chapterImage;
            prevChapterImage.enabled = true;
        }

        if (nextChapterImage != null && chapterList[nextIndex].chapterImage != null)
        {
            nextChapterImage.sprite = chapterList[nextIndex].chapterImage;
            nextChapterImage.enabled = true;
        }
    }

    private void PlayClickSound()
    {
        if (audioSource != null)
        {
            if (audioSource.clip != null)
            {
                audioSource.PlayOneShot(audioSource.clip);
            }
            else
            {
                audioSource.Play();
            }
        }
    }

    private IEnumerator LoadSceneByNameAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
