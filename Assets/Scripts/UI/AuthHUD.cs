using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;

/// <summary>
/// Quản lý giao diện Đăng ký / Đăng nhập / Quên mật khẩu / Đăng nhập Khách (Task 1.2):
/// - Chuyển đổi linh hoạt giữa LoginPanel, RegisterPanel, ForgotPassPanel.
/// - Xử lý thông báo lỗi trực quan (màu đỏ/xanh) và trạng thái Loading.
/// - Tự động đồng bộ UI khi đăng nhập thành công và mở Main Menu.
/// </summary>
public class AuthHUD : MonoBehaviour
{
    [Header("🚪 Panel References")]
    public GameObject authRootPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject forgotPasswordPanel;
    public GameObject nextPanelOnSuccess; // Thường là MainMenuPanel trong HomeMenu

    [Header("🔑 Login Form Elements")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginSubmitBtn;
    public Button loginGuestBtn;
    public Button switchToRegisterBtn;
    public Button switchToForgotPassBtn;

    [Header("📝 Register Form Elements")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;
    public Button registerSubmitBtn;
    public Button backToLoginFromRegisterBtn;

    [Header("❓ Forgot Password Elements")]
    public TMP_InputField forgotEmailInput;
    public Button forgotSubmitBtn;
    public Button backToLoginFromForgotBtn;

    [Header("📢 Status & Feedback")]
    public TextMeshProUGUI statusText;
    public GameObject loadingSpinner;

    [Header("⏳ Loading Screen / Progress Slider")]
    [Tooltip("Panel màn hình Loading giả lập / lúc mở game (Tùy chọn)")]
    public GameObject loadingPanel;
    [Tooltip("Thanh Slider phần trăm loading (0 -> 1 hoặc 0 -> 100)")]
    public Slider loadingSlider;
    [Tooltip("Text hiển thị % loading (VD: 85%, 100%)")]
    public TextMeshProUGUI loadingProgressText;
    [Tooltip("Text hiển thị trạng thái loading (VD: Đang kiểm tra dữ liệu...)")]
    public TextMeshProUGUI loadingStatusText;

    [Header("🔊 Audio")]
    public AudioSource audioSource;

    private Coroutine statusClearCoroutine;
    private Coroutine autoLoginCoroutine;

    private void Awake()
    {
        if (authRootPanel == null) authRootPanel = gameObject;
    }

    private void OnEnable()
    {
        FirebaseDataService.OnLoggedOutFromAnotherDevice += HandleLoggedOutFromAnotherDevice;
    }

    private void OnDisable()
    {
        FirebaseDataService.OnLoggedOutFromAnotherDevice -= HandleLoggedOutFromAnotherDevice;
    }

    private void HandleLoggedOutFromAnotherDevice()
    {
        // 1. Đóng Main Menu & các panel khác đang mở
        HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
        if (home != null)
        {
            if (home.mainMenuPanel != null) home.mainMenuPanel.SetActive(false);
            if (home.infoPanel != null) home.infoPanel.SetActive(false);
            if (home.characterSelectPanel != null) home.characterSelectPanel.SetActive(false);
            if (home.settingPanel != null) home.settingPanel.SetActive(false);
        }

        // 2. Mở lại giao diện AuthHUD
        if (authRootPanel != null) authRootPanel.SetActive(true);
        gameObject.SetActive(true);
        ShowLoginPanel(clearStatus: false);

        // 3. Xóa trắng pass để yêu cầu đăng nhập lại
        if (loginPasswordInput != null) loginPasswordInput.text = "";

        // 4. Hiển thị thông báo màu đỏ cố định
        ShowStatus("This account was logged in on another device. You have been logged out.", true, permanent: true);
    }

    private void Start()
    {
        BindButtonEvents();

        // Nếu người chơi ĐÃ ĐĂNG NHẬP (VD: vừa từ màn chơi Gameplay thoát về Menu) -> Bỏ qua Loading & AutoLogin
        if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn)
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (authRootPanel != null) authRootPanel.SetActive(false);
            OnAuthSuccess();
            return;
        }

        ShowLoginPanel();
        autoLoginCoroutine = StartCoroutine(AutoLoginRoutine());
    }

    /// <summary>
    /// Giả lập tự động đăng nhập khi mở lại APK kèm thanh Loading Slider:
    /// - TH1: Đăng nhập thành công -> Slider chạy lên mốc 100%, vào Main Menu.
    /// - TH2: Mất mạng / Lỗi -> Tắt Loading, xóa 2 ô Email/Pass, Status 'No Internet Connection!' hiện vĩnh viễn, làm mờ các nút Online.
    /// </summary>
    private IEnumerator AutoLoginRoutine()
    {
        var (savedEmail, savedPass) = FirebaseAuthService.GetSavedAutoLoginCredentials();
        bool hasSavedAccount = !string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPass);

        // Bật Loading Panel nếu có gán trong Inspector
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            SetLoadingProgress(0.1f, "Initializing system...");
        }

        // =====================================================================
        // TRƯỜNG HỢP 2 (TH2): MẤT MẠNG KHI MỞ GAME (OFFLINE)
        // =====================================================================
        if (!FirebaseAuthService.IsNetworkAvailable)
        {
            Debug.LogWarning("<color=yellow>[AuthHUD] Offline on startup -> Switching to Offline Mode.</color>");

            if (loadingPanel != null)
            {
                float p = 0.1f;
                while (p < 0.35f)
                {
                    p += Time.deltaTime * 0.9f;
                    SetLoadingProgress(p, "Checking connection...");
                    yield return null;
                }
                yield return new WaitForSeconds(0.2f);
                loadingPanel.SetActive(false);
            }

            HandleOfflineState();
            yield break;
        }

        // =====================================================================
        // NẾU CÓ MẠNG NHƯNG CHƯA TỪNG ĐĂNG NHẬP (LẦN ĐẦU / ĐÃ LOGOUT)
        // =====================================================================
        if (!hasSavedAccount)
        {
            if (loadingPanel != null)
            {
                float p = 0.1f;
                while (p < 0.65f)
                {
                    p += Time.deltaTime * 1.5f;
                    SetLoadingProgress(p, "Loading resources...");
                    yield return null;
                }
                loadingPanel.SetActive(false);
            }

            UpdateNetworkButtonStates(true);
            yield break;
        }

        // =====================================================================
        // CÓ MẠNG VÀ CÓ TÀI KHOẢN ĐÃ LƯU -> TIẾN HÀNH XÁC THỰC
        // =====================================================================
        if (loginEmailInput != null) loginEmailInput.text = savedEmail;
        if (loginPasswordInput != null) loginPasswordInput.text = savedPass;

        if (loadingPanel != null)
        {
            SetLoadingProgress(0.4f, "Authenticating...");
        }

        bool? loginSuccess = null;
        string loginMessage = "";

        FirebaseAuthService.Instance.SignInWithEmail(savedEmail, savedPass, (success, message) =>
        {
            loginSuccess = success;
            loginMessage = message;
        });

        // Chạy Slider mượt mà lên khoảng 85% trong lúc chờ máy chủ Firebase phản hồi
        float curProgress = 0.4f;
        float timeout = 9.0f;
        float elapsed = 0f;

        while (loginSuccess == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            curProgress = Mathf.MoveTowards(curProgress, 0.88f, Time.deltaTime * 0.35f);
            SetLoadingProgress(curProgress, "Syncing player data...");
            yield return null;
        }

        // ---------------------------------------------------------------------
        // TRƯỜNG HỢP 1 (TH1): ĐĂNG NHẬP THÀNH CÔNG -> LÊN MỐC 100%
        // ---------------------------------------------------------------------
        if (loginSuccess == true)
        {
            Debug.Log("<color=green>[AuthHUD] Auto login successful!</color>");
            while (curProgress < 1.0f)
            {
                curProgress = Mathf.MoveTowards(curProgress, 1.0f, Time.deltaTime * 3.0f);
                SetLoadingProgress(curProgress, "Login Successful!");
                yield return null;
            }

            SetLoadingProgress(1.0f, "Completed!");
            yield return new WaitForSeconds(0.3f);

            if (loadingPanel != null) loadingPanel.SetActive(false);
            OnAuthSuccess();
        }
        else
        {
            // TH2: LỖI KẾT NỐI MẠNG HOẶC ĐĂNG NHẬP THẤT BẠI
            Debug.LogWarning($"<color=yellow>[AuthHUD] Auto login failed: {loginMessage}</color>");
            if (loadingPanel != null) loadingPanel.SetActive(false);

            if (!FirebaseAuthService.IsNetworkAvailable)
            {
                HandleOfflineState();
            }
            else
            {
                // Có mạng nhưng sai pass hoặc tài khoản lỗi
                ShowStatus($"Auto Login Failed: {loginMessage}", true, permanent: false);
                UpdateNetworkButtonStates(true);
            }
        }
    }

    /// <summary>
    /// Xử lý trạng thái Ngoại tuyến (TH2):
    /// - Xóa 2 ô Input Email và Password.
    /// - Hiển thị thông báo 'No Internet Connection!' vĩnh viễn.
    /// - Làm mờ / Vô hiệu hóa cả nút lẫn chữ bên trong (Login, Register, Forgot...).
    /// - Nút Chơi Khách vẫn sáng để vào chơi Offline.
    /// </summary>
    private void HandleOfflineState()
    {
        // 1. Xóa 2 input email + pass
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";

        // 2. Status 'No Internet Connection!' hiển thị vĩnh viễn
        ShowStatus("No Internet Connection!", true, permanent: true);

        // 3. Làm mờ / vô hiệu hóa các nút và chữ bên trong
        UpdateNetworkButtonStates(false);

        // 4. Mở LoginPanel nhưng giữ nguyên dòng Status
        ShowLoginPanel(clearStatus: false);
    }

    /// <summary>
    /// Cập nhật khả năng tương tác và độ mờ của nút cùng toàn bộ chữ con (TextMeshPro) bên trong
    /// </summary>
    public void UpdateNetworkButtonStates(bool isOnline)
    {
        SetButtonInteractable(loginSubmitBtn, isOnline);
        SetButtonInteractable(switchToRegisterBtn, isOnline);
        SetButtonInteractable(switchToForgotPassBtn, isOnline);
        SetButtonInteractable(registerSubmitBtn, isOnline);
        SetButtonInteractable(forgotSubmitBtn, isOnline);

        // Nút Khách luôn luôn hoạt động bình thường
        SetButtonInteractable(loginGuestBtn, true);
    }

    /// <summary>
    /// Bật/tắt nút đồng thời làm mờ cả TextMeshPro bên trong nút khi bị disable
    /// </summary>
    private void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn == null) return;
        btn.interactable = interactable;

        // Làm mờ toàn bộ chữ TextMeshPro bên trong nút
        var texts = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            if (t == null) continue;
            Color c = t.color;
            c.a = interactable ? 1f : 0.35f;
            t.color = c;
        }

        // Làm mờ CanvasGroup (nếu nút có gắn CanvasGroup)
        var canvasGroup = btn.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? 1f : 0.4f;
        }
    }

    private void SetLoadingProgress(float progress01, string status)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (loadingSlider != null)
        {
            loadingSlider.value = progress01;
        }

        if (loadingProgressText != null)
        {
            loadingProgressText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
        }

        if (loadingStatusText != null && !string.IsNullOrEmpty(status))
        {
            loadingStatusText.text = status;
        }
    }

    private void BindButtonEvents()
    {
        // 1. Login Events
        if (loginSubmitBtn != null) loginSubmitBtn.onClick.AddListener(OnLoginClicked);
        if (loginGuestBtn != null) loginGuestBtn.onClick.AddListener(OnGuestClicked);
        if (switchToRegisterBtn != null) switchToRegisterBtn.onClick.AddListener(() => ShowRegisterPanel(true));
        if (switchToForgotPassBtn != null) switchToForgotPassBtn.onClick.AddListener(() => ShowForgotPasswordPanel(true));

        // 2. Register Events
        if (registerSubmitBtn != null) registerSubmitBtn.onClick.AddListener(OnRegisterClicked);
        if (backToLoginFromRegisterBtn != null) backToLoginFromRegisterBtn.onClick.AddListener(() => ShowLoginPanel(true, true));

        // 3. Forgot Pass Events
        if (forgotSubmitBtn != null) forgotSubmitBtn.onClick.AddListener(OnForgotPassClicked);
        if (backToLoginFromForgotBtn != null) backToLoginFromForgotBtn.onClick.AddListener(() => ShowLoginPanel(true, true));
    }

    // =========================================================================
    //                        CHUYỂN ĐỔI FORM UI
    // =========================================================================

    public void ShowLoginPanel()
    {
        ShowLoginPanel(clearStatus: true, playSound: false);
    }

    public void ShowLoginPanel(bool clearStatus, bool playSound = false)
    {
        if (playSound) PlayClickSound();
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (clearStatus) ClearStatus();
    }

    public void ShowRegisterPanel(bool playSound = false)
    {
        if (playSound) PlayClickSound();
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        ClearStatus();
    }

    public void ShowForgotPasswordPanel(bool playSound = false)
    {
        if (playSound) PlayClickSound();
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(true);
        ClearStatus();
    }

    // =========================================================================
    //                        XỬ LÝ SỰ KIỆN CLICK
    // =========================================================================

    private void OnLoginClicked()
    {
        PlayClickSound();

        string email = loginEmailInput != null ? loginEmailInput.text.Trim() : "";
        string password = loginPasswordInput != null ? loginPasswordInput.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please enter both Email and Password!", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Invalid Email format (e.g. player@gmail.com)!", true);
            return;
        }

        SetLoading(true);
        ShowStatus("Logging in...", false);

        FirebaseAuthService.Instance.SignInWithEmail(email, password, (success, message) =>
        {
            SetLoading(false);
            if (success)
            {
                ShowStatus(message, false);
                SafeCloseAuth(0.8f);
            }
            else
            {
                ShowStatus(message, true);
            }
        });
    }

    private void OnRegisterClicked()
    {
        PlayClickSound();

        string username = registerUsernameInput != null ? registerUsernameInput.text.Trim() : "Thief";
        string email = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please fill in all required fields!", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Invalid Email format (e.g. player@gmail.com)!", true);
            return;
        }

        if (password != confirmPassword)
        {
            ShowStatus("Passwords do not match!", true);
            return;
        }

        if (password.Length < 8)
        {
            ShowStatus("Password must be at least 8 characters!", true);
            return;
        }

        SetLoading(true);
        ShowStatus("Creating account...", false);

        FirebaseAuthService.Instance.SignUpWithEmail(email, password, username, (success, message) =>
        {
            SetLoading(false);
            if (success)
            {
                ShowStatus(message, false);

                if (FirebaseAuthService.Instance.RequireEmailVerification)
                {
                    // Tự động chuyển về form Đăng nhập và điền sẵn Email để người chơi đăng nhập sau khi xác thực
                    ShowLoginPanel();
                    if (loginEmailInput != null) loginEmailInput.text = email;
                    if (loginPasswordInput != null) loginPasswordInput.text = "";
                }
                else
                {
                    SafeCloseAuth(0.8f);
                }
            }
            else
            {
                ShowStatus(message, true);
            }
        });
    }

    private void OnGuestClicked()
    {
        PlayClickSound();

        SetLoading(true);
        ShowStatus("Initializing Guest account...", false);

        FirebaseAuthService.Instance.SignInAnonymously((success, message) =>
        {
            SetLoading(false);
            if (success)
            {
                ShowStatus(message, false);
                SafeCloseAuth(0.4f);
            }
            else
            {
                ShowStatus(message, true);
            }
        });
    }

    private void OnForgotPassClicked()
    {
        PlayClickSound();

        string email = forgotEmailInput != null ? forgotEmailInput.text.Trim() : "";
        if (string.IsNullOrEmpty(email))
        {
            ShowStatus("Please enter your Email address!", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Invalid Email format (e.g. player@gmail.com)!", true);
            return;
        }

        SetLoading(true);
        ShowStatus("Sending reset email...", false);

        FirebaseAuthService.Instance.SendPasswordResetEmail(email, (success, message) =>
        {
            SetLoading(false);
            ShowStatus(message, !success);
        });
    }

    /// <summary>
    /// Kiểm tra định dạng Email hợp lệ (Regex)
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // =========================================================================
    //                        UI FEEDBACK & TIỆN ÍCH
    // =========================================================================

    private void OnAuthSuccess()
    {
        if (authRootPanel != null) authRootPanel.SetActive(false);
        if (nextPanelOnSuccess != null) nextPanelOnSuccess.SetActive(true);

        HomeScreen home = FindFirstObjectByType<HomeScreen>(FindObjectsInactive.Include);
        if (home != null)
        {
            if (home.mainMenuPanel != null)
            {
                home.mainMenuPanel.SetActive(true);
            }
            home.UpdatePlayerProfileVisuals();
            home.SyncSavedCharacter();
        }
    }

    private void SafeCloseAuth(float delay)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CloseAuthAfterDelay(delay));
        }
        else
        {
            OnAuthSuccess();
        }
    }

    private IEnumerator CloseAuthAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnAuthSuccess();
    }

    public void ShowStatus(string message, bool isError, bool permanent = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.35f, 1f, 0.45f, 1f);
            statusText.gameObject.SetActive(true);
        }

        if (statusClearCoroutine != null)
        {
            StopCoroutine(statusClearCoroutine);
            statusClearCoroutine = null;
        }

        if (!permanent && !string.IsNullOrEmpty(message) && gameObject.activeInHierarchy)
        {
            statusClearCoroutine = StartCoroutine(ClearStatusRoutine(4.0f));
        }
    }

    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }
    }

    private IEnumerator ClearStatusRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearStatus();
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingSpinner != null) loadingSpinner.SetActive(isLoading);
        if (loginSubmitBtn != null) loginSubmitBtn.interactable = !isLoading;
        if (registerSubmitBtn != null) registerSubmitBtn.interactable = !isLoading;
        if (loginGuestBtn != null) loginGuestBtn.interactable = !isLoading;
    }

    private void PlayClickSound()
    {
        if (audioSource != null)
        {
            if (audioSource.clip != null) audioSource.PlayOneShot(audioSource.clip);
            else audioSource.Play();
        }
    }
}
