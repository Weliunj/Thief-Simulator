using StarterAssets;
using UnityEngine;
using Fusion;

public class Item : NetworkBehaviour, IInteractable
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

    [Header("🎯 Custom Hold Settings (Khi cầm trên Hotbar)")]
    [Tooltip("Cho phép trục xoay của vật phẩm ngửa lên / cúi xuống bám theo góc nhìn Camera (dành cho Đèn pin, Súng...)")]
    public bool followCameraPitch = false;

    [Tooltip("Ghi đè vị trí cầm tay riêng cho item này")]
    public bool useCustomHoldOffset = false;
    public Vector3 customHoldOffset = Vector3.zero;

    [Tooltip("Ghi đè góc xoay khi cầm tay riêng cho item này")]
    public bool useCustomHoldRotation = false;
    public Vector3 customHoldRotation = Vector3.zero;

    private bool _isSold = false;

    void Awake()
    {
        InitializeStats();
    }

    /// <summary>
    /// Khởi tạo và ngẫu nhiên hóa chỉ số dựa trên Seed đồng bộ theo tên vật phẩm
    /// Đảm bảo tất cả người chơi trong phòng đều nhận đúng 100% cùng mức giá và cân nặng cho cùng 1 item!
    /// </summary>
    public void InitializeStats()
    {
        // Đồng bộ Seed ngẫu nhiên theo tên duy nhất của Object
        int deterministicSeed = gameObject.name.GetHashCode();
        Random.State oldState = Random.state;
        Random.InitState(deterministicSeed);

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

        Random.state = oldState; // Khôi phục Random State mặc định
    }

    public bool IsSold => _isSold;

    public void SellItem()
    {
        if (_isSold) return;
        _isSold = true;

        // Vô hiệu hóa ngay lập tức mọi Collider để tránh kích hoạt nhiều lần liên tục
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = false;
        }

        NetworkItemSync.SyncSellItem(gameObject, Mathf.RoundToInt(Price), GetInteractableName());
    }

    public void OnTriggerEnter(Collider other)
    {
        if (_isSold || other == null) return;
        if (other.GetComponent<HomeSellZone>() != null || other.GetComponentInParent<HomeSellZone>() != null)
        {
            SellItem();
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (_isSold || collision == null || collision.gameObject == null) return;
        if (collision.gameObject.GetComponent<HomeSellZone>() != null || collision.gameObject.GetComponentInParent<HomeSellZone>() != null)
        {
            SellItem();
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
