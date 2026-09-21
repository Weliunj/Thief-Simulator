using StarterAssets;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [Header("📦 ScriptableObject Data")]
    public ItemSO itemData;

    [Header("📊 Current Stats (Generated at Runtime)")]
    public string itemName = "";
    public ItemRarity rarity = ItemRarity.Common;
    [TextArea(2, 4)]
    public string description = "";
    public float Price = 0f;
    public int kg = 0;

    [Header("⚙️ Fallback Settings (Nếu không gắn ItemSO)")]
    public float MinPrice = 10f;
    public float MaxPrice = 100f;
    public int MinKg = 1;
    public int MaxKg = 10;

    public PlayerManager playerManagerl;

    void Awake()
    {
        InitializeStats();
    }

    /// <summary>
    /// Khởi tạo và ngẫu nhiên hóa chỉ số mỗi khi màn chơi bắt đầu
    /// </summary>
    public void InitializeStats()
    {
        if (itemData != null)
        {
            var stats = itemData.GenerateRandomStats();
            Price = stats.price;
            kg = stats.kg;
            itemName = string.IsNullOrEmpty(itemData.itemName) ? gameObject.name : itemData.itemName;
            description = itemData.description;
            rarity = itemData.rarity;
        }
        else
        {
            // Fallback nếu item chưa được gán ItemSO
            MaxKg = MaxKg <= MinKg ? MinKg + 10 : MaxKg;
            MaxPrice = MaxPrice <= MinPrice ? MinPrice + 10 : MaxPrice;
            kg = Random.Range(MinKg, MaxKg);
            Price = Random.Range(MinPrice, MaxPrice);
            if (string.IsNullOrEmpty(itemName))
            {
                itemName = gameObject.name.Replace("(Clone)", "").Trim();
            }
            if (string.IsNullOrEmpty(description))
            {
                description = "A valuable item that can be collected.";
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("home"))
        {
            if (playerManagerl == null)
            {
                UI_Manager ui = FindFirstObjectByType<UI_Manager>();
                if (ui != null) playerManagerl = ui.playerManager;
            }

            if (playerManagerl != null)
            {
                playerManagerl.currpoint += (int)Price;
                Debug.Log($"Đã giao vật phẩm! Điểm hiện tại: {playerManagerl.currpoint} / {playerManagerl.totalpoint}");
            }
            Destroy(gameObject);
        }
    }

    // =========================================================================
    //                       IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => string.IsNullOrEmpty(itemName) ? gameObject.name : itemName;

    public string GetActionPrompt() => "Pick Up";

    public int GetPrice() => Mathf.RoundToInt(Price);

    public int GetWeight() => kg;

    public bool IsLootItem() => true;

    public string GetDescription() => string.IsNullOrEmpty(description) ? (itemData != null ? itemData.description : "") : description;

    public Sprite GetIcon() => itemData != null ? itemData.itemIcon : null;

    public ItemRarity GetRarity() => rarity;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        if (player == null || player.player == null)
        {
            failReason = "";
            return false;
        }

        if (player.isClimbingLadder)
        {
            failReason = "Cannot pick up while climbing";
            return false;
        }

        if (player.player.currweight + kg > player.player.Maxweight)
        {
            failReason = "Too Heavy! (Overweight)";
            return false;
        }

        if (player.hotbarManager == null)
        {
            player.hotbarManager = FindFirstObjectByType<HotbarManager>(FindObjectsInactive.Include);
        }

        if (player.hotbarManager != null && !player.hotbarManager.HasEmptySlot())
        {
            failReason = "Hotbar Full!";
            return false;
        }

        failReason = "";
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player != null)
        {
            player.TryPickupItem(gameObject, kg);
        }
    }
}
