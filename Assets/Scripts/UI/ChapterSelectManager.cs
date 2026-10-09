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

    [Header("🚪 Panels & Lobby Reference")]
    public GameObject chapterSelectPanel; // Panel chọn Chapter này
    public GameObject mainMenuPanel;      // Panel Main Menu chính (fallback)
    public NetworkLobbyHUD networkLobbyHUD; // Tham chiếu tới Sảnh Multiplayer khi tạo phòng

    [Header("👤 Default Player Data")]
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
        if (startChapterButton != null) startChapterButton.onClick.AddListener(SelectCurrentChapter);
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);

        UpdateChapterUI();
    }

    /// <summary>
    /// Mở panel chọn Chapter từ Modal Tạo phòng (Create Room) trong Multiplayer
    /// </summary>
    public void OpenForRoomCreation(NetworkLobbyHUD lobbyHUD, int initialIndex = 0)
    {
        networkLobbyHUD = lobbyHUD;
        if (chapterList != null && initialIndex >= 0 && initialIndex < chapterList.Count)
        {
            currentChapterIndex = initialIndex;
        }

        PlayClickSound();
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(true);
        else gameObject.SetActive(true);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (networkLobbyHUD != null && networkLobbyHUD.lobbyMainPanel != null)
        {
            networkLobbyHUD.lobbyMainPanel.SetActive(false);
        }

        // Ẩn 3D model ngoài sảnh khi vào màn hình chọn Chapter
        CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
        if (charHud != null)
        {
            charHud.SetLobbyModelVisible(false);
        }

        UpdateChapterUI();
    }

    public void OpenChapterSelect()
    {
        PlayClickSound();
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(true);
        else gameObject.SetActive(true);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (networkLobbyHUD != null && networkLobbyHUD.lobbyMainPanel != null)
        {
            networkLobbyHUD.lobbyMainPanel.SetActive(false);
        }

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
        ClosePanel();
    }

    public void ClosePanel()
    {
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(false);
        else gameObject.SetActive(false);

        if (networkLobbyHUD != null)
        {
            networkLobbyHUD.ShowCreateModal();
        }
        else if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            CharacterSelectionHUD charHud = FindFirstObjectByType<CharacterSelectionHUD>(FindObjectsInactive.Include);
            if (charHud != null)
            {
                charHud.ApplyLobbyModelVisuals();
            }
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

    /// <summary>
    /// Xác nhận chọn Chapter hiện tại cho Phòng chơi Multiplayer
    /// </summary>
    public void SelectCurrentChapter()
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

        // Trả kết quả đã chọn về cho Sảnh Tạo Phòng
        if (networkLobbyHUD != null)
        {
            networkLobbyHUD.OnChapterSelectedFromPanel(currentChapterIndex);
        }

        ClosePanel();
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
}
