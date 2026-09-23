using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// Quản lý dữ liệu người chơi trên Firebase Realtime Database:
/// - Đồng bộ 2 chiều giữa Firebase Cloud Database và file JSON cục bộ theo từng UID.
/// - File save riêng biệt: character_save_{uid}.json (Guest = character_save_Guest.json).
/// - Conflict Resolution: So sánh lastSaveTimestamp + HasMeaningfulProgress giữa Local và Cloud.
/// - Guest chỉ lưu Local, không đẩy Cloud.
/// - SemaphoreSlim chống race condition khi push Cloud.
/// - Auto-save khi OnApplicationPause / OnApplicationQuit.
/// </summary>
public class FirebaseDataService : MonoBehaviour
{
    private static FirebaseDataService _instance;
    public static FirebaseDataService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<FirebaseDataService>();
                if (_instance == null)
                {
                    GameObject go = GameObject.Find("[NetworkServices]");
                    if (go == null)
                    {
                        go = new GameObject("[NetworkServices]");
                        DontDestroyOnLoad(go);
                    }
                    _instance = go.AddComponent<FirebaseDataService>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public UserGameProfile CurrentUserProfile { get; private set; } = new UserGameProfile();
    public bool IsDataLoaded { get; private set; } = false;

    [Header("Firebase Regional Configuration")]
    [SerializeField] private string databaseCustomUrl = "https://thief-simulator-6ee8f-default-rtdb.asia-southeast1.firebasedatabase.app";

    private DatabaseReference dbReference;
    private DatabaseReference DBReference
    {
        get
        {
            if (dbReference == null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(databaseCustomUrl))
                    {
                        dbReference = FirebaseDatabase.GetInstance(databaseCustomUrl).RootReference;
                    }
                    else
                    {
                        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirebaseDataService] Đang chờ Firebase Realtime Database sẵn sàng: {ex.Message}");
                }
            }
            return dbReference;
        }
    }

    public static event Action<UserGameProfile> OnUserProfileLoaded;
    public static event Action<UserGameProfile> OnUserProfileUpdated;

    // =========================================================================
    //                        PER-UID SAVE MANAGEMENT
    // =========================================================================

    /// <summary>
    /// UID của người chơi đang hoạt động (dùng để xác định file save và node Cloud)
    /// </summary>
    private string _activeUserId = "";
    public string ActiveUserId => _activeUserId;

    /// <summary>
    /// Đánh dấu Cloud đã sẵn sàng cho user hiện tại (đã sync xong lần đầu)
    /// </summary>
    private bool _isCloudReady = false;

    /// <summary>
    /// Version đồng bộ - tăng mỗi khi đổi user để cancel pending push cũ
    /// </summary>
    private int _syncVersion = 0;

    /// <summary>
    /// SemaphoreSlim chống race condition khi push Cloud
    /// </summary>
    private readonly SemaphoreSlim _cloudPushSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Lấy đường dẫn file save theo UID (character_save_{safeUserId}.json)
    /// </summary>
    public static string GetSaveFilePath(string userId)
    {
        if (IsGuestUser(userId)) return Path.Combine(Application.persistentDataPath, "character_save_Guest.json");

        // Loại bỏ ký tự không hợp lệ trong tên file
        string safeId = userId;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safeId = safeId.Replace(c, '_');
        }
        return Path.Combine(Application.persistentDataPath, $"character_save_{safeId}.json");
    }

    /// <summary>
    /// Đường dẫn file save của user hiện tại
    /// </summary>
    public string CurrentSaveFilePath => GetSaveFilePath(_activeUserId);

    /// <summary>
    /// Đường dẫn file save legacy (cũ) dùng chung cho tất cả
    /// </summary>
    private static string LegacySaveFilePath => Path.Combine(Application.persistentDataPath, "character_save.json");

    /// <summary>
    /// Kiểm tra user có phải Guest không (Guest = không push Cloud)
    /// </summary>
    public static bool IsGuestUser(string userId)
    {
        return string.IsNullOrEmpty(userId) || userId == "Guest" || userId.StartsWith("Guest_");
    }

    public bool IsCurrentUserGuest => IsGuestUser(_activeUserId);

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // =========================================================================
    //                        LIFECYCLE & AUTO-SAVE
    // =========================================================================

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsDataLoaded)
        {
            SaveLocalSnapshot();
        }
    }

    private void OnApplicationQuit()
    {
        if (IsDataLoaded)
        {
            SaveLocalSnapshot();
        }
    }

    // =========================================================================
    //                        TẠO & TẢI HỒ SƠ
    // =========================================================================

    /// <summary>
    /// Khởi tạo hồ sơ mới cho người chơi trên Firebase
    /// </summary>
    public async Task<bool> CreateNewUserProfileAsync(string uid, string username, string email)
    {
        try
        {
            UserGameProfile newProfile = new UserGameProfile(uid, username, email);

            // Tài khoản mới tạo -> Bắt đầu với hồ sơ mặc định sạch (1 nhân vật mặc định char_01, cash = 0)

            string json = JsonUtility.ToJson(newProfile);

            // Guest không push Cloud
            if (!IsGuestUser(uid))
            {
                var refTarget = DBReference;
                if (refTarget != null)
                {
                    await refTarget.Child("users").Child(uid).SetRawJsonValueAsync(json);
                }
                else
                {
                    Debug.LogWarning("[FirebaseDataService] Không thể kết nối Database để lưu hồ sơ mới.");
                }
            }

            _activeUserId = uid;
            CurrentUserProfile = newProfile;
            IsDataLoaded = true;
            _isCloudReady = !IsGuestUser(uid);

            SyncProfileToLocalJson(CurrentUserProfile);
            SyncProfileToGameSession(CurrentUserProfile);
            OnUserProfileLoaded?.Invoke(CurrentUserProfile);
            _ = RegisterAndListenSessionAsync(uid);

            Debug.Log($"<color=green>[FirebaseDataService] Đã tạo hồ sơ người chơi mới: {newProfile.username} (UID: {uid})</color>");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseDataService] Lỗi khi tạo hồ sơ người chơi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Nạp dữ liệu người chơi từ Firebase với Conflict Resolution
    /// </summary>
    public async Task<bool> LoadUserProfileAsync(string uid)
    {
        try
        {
            _activeUserId = uid;
            _syncVersion++;
            int currentSyncVersion = _syncVersion;

            // 1. Tải Local Snapshot ngay lập tức (game không bị gián đoạn UI)
            UserGameProfile localProfile = LoadLocalSnapshot(uid);

            // Guest: chỉ dùng Local, không cần Cloud
            if (IsGuestUser(uid))
            {
                CurrentUserProfile = localProfile ?? new UserGameProfile(uid, "Guest", "");
                IsDataLoaded = true;
                _isCloudReady = false;
                SyncProfileToGameSession(CurrentUserProfile);
                OnUserProfileLoaded?.Invoke(CurrentUserProfile);
                Debug.Log($"<color=cyan>[FirebaseDataService] Guest mode: tải dữ liệu cục bộ.</color>");
                return true;
            }

            // 2. Tải Cloud Snapshot
            var refTarget = DBReference;
            if (refTarget == null)
            {
                Debug.LogWarning("[FirebaseDataService] DatabaseReference chưa sẵn sàng, dùng dữ liệu cục bộ.");
                CurrentUserProfile = localProfile ?? new UserGameProfile(uid, "Thief", "");
                IsDataLoaded = true;
                _isCloudReady = false;
                SyncProfileToGameSession(CurrentUserProfile);
                OnUserProfileLoaded?.Invoke(CurrentUserProfile);
                return false;
            }

            var snapshot = await refTarget.Child("users").Child(uid).GetValueAsync();

            // Kiểm tra xem user có bị đổi giữa chừng không
            if (currentSyncVersion != _syncVersion)
            {
                Debug.LogWarning("[FirebaseDataService] Sync bị hủy do đổi user giữa chừng.");
                return false;
            }

            if (snapshot != null && snapshot.Exists)
            {
                string cloudJson = snapshot.GetRawJsonValue();
                UserGameProfile cloudProfile = JsonUtility.FromJson<UserGameProfile>(cloudJson);
                if (cloudProfile == null) cloudProfile = new UserGameProfile(uid, "Thief", "");

                // 3. Conflict Resolution
                if (localProfile != null)
                {
                    bool useCloud = ShouldUseCloudData(localProfile, cloudProfile);
                    if (useCloud)
                    {
                        Debug.Log("<color=cyan>[FirebaseDataService] Cloud data thắng → dùng dữ liệu từ Cloud.</color>");
                        CurrentUserProfile = cloudProfile;
                        SyncProfileToLocalJson(cloudProfile);
                    }
                    else
                    {
                        Debug.Log("<color=cyan>[FirebaseDataService] Local data thắng → đẩy Local lên Cloud.</color>");
                        CurrentUserProfile = localProfile;
                        CurrentUserProfile.uid = uid; // Đảm bảo UID đúng
                        await PushCloudSaveAsync(CurrentUserProfile, currentSyncVersion);
                    }
                }
                else
                {
                    // Local không có → dùng Cloud
                    CurrentUserProfile = cloudProfile;
                    SyncProfileToLocalJson(cloudProfile);
                }
            }
            else
            {
                // Cloud không có → Tạo mới hoặc dùng Local
                if (localProfile != null && localProfile.HasMeaningfulProgress())
                {
                    Debug.Log("<color=cyan>[FirebaseDataService] Cloud trống, Local có tiến độ → đẩy Local lên Cloud.</color>");
                    CurrentUserProfile = localProfile;
                    CurrentUserProfile.uid = uid;
                    await PushCloudSaveAsync(CurrentUserProfile, currentSyncVersion);
                }
                else
                {
                    return await CreateNewUserProfileAsync(uid, "Thief", "");
                }
            }

            IsDataLoaded = true;
            _isCloudReady = true;

            SyncProfileToGameSession(CurrentUserProfile);
            OnUserProfileLoaded?.Invoke(CurrentUserProfile);
            _ = RegisterAndListenSessionAsync(uid);
            Debug.Log($"<color=green>[FirebaseDataService] Đã tải hồ sơ: {CurrentUserProfile.username} (Tiền: ${CurrentUserProfile.cash})</color>");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseDataService] Lỗi khi tải hồ sơ người chơi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tải hoặc tạo mới hồ sơ người chơi
    /// </summary>
    public async Task<bool> LoadOrCreateUserProfileAsync(string uid, string username, string email)
    {
        try
        {
            _activeUserId = uid;

            // Guest: tải Local hoặc tạo mới
            if (IsGuestUser(uid))
            {
                UserGameProfile localProfile = LoadLocalSnapshot(uid);
                if (localProfile != null)
                {
                    CurrentUserProfile = localProfile;
                    IsDataLoaded = true;
                    _isCloudReady = false;
                    SyncProfileToGameSession(CurrentUserProfile);
                    OnUserProfileLoaded?.Invoke(CurrentUserProfile);
                    return true;
                }
                return await CreateNewUserProfileAsync(uid, username, email);
            }

            var refTarget = DBReference;
            if (refTarget == null)
            {
                return await CreateNewUserProfileAsync(uid, username, email);
            }

            var snapshot = await refTarget.Child("users").Child(uid).GetValueAsync();
            if (snapshot != null && snapshot.Exists)
            {
                return await LoadUserProfileAsync(uid);
            }
            else
            {
                return await CreateNewUserProfileAsync(uid, username, email);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseDataService] Lỗi: {ex.Message}");
            return false;
        }
    }

    // =========================================================================
    //                        LƯU DỮ LIỆU (SAVE)
    // =========================================================================

    /// <summary>
    /// Lưu toàn bộ hồ sơ hiện tại (Local + Cloud nếu đã đăng nhập)
    /// </summary>
    public async Task<bool> SaveUserProfileAsync()
    {
        if (CurrentUserProfile == null || string.IsNullOrEmpty(CurrentUserProfile.uid)) return false;

        try
        {
            CurrentUserProfile.lastLoginTime = DateTime.UtcNow.ToString("o");
            CurrentUserProfile.lastSaveTimestamp = UserGameProfile.GetCurrentTimestampMs();

            // 1. Luôn ghi Local trước
            SyncProfileToLocalJson(CurrentUserProfile);
            SyncProfileToGameSession(CurrentUserProfile);

            // 2. Push Cloud nếu không phải Guest và Cloud sẵn sàng
            if (!IsCurrentUserGuest && _isCloudReady)
            {
                await PushCloudSaveAsync(CurrentUserProfile, _syncVersion);
            }

            OnUserProfileUpdated?.Invoke(CurrentUserProfile);
            Debug.Log("<color=cyan>[FirebaseDataService] Đã lưu hồ sơ thành công.</color>");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseDataService] Lỗi khi lưu hồ sơ: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lưu nhanh snapshot xuống Local (dùng cho OnApplicationPause/Quit)
    /// </summary>
    public void SaveLocalSnapshot()
    {
        if (CurrentUserProfile == null) return;
        CurrentUserProfile.lastSaveTimestamp = UserGameProfile.GetCurrentTimestampMs();
        SyncProfileToLocalJson(CurrentUserProfile);
    }

    /// <summary>
    /// Xóa dữ liệu hồ sơ người chơi trên Firebase Realtime Database
    /// </summary>
    public async Task<bool> DeleteUserProfileAsync(string uid)
    {
        try
        {
            // Xóa trên Cloud
            if (!IsGuestUser(uid))
            {
                var refTarget = DBReference;
                if (refTarget != null && !string.IsNullOrEmpty(uid))
                {
                    await refTarget.Child("users").Child(uid).RemoveValueAsync();
                    Debug.Log($"<color=yellow>[FirebaseDataService] Đã xóa dữ liệu hồ sơ của UID: {uid} trên Database.</color>");
                }
            }

            // Xóa file save JSON cục bộ theo UID
            string path = GetSaveFilePath(uid);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"<color=yellow>[FirebaseDataService] Đã xóa file save cục bộ: {path}</color>");
            }

            CurrentUserProfile = new UserGameProfile();
            IsDataLoaded = false;
            _isCloudReady = false;
            _activeUserId = "";
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseDataService] Lỗi khi xóa hồ sơ người chơi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Thực hiện đồng bộ thủ công 2 chiều giữa máy và Firebase Cloud (cho nút Đồng bộ)
    /// </summary>
    public async Task<(bool success, string message)> ManualSyncAsync()
    {
        if (CurrentUserProfile == null || string.IsNullOrEmpty(CurrentUserProfile.uid))
        {
            return (false, "Chưa đăng nhập tài khoản để đồng bộ.");
        }

        if (IsCurrentUserGuest)
        {
            return (false, "Tài khoản Khách chỉ lưu cục bộ, không thể đồng bộ Cloud.");
        }

        try
        {
            bool saved = await SaveUserProfileAsync();
            if (saved)
            {
                string timeStr = DateTime.Now.ToString("HH:mm:ss");
                return (true, $"Đồng bộ thành công lúc {timeStr}!");
            }
            return (false, "Đồng bộ thất bại. Vui lòng kiểm tra kết nối mạng.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi đồng bộ: {ex.Message}");
        }
    }

    // =========================================================================
    //               SINGLE ACTIVE SESSION (CONCURRENT LOGIN DETECTION)
    // =========================================================================

    private string _currentSessionToken = "";
    private DatabaseReference _sessionRef;
    private EventHandler<ValueChangedEventArgs> _sessionListener;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _mainThreadQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();

    public static event Action OnLoggedOutFromAnotherDevice;

    private void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseDataService] Error executing main thread action: {ex.Message}");
            }
        }
    }

    public void RunOnMainThread(Action action)
    {
        if (action == null) return;
        _mainThreadQueue.Enqueue(action);
    }

    /// <summary>
    /// Đăng ký sessionToken lên Firebase và lắng nghe sự thay đổi để phát hiện đăng nhập trên thiết bị khác
    /// </summary>
    public async Task RegisterAndListenSessionAsync(string uid)
    {
        if (IsGuestUser(uid)) return;

        StopListeningToSession();

        string newSessionToken = Guid.NewGuid().ToString();
        _currentSessionToken = newSessionToken;
        PlayerPrefs.SetString("current_session_token", newSessionToken);
        PlayerPrefs.Save();

        var refTarget = DBReference;
        if (refTarget == null) return;

        _sessionRef = refTarget.Child("users").Child(uid).Child("current_session");

        try
        {
            await _sessionRef.SetValueAsync(newSessionToken);
            Debug.Log($"<color=cyan>[FirebaseDataService] New session token registered: {newSessionToken}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseDataService] Failed to set session token: {ex.Message}");
        }

        _sessionListener = (sender, args) =>
        {
            if (args.DatabaseError != null) return;
            if (args.Snapshot == null || !args.Snapshot.Exists) return;

            string incomingToken = args.Snapshot.Value?.ToString();
            if (!string.IsNullOrEmpty(incomingToken) && !string.IsNullOrEmpty(_currentSessionToken) && incomingToken != _currentSessionToken)
            {
                Debug.LogWarning("<color=red>[FirebaseDataService] Account logged in on another device! Logging out...</color>");
                
                RunOnMainThread(() =>
                {
                    StopListeningToSession();
                    OnUserLogout();
                    if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.Auth != null)
                    {
                        FirebaseAuthService.Instance.Auth.SignOut();
                    }
                    OnLoggedOutFromAnotherDevice?.Invoke();
                });
            }
        };

        _sessionRef.ValueChanged += _sessionListener;
    }

    /// <summary>
    /// Hủy lắng nghe phiên đăng nhập
    /// </summary>
    public void StopListeningToSession()
    {
        if (_sessionRef != null && _sessionListener != null)
        {
            try
            {
                _sessionRef.ValueChanged -= _sessionListener;
            }
            catch { }
            _sessionRef = null;
            _sessionListener = null;
        }
        _currentSessionToken = "";
    }

    /// <summary>
    /// Hủy đồng bộ khi đổi tài khoản (tăng syncVersion để cancel pending push)
    /// </summary>
    public void CancelAccountSync()
    {
        StopListeningToSession();
        _syncVersion++;
        _isCloudReady = false;
        Debug.Log("<color=yellow>[FirebaseDataService] Đã hủy đồng bộ tài khoản cũ.</color>");
    }

    /// <summary>
    /// Đăng xuất: lưu dữ liệu an toàn và reset state
    /// </summary>
    public void OnUserLogout()
    {
        StopListeningToSession();
        if (IsDataLoaded && CurrentUserProfile != null)
        {
            SaveLocalSnapshot();
        }
        CancelAccountSync();
        CurrentUserProfile = new UserGameProfile();
        IsDataLoaded = false;
        _activeUserId = "";
    }

    // =========================================================================
    //                        CÁC TIỆN ÍCH GAMEPLAY
    // =========================================================================

    /// <summary>
    /// Cộng thêm Tiền mặt (Cash) và tự động đồng bộ lên Cloud
    /// </summary>
    public async void AddCash(int amount)
    {
        if (CurrentUserProfile == null) return;
        CurrentUserProfile.cash += amount;
        await SaveUserProfileAsync();
    }

    /// <summary>
    /// Trừ tiền mặt nếu đủ số dư
    /// </summary>
    public async Task<bool> TrySpendCash(int amount)
    {
        if (CurrentUserProfile == null || CurrentUserProfile.cash < amount) return false;
        CurrentUserProfile.cash -= amount;
        await SaveUserProfileAsync();
        return true;
    }

    // Tiện ích tương thích ngược
    public void AddGold(int amount) => AddCash(amount);
    public Task<bool> TrySpendGold(int amount) => TrySpendCash(amount);

    /// <summary>
    /// Mở khóa nhân vật và đồng bộ lên Cloud
    /// </summary>
    public async void UnlockCharacter(string characterId)
    {
        if (CurrentUserProfile == null) return;
        if (CurrentUserProfile.unlockedCharacterIds == null) CurrentUserProfile.unlockedCharacterIds = new System.Collections.Generic.List<string>();

        if (!CurrentUserProfile.unlockedCharacterIds.Contains(characterId))
        {
            CurrentUserProfile.unlockedCharacterIds.Add(characterId);
            await SaveUserProfileAsync();
        }
    }

    /// <summary>
    /// Đổi nhân vật và giới tính đang chọn
    /// </summary>
    public async void SetSelectedCharacter(string characterId, int index, bool isMale)
    {
        if (CurrentUserProfile == null) return;

        CurrentUserProfile.selectedCharacterId = characterId;
        CurrentUserProfile.selectedCharacterIndex = index;
        CurrentUserProfile.isMale = isMale;

        await SaveUserProfileAsync();
    }

    // =========================================================================
    //                    CONFLICT RESOLUTION (Local vs Cloud)
    // =========================================================================

    /// <summary>
    /// Thuật toán giải quyết xung đột: trả về true nếu nên dùng Cloud data
    /// </summary>
    private bool ShouldUseCloudData(UserGameProfile localData, UserGameProfile cloudData)
    {
        if (localData == null && cloudData == null) return true;
        if (localData == null) return true;
        if (cloudData == null) return false;

        bool localHasProgress = localData.HasMeaningfulProgress();
        bool cloudHasProgress = cloudData.HasMeaningfulProgress();

        // 1. Nếu 1 bên có tiến độ, bên kia trắng → bên có tiến độ thắng
        if (cloudHasProgress && !localHasProgress) return true;
        if (localHasProgress && !cloudHasProgress) return false;

        // 2. Cả 2 đều trắng → ưu tiên Cloud
        if (!localHasProgress && !cloudHasProgress) return true;

        // 3. Cả 2 đều có tiến độ → so sánh timestamp
        if (cloudData.lastSaveTimestamp > localData.lastSaveTimestamp) return true;
        if (localData.lastSaveTimestamp > cloudData.lastSaveTimestamp) return false;

        // 4. Timestamp bằng nhau → ưu tiên Cloud
        return true;
    }

    // =========================================================================
    //                    CLOUD PUSH (với SemaphoreSlim)
    // =========================================================================

    /// <summary>
    /// Đẩy dữ liệu lên Cloud an toàn (chống race condition)
    /// </summary>
    private async Task PushCloudSaveAsync(UserGameProfile profile, int expectedSyncVersion)
    {
        if (profile == null || string.IsNullOrEmpty(profile.uid)) return;
        if (IsGuestUser(profile.uid)) return;

        await _cloudPushSemaphore.WaitAsync();
        try
        {
            // Kiểm tra user có bị đổi giữa chừng không
            if (expectedSyncVersion != _syncVersion)
            {
                Debug.LogWarning("[FirebaseDataService] Cloud push bị hủy: user đã đổi.");
                return;
            }

            var refTarget = DBReference;
            if (refTarget == null)
            {
                Debug.LogWarning("[FirebaseDataService] Không thể kết nối Database để push Cloud.");
                return;
            }

            string json = JsonUtility.ToJson(profile);
            await refTarget.Child("users").Child(profile.uid).SetRawJsonValueAsync(json);
            Debug.Log("<color=cyan>[FirebaseDataService] Đã đẩy dữ liệu lên Cloud thành công.</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseDataService] Lỗi khi push Cloud: {ex.Message}");
        }
        finally
        {
            _cloudPushSemaphore.Release();
        }
    }

    // =========================================================================
    //                    ĐỒNG BỘ NỘI BỘ (LOCAL JSON & GAMESESSION)
    // =========================================================================

    /// <summary>
    /// Ghi profile xuống file JSON cục bộ theo UID
    /// </summary>
    private void SyncProfileToLocalJson(UserGameProfile profile)
    {
        if (profile == null) return;

        try
        {
            string savePath = GetSaveFilePath(profile.uid);
            string json = JsonUtility.ToJson(profile, true);
            File.WriteAllText(savePath, json);

            // Cũng ghi CharacterSaveData cho tương thích với CharacterSelectionHUD
            CharacterSaveData localSave = new CharacterSaveData
            {
                selectedCharacterId = profile.selectedCharacterId,
                selectedCharacterIndex = profile.selectedCharacterIndex,
                isMale = profile.isMale,
                unlockedCharacterIds = profile.unlockedCharacterIds ?? new System.Collections.Generic.List<string>() { "char_01" }
            };
            string charJson = JsonUtility.ToJson(localSave, true);
            // Ghi file character save riêng để CharacterSelectionHUD đọc được
            string charSavePath = Path.Combine(Application.persistentDataPath, $"character_save_{GetSafeUserId(profile.uid)}.json");
            File.WriteAllText(charSavePath, charJson);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseDataService] Lỗi ghi JSON cục bộ: {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc Local Snapshot của user theo UID
    /// </summary>
    private UserGameProfile LoadLocalSnapshot(string uid)
    {
        try
        {
            string savePath = GetSaveFilePath(uid);



            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                UserGameProfile profile = JsonUtility.FromJson<UserGameProfile>(json);
                return profile;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseDataService] Lỗi đọc Local Snapshot: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Nạp dữ liệu từ file save cục bộ vào profile (dùng khi tạo profile mới)
    /// </summary>
    private void LoadLocalSnapshotIntoProfile(UserGameProfile profile, string uid)
    {
        if (profile == null) return;

        try
        {
            string savePath = GetSaveFilePath(uid);



            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);

                // Thử đọc dưới dạng UserGameProfile trước
                UserGameProfile savedProfile = JsonUtility.FromJson<UserGameProfile>(json);
                if (savedProfile != null)
                {
                    profile.selectedCharacterId = savedProfile.selectedCharacterId;
                    profile.selectedCharacterIndex = savedProfile.selectedCharacterIndex;
                    profile.isMale = savedProfile.isMale;
                    if (savedProfile.unlockedCharacterIds != null && savedProfile.unlockedCharacterIds.Count > 0)
                    {
                        profile.unlockedCharacterIds = savedProfile.unlockedCharacterIds;
                    }
                    if (savedProfile.cash > 0) profile.cash = savedProfile.cash;
                    if (savedProfile.highestUnlockedChapter > 1) profile.highestUnlockedChapter = savedProfile.highestUnlockedChapter;
                    return;
                }

                // Fallback: đọc dưới dạng CharacterSaveData (file character_save.json cũ)
                CharacterSaveData localData = JsonUtility.FromJson<CharacterSaveData>(json);
                if (localData != null)
                {
                    profile.selectedCharacterId = localData.selectedCharacterId;
                    profile.selectedCharacterIndex = localData.selectedCharacterIndex;
                    profile.isMale = localData.isMale;
                    if (localData.unlockedCharacterIds != null && localData.unlockedCharacterIds.Count > 0)
                    {
                        profile.unlockedCharacterIds = localData.unlockedCharacterIds;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FirebaseDataService] Lỗi đọc JSON cục bộ: {ex.Message}");
        }
    }

    private void SyncProfileToGameSession(UserGameProfile profile)
    {
        if (profile == null) return;

        GameSession.IsMale = profile.isMale;
        // Cập nhật PlayerPrefs
        PlayerPrefs.SetInt("SelectedCharIndex", profile.selectedCharacterIndex);
        PlayerPrefs.SetInt("SelectedGender", profile.isMale ? 0 : 1);
        PlayerPrefs.SetString("PlayerNickname", profile.username);
        PlayerPrefs.SetInt("PlayerCash", profile.cash);
        PlayerPrefs.SetInt("PlayerGold", profile.cash);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lấy userId an toàn cho tên file
    /// </summary>
    private static string GetSafeUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return "Guest";
        string safe = userId;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(c, '_');
        }
        return safe;
    }
}
