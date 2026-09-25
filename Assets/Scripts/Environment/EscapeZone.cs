using StarterAssets;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý khu vực tẩu thoát (Escape Zone / Exit Van):
/// - Hiển thị 3D Billboard Text điểm số (Current Point / Target Point) lơ lửng trên khu vực và luôn xoay hướng về Camera.
/// - Đổi màu text (Vàng khi chưa đủ, Xanh lá kèm thông báo READY khi đã đạt chỉ tiêu).
/// - Phát hiện khi Local Player bước vào vùng thoát: nếu đã đạt chỉ tiêu, hiển thị nút Escape trên MainHUD để người chơi bấm về sảnh.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EscapeZone : MonoBehaviour
{
    [Header("📍 3D Point Display")]
    [Tooltip("Transform điểm neo hiển thị Text (nếu null sẽ tự tạo phía trên vị trí này)")]
    public Transform pointDisplayAnchor;
    public Vector3 displayOffset = new Vector3(0f, 2.2f, 0f);

    [Tooltip("TextMeshPro 3D hiển thị điểm")]
    public TextMeshPro pointText3D;

    [Header("🎨 Visual Colors")]
    public Color incompletedColor = new Color(1f, 0.85f, 0.2f); // Vàng sáng
    public Color completedColor = new Color(0.2f, 1f, 0.35f);    // Xanh lá sáng

    private UI_Manager _uiManager;
    private bool _isLocalPlayerInside = false;

    private void Awake()
    {
        // Đảm bảo Collider là Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Ensure3DText();
    }

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UI_Manager>(FindObjectsInactive.Include);
    }

    /// <summary>
    /// Tự động khởi tạo TextMeshPro 3D nếu chưa có
    /// </summary>
    private void Ensure3DText()
    {
        if (pointText3D == null)
        {
            pointText3D = GetComponentInChildren<TextMeshPro>(true);
        }

        if (pointText3D == null)
        {
            GameObject textObj = new GameObject("EscapePointText_3D");
            textObj.transform.SetParent(pointDisplayAnchor != null ? pointDisplayAnchor : transform, false);
            textObj.transform.localPosition = displayOffset;

            pointText3D = textObj.AddComponent<TextMeshPro>();
            pointText3D.alignment = TextAlignmentOptions.Center;
            pointText3D.fontSize = 6f;
            pointText3D.fontStyle = FontStyles.Bold;
            pointText3D.color = incompletedColor;
            pointText3D.text = "POINTS: $0 / $0";
        }
    }

    private void LateUpdate()
    {
        // 1. Billboard Effect: Text luôn xoay mặt hướng thẳng về Camera người chơi
        Camera mainCam = Camera.main;
        if (mainCam != null && pointText3D != null)
        {
            pointText3D.transform.rotation = mainCam.transform.rotation;
        }

        // 2. Cập nhật thông số điểm số hiển thị
        UpdatePointText();

        // 3. Nếu Player đang đứng trong vùng thoát, cập nhật trạng thái nút Escape theo điểm
        if (_isLocalPlayerInside && _uiManager != null && _uiManager.mainHUD != null)
        {
            bool canEscape = _uiManager.playerStats != null &&
                             _uiManager.playerStats.totalpoint > 0 &&
                             _uiManager.playerStats.currpoint >= _uiManager.playerStats.totalpoint;

            _uiManager.mainHUD.SetEscapeButtonActive(canEscape);
        }
    }

    private void UpdatePointText()
    {
        if (pointText3D == null) return;

        int curr = 0;
        int total = 0;

        if (_uiManager == null) _uiManager = FindFirstObjectByType<UI_Manager>(FindObjectsInactive.Include);

        if (_uiManager != null && _uiManager.playerStats != null)
        {
            curr = _uiManager.playerStats.currpoint;
            if (_uiManager.playerStats.totalpoint <= 0 && SceneItemSpawner.LastCalculatedTargetPoint > 0)
            {
                _uiManager.playerStats.totalpoint = SceneItemSpawner.LastCalculatedTargetPoint;
            }
            total = _uiManager.playerStats.totalpoint;
        }
        else if (SceneItemSpawner.LastCalculatedTargetPoint > 0)
        {
            total = SceneItemSpawner.LastCalculatedTargetPoint;
        }

        bool isReady = total > 0 && curr >= total;

        if (isReady)
        {
            pointText3D.color = completedColor;
            pointText3D.text = $"<size=120%><b>${curr} / ${total}</b></size>\n<color=#00FF66><size=80%>★ READY TO ESCAPE ★</size></color>";
        }
        else
        {
            pointText3D.color = incompletedColor;
            pointText3D.text = $"<size=110%><b>${curr} / ${total}</b></size>\n<color=#FFAA00><size=75%>ESCAPE ZONE</size></color>";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        PlayerController pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            var netSync = pc.GetComponent<NetworkPlayerSync>();
            bool isLocal = netSync == null || netSync.IsLocalPlayer;
            if (isLocal)
            {
                _isLocalPlayerInside = true;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;

        PlayerController pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            var netSync = pc.GetComponent<NetworkPlayerSync>();
            bool isLocal = netSync == null || netSync.IsLocalPlayer;
            if (isLocal)
            {
                _isLocalPlayerInside = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        PlayerController pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            var netSync = pc.GetComponent<NetworkPlayerSync>();
            bool isLocal = netSync == null || netSync.IsLocalPlayer;
            if (isLocal)
            {
                _isLocalPlayerInside = false;
                if (_uiManager != null && _uiManager.mainHUD != null)
                {
                    _uiManager.mainHUD.SetEscapeButtonActive(false);
                }
            }
        }
    }
}
