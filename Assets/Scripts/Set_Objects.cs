using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine.AI;
using Unity.AI.Navigation; // Cần thiết cho việc sử dụng NavMeshSurface (YÊU CẦU MỚI)
using System.Collections;

// Lưu ý: Để code này hoạt động, bạn cần cài đặt gói "AI Navigation" (Window -> Package Manager -> Unity Registry)
// và component NavMeshSurface phải được đính kèm vào GameObject chứa script hoặc một GameObject khác.

public class SetItem : MonoBehaviour
{
    // ... (Các Header HouseSpawn, NpcSpawn, ItemSpawn giữ nguyên) ...

    [Header("HouseSpawn")]
    public List<GameObject> item_House = new List<GameObject>();         //8 GameObject Nhà đã có sẵn
    public Transform[] spawnPoints_House;       //8 pos khác nhau
    private List<GameObject> spawnedHouses = new List<GameObject>(); 
    
    private int houseToExcludeItem = -1; 
    
    [Header("NpcSpawn")]
    [Tooltip("Kéo thả Prefab của NPC vào đây.")]
    public GameObject npcPrefab; // Prefab của NPC
    
    [Tooltip("Kéo thả 8 Transform vị trí NPC (bên trong mỗi nhà).")]
    public Transform[] npcSpawnPoints; 

    [Header("ItemSpawn")]
    [Tooltip("Kéo thả tất cả Prefab của các Item vào đây.")]
    public List<GameObject> itemPrefabs = new List<GameObject>();
    
    [Tooltip("Kéo thả 8 GameObject Cha (Base) để Item được spawn trong các Children của chúng.")]
    public GameObject[] itemCha; 

    [Header("Navigation")]
    [Tooltip("Kéo thả component NavMeshSurface từ GameObject chứa nó.")]
    public NavMeshSurface navMeshSurface; // Tham chiếu đến NavMeshSurface

    void Start()
    {
        // 1. Di chuyển Nhà đến Vị trí NGẪU NHIÊN + ROTATION NGẪU NHIÊN
        MoveHousesRandomly(); 
        
        // 2. Chọn ngẫu nhiên ngôi nhà từ 1-7 để loại trừ Item
        DetermineRandomItemExclusionHouse();

        // 3. Spawn NPC 
        SpawnNpcInHouse();

        // 4. Spawn Item (Sử dụng itemCha)
        SpawnRandomItemAtEachHouse();
        StartCoroutine(BuildNavMeshDelay());
        // 5. BAKE NAVMESH (YÊU CẦU MỚI)
        BakeNavMesh();
    }

    // =========================================================================
    //                            PHẦN NAVIGATION
    // =========================================================================

    /// <summary>
    /// Thực hiện việc tạo lại NavMesh sau khi di chuyển các vật thể tĩnh.
    /// </summary>
    public void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            // Kiểm tra xem GameObject của NavMeshSurface có bị tắt không, và bật nó lên nếu cần.
            if (!navMeshSurface.gameObject.activeInHierarchy)
            {
                navMeshSurface.gameObject.SetActive(true);
            }
            
            navMeshSurface.RemoveData(); // Xóa NavMesh cũ (Tùy chọn, nếu cần làm sạch)
            Debug.Log("[NAV MESH] Đã hoàn thành việc Bake NavMesh Surface.");
        }
        else
        {
            Debug.LogWarning("[NAV MESH] NavMeshSurface chưa được tham chiếu. Không thể Bake NavMesh.");
        }
    }
    IEnumerator BuildNavMeshDelay()
    {
        yield return null; // chờ 1 frame
        navMeshSurface.BuildNavMesh();
    }
    // =========================================================================
    // ... (Các hàm MoveHousesRandomly(), DetermineRandomItemExclusionHouse(), 
    // SpawnRandomItemAtEachHouse(), SpawnNpcInHouse() giữ nguyên) ...
    // =========================================================================
    
    /// <summary>
    /// Di chuyển các GameObject Nhà đã tham chiếu đến các vị trí ngẫu nhiên trong spawnPoints_House và xoay ngẫu nhiên 0/90/180/270 độ.
    /// </summary>
    public void MoveHousesRandomly() 
    {
        if (item_House.Count == 0 || spawnPoints_House.Length == 0)
        {
            Debug.LogWarning("Thiếu GameObject Nhà hoặc Spawn Points Nhà. Không thể di chuyển Nhà.");
            return;
        }
        
        if (item_House.Count != spawnPoints_House.Length)
        {
             Debug.LogWarning("Số lượng Nhà và Vị trí Spawn phải khớp nhau (cần 8 cho mỗi loại).");
        }

        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints_House);
        float[] validRotations = { 0f, 90f, 180f, 270f }; 
        
        spawnedHouses.Clear(); 
        int houseCount = item_House.Count;
        
        for (int i = 0; i < houseCount; i++)
        {
            if (availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning("Đã hết vị trí spawn trước khi hết GameObject Nhà.");
                break; 
            }
            
            int randomPosIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform selectedSpawnPoint = availableSpawnPoints[randomPosIndex];
            float randomYRotation = validRotations[Random.Range(0, validRotations.Length)];
            
            GameObject houseToMove = item_House[i];
            houseToMove.SetActive(true); 

            houseToMove.transform.position = selectedSpawnPoint.position;
            
            houseToMove.transform.rotation = Quaternion.Euler(
                selectedSpawnPoint.rotation.eulerAngles.x, 
                randomYRotation,                          
                selectedSpawnPoint.rotation.eulerAngles.z 
            );
            
            houseToMove.name = "House_" + i; 
            spawnedHouses.Add(houseToMove); 
            
            availableSpawnPoints.RemoveAt(randomPosIndex);

            Debug.Log($"Đã di chuyển Nhà: {houseToMove.name} đến vị trí NGẪU NHIÊN: {selectedSpawnPoint.name}, Góc xoay: {randomYRotation} độ.");
        }
    }

    public void DetermineRandomItemExclusionHouse()
    {
        if (itemCha != null && itemCha.Length >= 8) 
        {
            houseToExcludeItem = Random.Range(1, 8); 
            Debug.Log($"[ITEM RULE] Đã chọn Ngôi nhà ngẫu nhiên KHÔNG có Item: Base_{houseToExcludeItem} ");
        }
        else
        {
            Debug.LogWarning("[ITEM RULE] Thiếu GameObject trong mảng itemCha (Cần tối thiểu 8). Logic loại trừ ngẫu nhiên không áp dụng.");
            houseToExcludeItem = -2; 
        }
    }

    public void SpawnRandomItemAtEachHouse()
    {
        if (itemPrefabs.Count == 0 || itemCha == null || itemCha.Length < 8)
        {
            Debug.LogWarning("Thiếu Item Prefabs hoặc mảng itemCha (Cần 8 phần tử). Không thể spawn Item.");
            return;
        }

        for (int i = 0; i < itemCha.Length; i++)
        {
            GameObject itemBase = itemCha[i];

            if (i == houseToExcludeItem)
            {
                Debug.Log($"[ITEM RULE] BỎ QUA ItemBase_{i}: Nhà/Base này được chọn ngẫu nhiên để không có Item.");
                continue; 
            }
            
            List<Transform> itemSpawnPoints = itemBase.GetComponentsInChildren<Transform>()
                .Where(t => t != itemBase.transform) 
                .ToList();

            if (itemSpawnPoints.Count == 0)
            {
                Debug.LogWarning($"ItemBase_{i} ({itemBase.name}) không có Transform con nào để spawn Item.");
                continue;
            }

            foreach (Transform spawnPoint in itemSpawnPoints)
            {
                int randomItemIndex = Random.Range(0, itemPrefabs.Count);
                GameObject selectedItemPrefab = itemPrefabs[randomItemIndex];

                GameObject spawnedItem = Instantiate(selectedItemPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedItem.transform.SetParent(spawnPoint); 

                Debug.Log($"Đã spawn Item: {selectedItemPrefab.name} tại ItemBase_{i} - Vị trí: {spawnPoint.name}");
            }
        }
    }
    
    public void SpawnNpcInHouse()
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning("Thiếu Prefab NPC. Không thể spawn NPC.");
            return;
        }
        
        if (npcSpawnPoints.Length < 8)
        {
            Debug.LogWarning("Thiếu vị trí NPC (npcSpawnPoints). Cần có đủ 8 vị trí cho 8 nhà.");
            return;
        }

        int minHouseIndex = 1;
        int maxHouseIndex = 7;

        if (spawnedHouses.Count > maxHouseIndex)
        {
            int randomHouseIndex = Random.Range(minHouseIndex, maxHouseIndex + 1); 
            Transform npcPos = npcSpawnPoints[randomHouseIndex]; 

            // Cập nhật: Chỉ sử dụng góc quay Y của npcPos để NPC quay trên mặt đất (trục Z của nó hướng về phía trước)
            Quaternion npcRotation = Quaternion.Euler(
                0, // Đảm bảo không bị nghiêng theo trục X
                npcPos.rotation.eulerAngles.y, // Lấy hướng từ điểm spawn
                0  // Đảm bảo không bị nghiêng theo trục Z
            );
            
            GameObject spawnedNpc = Instantiate(npcPrefab, npcPos.position, npcRotation);
            spawnedNpc.transform.SetParent(npcPos); 

            Debug.Log($"Đã spawn NPC ({npcPrefab.name}) tại Ngôi nhà ngẫu nhiên: House_{randomHouseIndex}, tại vị trí: {npcPos.name}");
        }
        else
        {
            Debug.LogWarning("Không đủ số lượng Nhà đã spawn để thực hiện logic NPC (Cần tối thiểu 8 nhà).");
        }
    }
}