using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component gắn trực tiếp trên mỗi Slot nhân vật trong UI (Slot 1, Slot 2, Slot 3):
/// - Chứa trực tiếp Mesh Nam/Nữ và Avatar Nam/Nữ cho từng Slot.
/// - Khi người chơi chọn Nam/Nữ và bấm Lưu (Save), dữ liệu từ Slot này sẽ truyền trực tiếp vào PlayerSO.
/// </summary>
public class CharacterSlotItem : MonoBehaviour
{
    [Header("📚 Cấu hình PlayerSO liên kết")]
    [Tooltip("PlayerSO tương ứng cho Slot này")]
    public PlayerSO characterSO;

    [Header("👤 Ảnh đại diện Slot (Nam & Nữ)")]
    [Tooltip("Ảnh đại diện / Icon Nam cho Slot này")]
    public Sprite maleAvatar;

    [Tooltip("Ảnh đại diện / Icon Nữ cho Slot này")]
    public Sprite femaleAvatar;

    [Header("🎭 3D Meshes (Nam & Nữ)")]
    [Tooltip("Mesh 3D Nam của nhân vật")]
    public Mesh maleMesh;

    [Tooltip("Mesh 3D Nữ của nhân vật")]
    public Mesh femaleMesh;

    [Header("🎨 UI References trên Slot")]
    public Button slotButton;
    public Image slotBackground;
    public Image iconImage;
    public GameObject lockObject;
    public Button lockButton;
    public TextMeshProUGUI priceText;

    /// <summary>
    /// Lấy ảnh đại diện theo giới tính
    /// </summary>
    public Sprite GetAvatar(bool isMale)
    {
        if (isMale) return maleAvatar != null ? maleAvatar : (characterSO != null ? characterSO.GetAvatar(true) : null);
        return femaleAvatar != null ? femaleAvatar : (characterSO != null ? characterSO.GetAvatar(false) : null);
    }

    /// <summary>
    /// Lấy Mesh 3D theo giới tính
    /// </summary>
    public Mesh GetMesh(bool isMale)
    {
        if (isMale) return maleMesh != null ? maleMesh : (characterSO != null ? characterSO.GetMesh(true) : null);
        return femaleMesh != null ? femaleMesh : (characterSO != null ? characterSO.GetMesh(false) : null);
    }

    /// <summary>
    /// Truyền dữ liệu Mesh và Avatar từ Slot này vào PlayerSO khi Save
    /// </summary>
    public void ApplyDataToPlayerSO()
    {
        if (characterSO == null) return;

        if (maleAvatar != null) characterSO.maleAvatar = maleAvatar;
        if (femaleAvatar != null) characterSO.femaleAvatar = femaleAvatar;
        if (maleMesh != null) characterSO.maleMesh = maleMesh;
        if (femaleMesh != null) characterSO.femaleMesh = femaleMesh;
    }

    /// <summary>
    /// Cập nhật hiển thị giao diện của Slot theo giới tính, trạng thái chọn, và mở khóa
    /// </summary>
    public void UpdateSlotUI(bool isMale, bool isSelected, bool isUnlocked, Color selectedColor, Color defaultColor)
    {
        // 1. Cập nhật màu viền/nền Slot khi được chọn
        if (slotBackground != null)
        {
            slotBackground.color = isSelected ? selectedColor : defaultColor;
        }

        // 2. Cập nhật Icon Avatar theo Nam hoặc Nữ
        if (iconImage != null)
        {
            Sprite activeSprite = GetAvatar(isMale);
            if (activeSprite != null)
            {
                iconImage.sprite = activeSprite;
            }
        }

        // 3. Cập nhật Khóa & Giá tiền
        if (lockObject != null)
        {
            lockObject.SetActive(!isUnlocked);
        }

        if (priceText != null && characterSO != null)
        {
            priceText.text = $"${characterSO.unlockPrice}";
        }
    }

    [ContextMenu("Auto Bind Slot References")]
    public void AutoBindReferences()
    {
        if (slotButton == null) slotButton = GetComponent<Button>();
        if (slotBackground == null) slotBackground = GetComponent<Image>();

        if (iconImage == null)
        {
            Transform iconTrans = transform.Find("Icon") ?? transform.Find("Avatar") ?? transform.Find("Image");
            if (iconTrans != null) iconImage = iconTrans.GetComponent<Image>();
        }

        if (lockObject == null)
        {
            Transform lockTrans = transform.Find("LockImg") ?? transform.Find("Lock");
            if (lockTrans != null)
            {
                lockObject = lockTrans.gameObject;
                if (lockButton == null) lockButton = lockTrans.Find("Image")?.GetComponent<Button>() ?? lockTrans.GetComponent<Button>();
                if (priceText == null) priceText = lockTrans.Find("Price")?.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
