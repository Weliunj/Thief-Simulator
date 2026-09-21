using UnityEngine;

/// <summary>
/// Bảng phân loại độ hiếm của vật phẩm
/// </summary>
public enum ItemRarity
{
    Trash,      // Rác / Phế liệu (Vỏ lon, báo cũ, chai lọ rỗng...)
    Common,     // Phổ thông (Cốc, bát, sách, tranh nhỏ...)
    Uncommon,   // Khá (Ấm siêu tốc, máy sấy, quạt mini...)
    Rare,       // Hiếm (Điện thoại, máy tính bảng, loa bluetooth...)
    Epic,       // Cực hiếm (Laptop gaming, máy ảnh xịn, trang sức bạc...)
    Legendary,  // Huyền thoại (Trang sức vàng, kim cương, đồng hồ Rolex...)
    Mythic      // Thần thoại (Cổ vật vô giá, bảo vật quốc gia...)
}

public static class ItemRarityExtensions
{
    /// <summary>
    /// Lấy mã màu Color đại diện cho độ hiếm
    /// </summary>
    public static Color GetColor(this ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Trash: return new Color(0.6f, 0.6f, 0.6f);      // Xám
            case ItemRarity.Common: return Color.white;                      // Trắng
            case ItemRarity.Uncommon: return new Color(0.2f, 0.85f, 0.3f);   // Xanh lá
            case ItemRarity.Rare: return new Color(0.2f, 0.6f, 1f);          // Xanh dương
            case ItemRarity.Epic: return new Color(0.68f, 0.25f, 0.95f);    // Tím
            case ItemRarity.Legendary: return new Color(1f, 0.6f, 0.05f);    // Vàng cam
            case ItemRarity.Mythic: return new Color(1f, 0.1f, 0.35f);       // Đỏ hồng rực rỡ
            default: return Color.white;
        }
    }

    /// <summary>
    /// Lấy tên hiển thị độ hiếm (VD: "Common", "Rare", "Mythic")
    /// </summary>
    public static string GetDisplayName(this ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Trash: return "Trash";
            case ItemRarity.Common: return "Common";
            case ItemRarity.Uncommon: return "Uncommon";
            case ItemRarity.Rare: return "Rare";
            case ItemRarity.Epic: return "Epic";
            case ItemRarity.Legendary: return "Legendary";
            case ItemRarity.Mythic: return "Mythic";
            default: return rarity.ToString();
        }
    }
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Thief Simulator/Item Data", order = 2)]
public class ItemSO : ScriptableObject
{
    [Header("📌 Basic Information")]
    [Tooltip("Tên vật phẩm")]
    public string itemName = "Valuable Item";

    [Tooltip("Độ hiếm của vật phẩm")]
    public ItemRarity rarity = ItemRarity.Common;

    [Tooltip("Hình ảnh / Icon của vật phẩm")]
    public Sprite itemIcon;

    [TextArea(2, 5)]
    [Tooltip("Mô tả chi tiết về vật phẩm")]
    public string description = "A valuable item that can be stolen and sold for points.";

    [Header("💰 Value Settings ($)")]
    [Tooltip("Giá trị cơ bản gốc")]
    public float basePrice = 100f;
    [Tooltip("Giá trị tối thiểu khi random (nếu = 0 sẽ tự tính từ basePrice)")]
    public float minPrice = 80f;
    [Tooltip("Giá trị tối đa khi random (nếu = 0 sẽ tự tính từ basePrice)")]
    public float maxPrice = 120f;

    [Header("⚖️ Weight Settings (Kg)")]
    [Tooltip("Khối lượng cơ bản gốc")]
    public int baseKg = 5;
    [Tooltip("Khối lượng tối thiểu khi random")]
    public int minKg = 3;
    [Tooltip("Khối lượng tối đa khi random")]
    public int maxKg = 7;

    /// <summary>
    /// Hàm sinh ngẫu nhiên Giá ($) và Cân nặng (Kg) mỗi khi màn chơi bắt đầu
    /// </summary>
    public (float price, int kg) GenerateRandomStats()
    {
        // 1. Tính toán khoảng giá trị Price
        float actualMinPrice = (minPrice > 0) ? minPrice : basePrice * 0.8f;
        float actualMaxPrice = (maxPrice > 0 && maxPrice >= actualMinPrice) ? maxPrice : basePrice * 1.2f;
        float randomPrice = Mathf.Round(Random.Range(actualMinPrice, actualMaxPrice));

        // 2. Tính toán khoảng cân nặng Kg
        int actualMinKg = (minKg > 0) ? minKg : Mathf.Max(1, baseKg - 2);
        int actualMaxKg = (maxKg >= actualMinKg) ? maxKg : baseKg + 2;
        int randomKg = Random.Range(actualMinKg, actualMaxKg + 1);

        return (randomPrice, randomKg);
    }
}
