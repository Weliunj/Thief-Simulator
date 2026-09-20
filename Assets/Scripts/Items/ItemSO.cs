using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Thief Simulator/Item Data", order = 2)]
public class ItemSO : ScriptableObject
{
    [Header("📌 Basic Information")]
    [Tooltip("Tên vật phẩm")]
    public string itemName = "Valuable Item";

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
