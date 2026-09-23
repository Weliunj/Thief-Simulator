using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý giao diện Hồ sơ người chơi (InfoPanel / Profile HUD):
/// - Hiển thị Tên người chơi (Username), Email, UID, và Tiền mặt (Cash).
/// - Đổi tên nhân vật (Edit Username) trực tiếp và đồng bộ lên Firebase.
/// - Đăng xuất (Sign Out) và quay trở về màn hình Đăng nhập (AuthHUD).
/// - Đồng bộ dữ liệu 2 chiều (Manual Cloud Sync).
/// - Xóa tài khoản vĩnh viễn (Yêu cầu xác thực lại mật khẩu trước khi xóa).
/// </summary>
public class ProfileHUD : MonoBehaviour
{
    [Header("👤 Thông tin người chơi")]
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI uidText;
    public TextMeshProUGUI cashText;

    [Header("✏️ Chỉnh sửa tên (Change Name)")]
    public Button editNameBtn;
    public GameObject editNameGroup; // GameObject chứa ô Input và nút Lưu
    public TMP_InputField nameInputField;
    public Button saveNameBtn;
    public Button cancelNameBtn;




    [Header("🚪 Đăng xuất & Đóng")]
    public Button signOutBtn;
    public Button closeBtn;
    public GameObject mainMenuPanel;
    public GameObject authPanel;

    [Header("⚠️ Xóa tài khoản (Delete Account Modal)")]
    public Button openDeleteModalBtn;
    public GameObject deleteAccountModalPanel;
    public TMP_InputField deletePasswordInput;
    public Button confirmDeleteBtn;
    public Button cancelDeleteBtn;
    public TextMeshProUGUI deleteStatusText;

    [Header("🔊 Âm thanh")]
    public AudioSource audioSource;

    private void Awake()
    {
        AutoBindHierarchy();
        SetupAudio();
    }

    private void OnEnable()
    {
        RefreshProfileUI();
        if (editNameGroup != null) editNameGroup.SetActive(false);
        if (nameText != null) nameText.gameObject.SetActive(true);
        if (deleteAccountModalPanel != null) deleteAccountModalPanel.SetActive(false);
    }

    private void Start()
    {
        RegisterButtonEvents();
    }

    private void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        if (audioSource != null && SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            audioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
        }
    }

    private void PlayClickSound()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
    }

    private void RegisterButtonEvents()
    {
        if (editNameBtn != null) editNameBtn.onClick.AddListener(OnEditNameClicked);
        if (saveNameBtn != null) saveNameBtn.onClick.AddListener(OnSaveNameClicked);
        if (cancelNameBtn != null) cancelNameBtn.onClick.AddListener(OnCancelEditNameClicked);
        if (signOutBtn != null) signOutBtn.onClick.AddListener(OnSignOutClicked);
        if (closeBtn != null) closeBtn.onClick.AddListener(OnCloseClicked);

        if (openDeleteModalBtn != null) openDeleteModalBtn.onClick.AddListener(OnOpenDeleteModalClicked);
        if (confirmDeleteBtn != null) confirmDeleteBtn.onClick.AddListener(OnConfirmDeleteClicked);
        if (cancelDeleteBtn != null) cancelDeleteBtn.onClick.AddListener(OnCancelDeleteClicked);


    }

    /// <summary>
    /// Cập nhật toàn bộ thông tin người chơi lên giao diện
    /// </summary>
    public void RefreshProfileUI()
    {
        var auth = FirebaseAuthService.Instance;
        var data = FirebaseDataService.Instance;

        string username = "Thief";
        string email = "Guest (Offline)";
        string uid = "---";
        int cash = 0;
        bool isGuest = true;

        if (data != null && data.CurrentUserProfile != null)
        {
            username = data.CurrentUserProfile.username;
            email = string.IsNullOrEmpty(data.CurrentUserProfile.email) ? "Guest Account (Offline)" : data.CurrentUserProfile.email;
            uid = data.CurrentUserProfile.uid;
            cash = data.CurrentUserProfile.cash;
            isGuest = string.IsNullOrEmpty(data.CurrentUserProfile.email);
        }
        else if (auth != null && auth.CurrentUser != null)
        {
            username = string.IsNullOrEmpty(auth.CurrentUser.DisplayName) ? "Thief" : auth.CurrentUser.DisplayName;
            email = auth.CurrentUser.IsAnonymous ? "Guest Account (Offline)" : auth.CurrentUser.Email;
            uid = auth.CurrentUser.UserId;
            isGuest = auth.CurrentUser.IsAnonymous;
        }

        if (nameText != null) nameText.text = username;
        if (emailText != null) emailText.text = email;
        if (uidText != null) uidText.text = $"ID: {uid}";
        if (cashText != null) cashText.text = $"${cash}";
    }

    // =========================================================================
    //                        ĐỔI TÊN NGƯỜI CHƠI
    // =========================================================================

    private void OnEditNameClicked()
    {
        PlayClickSound();
        if (editNameGroup != null) editNameGroup.SetActive(true);
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (nameInputField != null)
        {
            nameInputField.text = nameText != null ? nameText.text : "";
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    private void OnCancelEditNameClicked()
    {
        PlayClickSound();
        if (editNameGroup != null) editNameGroup.SetActive(false);
        if (nameText != null) nameText.gameObject.SetActive(true);
    }

    private void OnSaveNameClicked()
    {
        PlayClickSound();
        string newName = nameInputField != null ? nameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(newName)) return;

        FirebaseAuthService.Instance.UpdateUsername(newName, (success, message) =>
        {
            if (success)
            {
                if (nameText != null) nameText.text = newName;
                if (editNameGroup != null) editNameGroup.SetActive(false);
                if (nameText != null) nameText.gameObject.SetActive(true);
            }
        });
    }

    // =========================================================================
    //                        ĐĂNG XUẤT (SIGN OUT)
    // =========================================================================

    private void OnSignOutClicked()
    {
        PlayClickSound();
        FirebaseAuthService.Instance.SignOut();

        // Đóng Profile Panel và MainMenu, mở lại AuthHUD
        gameObject.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        if (authPanel == null)
        {
            AuthHUD authHUD = FindFirstObjectByType<AuthHUD>(FindObjectsInactive.Include);
            if (authHUD != null) authPanel = authHUD.gameObject;
        }

        if (authPanel != null)
        {
            authPanel.SetActive(true);
        }
    }

    private void OnCloseClicked()
    {
        PlayClickSound();
        HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
        if (home != null)
        {
            home.CloseInfoPanel();
        }
        else
        {
            gameObject.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
    }

    // =========================================================================
    //                        XÓA TÀI KHOẢN (DELETE ACCOUNT)
    // =========================================================================

    private bool IsCurrentGuest()
    {
        var data = FirebaseDataService.Instance;
        var auth = FirebaseAuthService.Instance;

        if (data != null && data.CurrentUserProfile != null)
        {
            return string.IsNullOrEmpty(data.CurrentUserProfile.email) || FirebaseDataService.IsGuestUser(data.CurrentUserProfile.uid);
        }

        if (auth != null && auth.CurrentUser != null)
        {
            return auth.CurrentUser.IsAnonymous;
        }

        return true;
    }

    private void OnOpenDeleteModalClicked()
    {
        PlayClickSound();
        if (deleteAccountModalPanel != null)
        {
            deleteAccountModalPanel.SetActive(true);

            bool isGuest = IsCurrentGuest();

            // Nếu là Khách (Guest) -> Ẩn ô nhập mật khẩu
            if (deletePasswordInput != null)
            {
                deletePasswordInput.gameObject.SetActive(!isGuest);
                deletePasswordInput.text = "";
            }

            if (deleteStatusText != null)
            {
                deleteStatusText.text = isGuest
                    ? "Are you sure you want to reset and delete all Guest data on this device?"
                    : "Please enter your password to confirm permanent deletion:";
                deleteStatusText.color = isGuest ? new Color(1f, 0.8f, 0.2f) : Color.white;
            }
        }
    }

    private void OnCancelDeleteClicked()
    {
        PlayClickSound();
        if (deleteAccountModalPanel != null)
        {
            deleteAccountModalPanel.SetActive(false);
        }
    }

    private void OnConfirmDeleteClicked()
    {
        PlayClickSound();

        bool isGuest = IsCurrentGuest();
        string pass = (!isGuest && deletePasswordInput != null) ? deletePasswordInput.text : "";

        if (!isGuest && string.IsNullOrEmpty(pass))
        {
            if (deleteStatusText != null)
            {
                deleteStatusText.text = "Please enter your password!";
                deleteStatusText.color = Color.red;
            }
            return;
        }

        if (deleteStatusText != null)
        {
            deleteStatusText.text = "Deleting data...";
            deleteStatusText.color = Color.yellow;
        }

        if (confirmDeleteBtn != null) confirmDeleteBtn.interactable = false;

        FirebaseAuthService.Instance.DeleteAccount(pass, (success, message) =>
        {
            if (confirmDeleteBtn != null) confirmDeleteBtn.interactable = true;

            if (success)
            {
                if (deleteAccountModalPanel != null) deleteAccountModalPanel.SetActive(false);
                gameObject.SetActive(false);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

                if (authPanel == null)
                {
                    AuthHUD authHUD = FindFirstObjectByType<AuthHUD>(FindObjectsInactive.Include);
                    if (authHUD != null) authPanel = authHUD.gameObject;
                }

                if (authPanel != null)
                {
                    authPanel.SetActive(true);
                }
            }
            else
            {
                if (deleteStatusText != null)
                {
                    deleteStatusText.text = message;
                    deleteStatusText.color = Color.red;
                }
            }
        });
    }

    // =========================================================================
    //                        TỰ ĐỘNG BIND HIERARCHY
    // =========================================================================

    [ContextMenu("Auto Bind Hierarchy")]
    public void AutoBindHierarchy()
    {
        if (nameText == null) nameText = transform.Find("Email/Name")?.GetComponent<TextMeshProUGUI>() ?? transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (emailText == null) emailText = transform.Find("Email/EmailText")?.GetComponent<TextMeshProUGUI>() ?? transform.Find("EmailText")?.GetComponent<TextMeshProUGUI>();
        if (uidText == null) uidText = transform.Find("UID")?.GetComponent<TextMeshProUGUI>() ?? transform.Find("UidText")?.GetComponent<TextMeshProUGUI>();
        if (avatarImage == null) avatarImage = transform.Find("Avatar")?.GetComponent<Image>();
        if (signOutBtn == null) signOutBtn = transform.Find("SignOut")?.GetComponent<Button>() ?? transform.Find("SignOutBtn")?.GetComponent<Button>();
        if (closeBtn == null) closeBtn = transform.Find("CloseBtn")?.GetComponent<Button>() ?? transform.Find("Close")?.GetComponent<Button>();

        if (mainMenuPanel == null)
        {
            HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
            if (home != null)
            {
                mainMenuPanel = home.mainMenuPanel;
                authPanel = home.authPanel;
            }
        }
    }
}
