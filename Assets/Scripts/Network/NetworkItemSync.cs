using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đồng bộ nhặt và vứt/thả vật phẩm thế giới qua mạng Photon Fusion:
/// - Khi 1 người chơi nhặt vật phẩm, phát RPC thông báo cho tất cả các máy khác ẩn vật phẩm đó.
/// - Khi 1 người chơi thả/vứt vật phẩm, phát RPC thông báo cho tất cả các máy khác hiện lại vật phẩm tại vị trí rơi kèm lực vật lý.
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

    /// <summary>
    /// Đồng bộ thả/ném vật phẩm thế giới
    /// </summary>
    public static void SyncDropItem(GameObject itemObj, Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity, bool isLadder = false, bool isLadderPlaced = false)
    {
        if (itemObj == null) return;

        if (Instance != null && Instance.Runner != null && Instance.Runner.IsRunning)
        {
            string itemPath = GetGameObjectPath(itemObj);
            Instance.RpcShowDroppedSceneItem(itemPath, position, rotation, linearVelocity, angularVelocity, isLadder, isLadderPlaced);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcDespawnSceneItem(string itemHierarchyPath, RpcInfo info = default)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;

        GameObject targetItem = FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            targetItem.SetActive(false);
            Debug.Log($"<color=cyan>[NetworkItemSync] Đã ẩn vật phẩm nhặt bởi người chơi: {itemHierarchyPath}</color>");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcShowDroppedSceneItem(string itemHierarchyPath, Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity, bool isLadder, bool isLadderPlaced, RpcInfo info = default)
    {
        if (string.IsNullOrEmpty(itemHierarchyPath)) return;

        GameObject targetItem = FindSceneObjectByPath(itemHierarchyPath);
        if (targetItem != null)
        {
            // Nếu là chính máy người thả và item đã active tại đúng vị trí thì bỏ qua
            if (Runner != null && info.Source == Runner.LocalPlayer && targetItem.activeSelf)
            {
                return;
            }

            targetItem.transform.SetParent(null);
            targetItem.transform.position = position;
            targetItem.transform.rotation = rotation;
            targetItem.transform.localScale = Vector3.one;
            targetItem.SetActive(true);

            // Bật lại Renderers & Colliders
            var renderers = targetItem.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = true;

            var colliders = targetItem.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) if (c != null) c.enabled = true;

            var ladder = targetItem.GetComponent<LadderController>() ?? targetItem.GetComponentInChildren<LadderController>();
            if (ladder != null)
            {
                ladder.SetPlaced(isLadderPlaced);
            }

            Rigidbody rb = targetItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.None;
                rb.linearVelocity = linearVelocity;
                rb.angularVelocity = angularVelocity;
            }

            Debug.Log($"<color=green>[NetworkItemSync] Đã hiện lại vật phẩm rơi qua mạng: {itemHierarchyPath} tại {position}</color>");
        }
    }

    /// <summary>
    /// Tìm kiếm GameObject theo đường dẫn Hierarchy kể cả khi GameObject đang bị SetActive(false)
    /// </summary>
    public static GameObject FindSceneObjectByPath(string hierarchyPath)
    {
        if (string.IsNullOrEmpty(hierarchyPath)) return null;

        string[] parts = hierarchyPath.Split('/');
        if (parts.Length == 0) return null;

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == parts[0])
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            // Fallback: Tìm theo tên object đầu tiên không phân biệt hoa thường
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && string.Equals(roots[i].name, parts[0], System.StringComparison.OrdinalIgnoreCase))
                {
                    current = roots[i].transform;
                    break;
                }
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

            if (foundChild == null)
            {
                return null;
            }

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
