using System.Collections.Generic;
using Fusion;
using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Component quản lý vị trí và sinh nhân vật (Player Spawner) trong Scene:
/// - Quản lý danh sách các điểm xuất phát (spawnPoints Transform) trong Scene.
/// - Tự động lấy Prefab nhân vật từ GameSession.SelectedPlayer (hoặc defaultPlayerData / fallbackPrefab).
/// - Tự động Instantiate nhân vật tại điểm Spawn khi màn chơi khởi động.
/// - Tự động kết nối Cinemachine Virtual Camera và UI_Manager vào Player vừa sinh.
/// - Hỗ trợ Gizmos trực quan và Context Menu tiện lợi cho Level Design.
/// </summary>
public class ScenePlayerSpawner : MonoBehaviour
{
    [Header("👤 Player Configuration Reference")]
    [Tooltip("Danh sách toàn bộ nhân vật (PlayerSO) để nạp từ PlayerPrefs")]
    public List<PlayerSO> characterDatabase = new List<PlayerSO>();

    [Tooltip("Dữ liệu nhân vật mặc định nếu GameSession.SelectedPlayer là null và database trống")]
    public PlayerSO defaultPlayerData;

    [Tooltip("Prefab nhân vật dự phòng nếu PlayerSO chưa được gán characterPrefab")]
    public GameObject fallbackPlayerPrefab;

    [Header("📍 Scene Spawn Points")]
    [Tooltip("Danh sách các Transform điểm xuất phát đặt trong Scene")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("⚙️ Spawning Settings")]
    [Tooltip("Tự động spawn nhân vật ngay khi Scene khởi động (Start)")]
    public bool spawnOnStart = true;

    [Tooltip("Không spawn nếu trong Scene đã có sẵn GameObject mang Tag 'Player'")]
    public bool preventDuplicateIfPlayerExists = true;

    [Header("📦 Spawned Instance")]
    [SerializeField] private GameObject spawnedPlayerInstance;

    public static ScenePlayerSpawner Instance { get; private set; }

    public GameObject SpawnedPlayer => spawnedPlayerInstance;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // 1. Đảm bảo Scene luôn có ít nhất 1 AudioListener để không bị lỗi cảnh báo
        EnsureAudioListener();

        // 2. Tự động thu thập Transform con nếu danh sách spawnPoints đang trống
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            CollectChildSpawnPoints();
        }
    }

    private void Start()
    {
        EnsureAudioListener();

        if (spawnOnStart)
        {
            SpawnPlayer();
        }
    }

    /// <summary>
    /// Đảm bảo luôn có duy nhất 1 AudioListener trong Scene (trên Camera.main), tự động xóa các listener thừa
    /// </summary>
    public static void EnsureAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (listeners == null || listeners.Length == 0)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
                Debug.Log("<color=yellow>[ScenePlayerSpawner] Đã tự động thêm AudioListener vào Main Camera của Scene.</color>");
            }
        }
        else if (listeners.Length > 1)
        {
            // Nếu có nhiều hơn 1 AudioListener, giữ lại 1 cái duy nhất (ưu tiên trên Camera.main)
            AudioListener keepListener = null;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                keepListener = mainCam.GetComponent<AudioListener>();
            }

            if (keepListener == null)
            {
                keepListener = listeners[0];
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i] != keepListener)
                {
                    Debug.Log($"<color=orange>[ScenePlayerSpawner] Đã tự động xóa AudioListener thừa trên '{listeners[i].gameObject.name}' để tránh lỗi 2 AudioListener.</color>");
                    Destroy(listeners[i]);
                }
            }
        }
    }

    /// <summary>
    /// Thu thập toàn bộ Transform con làm điểm Spawn (có thể click chuột phải trong Inspector)
    /// </summary>
    [ContextMenu("Auto Collect Child Spawn Points")]
    public void CollectChildSpawnPoints()
    {
        spawnPoints.Clear();
        foreach (Transform child in transform)
        {
            if (child != transform)
            {
                spawnPoints.Add(child);
            }
        }
        Debug.Log($"[ScenePlayerSpawner] Đã thu thập {spawnPoints.Count} điểm spawn từ các đối tượng con.");
    }

    /// <summary>
    /// Thực hiện sinh nhân vật tại điểm Spawn được chọn
    /// </summary>
    [ContextMenu("Spawn Player Now")]
    public GameObject SpawnPlayer()
    {
        bool isMultiplayer = FusionConnectionManager.Instance != null &&
                             FusionConnectionManager.Instance.IsInGameplaySession;

        // 1. Xác định điểm xuất phát từ danh sách spawnPoints (Phân bổ vị trí khác nhau cho từng người chơi khi Multiplayer)
        Transform targetSpawnPoint = transform;
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            int targetIdx = 0;
            if (isMultiplayer && FusionConnectionManager.Instance.currentRunner != null)
            {
                var runner = FusionConnectionManager.Instance.currentRunner;
                targetIdx = Mathf.Abs(runner.LocalPlayer.PlayerId) % spawnPoints.Count;
            }

            if (targetIdx < spawnPoints.Count && spawnPoints[targetIdx] != null)
            {
                targetSpawnPoint = spawnPoints[targetIdx];
            }
            else
            {
                foreach (var pt in spawnPoints)
                {
                    if (pt != null)
                    {
                        targetSpawnPoint = pt;
                        break;
                    }
                }
            }
        }

        Vector3 spawnPos = targetSpawnPoint.position;
        Quaternion spawnRot = targetSpawnPoint.rotation;

        // 2. Kiểm tra nếu trong Scene đã có sẵn Player (CHỈ ÁP DỤNG OFFLINE) -> Dịch chuyển về đúng spawnPoints
        if (!isMultiplayer && preventDuplicateIfPlayerExists)
        {
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null && existingPlayer != spawnedPlayerInstance)
            {
                Debug.Log($"[ScenePlayerSpawner] Đã phát hiện Player '{existingPlayer.name}' trong Scene. Đặt lại đúng vị trí spawn {spawnPos}.");
                spawnedPlayerInstance = existingPlayer;

                // Tắt CharacterController để dịch chuyển vị trí chính xác không bị kẹt va chạm
                CharacterController ccExisting = spawnedPlayerInstance.GetComponent<CharacterController>();
                if (ccExisting != null) ccExisting.enabled = false;

                spawnedPlayerInstance.transform.position = spawnPos;
                spawnedPlayerInstance.transform.rotation = spawnRot;
                Physics.SyncTransforms();

                if (ccExisting != null) ccExisting.enabled = true;

                SetupCameraAndUI(spawnedPlayerInstance);
                return spawnedPlayerInstance;
            }
        }

        // 3. Lấy cấu hình PlayerSO (GameSession, PlayerPrefs hoặc default)
        PlayerSO activeData = GameSession.SelectedPlayer;
        if (activeData == null && characterDatabase != null && characterDatabase.Count > 0)
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedCharIndex", 0);
            int savedGender = PlayerPrefs.GetInt("SelectedGender", 0);
            GameSession.IsMale = (savedGender == 0);

            if (savedIndex >= 0 && savedIndex < characterDatabase.Count)
            {
                activeData = characterDatabase[savedIndex];
                GameSession.SelectedPlayer = activeData;
            }
        }

        if (activeData == null)
        {
            activeData = defaultPlayerData;
        }

        // 4. Lấy Prefab cần Instantiate
        GameObject prefabToInstantiate = null;
        if (activeData != null && activeData.characterPrefab != null)
        {
            prefabToInstantiate = activeData.characterPrefab;
        }
        else if (defaultPlayerData != null && defaultPlayerData.characterPrefab != null)
        {
            prefabToInstantiate = defaultPlayerData.characterPrefab;
        }
        else if (fallbackPlayerPrefab != null)
        {
            prefabToInstantiate = fallbackPlayerPrefab;
        }

        if (prefabToInstantiate == null)
        {
            Debug.LogWarning("[ScenePlayerSpawner] Không tìm thấy characterPrefab trong PlayerSO hoặc fallbackPlayerPrefab!");
            return null;
        }

        // 5. Spawn Player (Fusion Multiplayer nếu đang kết nối phòng, hoặc Instantiate nếu chơi Offline)
        if (isMultiplayer)
        {
            var runner = FusionConnectionManager.Instance.currentRunner;
            try
            {
                var networkObj = runner.Spawn(prefabToInstantiate, spawnPos, spawnRot, runner.LocalPlayer);
                spawnedPlayerInstance = networkObj != null ? networkObj.gameObject : null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ScenePlayerSpawner] runner.Spawn error: {ex.Message}. Falling back to local Instantiate.");
                spawnedPlayerInstance = Instantiate(prefabToInstantiate, spawnPos, spawnRot);
                DisableUnusedNetworkComponents(spawnedPlayerInstance);
            }
        }
        else
        {
            spawnedPlayerInstance = Instantiate(prefabToInstantiate, spawnPos, spawnRot);
            DisableUnusedNetworkComponents(spawnedPlayerInstance);
        }

        if (spawnedPlayerInstance == null) return null;
        spawnedPlayerInstance.name = prefabToInstantiate.name;

        // Đảm bảo vị trí chính xác trên CharacterController & NetworkTransform
        CharacterController ccNew = spawnedPlayerInstance.GetComponent<CharacterController>();
        if (ccNew != null) ccNew.enabled = false;
        spawnedPlayerInstance.transform.position = spawnPos;
        spawnedPlayerInstance.transform.rotation = spawnRot;

        var ntNew = spawnedPlayerInstance.GetComponent<NetworkTransform>();
        if (ntNew != null)
        {
            ntNew.Teleport(spawnPos, spawnRot);
        }

        Physics.SyncTransforms();
        if (ccNew != null) ccNew.enabled = true;

        // 6. Nạp dữ liệu PlayerSO vào PlayerStats & PlayerController
        PlayerController pc = spawnedPlayerInstance.GetComponent<PlayerController>();
        PlayerStats stats = spawnedPlayerInstance.GetComponent<PlayerStats>();

        if (stats != null && activeData != null)
        {
            stats.InitializeFromData(activeData);
        }

        if (pc != null && activeData != null)
        {
            pc.playerData = activeData;
        }

        if (stats != null)
        {
            stats.ApplyCharacterMeshAndSkin(activeData);
        }

        var netSync = spawnedPlayerInstance.GetComponent<NetworkPlayerSync>();
        if (netSync != null)
        {
            netSync.SetLocalMeshVisibility(false);
        }

        // 7. Tự động kết nối Camera & UI
        SetupCameraAndUI(spawnedPlayerInstance);

        Debug.Log($"<color=cyan>[ScenePlayerSpawner] Đã sinh thành công '{spawnedPlayerInstance.name}' tại {spawnPos}!</color>");
        return spawnedPlayerInstance;
    }

    /// <summary>
    /// Offline Instantiate: tắt NetworkTransform / NetworkMecanimAnimator để không khóa vị trí nhân vật.
    /// </summary>
    public static void DisableUnusedNetworkComponents(GameObject playerObj)
    {
        if (playerObj == null) return;

        var networkTransform = playerObj.GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.enabled = false;
        }

        var networkAnimator = playerObj.GetComponent<NetworkMecanimAnimator>();
        if (networkAnimator != null)
        {
            networkAnimator.enabled = false;
        }
    }

    /// <summary>
    /// Tự động kết nối Cinemachine Virtual Camera và UI_Manager vào Player vừa sinh
    /// </summary>
    public void SetupCameraAndUI(GameObject playerObj)
    {
        if (playerObj == null) return;

        PlayerController pc = playerObj.GetComponent<PlayerController>();
        PlayerStats stats = playerObj.GetComponent<PlayerStats>();

        // 1. Tìm Target Camera
        Transform cameraTarget = null;
        if (pc != null && pc.CinemachineCameraTarget != null)
        {
            cameraTarget = pc.CinemachineCameraTarget.transform;
        }
        else
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag("CinemachineTarget");
            if (targetObj != null) cameraTarget = targetObj.transform;
        }

        // 2. Kết nối Cinemachine Virtual Camera
        if (cameraTarget != null)
        {
            ConnectCinemachineCamera(cameraTarget);
        }

        // 3. Kết nối UI_Manager
        UI_Manager ui = FindFirstObjectByType<UI_Manager>();
        if (ui != null && stats != null)
        {
            ui.BindPlayer(stats);
        }

        // 4. Kết nối PostProcess và kích hoạt Global Volume
        PostProcess post = FindFirstObjectByType<PostProcess>(FindObjectsInactive.Include);
        if (post != null)
        {
            post.gameObject.SetActive(true);
            post.enabled = true;
            if (pc != null) post.controller = pc;
        }

        // 5. Đảm bảo Camera.main bật renderPostProcessing
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var camData = mainCam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
            }
        }

        // 6. Kết nối HotbarManager với Local Player
        HotbarManager hotbar = FindFirstObjectByType<HotbarManager>(FindObjectsInactive.Include);
        if (hotbar != null && pc != null)
        {
            hotbar.RebindPlayer(pc);
        }
    }

    /// <summary>
    /// Tìm và gán Follow / LookAt cho Cinemachine Camera trong Scene
    /// </summary>
    private void ConnectCinemachineCamera(Transform target)
    {
        if (target == null) return;

        // Tìm tất cả các component có tên chứa CinemachineCamera hoặc CinemachineVirtualCamera
        Component[] allComponents = FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;
            if (typeName.Contains("CinemachineVirtualCamera") || typeName.Contains("CinemachineCamera"))
            {
                var followProp = comp.GetType().GetProperty("Follow");
                var lookAtProp = comp.GetType().GetProperty("LookAt");

                if (followProp != null) followProp.SetValue(comp, target);
                if (lookAtProp != null) lookAtProp.SetValue(comp, target);

                Debug.Log($"[ScenePlayerSpawner] Đã kết nối Camera ({typeName}) theo dõi '{target.name}'.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f); // Xanh lá sáng

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Vector3 pos = spawnPoints[i].position;
                    // Vẽ hình trụ / quả cầu đại diện cho nhân vật
                    Gizmos.DrawSphere(pos + Vector3.up * 0.9f, 0.35f);
                    Gizmos.DrawWireCube(pos + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));

                    // Vẽ mũi tên hướng nhìn xuất phát
                    Vector3 forward = spawnPoints[i].forward * 1.2f;
                    Gizmos.DrawRay(pos + Vector3.up * 0.9f, forward);
                }
            }
        }
        else
        {
            Vector3 pos = transform.position;
            Gizmos.DrawSphere(pos + Vector3.up * 0.9f, 0.35f);
            Gizmos.DrawWireCube(pos + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
            Gizmos.DrawRay(pos + Vector3.up * 0.9f, transform.forward * 1.2f);
        }
    }
}
