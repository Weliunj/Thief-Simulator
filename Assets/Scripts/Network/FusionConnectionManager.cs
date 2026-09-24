using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý kết nối mạng Photon Fusion (Singleton):
/// - Quản lý vòng đời NetworkRunner (Khởi tạo, Kết nối Lobby, Tạo phòng/Session, Tham gia phòng, Rời phòng).
/// - Triển khai INetworkRunnerCallbacks để lắng nghe sự kiện mạng (Player vào/ra, Danh sách phòng cập nhật, Mất kết nối).
/// - Cung cấp Event để UI (NetworkLobbyHUD) dễ dàng đăng ký và cập nhật giao diện thời gian thực.
/// </summary>
public class FusionConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionConnectionManager Instance { get; private set; }

    [Header("⚙️ Network Runner Settings")]
    [Tooltip("Prefab NetworkRunner tùy chỉnh (nếu để trống sẽ tự động tạo runtime)")]
    public NetworkRunner runnerPrefab;

    [Tooltip("Chế độ chơi mạng mặc định (Shared Mode phù hợp cho Co-op di động P2P mượt mà)")]
    public GameMode defaultGameMode = GameMode.Shared;

    [Tooltip("Số lượng người chơi tối đa trong 1 phòng")]
    public int maxPlayerCount = 4;

    [Header("🎯 Trạng thái")]
    public NetworkRunner currentRunner;
    public string currentSessionName = "";
    public bool isConnecting = false;

    // Events cho UI đăng ký lắng nghe
    public static event Action<List<SessionInfo>> OnSessionListUpdatedEvent;
    public static event Action<NetworkRunner> OnConnectedToServerEvent;
    public static event Action<NetworkRunner, ShutdownReason> OnShutdownEvent;
    public static event Action<NetworkRunner, PlayerRef> OnPlayerJoinedEvent;
    public static event Action<NetworkRunner, PlayerRef> OnPlayerLeftEvent;
    public static event Action<string, bool> OnStatusMessageEvent; // (message, isError)
    public static event Action<int, bool> OnPlayerReadyStatusReceived; // (playerId, isReady)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Lấy hoặc khởi tạo NetworkRunner
    /// </summary>
    public NetworkRunner GetOrCreateRunner()
    {
        if (currentRunner == null)
        {
            if (runnerPrefab != null)
            {
                currentRunner = Instantiate(runnerPrefab);
            }
            else
            {
                GameObject runnerObj = new GameObject("FusionNetworkRunner");
                currentRunner = runnerObj.AddComponent<NetworkRunner>();
            }

            // Đảm bảo có NetworkSceneManagerDefault và NetworkObjectProviderDefault
            if (currentRunner.GetComponent<INetworkSceneManager>() == null)
            {
                currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
            if (currentRunner.GetComponent<INetworkObjectProvider>() == null)
            {
                currentRunner.gameObject.AddComponent<NetworkObjectProviderDefault>();
            }

            DontDestroyOnLoad(currentRunner.gameObject);
            currentRunner.AddCallbacks(this);
        }

        return currentRunner;
    }

    /// <summary>
    /// Kết nối vào Session Lobby để nhận danh sách phòng trực tuyến
    /// </summary>
    public async Task<bool> JoinLobby()
    {
        if (isConnecting) return false;
        isConnecting = true;
        OnStatusMessageEvent?.Invoke("Connecting to Photon Cloud Lobby...", false);

        NetworkRunner runner = GetOrCreateRunner();

        try
        {
            var result = await runner.JoinSessionLobby(SessionLobby.Shared);
            isConnecting = false;

            if (result.Ok)
            {
                OnStatusMessageEvent?.Invoke("Connected to Lobby successfully!", false);
                Debug.Log("<color=green>[FusionConnectionManager] Đã kết nối vào Shared Lobby thành công!</color>");
                return true;
            }
            else
            {
                OnStatusMessageEvent?.Invoke($"Failed to join lobby: {result.ShutdownReason}", true);
                Debug.LogError($"[FusionConnectionManager] Lỗi kết nối Lobby: {result.ShutdownReason}");
                return false;
            }
        }
        catch (Exception ex)
        {
            isConnecting = false;
            OnStatusMessageEvent?.Invoke($"Connection error: {ex.Message}", true);
            Debug.LogError($"[FusionConnectionManager] Exception kết nối Lobby: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Tạo một phòng chơi mới (Host/Shared Session)
    /// </summary>
    public async Task<bool> CreateSession(string sessionName, int maxPlayers = 4, bool isPrivate = false, string mapName = "Chapter1")
    {
        if (string.IsNullOrEmpty(sessionName))
        {
            sessionName = "Room_" + UnityEngine.Random.Range(1000, 9999);
        }

        if (isConnecting) return false;
        isConnecting = true;
        OnStatusMessageEvent?.Invoke($"Creating room '{sessionName}'...", false);

        NetworkRunner runner = GetOrCreateRunner();

        var sceneInfo = new NetworkSceneInfo();
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.IsValid())
        {
            sceneInfo.AddSceneRef(SceneRef.FromIndex(currentScene.buildIndex), LoadSceneMode.Single);
        }

        var customProps = new Dictionary<string, SessionProperty>();
        if (!string.IsNullOrEmpty(mapName))
        {
            customProps["map"] = mapName;
        }

        try
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = defaultGameMode,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                IsVisible = !isPrivate,
                IsOpen = true,
                Scene = sceneInfo,
                SceneManager = runner.GetComponent<INetworkSceneManager>(),
                ObjectProvider = runner.GetComponent<INetworkObjectProvider>(),
                SessionProperties = customProps
            });

            isConnecting = false;

            if (result.Ok)
            {
                currentSessionName = sessionName;
                OnStatusMessageEvent?.Invoke($"Room '{sessionName}' created!", false);
                Debug.Log($"<color=green>[FusionConnectionManager] Đã tạo phòng '{sessionName}' thành công!</color>");
                return true;
            }
            else
            {
                OnStatusMessageEvent?.Invoke($"Create room failed: {result.ShutdownReason}", true);
                Debug.LogError($"[FusionConnectionManager] Lỗi tạo phòng: {result.ShutdownReason}");
                return false;
            }
        }
        catch (Exception ex)
        {
            isConnecting = false;
            OnStatusMessageEvent?.Invoke($"Error creating room: {ex.Message}", true);
            Debug.LogError($"[FusionConnectionManager] Exception tạo phòng: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Tham gia vào phòng chơi theo tên hoặc mã phòng
    /// </summary>
    public async Task<bool> JoinSession(string sessionName)
    {
        if (string.IsNullOrEmpty(sessionName))
        {
            OnStatusMessageEvent?.Invoke("Room name/code cannot be empty.", true);
            return false;
        }

        if (isConnecting) return false;
        isConnecting = true;
        OnStatusMessageEvent?.Invoke($"Joining room '{sessionName}'...", false);

        NetworkRunner runner = GetOrCreateRunner();

        try
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = defaultGameMode,
                SessionName = sessionName,
                SceneManager = runner.GetComponent<INetworkSceneManager>(),
                ObjectProvider = runner.GetComponent<INetworkObjectProvider>()
            });

            isConnecting = false;

            if (result.Ok)
            {
                currentSessionName = sessionName;
                OnStatusMessageEvent?.Invoke($"Joined room '{sessionName}'!", false);
                Debug.Log($"<color=green>[FusionConnectionManager] Đã tham gia phòng '{sessionName}' thành công!</color>");
                return true;
            }
            else
            {
                OnStatusMessageEvent?.Invoke($"Join failed: {result.ShutdownReason}", true);
                Debug.LogError($"[FusionConnectionManager] Lỗi tham gia phòng: {result.ShutdownReason}");
                return false;
            }
        }
        catch (Exception ex)
        {
            isConnecting = false;
            OnStatusMessageEvent?.Invoke($"Error joining room: {ex.Message}", true);
            Debug.LogError($"[FusionConnectionManager] Exception tham gia phòng: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Tham gia ngẫu nhiên một phòng đang mở bất kỳ
    /// </summary>
    public async Task<bool> JoinRandomSession()
    {
        if (isConnecting) return false;
        isConnecting = true;
        OnStatusMessageEvent?.Invoke("Finding a quick match...", false);

        NetworkRunner runner = GetOrCreateRunner();

        try
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = defaultGameMode,
                SceneManager = runner.GetComponent<INetworkSceneManager>(),
                ObjectProvider = runner.GetComponent<INetworkObjectProvider>()
            });

            isConnecting = false;

            if (result.Ok)
            {
                currentSessionName = runner.SessionInfo.Name;
                OnStatusMessageEvent?.Invoke($"Connected to '{currentSessionName}'!", false);
                return true;
            }
            else
            {
                OnStatusMessageEvent?.Invoke($"Quick match failed: {result.ShutdownReason}", true);
                return false;
            }
        }
        catch (Exception ex)
        {
            isConnecting = false;
            OnStatusMessageEvent?.Invoke($"Quick match error: {ex.Message}", true);
            return false;
        }
    }

    /// <summary>
    /// Rời khỏi phòng hiện tại và ngắt kết nối an toàn
    /// </summary>
    public async Task LeaveSession()
    {
        if (currentRunner != null && currentRunner.IsRunning)
        {
            OnStatusMessageEvent?.Invoke("Leaving room...", false);
            await currentRunner.Shutdown();
            currentSessionName = "";
            Debug.Log("[FusionConnectionManager] Đã rời phòng.");
        }
    }

    /// <summary>
    /// Chuyển Scene Gameplay cho toàn bộ người chơi trong phòng (Host/Master Client)
    /// </summary>
    public void LoadGameplayScene(string sceneName)
    {
        if (currentRunner != null && currentRunner.IsRunning)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (sceneIndex < 0)
            {
                // Fallback nếu sceneName là tên thuần không có đường dẫn
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    if (path.Contains(sceneName))
                    {
                        sceneIndex = i;
                        break;
                    }
                }
            }

            if (sceneIndex >= 0)
            {
                currentRunner.LoadScene(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError($"[FusionConnectionManager] Không tìm thấy Scene '{sceneName}' trong Build Settings!");
            }
        }
    }

    // =========================================================================
    //                     INETWORKRUNNERCALLBACKS IMPLEMENTATION
    // =========================================================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"<color=cyan>[FusionConnectionManager] Player {player} đã tham gia Session.</color>");
        OnPlayerJoinedEvent?.Invoke(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"<color=yellow>[FusionConnectionManager] Player {player} đã rời Session.</color>");
        OnPlayerLeftEvent?.Invoke(runner, player);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[FusionConnectionManager] Danh sách Session cập nhật: {sessionList.Count} phòng.");
        OnSessionListUpdatedEvent?.Invoke(sessionList);
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("<color=green>[FusionConnectionManager] Đã kết nối thành công tới Server!</color>");
        OnConnectedToServerEvent?.Invoke(runner);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[FusionConnectionManager] Mất kết nối tới Server: {reason}");
        OnStatusMessageEvent?.Invoke($"Disconnected: {reason}", true);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[FusionConnectionManager] NetworkRunner Shutdown: {shutdownReason}");
        OnShutdownEvent?.Invoke(runner, shutdownReason);
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        OnStatusMessageEvent?.Invoke($"Connect failed: {reason}", true);
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (data.Count >= 3 && data.Array != null)
        {
            int offset = data.Offset;
            byte header = data.Array[offset];
            if (header == (byte)'R')
            {
                int pId = data.Array[offset + 1];
                bool isReady = data.Array[offset + 2] == 1;
                OnPlayerReadyStatusReceived?.Invoke(pId, isReady);
            }
        }
    }

    /// <summary>
    /// Phát trạng thái Sẵn sàng (Ready) của người chơi hiện tại tới tất cả thành viên trong phòng
    /// </summary>
    public void SendReadyStatus(bool isReady)
    {
        if (currentRunner == null || !currentRunner.IsRunning) return;
        byte[] payload = new byte[] { (byte)'R', (byte)currentRunner.LocalPlayer.PlayerId, (byte)(isReady ? 1 : 0) };
        foreach (var p in currentRunner.ActivePlayers)
        {
            if (p != currentRunner.LocalPlayer)
            {
                currentRunner.SendReliableDataToPlayer(p, default, payload);
            }
        }
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
