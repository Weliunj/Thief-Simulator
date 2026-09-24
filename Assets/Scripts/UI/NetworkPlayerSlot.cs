using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý 1 dòng/thẻ người chơi trong Sảnh Chờ (Waiting Room)
/// Cấu trúc phân cấp hỗ trợ:
/// - playerSlot
///   - BackGround
///   - Avatar/PlayerImg
///   - PlayerNameText
///   - HostImg
///   - GuestImg
///   - HostBadgeText
///   - ReadyStatusText
/// </summary>
public class NetworkPlayerSlot : MonoBehaviour
{
    [Header("🖼️ UI Components")]
    public Image playerImg;
    public TextMeshProUGUI playerNameText;
    public GameObject hostImg;
    public GameObject guestImg;
    public TextMeshProUGUI hostBadgeText;
    public TextMeshProUGUI readyStatusText;

    private void Awake()
    {
        AutoBind();
    }

    private void Reset()
    {
        AutoBind();
    }

    /// <summary>
    /// Tự động dò tìm các component con theo tên phân cấp trong Hierarchy
    /// </summary>
    public void AutoBind()
    {
        if (playerImg == null)
        {
            Transform imgTrans = transform.Find("Avatar/PlayerImg") ?? transform.Find("PlayerImg") ?? transform.Find("Avatar") ?? transform.Find("AvatarImg");
            if (imgTrans != null) playerImg = imgTrans.GetComponent<Image>();
        }

        if (playerNameText == null)
        {
            Transform nameTrans = transform.Find("PlayerNameText") ?? transform.Find("NameText") ?? transform.Find("PlayerName") ?? transform.Find("UsernameText");
            if (nameTrans != null) playerNameText = nameTrans.GetComponent<TextMeshProUGUI>();
        }

        if (hostImg == null)
        {
            Transform hTrans = transform.Find("HostImg") ?? transform.Find("HostIcon") ?? transform.Find("Host") ?? transform.Find("HostLogo");
            if (hTrans != null) hostImg = hTrans.gameObject;
        }

        if (guestImg == null)
        {
            Transform gTrans = transform.Find("GuestImg") ?? transform.Find("GuestIcon") ?? transform.Find("Guest") ?? transform.Find("GuestLogo");
            if (gTrans != null) guestImg = gTrans.gameObject;
        }

        if (hostBadgeText == null)
        {
            Transform badgeTrans = transform.Find("HostBadgeText") ?? transform.Find("HostBadge") ?? transform.Find("BadgeText") ?? transform.Find("HostText");
            if (badgeTrans != null) hostBadgeText = badgeTrans.GetComponent<TextMeshProUGUI>();
        }

        if (readyStatusText == null)
        {
            Transform statusTrans = transform.Find("ReadyStatusText") ?? transform.Find("ReadyText") ?? transform.Find("StatusText") ?? transform.Find("ReadyStatus") ?? transform.Find("GuestText");
            if (statusTrans != null) readyStatusText = statusTrans.GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Cập nhật hiển thị dòng người chơi:
    /// - Chủ phòng (isHost = true): Hiện HostImg + HostBadgeText ("HOST"), Ẩn GuestImg + ReadyStatusText
    /// - Khách vào (isHost = false): Hiện GuestImg + ReadyStatusText ("READY" / "NOT READY"), Ẩn HostImg + HostBadgeText
    /// </summary>
    public void Setup(string playerName, bool isHost, bool isReady, Sprite avatarSprite = null)
    {
        AutoBind();

        // 1. Tên người chơi
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // 2. Ảnh đại diện
        if (playerImg != null && avatarSprite != null)
        {
            playerImg.sprite = avatarSprite;
        }

        // 3. Icon Chủ phòng (Host) vs Khách (Guest)
        if (hostImg != null) hostImg.SetActive(isHost);
        if (guestImg != null) guestImg.SetActive(!isHost);

        // 4. Huy hiệu Chủ phòng (Host Badge): Chỉ hiện khi là CHỦ PHÒNG
        if (hostBadgeText != null)
        {
            hostBadgeText.gameObject.SetActive(isHost);
            if (isHost)
            {
                hostBadgeText.text = "HOST";
            }
        }

        // 5. Trạng thái Sẵn sàng (Ready Status): Chỉ hiện khi là KHÁCH
        if (readyStatusText != null)
        {
            readyStatusText.gameObject.SetActive(!isHost);
            if (!isHost)
            {
                readyStatusText.text = isReady
                    ? "<color=#00FF88>READY ✓</color>"
                    : "NOT READY";
            }
        }
    }
}
