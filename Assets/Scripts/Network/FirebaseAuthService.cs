using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

/// <summary>
/// Quản lý toàn bộ dịch vụ xác thực tài khoản Firebase (Firebase Authentication):
/// - Khởi tạo Firebase SDK an toàn trên Unity 6 / Windows / Android.
/// - Đăng ký (Sign Up) tài khoản với Email & Mật khẩu.
/// - Đăng nhập (Sign In) với Email & Mật khẩu.
/// - Đăng nhập Khách (Guest / Anonymous Sign In).
/// - Quên mật khẩu (Reset Password qua Email).
/// - Đăng xuất (Sign Out) và tự động ghi nhận trạng thái phiên đăng nhập.
/// </summary>
public class FirebaseAuthService : MonoBehaviour
{
    private static FirebaseAuthService _instance;
    public static FirebaseAuthService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<FirebaseAuthService>();
                if (_instance == null)
                {
                    GameObject go = GameObject.Find("[NetworkServices]");
                    if (go == null)
                    {
                        go = new GameObject("[NetworkServices]");
                        DontDestroyOnLoad(go);
                    }
                    _instance = go.AddComponent<FirebaseAuthService>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser CurrentUser => Auth != null ? Auth.CurrentUser : null;
    public bool IsInitialized { get; private set; } = false;
    public bool IsLoggedIn => CurrentUser != null && (!requireEmailVerification || CurrentUser.IsAnonymous || CurrentUser.IsEmailVerified);
    public bool IsGuest => CurrentUser != null && CurrentUser.IsAnonymous;

    /// <summary>
    /// Kiểm tra thiết bị có đang kết nối mạng Internet (Wifi/4G/LAN) hay không
    /// </summary>
    public static bool IsNetworkAvailable => Application.internetReachability != NetworkReachability.NotReachable;

    /// <summary>
    /// Kiểm tra game đã kết nối Online và Firebase đã sẵn sàng hoạt động
    /// </summary>
    public static bool IsOnlineReady => IsNetworkAvailable && Instance != null && Instance.IsInitialized;

    // Events thông báo trạng thái
    [Header("Email Verification")]
    [Tooltip("Bắt buộc người chơi phải kích hoạt tài khoản qua link gửi về email trước khi đăng nhập")]
    [SerializeField] private bool requireEmailVerification = true;
    public bool RequireEmailVerification => requireEmailVerification;

    public static event Action<FirebaseUser> OnUserSignedIn;
    public static event Action OnUserSignedOut;
    public static event Action<bool> OnFirebaseInitialized;

    private static readonly TaskCompletionSource<bool> _initTcs = new TaskCompletionSource<bool>();

    /// <summary>
    /// Đảm bảo Firebase đã khởi tạo xong trước khi thực hiện bất kỳ tác vụ mạng nào
    /// </summary>
    public static async Task<bool> EnsureInitializedAsync()
    {
        if (Instance != null && Instance.IsInitialized && Instance.Auth != null)
        {
            return true;
        }
        return await _initTcs.Task;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Kiểm tra và sửa đổi dependencies để khởi tạo Firebase
    /// </summary>
    public void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Auth = FirebaseAuth.DefaultInstance;

                // Nếu có phiên đăng nhập cũ lưu trong máy nhưng chưa xác thực email -> Tự động đăng xuất
                if (requireEmailVerification && Auth.CurrentUser != null && !Auth.CurrentUser.IsAnonymous)
                {
                    try { await Auth.CurrentUser.ReloadAsync(); } catch { }
                    if (!Auth.CurrentUser.IsEmailVerified)
                    {
                        Debug.Log("<color=yellow>[FirebaseAuthService] Hủy phiên đăng nhập cũ do tài khoản chưa xác thực Email.</color>");
                        Auth.SignOut();
                    }
                }

                Auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);

                IsInitialized = true;
                _initTcs.TrySetResult(true);
                Debug.Log("<color=green>[FirebaseAuthService] Firebase Authentication đã khởi tạo thành công!</color>");
                OnFirebaseInitialized?.Invoke(true);
            }
            else
            {
                IsInitialized = false;
                _initTcs.TrySetResult(false);
                Debug.LogError($"[FirebaseAuthService] Không thể khởi tạo Firebase: {dependencyStatus}");
                OnFirebaseInitialized?.Invoke(false);
            }
        });
    }

    private void OnDestroy()
    {
        if (Auth != null)
        {
            Auth.StateChanged -= AuthStateChanged;
            Auth = null;
        }
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (Auth != null && Auth.CurrentUser != null)
        {
            if (IsLoggedIn)
            {
                Debug.Log($"<color=cyan>[FirebaseAuthService] Người chơi đăng nhập: {Auth.CurrentUser.UserId} (Anonymous: {Auth.CurrentUser.IsAnonymous})</color>");
                OnUserSignedIn?.Invoke(Auth.CurrentUser);
            }
        }
        else
        {
            Debug.Log("<color=yellow>[FirebaseAuthService] Người chơi đã đăng xuất.</color>");
            OnUserSignedOut?.Invoke();
        }
    }

    // =========================================================================
    //                        ĐĂNG KÝ (SIGN UP)
    // =========================================================================

    /// <summary>
    /// Đăng ký tài khoản mới bằng Email và Mật khẩu (Nếu đang là Guest sẽ đăng xuất Guest trước rồi tạo account mới hoàn toàn)
    /// </summary>
    public async void SignUpWithEmail(string email, string password, string username, Action<bool, string> callback)
    {
        if (!IsInitialized || Auth == null)
        {
            bool ready = await EnsureInitializedAsync();
            if (!ready || Auth == null)
            {
                callback?.Invoke(false, "Could not connect to authentication service. Please check your network!");
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            callback?.Invoke(false, "Please enter both Email and Password.");
            return;
        }

        // Nếu đang là Guest -> Đăng xuất Guest trước, sau đó tạo tài khoản mới hoàn toàn (không chuyển giao dữ liệu)
        if (CurrentUser != null && CurrentUser.IsAnonymous)
        {
            Debug.Log("<color=yellow>[FirebaseAuthService] Logging out Guest before creating new account...</color>");
            Auth.SignOut();
        }

        // Trường hợp 2: Đăng ký tài khoản mới tinh thông thường
        Auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(async task =>
        {
            if (task.IsCanceled)
            {
                callback?.Invoke(false, "Registration was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                string errorMsg = GetFriendlyErrorMessage(task.Exception);
                Debug.LogError($"[FirebaseAuthService] Sign up failed: {errorMsg}");
                callback?.Invoke(false, errorMsg);
                return;
            }

            AuthResult result = task.Result;
            FirebaseUser newUser = result.User;

            // Cập nhật DisplayName (Username)
            if (!string.IsNullOrEmpty(username) && newUser != null)
            {
                UserProfile profile = new UserProfile { DisplayName = username };
                await newUser.UpdateUserProfileAsync(profile);
            }

            Debug.Log($"<color=green>[FirebaseAuthService] Account created successfully: {newUser.Email} (UID: {newUser.UserId})</color>");

            // Lưu thông tin đăng nhập tạm thời
            SaveAutoLoginCredentials(email, password);

            // Khởi tạo hồ sơ người chơi ban đầu trên Database
            if (FirebaseDataService.Instance != null)
            {
                await FirebaseDataService.Instance.CreateNewUserProfileAsync(newUser.UserId, username, email);
            }

            // Gửi email xác thực chống tài khoản ảo
            if (requireEmailVerification && newUser != null)
            {
                try
                {
                    await newUser.SendEmailVerificationAsync();
                    Debug.Log($"<color=cyan>[FirebaseAuthService] Verification email sent to: {newUser.Email}</color>");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirebaseAuthService] Could not send verification email: {ex.Message}");
                }

                // Đăng xuất ngay để bắt buộc người chơi phải mở mail click link trước khi đăng nhập
                Auth.SignOut();
                callback?.Invoke(true, "Registration successful! Verification email sent. Please check your inbox/spam before logging in.");
                return;
            }

            callback?.Invoke(true, "Account registered successfully!");
        });
    }

    // =========================================================================
    //                        ĐĂNG NHẬP (SIGN IN)
    // =========================================================================

    /// <summary>
    /// Đăng nhập bằng Email và Mật khẩu. Tự động kiểm tra trạng thái xác thực email nếu tính năng được bật.
    /// </summary>
    public async void SignInWithEmail(string email, string password, Action<bool, string> callback)
    {
        if (!IsInitialized || Auth == null)
        {
            bool ready = await EnsureInitializedAsync();
            if (!ready || Auth == null)
            {
                callback?.Invoke(false, "Could not connect to authentication service. Please check your network!");
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            callback?.Invoke(false, "Please enter both Email and Password.");
            return;
        }

        Auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(async task =>
        {
            if (task.IsCanceled)
            {
                callback?.Invoke(false, "Login was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                string errorMsg = GetFriendlyErrorMessage(task.Exception);
                Debug.LogError($"[FirebaseAuthService] Sign in failed: {errorMsg}");
                callback?.Invoke(false, errorMsg);
                return;
            }

            AuthResult result = task.Result;
            FirebaseUser user = result.User;

            // Kiểm tra trạng thái xác thực Email
            if (requireEmailVerification && user != null)
            {
                try
                {
                    // Tải lại trạng thái mới nhất từ máy chủ Firebase
                    await user.ReloadAsync();
                }
                catch { }

                if (!user.IsEmailVerified)
                {
                    Auth.SignOut();
                    Debug.LogWarning($"[FirebaseAuthService] Account {user.Email} has not verified email.");
                    callback?.Invoke(false, "Email is not verified. Please check your inbox/spam before logging in.");
                    return;
                }
            }

            Debug.Log($"<color=green>[FirebaseAuthService] Sign in successful: {user.Email} (UID: {user.UserId})</color>");

            // Lưu thông tin đăng nhập tạm thời để giả lập tự động đăng nhập khi mở lại APK
            SaveAutoLoginCredentials(email, password);

            // Tải dữ liệu người chơi từ Database
            if (FirebaseDataService.Instance != null)
            {
                await FirebaseDataService.Instance.LoadUserProfileAsync(user.UserId);
            }

            callback?.Invoke(true, "Login successful!");
        });
    }

    /// <summary>
    /// Lưu thông tin tài khoản tạm thời để tự động đăng nhập khi khởi động lại game
    /// </summary>
    public static void SaveAutoLoginCredentials(string email, string password)
    {
        try
        {
            PlayerPrefs.SetString("SavedUserEmail", email);
            string encodedPass = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            PlayerPrefs.SetString("SavedUserPass", encodedPass);
            PlayerPrefs.SetInt("AutoLoginEnabled", 1);
            PlayerPrefs.Save();
        }
        catch { }
    }

    /// <summary>
    /// Lấy thông tin tài khoản đã lưu (email, password) để tự động đăng nhập
    /// </summary>
    public static (string email, string password) GetSavedAutoLoginCredentials()
    {
        if (PlayerPrefs.GetInt("AutoLoginEnabled", 0) != 1) return (null, null);
        string email = PlayerPrefs.GetString("SavedUserEmail", "");
        string encodedPass = PlayerPrefs.GetString("SavedUserPass", "");
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(encodedPass)) return (null, null);

        try
        {
            string password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPass));
            return (email, password);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Xóa thông tin đăng nhập tự động (khi người chơi chủ động bấm Đăng xuất)
    /// </summary>
    public static void ClearAutoLoginCredentials()
    {
        PlayerPrefs.SetInt("AutoLoginEnabled", 0);
        PlayerPrefs.DeleteKey("SavedUserPass");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Gửi lại email xác thực cho người dùng nếu chưa nhận được
    /// </summary>
    public void ResendVerificationEmail(string email, string password, Action<bool, string> callback)
    {
        if (!IsInitialized || Auth == null)
        {
            callback?.Invoke(false, "Firebase is not ready.");
            return;
        }

        Auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(async task =>
        {
            if (task.IsFaulted)
            {
                callback?.Invoke(false, GetFriendlyErrorMessage(task.Exception));
                return;
            }

            FirebaseUser user = task.Result.User;
            if (user != null)
            {
                if (user.IsEmailVerified)
                {
                    callback?.Invoke(true, "This account is already verified.");
                    return;
                }

                await user.SendEmailVerificationAsync();
                Auth.SignOut();
                callback?.Invoke(true, "Verification email resent successfully! Please check your inbox or spam folder.");
            }
        });
    }

    // =========================================================================
    //                   ĐĂNG NHẬP KHÁCH (ANONYMOUS / GUEST)
    // =========================================================================

    /// <summary>
    /// Lấy mã định danh tài khoản Khách cố định cho thiết bị này
    /// </summary>
    public static string GetPersistentGuestId()
    {
        string id = PlayerPrefs.GetString("PersistentGuestUID", "");
        if (string.IsNullOrEmpty(id))
        {
            string rawDevice = SystemInfo.deviceUniqueIdentifier;
            string shortDevice = string.IsNullOrEmpty(rawDevice) ? Guid.NewGuid().ToString("N") : rawDevice;
            id = "Guest_" + shortDevice.Substring(0, Math.Min(6, shortDevice.Length));
            PlayerPrefs.SetString("PersistentGuestUID", id);
            PlayerPrefs.Save();
        }
        return id;
    }

    /// <summary>
    /// Chơi ngay dưới danh nghĩa tài khoản Khách (Guest) - cố định theo thiết bị
    /// </summary>
    public async void SignInAnonymously(Action<bool, string> callback)
    {
        string persistentGuestId = GetPersistentGuestId();
        string guestName = persistentGuestId;

        // Nếu offline -> Cho vào chơi Offline cục bộ ngay lập tức
        if (!IsNetworkAvailable)
        {
            if (FirebaseDataService.Instance != null)
            {
                await FirebaseDataService.Instance.LoadOrCreateUserProfileAsync(persistentGuestId, guestName, "");
            }
            callback?.Invoke(true, "Guest login (Offline Mode) successful!");
            return;
        }

        if (!IsInitialized || Auth == null)
        {
            bool ready = await EnsureInitializedAsync();
            if (!ready || Auth == null)
            {
                // Fallback nếu không kết nối được Firebase
                if (FirebaseDataService.Instance != null)
                {
                    await FirebaseDataService.Instance.LoadOrCreateUserProfileAsync(persistentGuestId, guestName, "");
                }
                callback?.Invoke(true, "Guest login (Offline Mode) successful!");
                return;
            }
        }

        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.IsCanceled)
            {
                callback?.Invoke(false, "Guest login was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogWarning($"[FirebaseAuthService] Guest login Firebase error ({task.Exception?.Message}), using Offline Guest.");
            }
            else
            {
                Debug.Log($"<color=green>[FirebaseAuthService] Guest sign-in successful (Persistent ID: {persistentGuestId})</color>");
            }

            // Tải hoặc tạo hồ sơ cho tài khoản Khách dùng ID cố định của thiết bị
            if (FirebaseDataService.Instance != null)
            {
                await FirebaseDataService.Instance.LoadOrCreateUserProfileAsync(persistentGuestId, guestName, "");
            }

            callback?.Invoke(true, "Guest login successful!");
        });
    }

    // =========================================================================
    //                        QUÊN MẬT KHẨU (RESET PASSWORD)
    // =========================================================================

    /// <summary>
    /// Gửi email đặt lại mật khẩu
    /// </summary>
    public async void SendPasswordResetEmail(string email, Action<bool, string> callback)
    {
        if (!IsInitialized || Auth == null)
        {
            bool ready = await EnsureInitializedAsync();
            if (!ready || Auth == null)
            {
                callback?.Invoke(false, "Could not connect to authentication service. Please check your network!");
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            callback?.Invoke(false, "Please enter your Email address.");
            return;
        }

        Auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                callback?.Invoke(false, "Request was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                string errorMsg = GetFriendlyErrorMessage(task.Exception);
                Debug.LogError($"[FirebaseAuthService] Password reset failed: {errorMsg}");
                callback?.Invoke(false, errorMsg);
                return;
            }

            Debug.Log($"<color=green>[FirebaseAuthService] Password reset email sent to: {email}</color>");
            callback?.Invoke(true, "Password reset email sent! Please check your inbox or spam folder.");
        });
    }

    // =========================================================================
    //                        ĐĂNG XUẤT (SIGN OUT)
    // =========================================================================

    /// <summary>
    /// Đăng xuất khỏi phiên hiện tại (tự động lưu dữ liệu an toàn trước khi đăng xuất)
    /// </summary>
    public void SignOut()
    {
        ClearAutoLoginCredentials();

        if (Auth != null && CurrentUser != null)
        {
            // Lưu dữ liệu an toàn và hủy sync trước khi đăng xuất
            if (FirebaseDataService.Instance != null)
            {
                FirebaseDataService.Instance.OnUserLogout();
            }

            Auth.SignOut();
            Debug.Log("<color=yellow>[FirebaseAuthService] User signed out.</color>");
        }
    }

    // =========================================================================
    //                    CẬP NHẬT TÊN & XÓA TÀI KHOẢN
    // =========================================================================

    /// <summary>
    /// Đổi tên hiển thị (Username) của người chơi
    /// </summary>
    public async void UpdateUsername(string newUsername, Action<bool, string> callback)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            callback?.Invoke(false, "Username cannot be empty.");
            return;
        }

        if (CurrentUser == null)
        {
            callback?.Invoke(false, "Not logged in.");
            return;
        }

        try
        {
            UserProfile profile = new UserProfile { DisplayName = newUsername.Trim() };
            await CurrentUser.UpdateUserProfileAsync(profile);

            // Cập nhật lên Database
            if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
            {
                FirebaseDataService.Instance.CurrentUserProfile.username = newUsername.Trim();
                await FirebaseDataService.Instance.SaveUserProfileAsync();
            }

            Debug.Log($"<color=green>[FirebaseAuthService] Username updated: {newUsername}</color>");
            callback?.Invoke(true, "Username updated successfully!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseAuthService] Failed to update username: {ex.Message}");
            callback?.Invoke(false, $"Failed to update username: {ex.Message}");
        }
    }

    /// <summary>
    /// Xóa tài khoản vĩnh viễn (Khách: Xóa save local; Email: Yêu cầu xác thực mật khẩu)
    /// </summary>
    public async void DeleteAccount(string confirmPassword, Action<bool, string> callback)
    {
        bool isGuest = CurrentUser == null || CurrentUser.IsAnonymous;

        // Trường hợp 1: Guest (Cả offline lẫn online anonymous) -> Xóa save JSON cục bộ trực tiếp
        if (isGuest)
        {
            string guestId = (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
                ? FirebaseDataService.Instance.CurrentUserProfile.uid
                : GetPersistentGuestId();

            if (FirebaseDataService.Instance != null)
            {
                await FirebaseDataService.Instance.DeleteUserProfileAsync(guestId);
            }

            if (CurrentUser != null)
            {
                try { await CurrentUser.DeleteAsync(); } catch { }
            }

            Debug.Log("<color=yellow>[FirebaseAuthService] Guest local save deleted.</color>");
            callback?.Invoke(true, "Guest data deleted successfully!");
            return;
        }

        // Trường hợp 2: Tài khoản Email -> Bắt buộc Re-authenticate với mật khẩu vừa nhập
        if (string.IsNullOrEmpty(confirmPassword))
        {
            callback?.Invoke(false, "Please enter your current password to confirm deletion.");
            return;
        }

        string email = CurrentUser.Email;
        Credential credential = EmailAuthProvider.GetCredential(email, confirmPassword);

        CurrentUser.ReauthenticateAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMsg = GetFriendlyErrorMessage(task.Exception);
                Debug.LogError($"[FirebaseAuthService] Re-authentication failed: {errorMsg}");
                callback?.Invoke(false, "Incorrect confirmation password!");
                return;
            }

            ExecuteAccountDeletion(callback);
        });
    }

    private async void ExecuteAccountDeletion(Action<bool, string> callback)
    {
        try
        {
            string uid = CurrentUser.UserId;

            // 1. Xóa dữ liệu trên Realtime Database
            if (FirebaseDataService.Instance != null)
            {
                await FirebaseDataService.Instance.DeleteUserProfileAsync(uid);
            }

            // 2. Xóa tài khoản trên Firebase Authentication
            await CurrentUser.DeleteAsync();

            Debug.Log("<color=yellow>[FirebaseAuthService] Account permanently deleted.</color>");
            callback?.Invoke(true, "Account deleted successfully!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseAuthService] Error deleting account: {ex.Message}");
            callback?.Invoke(false, $"Error deleting account: {ex.Message}");
        }
    }

    // =========================================================================
    //                   CHUYỂN ĐỔI MÃ LỖI THÂN THIỆN
    // =========================================================================

    private string GetFriendlyErrorMessage(AggregateException exception)
    {
        if (exception == null) return "An unknown error occurred.";

        Exception inner = exception.Flatten().InnerException ?? exception;
        string fullMessage = exception.ToString();

        if (inner is FirebaseException fbEx)
        {
            AuthError errorCode = (AuthError)fbEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    return "Invalid email format.";
                case AuthError.WrongPassword:
                    return "Incorrect password.";
                case AuthError.UserNotFound:
                    return "Account with this email does not exist.";
                case AuthError.EmailAlreadyInUse:
                    return "This email is already in use.";
                case AuthError.WeakPassword:
                    return "Password is too weak (minimum 8 characters).";
                case AuthError.MissingEmail:
                    return "Please enter an email.";
                case AuthError.MissingPassword:
                    return "Please enter a password.";
                case AuthError.NetworkRequestFailed:
                    return "Network connection error. Please check your internet.";
                case AuthError.TooManyRequests:
                    return "Too many requests. Please wait a moment.";
                case AuthError.UserDisabled:
                    return "This account has been disabled.";
                case AuthError.OperationNotAllowed:
                    return "Sign-in operation is not allowed.";
            }

            string rawMsg = fbEx.Message;
            if (rawMsg.Contains("An internal error has occurred") || rawMsg.Contains("INVALID_LOGIN_CREDENTIALS") || rawMsg.Contains("INVALID_CREDENTIAL"))
            {
                return "Invalid email or password.";
            }

            return $"Authentication error: {rawMsg}";
        }

        if (fullMessage.Contains("INVALID_LOGIN_CREDENTIALS") || fullMessage.Contains("An internal error has occurred"))
        {
            return "Invalid email or password.";
        }

        return inner != null ? inner.Message : exception.Message;
    }
}
