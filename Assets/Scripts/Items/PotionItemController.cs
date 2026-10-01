using StarterAssets;
using UnityEngine;

/// <summary>
/// Controller tổng quát cho tất cả các loại Thuốc / Potion (Speed I, Speed II, Jump I, Night Vision...):
/// - Triển khai IInteractable (nhặt vào balo khi ở dưới đất)
/// - Triển khai IHeldInteractable (uống / sử dụng trực tiếp khi chọn trên Hotbar)
/// - Tự động xóa khỏi Hotbar & Inventory sau khi uống
/// - Kích hoạt PlayerBuffManager để buff chỉ số & tạo hiệu ứng UI HUD / Overhead Tag
/// - Đồng bộ hiệu ứng và âm thanh uống thuốc 3D qua mạng Photon Fusion (Shared Mode)
/// </summary>
public class PotionItemController : MonoBehaviour, IInteractable, IHeldInteractable
{
    [Header("🧪 Potion Configuration")]
    public BuffType potionType = BuffType.SpeedBoost;

    [Tooltip("Tên hiển thị của thuốc (ví dụ: 'Speed', 'Jump', 'Night Vision')")]
    public string effectName = "Speed";

    [Tooltip("Cấp độ của thuốc (ví dụ: 'I', 'II', 'III', 'MAX')")]
    public string tier = "I";

    [Tooltip("Thời gian tác dụng (giây)")]
    public float duration = 15f;

    [Tooltip("Hệ số nhân chỉ số (ví dụ: 1.3 cho Speed I (+30%), 1.6 cho Speed II (+60%))")]
    public float statMultiplier = 1.3f;

    [Tooltip("Icon của hiệu ứng hiển thị trên HUD và trên đầu nhân vật")]
    public Sprite effectIcon;

    [Header("🔊 Audio & Effects")]
    public AudioClip drinkSound;

    private Item _itemComp;

    private void Awake()
    {
        _itemComp = GetComponent<Item>();
        if (effectIcon == null && _itemComp != null)
        {
            effectIcon = _itemComp.GetIcon();
        }
    }

    // =========================================================================
    //               KHI CẦM TRÊN HOTBAR (IHELDINTERACTABLE)
    // =========================================================================

    public string GetHeldActionPrompt() => "Drink";

    public Sprite GetHeldActionIcon() => effectIcon != null ? effectIcon : (_itemComp != null ? _itemComp.GetIcon() : null);

    public bool CanInteractWhileHeld() => true;

    public void OnHeldInteract(PlayerController player)
    {
        if (player == null) return;

        // 1. Lấy hoặc gắn thêm PlayerBuffManager vào Player
        var buffManager = player.GetComponent<PlayerBuffManager>();
        if (buffManager == null)
        {
            buffManager = player.gameObject.AddComponent<PlayerBuffManager>();
        }

        // 2. Kích hoạt buff lên Player (Logic + HUD + Overhead Tag)
        buffManager.ApplyBuff(potionType, effectName, tier, duration, statMultiplier, effectIcon);

        // 3. Phát âm thanh uống thuốc tại máy mình
        if (drinkSound != null)
        {
            buffManager.PlayDrinkAudio(drinkSound);
        }

        // 4. Đồng bộ hiệu ứng icon trên đầu VÀ ÂM THANH UỐNG THUỐC 3D cho các người chơi khác (Shared Mode)
        var netSync = player.GetComponent<NetworkPlayerSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcApplyOverheadBuff((int)potionType, effectName, tier, duration);
        }

        // 5. Xóa vật phẩm khỏi túi đồ và Hotbar
        ConsumeItem(player);
    }

    private void ConsumeItem(PlayerController player)
    {
        if (player == null) return;

        // 1. Trừ trọng lượng
        if (player.stats != null && _itemComp != null)
        {
            player.stats.RemoveWeight(_itemComp.kg);
        }

        // 2. Xóa khỏi danh sách heldItems của PlayerInventory
        if (player.inventory != null && player.inventory.heldItems != null)
        {
            player.inventory.heldItems.Remove(gameObject);
        }

        // 3. Xóa khỏi UI Hotbar
        if (player.inventory != null && player.inventory.hotbarManager != null)
        {
            var hotbar = player.inventory.hotbarManager;
            if (hotbar.currentSelectedIndex >= 0 && hotbar.currentSelectedIndex < hotbar.slots.Count)
            {
                var slot = hotbar.slots[hotbar.currentSelectedIndex];
                if (slot != null && slot.itemObject == gameObject)
                {
                    slot.ClearSlot();
                }
            }
            hotbar.DeselectAll();
        }

        // 4. Đồng bộ ẩn GameObject trên tất cả các máy
        NetworkItemSync.SyncPickupItem(gameObject);

        // 5. Hủy hoặc ẩn GameObject
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    // =========================================================================
    //               KHI Ở DƯỚI ĐẤT (IINTERACTABLE)
    // =========================================================================

    public string GetInteractableName() => string.IsNullOrEmpty(tier) ? effectName : $"{effectName} {tier}";
    public string GetActionPrompt() => string.IsNullOrEmpty(tier) ? $"Take {effectName}" : $"Take {effectName} {tier}";
    public int GetPrice() => _itemComp != null ? _itemComp.GetPrice() : (tier == "II" ? 80 : 40);
    public int GetWeight() => _itemComp != null ? _itemComp.GetWeight() : 1;
    public bool IsLootItem() => true;
    public string GetDescription() => $"Increases {effectName} by {((statMultiplier - 1f) * 100):F0}% for {duration}s.";
    public Sprite GetIcon() => effectIcon != null ? effectIcon : (_itemComp != null ? _itemComp.GetIcon() : null);
    public ItemRarity GetRarity() => tier == "II" ? ItemRarity.Rare : ItemRarity.Uncommon;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        failReason = "";
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player != null && player.inventory != null)
        {
            player.inventory.TryPickupItem(gameObject, GetWeight());
        }
    }
}
