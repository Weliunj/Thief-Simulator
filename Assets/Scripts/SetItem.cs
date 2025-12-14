using UnityEngine;
using System.Collections.Generic; // Cần cho List

public class SetItem : MonoBehaviour
{
    // Danh sách các Prefabs Item có thể được spawn
    [Tooltip("Kéo thả tất cả Prefab của các Item vào đây.")]
    public List<GameObject> itemPrefabs = new List<GameObject>();

    // Mảng các vị trí (Transform) mà Item sẽ được spawn
    [Tooltip("Kéo thả tất cả các GameObject đánh dấu vị trí spawn vào đây.")]
    public Transform[] spawnPoints;

    void Start()
    {
        // Gọi hàm spawn khi Scene được load
        SpawnRandomItemAtEachPosition();
    }

    /// <summary>
    /// Sinh ra MỘT item ngẫu nhiên tại MỖI vị trí spawn đã định trước.
    /// </summary>
    public void SpawnRandomItemAtEachPosition()
    {
        // 1. Kiểm tra điều kiện
        if (itemPrefabs.Count == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Thiếu Item Prefabs hoặc Spawn Points. Không thể spawn.");
            return;
        }

        // 2. Lặp qua TẤT CẢ các vị trí spawn (Transform)
        foreach (Transform spawnPoint in spawnPoints)
        {
            // 3. Chọn một Item ngẫu nhiên từ danh sách itemPrefabs
            int randomItemIndex = Random.Range(0, itemPrefabs.Count);
            GameObject selectedItemPrefab = itemPrefabs[randomItemIndex];

            // 4. Thực hiện lệnh spawn (Instantiate)
            // Sinh ra Item tại vị trí và góc quay của spawnPoint
            Instantiate(selectedItemPrefab, spawnPoint.position, spawnPoint.rotation);
            
            Debug.Log("Đã spawn Item: " + selectedItemPrefab.name + 
                      " tại vị trí: " + spawnPoint.name);
        }
    }
}