using System.Collections.Generic;
using Fusion;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý đồng bộ mạng cho Player (Photon Fusion NetworkBehaviour):
/// - Phân định quyền điều khiển: HasInputAuthority (Local Player) vs Remote Player (Người chơi khác).
/// - Đồng bộ tên người chơi và ngoại hình nhân vật (Selected Character Index) qua biến [Networked].
/// - Tự động bật Camera, Joystick, Input và HUD cho Local Player, đồng thời vô hiệu hóa trên Remote Players để tránh xung đột điều khiển.
/// </summary>
public class NetworkPlayerSync : NetworkBehaviour
{
    [Header("👤 Networked Properties")]
    [Networked, OnChangedRender(nameof(OnCharacterSkinChanged))]
    public int CharacterSkinIndex { get; set; } = 0;

    [Networked, OnChangedRender(nameof(OnCharacterSkinChanged))]
    public NetworkBool NetworkIsMale { get; set; } = true;

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> NetworkPlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnHeldItemPathChanged))]
    public NetworkString<_64> NetworkHeldItemPath { get; set; }


    public void SetHeldItem(GameObject itemObj)
        {
            if (Runner == null || !Runner.IsRunning || !IsLocalPlayer) return;

            if (itemObj != null)
            {
                // Truyền trực tiếp tên duy nhất của item (VD: Item_0_Flashlight)
                NetworkHeldItemPath = itemObj.name;
            }
            else
            {
                NetworkHeldItemPath = "";
            }
        }

   private void OnHeldItemPathChanged()
    {
        if (IsLocalPlayer) return;

        string itemPath = NetworkHeldItemPath.ToString();

        if (string.IsNullOrEmpty(itemPath))
        {
            ClearRemoteHeldItem();
            return;
        }

        GameObject itemObj = NetworkItemSync.FindSceneObjectByPath(itemPath);

        if (itemObj != null)
        {
            if (_remoteHeldInstance != null && _remoteHeldInstance != itemObj)
            {
                ClearRemoteHeldItem();
            }

            _remoteHeldInstance = itemObj;

            Transform socket = GetOrCreateRemoteSocket();

            // Khóa Rigidbody
            var rb = _remoteHeldInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            // Gắn vào socket con của Player và áp dụng Custom Hold Offset & Rotation nếu có
            Item itemComp = _remoteHeldInstance.GetComponent<Item>() ?? _remoteHeldInstance.GetComponentInChildren<Item>();
            Vector3 customPos = (itemComp != null && itemComp.useCustomHoldOffset) ? itemComp.customHoldOffset : Vector3.zero;
            Quaternion customRot = (itemComp != null && itemComp.useCustomHoldRotation) ? Quaternion.Euler(itemComp.customHoldRotation) : Quaternion.identity;

            _remoteHeldInstance.transform.SetParent(socket, false);
            _remoteHeldInstance.transform.localPosition = customPos;
            _remoteHeldInstance.transform.localRotation = customRot;

            // Bù trừ Scale chống phóng to
            Vector3 parentLossy = socket.lossyScale;
            if (parentLossy.x != 0 && parentLossy.y != 0 && parentLossy.z != 0)
            {
                _remoteHeldInstance.transform.localScale = new Vector3(1f / parentLossy.x, 1f / parentLossy.y, 1f / parentLossy.z);
            }
            else
            {
                _remoteHeldInstance.transform.localScale = Vector3.one;
            }

            // Tắt Collider
            var colliders = _remoteHeldInstance.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = false;

            // Bật Renderers
            var renderers = _remoteHeldInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;

            _remoteHeldInstance.SetActive(true);
        }
    }

    private void ClearRemoteHeldItem()
    {
        if (_remoteHeldInstance != null)
        {
            // Chỉ tắt GameObject nếu item này vẫn đang nằm trên người (socket) của Player
            // Nếu item đã được ném/thả ra ngoài thế giới (parent == null hoặc không còn ở socket), KHÔNG ĐƯỢC tắt GameObject!
            if (_remoteHoldSocket != null && _remoteHeldInstance.transform.parent == _remoteHoldSocket)
            {
                _remoteHeldInstance.SetActive(false);
                _remoteHeldInstance.transform.SetParent(null);
            }
            _remoteHeldInstance = null;
        }
    }

    #region 🌐 Item Network RPCs (Đồng bộ nhặt / ném / đèn pin qua Player)

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcDespawnSceneItem(string itemHierarchyPath)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;

        GameObject targetItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            targetItem.SetActive(false);
            Debug.Log($"<color=cyan>[NetworkPlayerSync] Đã ẩn vật phẩm trên map: {itemHierarchyPath}</color>");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcShowDroppedSceneItem(string itemHierarchyPath, Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity, bool isLadder, bool isLadderPlaced, RpcInfo info = default)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;

        GameObject targetItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            if (Runner != null && info.Source == Runner.LocalPlayer && targetItem.activeSelf)
            {
                return;
            }

            targetItem.transform.SetParent(null);
            targetItem.transform.position = position;
            targetItem.transform.rotation = rotation;
            targetItem.transform.localScale = Vector3.one;
            targetItem.SetActive(true);

            var renderers = targetItem.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;

            var colliders = targetItem.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = true;

            var ladder = targetItem.GetComponent<LadderController>() ?? targetItem.GetComponentInChildren<LadderController>();
            if (ladder != null)
            {
                ladder.SetPlaced(isLadderPlaced);
            }
            if (!isLadderPlaced)
            {
                HotbarManager.IgnoreCollisionWithAllPlayers(targetItem, true);
            }

            Rigidbody rb = targetItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.WakeUp();
                rb.linearVelocity = linearVelocity;
                rb.angularVelocity = angularVelocity;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSetFlashlightState(string itemHierarchyPath, bool isOn)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;

        GameObject targetItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            FlashlightController flash = targetItem.GetComponent<FlashlightController>() ?? targetItem.GetComponentInChildren<FlashlightController>();
            if (flash != null)
            {
                flash.ApplyFlashlightVisualAndAudio(isOn);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcBroadcastStatusMessage(string message)
    {
        GameStatusHUD.Show(message);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcTriggerPlayerDeath(string caughtBy)
    {
        var deathHandler = GetComponent<PlayerDeathHandler>() ?? GetComponentInChildren<PlayerDeathHandler>();
        if (deathHandler != null)
        {
            deathHandler.ExecuteDeath(caughtBy, IsLocalPlayer);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSetLadderOccupied(string itemHierarchyPath, bool isOccupied)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;
        GameObject targetItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            var ladder = targetItem.GetComponent<LadderController>() ?? targetItem.GetComponentInChildren<LadderController>();
            if (ladder != null)
            {
                ladder.isOccupied = isOccupied;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSetLadderAudio(string itemHierarchyPath, bool isPlaying)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;
        GameObject targetItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            var ladder = targetItem.GetComponent<LadderController>() ?? targetItem.GetComponentInChildren<LadderController>();
            if (ladder != null)
            {
                ladder.ApplyClimbAudioState(isPlaying);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSyncSellItem(string itemHierarchyPath, int earnedPoints, string itemName)
    {
        // 1. Kiểm tra chống cộng điểm x2 khi nhiều máy gửi RPC cùng lúc
        if (!string.IsNullOrEmpty(itemHierarchyPath))
        {
            if (NetworkItemSync.IsItemAlreadySold(itemHierarchyPath))
            {
                // Item này đã được cộng điểm rồi -> Chỉ đảm bảo GameObject bị ẩn/hủy nếu còn tồn tại
                GameObject existItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
                if (existItem != null)
                {
                    existItem.SetActive(false);
                    Destroy(existItem);
                }
                return;
            }
            NetworkItemSync.MarkItemAsSold(itemHierarchyPath);
        }

        // 2. Cộng điểm đồng bộ 100% cho mọi người chơi trong phòng
        UI_Manager ui = FindFirstObjectByType<UI_Manager>();
        if (ui != null && ui.playerManager != null)
        {
            ui.playerManager.currpoint += earnedPoints;
            Debug.Log($"<color=green>[NetworkPlayerSync] Đã bán '{itemName}' (+${earnedPoints}). Điểm phòng: {ui.playerManager.currpoint}/{ui.playerManager.totalpoint}</color>");
        }

        // 3. Hiện banner thông báo nổi trên màn hình
        GameStatusHUD.Show($"Sold {itemName} ( +${earnedPoints} )", 2.5f);

        // 4. Ẩn & hủy vật phẩm trên tất cả các máy
        if (!string.IsNullOrEmpty(itemHierarchyPath))
        {
            GameObject targetItem = NetworkItemSync.FindSceneObjectByPath(itemHierarchyPath);
            if (targetItem != null)
            {
                targetItem.SetActive(false);
                Destroy(targetItem);
            }
        }
    }

    #endregion

    [Header("✋ Hand Attachment (Điểm gắn item trên người khác)")]
    [Tooltip("Kéo trực tiếp xương tay phải (RightHand / Hand.R) của Prefab nhân vật vào đây")]
    public Transform rightHandSocket;

    private Transform _remoteRightHandBone;
    private GameObject _remoteHeldInstance;
    private Transform _remoteHoldSocket;

    [Header("🎯 Remote Held Item Offset")]
public Vector3 remoteHoldPosition = new Vector3(0f, 1.2f, 0.5f);

    private Transform GetOrCreateRemoteSocket()
    {
        if (_remoteHoldSocket == null)
        {
            Transform existing = transform.Find("RemoteFixedHoldSocket");
            if (existing != null)
            {
                _remoteHoldSocket = existing;
            }
            else
            {
                GameObject socket = new GameObject("RemoteFixedHoldSocket");
                socket.transform.SetParent(transform, false);
                _remoteHoldSocket = socket.transform;
            }
        }

        // Cố định cứng tọa độ trước mặt Remote Player
        _remoteHoldSocket.localPosition = remoteHoldPosition;
        _remoteHoldSocket.localRotation = Quaternion.identity;
        _remoteHoldSocket.localScale = Vector3.one;

        return _remoteHoldSocket;
    }

    private void EnsureRightHandBone()
    {
        // 1. Ưu tiên biến gán cứng từ Inspector (chính xác 100%)
        if (rightHandSocket != null)
        {
            _remoteRightHandBone = rightHandSocket;
            return;
        }

        if (_remoteRightHandBone != null) return;

        // 2. Tìm qua Animator Humanoid Bone của nhân vật
        Animator anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        if (anim != null && anim.isHuman)
        {
            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null)
            {
                _remoteRightHandBone = hand;
                return;
            }
        }

        // 3. Quét tất cả Transform con tìm các tên quy chuẩn phổ biến của Rig xương
        string[] handPatterns = new string[] 
        { 
            "righthand", "hand.r", "hand_r", "hand (r)", "right_hand", 
            "b_r_hand", "wrist_r", "wrist.r", "mixamorig:righthand" 
        };

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            string lowerName = t.name.ToLower().Trim();
            foreach (var pattern in handPatterns)
            {
                if (lowerName == pattern || lowerName.EndsWith(pattern))
                {
                    _remoteRightHandBone = t;
                    return;
                }
            }
        }

        // Nếu quét hết vẫn không thấy, log cảnh báo để kiểm tra Rig nhân vật
        Debug.LogWarning($"<color=orange>[NetworkPlayerSync] Không tìm thấy xương tay phải trên {gameObject.name}! Tạm thời gắn vào root.</color>");
        _remoteRightHandBone = transform;
    }

    [Header("🏃 Networked Animation & HeadLook State")]
    [Networked] public float NetworkSpeed { get; set; }
    [Networked] public float NetworkMotionSpeed { get; set; }
    [Networked] public NetworkBool NetworkGrounded { get; set; }
    [Networked] public NetworkBool NetworkCrouch { get; set; }
    [Networked] public NetworkBool NetworkIsCrouching { get; set; }
    [Networked] public NetworkBool NetworkIsClimbing { get; set; }
    [Networked] public float NetworkClimbSpeed { get; set; }
    [Networked] public float NetworkHeadPitch { get; set; }
    [Networked] public float NetworkHeadYaw { get; set; }

    private Transform _headLookBone;
    private Transform _spineLookBone;
    private float _smoothRemotePitch;
    private float _smoothRemoteYaw;
    private Animator _remoteAnimator;

    [Header("🏷️ Overhead Name Tag (Tên hiển thị trên đầu)")]
    [Tooltip("Component Text hiển thị tên (hỗ trợ cả TextMeshPro 3D lẫn TextMeshProUGUI trên Canvas)")]
    public TMP_Text nameTagText;
    [Tooltip("World Space Canvas chứa bảng tên (nếu có)")]
    public Canvas nameTagCanvas;
    [Tooltip("Độ cao của bảng tên so với chân nhân vật")]
    public float nameTagHeight = 2.15f;
    [Tooltip("Cỡ chữ của bảng tên (khi tự động tạo 3D Text)")]
    public float nameTagFontSize = 3.5f;
    [Tooltip("Màu chữ tên người chơi khác")]
    public Color remoteNameColor = new Color(0.9f, 0.95f, 1f, 1f);
    [Tooltip("Có hiển thị bảng tên cho chính bản thân không")]
    public bool showForLocalPlayer = false;

    [Header("🎮 Local Components to Control")]
    public PlayerController playerController;
    public StarterAssetsInputs starterInputs;
    public PlayerInput playerInput;
    public CharacterController characterController;
    public GameObject cinemachineCameraTarget;

    [Header("📦 Character Mesh & Skin Customization")]
    [Tooltip("SkinnedMeshRenderer của nhân vật để tự động đổi Mesh và Material")]
    public SkinnedMeshRenderer characterSkinnedMesh;
    [Tooltip("Danh sách PlayerSO để nạp Mesh/Material")]
    public List<PlayerSO> characterDatabase = new List<PlayerSO>();
    [Tooltip("Danh sách các Model con tương ứng với từng nhân vật (nếu dùng cơ chế bật/tắt Model)")]
    public List<GameObject> characterModelObjects = new List<GameObject>();

    [Header("👁️ Local FPS Mesh Visibility")]
    [Tooltip("Tự động tắt Mesh của bản thân (Local Player) để không che tầm nhìn Camera góc nhìn thứ nhất (FPS)")]
    public bool hideLocalPlayerMesh = true;
    [Tooltip("true = vẫn đổ bóng xuống sàn (ShadowsOnly); false = tắt hẳn SkinnedMeshRenderer")]
    public bool useShadowsOnly = false;

    /// <summary>
    /// Kiểm tra người chơi cục bộ: hỗ trợ cả Offline lẫn Fusion Shared Mode (HasStateAuthority) và Host/Server Mode (HasInputAuthority)
    /// </summary>
    public bool IsLocalPlayer
    {
        get
        {
            if (Runner == null || !Runner.IsRunning) return true;
            return Object.HasStateAuthority || Object.HasInputAuthority;
        }
    }

    private void Awake()
    {
        EnsureNameTagComponent();
        EnsureMeshComponents();
    }

    private void Start()
    {
        if (Runner == null || !Runner.IsRunning)
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (starterInputs == null) starterInputs = GetComponent<StarterAssetsInputs>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (characterController == null) characterController = GetComponent<CharacterController>();

            if (characterController != null) characterController.enabled = true;
            if (starterInputs != null) starterInputs.enabled = true;
            if (playerInput != null) playerInput.enabled = true;
            if (playerController != null) playerController.enabled = true;

            var localInteraction = GetComponent<PlayerInteraction>();
            if (localInteraction != null) localInteraction.enabled = true;

            var localHead = GetComponent<PlayerHeadLook>();
            if (localHead != null) localHead.enabled = true;

            ScenePlayerSpawner.DisableUnusedNetworkComponents(gameObject);
            ApplySkinFromGameSession();
            SetLocalMeshVisibility(false);
            SetNameTagVisible(showForLocalPlayer);
        }
        else if (IsLocalPlayer)
        {
            ConfigureLocalNetworkTransform();
        }
    }

    public override void Spawned()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (starterInputs == null) starterInputs = GetComponent<StarterAssetsInputs>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        EnsureNameTagComponent();
        EnsureMeshComponents();

        if (IsLocalPlayer)
        {
            // =========================================================================
            //                         LOCAL PLAYER SETUP
            // =========================================================================
            Debug.Log("<color=green>[NetworkPlayerSync] Thiết lập Local Player (IsLocalPlayer = true).</color>");

            // 1. Gửi tên, giới tính và nhân vật đã chọn lên mạng
            string myName = "Player";
            if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
            {
                myName = FirebaseDataService.Instance.CurrentUserProfile.username;
            }
            NetworkPlayerName = myName;

            int savedCharIndex = PlayerPrefs.GetInt("SelectedCharIndex", 0);
            if (GameSession.SelectedPlayer != null && characterDatabase != null)
            {
                int idx = characterDatabase.IndexOf(GameSession.SelectedPlayer);
                if (idx >= 0) savedCharIndex = idx;
            }

            CharacterSkinIndex = savedCharIndex;
            NetworkIsMale = GameSession.IsMale;

            // Bảng tên của chính mình
            if (nameTagText != null)
            {
                nameTagText.text = myName;
            }
            SetNameTagVisible(showForLocalPlayer);

            ConfigureLocalNetworkTransform();

            // Đặt vị trí xuất phát chính xác từ ScenePlayerSpawner
            ScenePlayerSpawner spawner = FindFirstObjectByType<ScenePlayerSpawner>();
            if (spawner != null && spawner.spawnPoints != null && spawner.spawnPoints.Count > 0)
            {
                int targetIdx = 0;
                if (Object != null && Object.IsValid)
                {
                    targetIdx = Mathf.Abs(Object.InputAuthority.PlayerId) % spawner.spawnPoints.Count;
                }
                else if (Runner != null)
                {
                    targetIdx = Mathf.Abs(Runner.LocalPlayer.PlayerId) % spawner.spawnPoints.Count;
                }

                if (targetIdx < spawner.spawnPoints.Count && spawner.spawnPoints[targetIdx] != null)
                {
                    Vector3 targetPos = spawner.spawnPoints[targetIdx].position;
                    Quaternion targetRot = spawner.spawnPoints[targetIdx].rotation;

                    var nt = GetComponent<NetworkTransform>();
                    if (characterController != null) characterController.enabled = false;

                    transform.position = targetPos;
                    transform.rotation = targetRot;

                    if (nt != null)
                    {
                        nt.Teleport(targetPos, targetRot);
                    }

                    Physics.SyncTransforms();
                    Debug.Log($"<color=cyan>[NetworkPlayerSync] Đã teleport Local Player về SpawnPoint [{targetIdx}] tại {targetPos}!</color>");
                }
            }

            // Cập nhật skin cho chính mình
            ApplySkin(CharacterSkinIndex, NetworkIsMale);

            // Tắt mesh bản thân để không che camera FPS
            SetLocalMeshVisibility(false);

            // 2. Kích hoạt Input, Controller, Physics & Camera cho Local Player
            if (characterController != null) characterController.enabled = true;
            if (starterInputs != null) starterInputs.enabled = true;
            if (playerInput != null) playerInput.enabled = true;
            if (playerController != null) playerController.enabled = true;

            var localInteraction = GetComponent<PlayerInteraction>();
            if (localInteraction != null) localInteraction.enabled = true;

            var localHead = GetComponent<PlayerHeadLook>();
            if (localHead != null) localHead.enabled = true;

            // 3. Kết nối Camera và UI
            SetupLocalCameraAndHUD();
        }
        else
        {
            // =========================================================================
            //                        REMOTE PLAYER SETUP
            // =========================================================================
            Debug.Log("<color=cyan>[NetworkPlayerSync] Thiết lập Remote Player (Người chơi khác trong phòng).</color>");

            // 1. Vô hiệu hóa Input, Controller & Physics của máy mình trên người chơi khác
            if (starterInputs != null) starterInputs.enabled = false;
            if (playerInput != null) playerInput.enabled = false;
            if (playerController != null) playerController.enabled = false;
            if (characterController != null) characterController.enabled = false;

            var remoteInteraction = GetComponent<PlayerInteraction>();
            if (remoteInteraction != null) remoteInteraction.enabled = false;

            var remoteHead = GetComponent<PlayerHeadLook>();
            if (remoteHead != null) remoteHead.enabled = false;

            var audioListener = GetComponentInChildren<AudioListener>(true);
            if (audioListener != null) Destroy(audioListener);

            // 2. Hiện bảng tên trên đầu
            if (nameTagText != null)
            {
                nameTagText.text = string.IsNullOrEmpty(NetworkPlayerName.ToString()) ? "Player" : NetworkPlayerName.ToString();
            }
            SetNameTagVisible(true);

            // 3. Cập nhật Skin & Mesh ngoại hình người chơi khác (hiển thị đầy đủ cho người khác nhìn thấy)
            ApplySkin(CharacterSkinIndex, NetworkIsMale);
            SetLocalMeshVisibility(true);
        }
    }

    /// <summary>
    /// Bật/Tắt hiển thị bảng tên (bao gồm cả Canvas hoặc GameObject chứa Text)
    /// </summary>
    private void SetNameTagVisible(bool visible)
    {
        if (nameTagCanvas != null)
        {
            nameTagCanvas.gameObject.SetActive(visible);
        }
        else if (nameTagText != null)
        {
            nameTagText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Tự động tìm hoặc tạo component bảng tên trên đầu nhân vật
    /// </summary>
    public void EnsureNameTagComponent()
    {
        if (nameTagCanvas == null)
        {
            nameTagCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (nameTagCanvas != null && nameTagCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (nameTagCanvas.worldCamera == null)
            {
                nameTagCanvas.worldCamera = Camera.main;
            }
        }

        if (nameTagText == null)
        {
            nameTagText = GetComponentInChildren<TMP_Text>(true);
        }

        if (nameTagText == null)
        {
            GameObject tagObj = new GameObject("OverheadNameTag");
            tagObj.transform.SetParent(transform);
            tagObj.transform.localPosition = new Vector3(0f, nameTagHeight, 0f);
            tagObj.transform.localRotation = Quaternion.identity;
            tagObj.transform.localScale = Vector3.one;

            var tmp = tagObj.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = nameTagFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = remoteNameColor;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;
            tmp.rectTransform.sizeDelta = new Vector2(5f, 1f);
            nameTagText = tmp;
        }
    }

    private void EnsureMeshComponents()
    {
        if (characterSkinnedMesh == null)
        {
            characterSkinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>(true);
        }

        if ((characterDatabase == null || characterDatabase.Count == 0))
        {
            ScenePlayerSpawner spawner = FindFirstObjectByType<ScenePlayerSpawner>();
            if (spawner != null && spawner.characterDatabase != null && spawner.characterDatabase.Count > 0)
            {
                characterDatabase = new List<PlayerSO>(spawner.characterDatabase);
            }
        }
    }

    private void SetupLocalCameraAndHUD()
    {
        ScenePlayerSpawner spawner = FindFirstObjectByType<ScenePlayerSpawner>();
        if (spawner != null)
        {
            spawner.SetupCameraAndUI(gameObject);
        }
    }

    /// <summary>
    /// CharacterController tự di chuyển local: tắt interpolation Shared Mode để NetworkTransform không kéo nhân vật về chỗ cũ.
    /// </summary>
    private void ConfigureLocalNetworkTransform()
    {
        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.DisableSharedModeInterpolation = true;
            networkTransform.enabled = true;
        }
    }

    private void ApplySkinFromGameSession()
    {
        int skinIndex = PlayerPrefs.GetInt("SelectedCharIndex", 0);
        if (GameSession.SelectedPlayer != null && characterDatabase != null)
        {
            int idx = characterDatabase.IndexOf(GameSession.SelectedPlayer);
            if (idx >= 0) skinIndex = idx;
        }
        ApplySkin(skinIndex, GameSession.IsMale);
    }

    private void OnPlayerNameChanged()
    {
        if (nameTagText != null)
        {
            nameTagText.text = NetworkPlayerName.ToString();
        }
        SetNameTagVisible(!IsLocalPlayer || showForLocalPlayer);
    }

    private void OnCharacterSkinChanged()
    {
        ApplySkin(CharacterSkinIndex, NetworkIsMale);
    }

    /// <summary>
    /// Đồng bộ ngoại hình: hỗ trợ cả bật/tắt Model con lẫn đổi SkinnedMesh / Material tự động từ PlayerSO
    /// </summary>
    private void ApplySkin(int skinIndex, bool isMale)
    {
        // Cách 1: Bật/tắt GameObjects nếu danh sách characterModelObjects có phần tử
        if (characterModelObjects != null && characterModelObjects.Count > 0)
        {
            for (int i = 0; i < characterModelObjects.Count; i++)
            {
                if (characterModelObjects[i] != null)
                {
                    characterModelObjects[i].SetActive(i == skinIndex);
                }
            }
        }

        // Cách 2: Tự động đổi Mesh 3D & Material trên SkinnedMeshRenderer từ PlayerSO
        PlayerSO so = null;
        if (characterDatabase != null && skinIndex >= 0 && skinIndex < characterDatabase.Count)
        {
            so = characterDatabase[skinIndex];
        }
        if (so == null)
        {
            so = GameSession.SelectedPlayer;
        }

        if (so != null)
        {
            Mesh targetMesh = so.GetMesh(isMale);
            if (characterSkinnedMesh != null && targetMesh != null)
            {
                characterSkinnedMesh.sharedMesh = targetMesh;
            }
            else
            {
                PlayerStats.ApplyMeshToModel(gameObject, targetMesh);
            }

            PlayerStats.ApplySkinToModel(gameObject, so.characterMaterial, so.characterTexture);
        }

        // Đảm bảo trạng thái ẩn/hiện mesh của bản thân (FPS) vẫn được giữ đúng sau khi đổi skin
        SetLocalMeshVisibility(!IsLocalPlayer);
    }

    /// <summary>
    /// Bật/Tắt hiển thị Mesh nhân vật (Trong góc nhìn FPV: tắt mesh của chính mình để tránh che Camera)
    /// </summary>
    public void SetLocalMeshVisibility(bool visible)
    {
        if (!visible && hideLocalPlayerMesh)
        {
            ApplyMeshVisibility(false, useShadowsOnly);
        }
        else
        {
            ApplyMeshVisibility(true, false);
        }
    }

    private void ApplyMeshVisibility(bool visible, bool shadowsOnly)
    {
        if (characterSkinnedMesh != null)
        {
            if (shadowsOnly)
            {
                characterSkinnedMesh.enabled = true;
                characterSkinnedMesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                characterSkinnedMesh.enabled = visible;
                characterSkinnedMesh.shadowCastingMode = visible ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            if (smr == null) continue;
            if (smr.GetComponentInParent<Item>() != null) continue;

            if (shadowsOnly)
            {
                smr.enabled = true;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                smr.enabled = visible;
                smr.shadowCastingMode = visible ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    public void SetClimbing(bool climbing, float speed = 0f)
    {
        if (Runner == null || !Runner.IsRunning || !IsLocalPlayer) return;
        NetworkIsClimbing = climbing;
        NetworkClimbSpeed = speed;
    }

    public override void FixedUpdateNetwork()
    {
        if (IsLocalPlayer)
        {
            if (playerController != null)
            {
                NetworkSpeed = playerController._animationBlend;
                NetworkMotionSpeed = playerController._input != null ? playerController._input.move.magnitude : 0f;
                NetworkGrounded = playerController.Grounded;
                NetworkCrouch = playerController.Crouching;
                NetworkIsCrouching = playerController.Crouching && (playerController._input != null && playerController._input.move.sqrMagnitude > 0.01f);
                NetworkIsClimbing = playerController.isClimbingLadder;
            }

            var localHead = GetComponent<PlayerHeadLook>();
            if (localHead != null)
            {
                NetworkHeadPitch = localHead.CurrentPitch;
                NetworkHeadYaw = localHead.CurrentYaw;
            }
            else
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    Vector3 camForward = mainCam.transform.forward;
                    float rawPitch = Mathf.Asin(Mathf.Clamp(camForward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    Vector3 horizontalCamForward = Vector3.ProjectOnPlane(camForward, transform.up).normalized;
                    if (horizontalCamForward.sqrMagnitude > 0.0001f)
                    {
                        NetworkHeadYaw = Vector3.SignedAngle(transform.forward, horizontalCamForward, transform.up);
                    }
                    NetworkHeadPitch = rawPitch;
                }
            }
        }
    }

    public override void Render()
    {
        if (!IsLocalPlayer)
        {
            if (_remoteAnimator == null)
            {
                _remoteAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            }

            if (_remoteAnimator != null)
            {
                _remoteAnimator.SetFloat("Speed", NetworkSpeed);
                _remoteAnimator.SetFloat("MotionSpeed", NetworkMotionSpeed);
                _remoteAnimator.SetBool("Grounded", NetworkGrounded);
                _remoteAnimator.SetBool("Crouch", NetworkCrouch);
                _remoteAnimator.SetBool("IsCrouching", NetworkIsCrouching);
                _remoteAnimator.SetBool("Climb", NetworkIsClimbing);
                if (NetworkIsClimbing)
                {
                    _remoteAnimator.SetFloat("ClimbSpeed", NetworkClimbSpeed);
                    _remoteAnimator.speed = Mathf.Abs(NetworkClimbSpeed) > 0.01f ? 1.0f : 0f;
                }
                else
                {
                    if (_remoteAnimator.speed <= 0.01f)
                    {
                        _remoteAnimator.speed = 1.0f;
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        // 1. Procedural Head Look cho Remote Player: Xoay xương Head & Spine SAU KHI Animator đã tính toán animation của frame
        if (!IsLocalPlayer)
        {
            if (_headLookBone == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Head" || t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                    {
                        _headLookBone = t;
                        break;
                    }
                }
            }

            // if (_spineLookBone == null)
            // {
            //     foreach (var t in GetComponentsInChildren<Transform>(true))
            //     {
            //         if (t.name == "Stomach" || t.name == "Spine" || t.name == "Chest")
            //         {
            //             _spineLookBone = t;
            //             break;
            //         }
            //     }
            // }

            float targetPitch = Mathf.Clamp(NetworkHeadPitch, -45f, 60f);
            float targetYaw = Mathf.Clamp(NetworkHeadYaw, -75f, 75f);

            _smoothRemotePitch = Mathf.Lerp(_smoothRemotePitch, targetPitch, Time.deltaTime * 12f);
            _smoothRemoteYaw = Mathf.Lerp(_smoothRemoteYaw, targetYaw, Time.deltaTime * 12f);

            // if (_spineLookBone != null)
            // {
            //     Quaternion spineRot = Quaternion.AngleAxis(_smoothRemoteYaw * 0.25f, transform.up)
            //                         * Quaternion.AngleAxis(-_smoothRemotePitch * 0.25f, transform.right);
            //     _spineLookBone.rotation = spineRot * _spineLookBone.rotation;
            // }

            if (_headLookBone != null)
            {
                Quaternion headRot = Quaternion.AngleAxis(_smoothRemoteYaw * 0.75f, transform.up)
                                   * Quaternion.AngleAxis(-_smoothRemotePitch * 0.75f, transform.right);
                _headLookBone.rotation = headRot * _headLookBone.rotation;
            }
        }

        // 2. Hiệu ứng Billboard: Bảng tên (hoặc LocalCanvas) luôn quay mặt đối diện trực tiếp Camera
        Transform targetBillboard = (nameTagCanvas != null) ? nameTagCanvas.transform : ((nameTagText != null) ? nameTagText.transform : null);
        if (targetBillboard != null && targetBillboard.gameObject.activeSelf)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                targetBillboard.rotation = mainCam.transform.rotation;
            }
        }

        // ĐỒNG BỘ GÓC NGỬA/CÚI (Đã đảo dấu -NetworkHeadPitch để không bị ngược chiều)
        if (!IsLocalPlayer && _remoteHoldSocket != null && _remoteHeldInstance != null && _remoteHeldInstance.activeSelf)
        {
            Item itemComp = _remoteHeldInstance.GetComponent<Item>();
            if (itemComp != null && itemComp.followCameraPitch)
            {
                // Dấu trừ (-) ở đây để sửa lỗi ngửa thành chúi xuống
                _remoteHoldSocket.localRotation = Quaternion.Euler(-NetworkHeadPitch, 0f, 0f);
            }
            else
            {
                _remoteHoldSocket.localRotation = Quaternion.identity;
            }
        }
    }
}
