using Fusion;
using UnityEngine;

/// <summary>
/// Đồng bộ nhặt vật phẩm thế giới qua mạng Photon Fusion:
/// - Khi 1 người chơi nhặt vật phẩm, phát RPC thông báo cho tất cả các máy khác ẩn/xóa vật phẩm đó để tránh nhặt trùng.
/// </summary>
public class NetworkItemSync : NetworkBehaviour
{
    public static NetworkItemSync Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;
    }

    /// <summary>
    /// Đồng bộ nhặt vật phẩm thế giới
    /// </summary>
    public static void SyncPickupItem(GameObject itemObj)
    {
        if (itemObj == null) return;

        // Nếu có NetworkObject trên item, despawn qua Runner
        NetworkObject netObj = itemObj.GetComponent<NetworkObject>();
        if (netObj != null && netObj.Runner != null && netObj.Runner.IsRunning)
        {
            if (netObj.HasStateAuthority)
            {
                netObj.Runner.Despawn(netObj);
            }
            return;
        }

        // Nếu là GameObject thông thường trong Scene, gửi RPC theo đường dẫn Hierarchy
        if (Instance != null && Instance.Runner != null && Instance.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            Instance.RpcDespawnSceneItem(itemPath);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcDespawnSceneItem(string itemHierarchyPath)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;

        GameObject targetItem = GameObject.Find(itemHierarchyPath);
        if (targetItem != null && targetItem.activeSelf)
        {
            targetItem.SetActive(false);
            Debug.Log($"<color=cyan>[NetworkItemSync] Đã ẩn vật phẩm nhặt bởi người chơi khác: {itemHierarchyPath}</color>");
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
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
