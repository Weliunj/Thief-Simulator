using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý UI HUD hiển thị thông tin đối tượng (Tên, Mô tả, Giá trị, Cân nặng, Phím tương tác)
/// và tâm ngắm Crosshair (hiệu ứng phóng to / làm rõ khi rọi trúng item và thu nhỏ / làm mờ khi ở trạng thái idle).
/// </summary>
public class ItemInfoHUD : MonoBehaviour
{
    [Header("🖼️ Info Content Panel")]
    [Tooltip("Panel chứa nội dung chữ (Tên, Giá, Cân nặng, Mô tả). Sẽ ẩn khi không nhìn vào item")]
    public GameObject infoContentPanel;

    [Tooltip("Icon hoặc nút bấm tương tác (chỉ hiện khi rọi trúng item)")]
    public GameObject interactIcon;

    [Tooltip("Text gộp chung đơn giản (VD: '[E] Laptop ($150 - 4Kg)')")]
    public TextMeshProUGUI singleCombinedText;

    [Tooltip("Text hiển thị tên đối tượng")]
    public TextMeshProUGUI nameText;

    [Tooltip("Text hiển thị hành động tương tác (VD: Pick Up, Climb Up, Pick Lock, Turn On)")]
    public TextMeshProUGUI actionPromptText;

    [Tooltip("Text hiển thị mô tả chi tiết")]
    public TextMeshProUGUI descriptionText;

    [Tooltip("Text hiển thị giá trị ($)")]
    public TextMeshProUGUI priceText;

    [Tooltip("Text hiển thị khối lượng (Kg)")]
    public TextMeshProUGUI weightText;

    [Tooltip("Text hiển thị độ hiếm của vật phẩm")]
    public TextMeshProUGUI rarityText;

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

    private CanvasGroup infoCanvasGroup;
    private Color defaultCrosshairColor = Color.white;
    private bool isHoveringItem = false;

    void Awake()
    {
        // Tự động tìm Crosshair Image nếu chưa gán
        if (crosshairImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("crosshair"))
                {
                    crosshairImage = img;
                    break;
                }
            }
        }

        if (crosshairImage != null)
        {
            crosshairImage.raycastTarget = false;
            defaultCrosshairColor = crosshairImage.color;
            crosshairImage.transform.localScale = Vector3.one * idleScale;
            crosshairImage.color = new Color(defaultCrosshairColor.r, defaultCrosshairColor.g, defaultCrosshairColor.b, idleAlpha);
            crosshairImage.gameObject.SetActive(true);
        }

        // Tự động tìm infoContentPanel nếu chưa gán
        if (infoContentPanel == null)
        {
            Transform infoChild = transform.Find("InfoPanel") ?? transform.Find("Content") ?? transform.Find("Panel");
            if (infoChild != null)
            {
                infoContentPanel = infoChild.gameObject;
            }
            else
            {
                // Nếu không có panel con riêng biệt, dùng CanvasGroup để ẩn hiện nội dung mà không tắt cả GameObject HUD
                infoCanvasGroup = GetComponent<CanvasGroup>();
                if (infoCanvasGroup == null) infoCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        else
        {
            infoCanvasGroup = infoContentPanel.GetComponent<CanvasGroup>();
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
                    else if ((low.Contains("prompt") || low.Contains("action")) && actionPromptText == null) actionPromptText = t;
                    else if ((low.Contains("desc") || low.Contains("detail")) && descriptionText == null) descriptionText = t;
                    else if (low.Contains("price") && priceText == null) priceText = t;
                    else if ((low.Contains("weight") || low.Contains("kg")) && weightText == null) weightText = t;
                    else if (low.Contains("rarity") && rarityText == null) rarityText = t;
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

        // Bật hiển thị panel thông tin
        if (infoContentPanel != null)
        {
            infoContentPanel.SetActive(true);
        }

        if (infoCanvasGroup != null)
        {
            infoCanvasGroup.alpha = 1f;
            infoCanvasGroup.interactable = true;
            infoCanvasGroup.blocksRaycasts = true;
        }

        if (interactIcon != null)
        {
            interactIcon.SetActive(true);
        }

        bool hasPriceOrWeight = (interactable.GetPrice() > 0 || interactable.GetWeight() > 0);

        // 1. Tên đối tượng & Combined Text
        string actionPrompt = interactable.GetActionPrompt();
        if (singleCombinedText != null)
        {
            singleCombinedText.gameObject.SetActive(true);
            string keyPrompt = interactable.IsLootItem() ? "[E]" : "[F]";
            if (hasPriceOrWeight)
            {
                singleCombinedText.text = $"{keyPrompt} {actionPrompt} - {interactable.GetInteractableName()} (${interactable.GetPrice()} - {interactable.GetWeight()}Kg)";
            }
            else
            {
                singleCombinedText.text = $"{keyPrompt} {actionPrompt} ({interactable.GetInteractableName()})";
            }
        }

        if (nameText != null)
        {
            nameText.gameObject.SetActive(true);
            nameText.text = interactable.GetInteractableName();
        }

        if (actionPromptText != null)
        {
            actionPromptText.gameObject.SetActive(true);
            actionPromptText.text = actionPrompt;
        }

        // Mô tả chi tiết (Description)
        if (descriptionText != null)
        {
            string desc = interactable.GetDescription();
            if (!string.IsNullOrEmpty(desc))
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;
            }
            else
            {
                descriptionText.gameObject.SetActive(false);
            }
        }

        // 2. Nếu là vật phẩm (Loot Item) hoặc có giá tiền / cân nặng thì hiển thị Giá tiền, Cân nặng & Độ hiếm
        if (interactable.IsLootItem() || hasPriceOrWeight)
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

            if (rarityText != null)
            {
                ItemRarity r = interactable.GetRarity();
                rarityText.gameObject.SetActive(true);
                rarityText.text = r.GetDisplayName();
                rarityText.color = r.GetColor();
            }
        }
        else
        {
            // Cửa, switch... không hiển thị giá tiền, cân nặng & độ hiếm
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (weightText != null) weightText.gameObject.SetActive(false);
            if (rarityText != null) rarityText.gameObject.SetActive(false);
        }
    }

    private Coroutine warningCoroutine;

    /// <summary>
    /// Hiển thị thông báo cảnh báo tạm thời khi bấm nút tương tác thất bại (Hotbar đầy, quá tải...)
    /// </summary>
    public void ShowWarning(string message, float duration = 2.0f)
    {
        if (warningText == null || string.IsNullOrEmpty(message)) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(WarningRoutine(message, duration));
    }

    private System.Collections.IEnumerator WarningRoutine(string message, float duration)
    {
        warningText.gameObject.SetActive(true);
        warningText.text = message;
        warningText.color = warningColor;

        yield return new WaitForSeconds(duration);

        warningText.gameObject.SetActive(false);
        warningCoroutine = null;
    }

    /// <summary>
    /// Ẩn nội dung HUD khi không nhìn vào đối tượng nào (Tâm ngắm vẫn giữ và quay về trạng thái idle)
    /// </summary>
    public void Hide()
    {
        isHoveringItem = false;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        if (infoContentPanel != null)
        {
            infoContentPanel.SetActive(false);
        }

        if (infoCanvasGroup != null)
        {
            infoCanvasGroup.alpha = 0f;
            infoCanvasGroup.interactable = false;
            infoCanvasGroup.blocksRaycasts = false;
        }

        if (interactIcon != null)
        {
            interactIcon.SetActive(false);
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(false);
        }

        if (rarityText != null)
        {
            rarityText.gameObject.SetActive(false);
        }

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }

        if (singleCombinedText != null)
        {
            singleCombinedText.gameObject.SetActive(false);
        }

        if (actionPromptText != null)
        {
            actionPromptText.gameObject.SetActive(false);
        }
    }
}
