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

    private Transform _remoteRightHandBone;
    private GameObject _remoteHeldInstance;

    public void SetHeldItem(GameObject itemObj)
    {
        if (Runner == null || !Runner.IsRunning || !IsLocalPlayer) return;

        if (itemObj != null)
        {
            NetworkHeldItemPath = NetworkItemSync.GetGameObjectPath(itemObj);
        }
        else
        {
            NetworkHeldItemPath = "";
        }
    }

    private void OnHeldItemPathChanged()
    {
        if (IsLocalPlayer) return; // Local player hiển thị qua HotbarManager

        string itemPath = NetworkHeldItemPath.ToString();

        if (string.IsNullOrEmpty(itemPath))
        {
            if (_remoteHeldInstance != null)
            {
                _remoteHeldInstance.SetActive(false);
                _remoteHeldInstance.transform.SetParent(null);
                _remoteHeldInstance = null;
            }
            return;
        }

        if (_remoteRightHandBone == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Hand.R" || t.name == "RightHand" || t.name.Contains("Hand_R") || t.name.Contains("RightHand"))
                {
                    _remoteRightHandBone = t;
                    break;
                }
            }
            if (_remoteRightHandBone == null) _remoteRightHandBone = transform;
        }

        GameObject itemObj = NetworkItemSync.FindSceneObjectByPath(itemPath);
        if (itemObj != null)
        {
            _remoteHeldInstance = itemObj;
            _remoteHeldInstance.transform.SetParent(_remoteRightHandBone);
            _remoteHeldInstance.transform.localPosition = Vector3.zero;
            _remoteHeldInstance.transform.localRotation = Quaternion.identity;

            Vector3 lossy = _remoteRightHandBone.lossyScale;
            if (lossy.x != 0 && lossy.y != 0 && lossy.z != 0)
            {
                _remoteHeldInstance.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
            }
            else
            {
                _remoteHeldInstance.transform.localScale = Vector3.one;
            }

            var colliders = _remoteHeldInstance.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = false;

            var renderers = _remoteHeldInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;

            var rb = _remoteHeldInstance.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            _remoteHeldInstance.SetActive(true);
        }
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

            if (_spineLookBone == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Stomach" || t.name == "Spine" || t.name == "Chest")
                    {
                        _spineLookBone = t;
                        break;
                    }
                }
            }

            float targetPitch = Mathf.Clamp(NetworkHeadPitch, -45f, 60f);
            float targetYaw = Mathf.Clamp(NetworkHeadYaw, -75f, 75f);

            _smoothRemotePitch = Mathf.Lerp(_smoothRemotePitch, targetPitch, Time.deltaTime * 12f);
            _smoothRemoteYaw = Mathf.Lerp(_smoothRemoteYaw, targetYaw, Time.deltaTime * 12f);

            if (_spineLookBone != null)
            {
                Quaternion spineRot = Quaternion.AngleAxis(_smoothRemoteYaw * 0.25f, transform.up)
                                    * Quaternion.AngleAxis(-_smoothRemotePitch * 0.25f, transform.right);
                _spineLookBone.rotation = spineRot * _spineLookBone.rotation;
            }

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
    }
}
