using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đại diện cho 1 Slot trên Hotbar.
/// Quản lý hình ảnh item (itemIcon), nền (bg), trạng thái chọn và dữ liệu item.
/// </summary>
public class HotbarSlot : MonoBehaviour
{
    [Header("🖼️ UI References")]
    [Tooltip("Image nền của slot - sẽ đổi màu vàng khi được chọn")]
    public Image bg;

    [Tooltip("Image hiển thị icon của vật phẩm")]
    public Image itemIcon;

    [Tooltip("Nút bấm chọn slot (nếu có)")]
    public Button slotButton;

    [Header("🎨 Màu sắc")]
    public Color normalBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedBgColor = new Color(1.0f, 0.9f, 0.0f, 0.9f); // Màu vàng nổi bật

    [Header("📦 Dữ liệu Item trong Slot")]
    public Item itemData;
    public GameObject itemObject;
    public int slotIndex = 0;

    private HotbarManager manager;

    void Awake()
    {
        // Tự động tìm kiếm các component con nếu chưa kéo thả vào Inspector
        if (bg == null)
        {
            Transform bgTrans = transform.Find("bg");
            if (bgTrans != null) bg = bgTrans.GetComponent<Image>();
            else bg = GetComponent<Image>();
        }

        if (itemIcon == null)
        {
            Transform iconTrans = transform.Find("itemIcon");
            if (iconTrans != null) itemIcon = iconTrans.GetComponent<Image>();
        }

        if (slotButton == null)
        {
            slotButton = GetComponent<Button>();
            if (slotButton == null)
            {
                slotButton = gameObject.AddComponent<Button>();
            }
        }

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        if (bg != null)
        {
            normalBgColor = bg.color;
        }
    }

    public void Initialize(HotbarManager hotbarManager, int index)
    {
        manager = hotbarManager;
        slotIndex = index;
        if (itemObject == null)
        {
            ClearSlot();
        }
        else
        {
            UpdateVisuals();
        }
    }

    public bool HasItem()
    {
        if (itemObject == null)
        {
            if (itemData != null || (itemIcon != null && itemIcon.gameObject.activeSelf))
            {
                ClearSlot();
            }
            return false;
        }
        return true;
    }

    /// <summary>
    /// Đặt item vào slot
    /// </summary>
    public void SetItem(Item item, GameObject obj)
    {
        itemData = item;
        itemObject = obj;

        if (itemIcon != null)
        {
            Sprite icon = null;
            if (item != null && item.itemData != null && item.itemData.itemIcon != null)
            {
                icon = item.itemData.itemIcon;
            }

            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.color = Color.white;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                // Nếu chưa có sprite icon, ẩn Image icon đi
                itemIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Xóa item khỏi slot
    /// </summary>
    public void ClearSlot()
    {
        itemData = null;
        itemObject = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        SetSelected(false);
    }

    /// <summary>
    /// Đổi màu nền slot khi được chọn / bỏ chọn
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (bg != null)
        {
            bg.color = isSelected ? selectedBgColor : normalBgColor;
        }
    }

    public void UpdateVisuals()
    {
        if (itemObject == null)
        {
            itemData = null;
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.gameObject.SetActive(false);
            }
            SetSelected(false);
        }
        else
        {
            if (itemIcon != null && itemIcon.sprite != null)
            {
                itemIcon.gameObject.SetActive(true);
            }
        }
    }

    private void OnSlotClicked()
    {
        if (manager != null)
        {
            manager.OnSlotClicked(slotIndex);
        }
    }
}
