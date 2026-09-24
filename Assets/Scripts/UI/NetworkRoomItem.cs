using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý 1 dòng hiển thị phòng trong danh sách phòng (Room List)
/// </summary>
public class NetworkRoomItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image mapBgImage;
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI chapterNameText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI pingOrStatusText;
    public Button joinButton;

    private string currentRoomName = "";
    private Action<string> onJoinCallback;

    public void Setup(string roomName, int currentPlayers, int maxPlayers, Action<string> onJoin, Sprite mapSprite = null, string mapTitle = "")
    {
        currentRoomName = roomName;
        onJoinCallback = onJoin;

        if (roomNameText != null) roomNameText.text = roomName;
        if (playerCountText != null) playerCountText.text = $"{currentPlayers}/{maxPlayers}";
        if (chapterNameText != null && !string.IsNullOrEmpty(mapTitle)) chapterNameText.text = mapTitle;

        if (mapBgImage != null && mapSprite != null)
        {
            mapBgImage.sprite = mapSprite;
            mapBgImage.gameObject.SetActive(true);
        }

        if (pingOrStatusText != null)
        {
            if (currentPlayers >= maxPlayers)
            {
                pingOrStatusText.text = "<color=red>FULL</color>";
            }
            else
            {
                pingOrStatusText.text = "<color=green>OPEN</color>";
            }
        }

        if (joinButton != null)
        {
            joinButton.interactable = currentPlayers < maxPlayers;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }
    }

    private void OnJoinClicked()
    {
        onJoinCallback?.Invoke(currentRoomName);
    }
}
