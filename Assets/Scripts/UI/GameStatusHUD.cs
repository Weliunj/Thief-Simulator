using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý thông báo trạng thái nổi trên màn hình (Status Banner / Feed Notification):
/// - Hiển thị các sự kiện quan trọng trong trận (VD: "Player1 got caught by Guard!", "Player2 escaped!")
/// - Tự động tạo TextMeshProUGUI và Panel nền đẹp mắt nếu chưa có sẵn trong Canvas
/// - Hỗ trợ gọi từ cả Cục bộ lẫn RPC Mạng Photon Fusion
/// </summary>
public class GameStatusHUD : MonoBehaviour
{
    public static GameStatusHUD Instance { get; private set; }

    [Header("📢 UI References")]
    public GameObject bannerPanel;
    public TextMeshProUGUI statusText;
    public Image bannerBackground;

    [Header("🎨 Style & Animation")]
    public Color defaultTextColor = new Color(1f, 0.25f, 0.25f, 1f); // Màu đỏ cam cảnh báo
    public Color defaultBgColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
    public float defaultDuration = 4.0f;

    private Coroutine hideCoroutine;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        EnsureUIElements();
    }

    private void Start()
    {
        EnsureUIElements();
        HideImmediate();
    }

    private void EnsureUIElements()
    {
        if (bannerPanel == null)
        {
            // Tìm hoặc tự tạo Banner GameObject trên Canvas
            Transform existing = transform.Find("StatusBannerPanel");
            if (existing != null)
            {
                bannerPanel = existing.gameObject;
            }
            else
            {
                bannerPanel = new GameObject("StatusBannerPanel");
                bannerPanel.transform.SetParent(transform, false);

                RectTransform rect = bannerPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -80f);
                rect.sizeDelta = new Vector2(600f, 60f);

                bannerBackground = bannerPanel.AddComponent<Image>();
                bannerBackground.color = defaultBgColor;

                // Bo tròn hoặc padding nếu có thể
                var outline = bannerPanel.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.2f, 0.2f, 0.6f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        if (canvasGroup == null && bannerPanel != null)
        {
            canvasGroup = bannerPanel.GetComponent<CanvasGroup>() ?? bannerPanel.AddComponent<CanvasGroup>();
        }

        if (statusText == null && bannerPanel != null)
        {
            statusText = bannerPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (statusText == null)
            {
                GameObject textObj = new GameObject("StatusText");
                textObj.transform.SetParent(bannerPanel.transform, false);

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.offsetMin = new Vector2(20f, 5f);
                textRect.offsetMax = new Vector2(-20f, -5f);

                statusText = textObj.AddComponent<TextMeshProUGUI>();
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.fontSize = 24;
                statusText.fontStyle = FontStyles.Bold;
                statusText.color = defaultTextColor;
                statusText.enableWordWrapping = true;
            }
        }
    }

    /// <summary>
    /// Hiển thị thông báo trạng thái nổi trên màn hình
    /// </summary>
    public static void Show(string message, float duration = 4f, Color? textColor = null)
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<GameStatusHUD>(FindObjectsInactive.Include);
            if (Instance == null)
            {
                // Tự động tìm Canvas và tạo GameStatusHUD
                Canvas mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    GameObject hudObj = new GameObject("GameStatusHUD");
                    hudObj.transform.SetParent(mainCanvas.transform, false);
                    Instance = hudObj.AddComponent<GameStatusHUD>();
                }
            }
        }

        if (Instance != null)
        {
            Instance.DisplayMessage(message, duration, textColor);
        }
        else
        {
            Debug.Log($"<color=red>[STATUS MESSAGE] {message}</color>");
        }
    }

    public void DisplayMessage(string message, float duration, Color? textColor)
    {
        EnsureUIElements();

        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = textColor ?? defaultTextColor;
        }

        if (bannerPanel != null)
        {
            bannerPanel.SetActive(true);
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(AnimateBannerRoutine(duration));
    }

    private IEnumerator AnimateBannerRoutine(float duration)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float fadeIn = 0f;
            while (fadeIn < 0.25f)
            {
                fadeIn += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(fadeIn / 0.25f);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(duration);

        if (canvasGroup != null)
        {
            float fadeOut = 0f;
            while (fadeOut < 0.5f)
            {
                fadeOut += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(fadeOut / 0.5f);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        HideImmediate();
    }

    private void HideImmediate()
    {
        if (bannerPanel != null)
        {
            bannerPanel.SetActive(false);
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }
}
