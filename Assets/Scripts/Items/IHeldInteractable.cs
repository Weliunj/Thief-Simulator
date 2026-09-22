using StarterAssets;
using UnityEngine;

/// <summary>
/// Interface dành riêng cho các Vật phẩm có thể Tương tác trực tiếp khi đang CẦM TRÊN TAY (Hotbar):
/// - FlashlightController: Bật/Tắt ánh sáng đèn pin
/// - MedicalKit / Drink: Sử dụng hồi phục stamina / máu
/// - Tool: Kích hoạt chức năng đặc biệt của công cụ
/// </summary>
public interface IHeldInteractable
{
    /// <summary>
    /// Chuỗi văn bản hiển thị hành động khi đang cầm (VD: "Turn On", "Turn Off", "Use")
    /// </summary>
    string GetHeldActionPrompt();

    /// <summary>
    /// Icon đại diện cho hành động khi đang cầm (tùy chọn)
    /// </summary>
    Sprite GetHeldActionIcon();

    /// <summary>
    /// Kiểm tra xem item có đang ở trạng thái có thể tương tác trên tay không
    /// </summary>
    bool CanInteractWhileHeld();

    /// <summary>
    /// Thực hiện hành vi tương tác khi bấm phím F hoặc nút Interact trên Mobile
    /// </summary>
    void OnHeldInteract(PlayerController player);
}
