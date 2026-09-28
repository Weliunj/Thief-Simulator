using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Quản lý giao diện UI nút bấm thoát khỏi tủ khi đang trốn.
/// Tự động hiển thị khi vào tủ và ẩn khi ra ngoài.
/// Hỗ trợ bấm trực tiếp trên màn hình (Mobile/Mouse) hoặc nhấn phím Space / E / F trên bàn phím.
/// </summary>
public class HidingExitHUD : MonoBehaviour
{
    public static HidingExitHUD Instance { get; private set; }

    [Header("🎯 UI References")]
    [Tooltip("Panel hoặc Root GameObject chứa nút thoát")]
    public GameObject hudRoot;

    [Tooltip("Nút bấm thoát khỏi tủ")]
    public Button exitButton;

    [Tooltip("Text hướng dẫn (VD: 'Rời khỏi tủ [E / Space]')")]
    public TextMeshProUGUI promptText;

    [Header("⚙️ Settings")]
    public string defaultPrompt = "RỜI KHỎI TỦ (E / SPACE)";

    private HidingSpotController currentSpot;
    private PlayerController currentPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (exitButton == null)
        {
            exitButton = GetComponentInChildren<Button>(true);
        }

        // ⭐ KHÔNG dùng gameObject làm hudRoot để tránh tắt toàn bộ script
        if (hudRoot == null && exitButton != null)
        {
            hudRoot = exitButton.transform.parent != null
                ? exitButton.transform.parent.gameObject
                : exitButton.gameObject;
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        // ⭐ KHÔNG gọi Hide() trong Awake làm tắt gameObject, chỉ ẩn panel con
        if (hudRoot != null && hudRoot != gameObject)
        {
            hudRoot.SetActive(false);
        }
        else if (exitButton != null && exitButton.gameObject != gameObject)
        {
            exitButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Log mỗi 60 frame để không spam
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[HidingExitHUD.Update] active={gameObject.activeInHierarchy}, " +
                      $"spot={(currentSpot != null ? currentSpot.name : "null")}, " +
                      $"player={(currentPlayer != null ? currentPlayer.name : "null")}");
        }

        // Hỗ trợ bấm phím nóng trên bàn phím (E, Space, F) để thoát nhanh
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.fKey.wasPressedThisFrame)
            {
                OnExitButtonClicked();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
        {
            OnExitButtonClicked();
        }
#endif
    }

    /// <summary>
    /// Hiển thị UI nút thoát khi Player chui vào tủ.
    /// </summary>
    public void Show(HidingSpotController spot, PlayerController player)
    {
        Debug.Log($"[HidingExitHUD.Show] spot={spot?.name}, player={player?.name}");
        currentSpot = spot;
        currentPlayer = player;

        if (promptText != null)
        {
            promptText.text = defaultPrompt;
        }

        // ⭐ Đảm bảo GameObject chứa script luôn active
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // Chỉ bật panel con
        if (hudRoot != null && hudRoot != gameObject)
        {
            hudRoot.SetActive(true);
        }
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Ẩn UI nút thoát khi Player đã ra ngoài.
    /// </summary>
    public void Hide()
    {
        currentSpot = null;
        currentPlayer = null;

        // ⭐ Chỉ tắt panel con, KHÔNG tắt gameObject chứa script
        if (hudRoot != null && hudRoot != gameObject)
        {
            hudRoot.SetActive(false);
        }
        if (exitButton != null && exitButton.gameObject != hudRoot && exitButton.gameObject != gameObject)
        {
            exitButton.gameObject.SetActive(false);
        }
    }

    private float _lastClickTime = -10f;

    public void OnExitButtonClicked()
    {
        // ⭐ Chống double-click
        if (Time.unscaledTime - _lastClickTime < 0.3f) return;
        _lastClickTime = Time.unscaledTime;

        Debug.Log($"[HUD] Click! Instance={Instance != null}, spot={currentSpot}, player={currentPlayer}");
        var spot = currentSpot;
        var player = currentPlayer;

        // Dự phòng: Tìm Local Player và Tủ đang trốn nếu tham chiếu bị mất
        if (spot == null || player == null)
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].isHiding)
                {
                    player = players[i];
                    spot = players[i].currentHidingSpot;
                    break;
                }
            }
        }

        if (spot != null)
        {
            spot.RequestExit(player);
        }
        else if (player != null)
        {
            // Thoát cục bộ khẩn cấp
            player.isHiding = false;
        }

        Hide();
    }
}
