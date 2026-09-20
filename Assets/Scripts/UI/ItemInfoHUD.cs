using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý UI HUD hiển thị thông tin đối tượng (Tên, Giá trị, Cân nặng, Phím tương tác)
/// khi tâm ngắm của người chơi rọi trúng đối tượng tương tác.
/// </summary>
public class ItemInfoHUD : MonoBehaviour
{
    [Header("🖼️ UI Elements")]
    [Tooltip("Panel gốc chứa toàn bộ UI HUD thông tin")]
    public GameObject hudPanel;

    [Tooltip("Icon hoặc nút bấm tương tác (nếu có - chỉ hiện khi rọi trúng item)")]
    public GameObject interactIcon;

    [Tooltip("Text gộp chung đơn giản nếu không muốn chia nhiều text box (VD: '[E] Laptop ($150 - 4Kg)')")]
    public TextMeshProUGUI singleCombinedText;

    [Tooltip("Text hiển thị tên đối tượng")]
    public TextMeshProUGUI nameText;

    [Tooltip("Text hiển thị giá trị ($)")]
    public TextMeshProUGUI priceText;

    [Tooltip("Text hiển thị khối lượng (Kg)")]
    public TextMeshProUGUI weightText;

    [Tooltip("Text cảnh báo nếu không thể tương tác (VD: Quá tải, Cửa đã mở...)")]
    public TextMeshProUGUI warningText;

    [Header("🎨 Styling Colors")]
    public Color priceColor = new Color(0.2f, 0.9f, 0.3f); // Xanh lá cây
    public Color weightColor = new Color(0.9f, 0.8f, 0.2f); // Vàng
    public Color warningColor = new Color(0.95f, 0.25f, 0.25f); // Đỏ

    [Header("🎯 Crosshair Settings (Tâm ngắm)")]
    [Tooltip("Image tâm ngắm ở giữa màn hình")]
    public Image crosshairImage;

    [Tooltip("Độ mờ khi không nhìn vào item (0 = tàng hình, 1 = rõ hoàn toàn)")]
    [Range(0f, 1f)]
    public float idleAlpha = 0.35f;

    [Tooltip("Độ rõ khi nhìn vào item")]
    [Range(0f, 1f)]
    public float targetAlpha = 1.0f;

    [Tooltip("Scale của tâm khi không nhìn vào item")]
    public float idleScale = 0.85f;

    [Tooltip("Scale của tâm khi nhìn trúng item")]
    public float targetScale = 1.35f;

    [Tooltip("Tốc độ chuyển đổi mượt mà")]
    public float animSpeed = 12f;

    [Tooltip("Màu của tâm khi rọi trúng item")]
    public Color targetCrosshairColor = Color.white;

    private CanvasGroup canvasGroup;
    private Color defaultCrosshairColor = Color.white;
    private bool isHoveringItem = false;

    void Awake()
    {
        if (hudPanel == null)
        {
            hudPanel = gameObject;
        }

        canvasGroup = hudPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (crosshairImage != null)
        {
            // Tắt raycastTarget để tâm không bao giờ chặn click chuột / vuốt cảm ứng
            crosshairImage.raycastTarget = false;
            defaultCrosshairColor = crosshairImage.color;
            crosshairImage.transform.localScale = Vector3.one * idleScale;
            crosshairImage.color = new Color(defaultCrosshairColor.r, defaultCrosshairColor.g, defaultCrosshairColor.b, idleAlpha);
        }

        // Tự động tìm TextMeshProUGUI nếu chưa được gán trong Inspector
        if (singleCombinedText == null && nameText == null)
        {
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length == 1)
            {
                singleCombinedText = tmps[0];
            }
            else if (tmps.Length > 1)
            {
                foreach (var t in tmps)
                {
                    string low = t.gameObject.name.ToLower();
                    if (low.Contains("name") && nameText == null) nameText = t;
                    else if (low.Contains("price") && priceText == null) priceText = t;
                    else if ((low.Contains("weight") || low.Contains("kg")) && weightText == null) weightText = t;
                    else if ((low.Contains("warn") || low.Contains("fail")) && warningText == null) warningText = t;
                }
            }
        }

        Hide();
    }

    void Start()
    {
        Hide();
    }

    void Update()
    {
        // Hiệu ứng phóng to / thu nhỏ và làm mờ / làm rõ tâm ngắm mượt mà
        if (crosshairImage != null)
        {
            float targetA = isHoveringItem ? targetAlpha : idleAlpha;
            float targetS = isHoveringItem ? targetScale : idleScale;
            Color baseCol = isHoveringItem ? targetCrosshairColor : defaultCrosshairColor;

            // 1. Phóng to / Thu nhỏ scale
            crosshairImage.transform.localScale = Vector3.Lerp(
                crosshairImage.transform.localScale,
                Vector3.one * targetS,
                Time.deltaTime * animSpeed
            );

            // 2. Làm rõ / Làm mờ alpha
            Color curCol = crosshairImage.color;
            Color desiredCol = new Color(baseCol.r, baseCol.g, baseCol.b, targetA);
            crosshairImage.color = Color.Lerp(curCol, desiredCol, Time.deltaTime * animSpeed);
        }
    }

    /// <summary>
    /// Hiển thị thông tin tương tác của đối tượng
    /// </summary>
    public void ShowInteractable(IInteractable interactable, bool canInteract, string failReason)
    {
        if (interactable == null)
        {
            Hide();
            return;
        }

        isHoveringItem = true;

        if (hudPanel != null && !hudPanel.activeSelf)
        {
            hudPanel.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (interactIcon != null && !interactIcon.activeSelf)
        {
            interactIcon.SetActive(true);
        }

        // 1. Tên đối tượng & Combined Text
        if (singleCombinedText != null)
        {
            if (interactable.IsLootItem())
            {
                if (canInteract)
                    singleCombinedText.text = $"{interactable.GetInteractableName()} (${interactable.GetPrice()} - {interactable.GetWeight()}Kg)";
                else
                    singleCombinedText.text = $"{interactable.GetInteractableName()} ({failReason})";
            }
            else
            {
                if (canInteract)
                    singleCombinedText.text = $"{interactable.GetInteractableName()}";
                else
                    singleCombinedText.text = $"{interactable.GetInteractableName()} ({failReason})";
            }
        }

        if (nameText != null)
        {
            nameText.text = interactable.GetInteractableName();
        }

        // 2. Nếu là vật phẩm (Loot Item) thì hiển thị Giá tiền & Cân nặng
        if (interactable.IsLootItem())
        {
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = $"${interactable.GetPrice()}";
                priceText.color = priceColor;
            }

            if (weightText != null)
            {
                weightText.gameObject.SetActive(true);
                weightText.text = $"{interactable.GetWeight()} Kg";
                weightText.color = weightColor;
            }
        }
        else
        {
            // Cửa, thang... không hiển thị giá tiền & cân nặng
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (weightText != null) weightText.gameObject.SetActive(false);
        }

        // 3. Cảnh báo (nếu quá tải hoặc không thể tương tác)
        if (canInteract)
        {
            if (warningText != null) warningText.gameObject.SetActive(false);
        }
        else
        {
            if (warningText != null)
            {
                if (!string.IsNullOrEmpty(failReason))
                {
                    warningText.gameObject.SetActive(true);
                    warningText.text = failReason;
                    warningText.color = warningColor;
                }
                else
                {
                    warningText.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Ẩn HUD khi không nhìn vào đối tượng nào
    /// </summary>
    public void Hide()
    {
        isHoveringItem = false;

        if (hudPanel != null && hudPanel.activeSelf)
        {
            hudPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (interactIcon != null && interactIcon.activeSelf)
        {
            interactIcon.SetActive(false);
        }
    }
}
