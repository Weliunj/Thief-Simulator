using StarterAssets;
using UnityEngine;

/// <summary>
/// Giao diện chuẩn cho tất cả các đối tượng có thể tương tác (Vật phẩm, Cửa, Thang, v.v.)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Tên của đối tượng hiển thị trên UI HUD (VD: "Laptop", "Locked Door", "Wooden Ladder")
    /// </summary>
    string GetInteractableName();

    /// <summary>
    /// Gợi ý hành động (VD: "Pick Up", "Pick Lock", "Climb")
    /// </summary>
    string GetActionPrompt();

    /// <summary>
    /// Giá trị tiền tệ ($). Trả về 0 nếu không phải vật phẩm nhặt
    /// </summary>
    int GetPrice();

    /// <summary>
    /// Khối lượng (Kg). Trả về 0 nếu không phải vật phẩm nhặt
    /// </summary>
    int GetWeight();

    /// <summary>
    /// True nếu là vật phẩm nhặt vào Balo, False nếu là đối tượng kích hoạt (Cửa/Thang)
    /// </summary>
    bool IsLootItem();

    /// <summary>
    /// Kiểm tra xem người chơi có đủ điều kiện tương tác không (VD: Balo còn chỗ chứa không)
    /// </summary>
    bool CanInteract(PlayerController player, out string failReason);

    /// <summary>
    /// Thực hiện hành động tương tác
    /// </summary>
    void Interact(PlayerController player);
}
