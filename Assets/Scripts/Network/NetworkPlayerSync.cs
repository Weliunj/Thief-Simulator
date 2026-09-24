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

            SetNameTagVisible(showForLocalPlayer);
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

        if (HasInputAuthority)
        {
            // =========================================================================
            //                         LOCAL PLAYER SETUP
            // =========================================================================
            Debug.Log("<color=green>[NetworkPlayerSync] Thiết lập Local Player (HasInputAuthority = true).</color>");

            // 1. Gửi tên, giới tính và nhân vật đã chọn lên mạng
            string myName = "Player";
            if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null)
            {
                myName = FirebaseDataService.Instance.CurrentUserProfile.username;
            }
            NetworkPlayerName = myName;

            int savedCharIndex = PlayerPrefs.GetInt("SelectedCharIndex", 0);
            int savedGender = PlayerPrefs.GetInt("SelectedGender", 0); // 0 = Male, 1 = Female

            CharacterSkinIndex = savedCharIndex;
            NetworkIsMale = (savedGender == 0);

            // Bảng tên của chính mình
            if (nameTagText != null)
            {
                nameTagText.text = myName;
            }
            SetNameTagVisible(showForLocalPlayer);

            // Cập nhật skin cho chính mình
            ApplySkin(savedCharIndex, savedGender == 0);

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

            // 3. Cập nhật Skin & Mesh ngoại hình người chơi khác
            ApplySkin(CharacterSkinIndex, NetworkIsMale);
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

    private void OnPlayerNameChanged()
    {
        if (nameTagText != null)
        {
            nameTagText.text = NetworkPlayerName.ToString();
        }
        SetNameTagVisible(!HasInputAuthority || showForLocalPlayer);
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
        if (characterSkinnedMesh != null && characterDatabase != null && skinIndex >= 0 && skinIndex < characterDatabase.Count)
        {
            PlayerSO so = characterDatabase[skinIndex];
            if (so != null)
            {
                Mesh targetMesh = so.GetMesh(isMale);
                if (targetMesh != null)
                {
                    characterSkinnedMesh.sharedMesh = targetMesh;
                }

                if (so.characterMaterial != null)
                {
                    characterSkinnedMesh.material = so.characterMaterial;
                }
                else if (so.characterTexture != null)
                {
                    characterSkinnedMesh.material.mainTexture = so.characterTexture;
                }
            }
        }
    }

    private void LateUpdate()
    {
        // Hiệu ứng Billboard: Bảng tên (hoặc LocalCanvas) luôn quay mặt đối diện trực tiếp Camera
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
