using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đồng bộ nhặt và vứt/thả vật phẩm thế giới qua mạng Photon Fusion:
/// - Khi 1 người chơi nhặt vật phẩm, phát RPC thông báo cho tất cả các máy khác ẩn vật phẩm đó.
/// - Khi 1 người chơi thả/vứt vật phẩm, phát RPC thông báo cho tất cả các máy khác hiện lại vật phẩm tại vị trí rơi kèm lực vật lý.
/// </summary>
public static class NetworkItemSync
{
    /// <summary>
    /// Tìm NetworkPlayerSync của chính người chơi cục bộ
    /// </summary>
    public static NetworkPlayerSync GetLocalPlayerSync()
    {
        var allPlayers = Object.FindObjectsByType<NetworkPlayerSync>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p != null && p.IsLocalPlayer)
            {
                return p;
            }
        }
        return (allPlayers != null && allPlayers.Length > 0) ? allPlayers[0] : null;
    }

    /// <summary>
    /// Đồng bộ nhặt vật phẩm thế giới (Ẩn ngay lập tức trên tất cả máy)
    /// </summary>
    public static void SyncPickupItem(GameObject itemObj)
    {
        if (itemObj == null) return;

        NetworkPlayerSync localSync = GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            localSync.RpcDespawnSceneItem(itemPath);
        }
    }

    /// <summary>
    /// Đồng bộ thả/ném vật phẩm thế giới (Hiện lại và áp dụng lực vật lý trên tất cả máy)
    /// </summary>
    public static void SyncDropItem(GameObject itemObj, Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity, bool isLadder = false, bool isLadderPlaced = false)
    {
        if (itemObj == null) return;

        NetworkPlayerSync localSync = GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            localSync.RpcShowDroppedSceneItem(itemPath, position, rotation, linearVelocity, angularVelocity, isLadder, isLadderPlaced);
        }
    }

    /// <summary>
    /// Đồng bộ bật/tắt đèn pin và âm thanh qua mạng
    /// </summary>
    public static void SyncFlashlightState(GameObject itemObj, bool isOn)
    {
        if (itemObj == null) return;

        NetworkPlayerSync localSync = GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            localSync.RpcSetFlashlightState(itemPath, isOn);
        }
    }

    /// <summary>
    /// Đồng bộ trạng thái thang đang có người leo hay không (Khóa tương tác tránh 2 người leo cùng lúc)
    /// </summary>
    public static void SyncLadderOccupied(GameObject itemObj, bool isOccupied)
    {
        if (itemObj == null) return;

        NetworkPlayerSync localSync = GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            localSync.RpcSetLadderOccupied(itemPath, isOccupied);
        }
    }

    /// <summary>
    /// Đồng bộ âm thanh bước chân leo thang 3D cho những người xung quanh nghe thấy
    /// </summary>
    public static void SyncLadderAudio(GameObject itemObj, bool isPlaying)
    {
        if (itemObj == null) return;

        NetworkPlayerSync localSync = GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            localSync.RpcSetLadderAudio(itemPath, isPlaying);
        }
    }

    /// <summary>
    /// Đồng bộ bán vật phẩm khi ném/đưa vào xe (Giao hàng) cho toàn bộ người chơi trong phòng
    /// </summary>
    public static void SyncSellItem(GameObject itemObj, int earnedPoints, string itemName)
    {
        if (itemObj == null) return;

        NetworkPlayerSync localSync = GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            localSync.RpcSyncSellItem(itemPath, earnedPoints, itemName);
        }
        else
        {
            // Fallback khi chơi Offline / Solo
            UI_Manager ui = Object.FindFirstObjectByType<UI_Manager>();
            if (ui != null && ui.playerManager != null)
            {
                ui.playerManager.currpoint += earnedPoints;
            }
            GameStatusHUD.Show($"Sold {itemName} (+${earnedPoints})!", 2.5f);
            itemObj.SetActive(false);
            Object.Destroy(itemObj);
        }
    }

    /// <summary>
    /// Tìm kiếm GameObject theo đường dẫn Hierarchy hoặc tìm theo tên duy nhất kể cả khi Object đang SetActive(false)
    /// </summary>
    public static GameObject FindSceneObjectByPath(string hierarchyPath)
    {
        if (string.IsNullOrEmpty(hierarchyPath)) return null;

        // 1. Thử tìm nhanh theo tên cuối cùng trước (nếu là dạng Item_x_...)
        string targetName = hierarchyPath;
        if (hierarchyPath.Contains("/"))
        {
            int lastSlash = hierarchyPath.LastIndexOf('/');
            targetName = hierarchyPath.Substring(lastSlash + 1);
        }

        // Tìm trong toàn bộ Transform (kể cả object bị inactive)
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        foreach (var root in roots)
        {
            if (root == null) continue;
            if (root.name == targetName) return root;

            var allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child != null && child.name == targetName)
                {
                    return child.gameObject;
                }
            }
        }

        // 2. Fallback: Nếu không tìm thấy theo tên, thử duyệt từng cấp theo Hierarchy path cũ
        string[] parts = hierarchyPath.Split('/');
        Transform current = null;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == parts[0])
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null) return null;

        for (int p = 1; p < parts.Length; p++)
        {
            string childName = parts[p];
            Transform foundChild = null;

            for (int c = 0; c < current.childCount; c++)
            {
                Transform child = current.GetChild(c);
                if (child.name == childName || string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundChild = child;
                    break;
                }
            }

            if (foundChild == null) return null;
            current = foundChild;
        }

        return current != null ? current.gameObject : null;
    }

    public static string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "";
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
