using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Tooltip("Transforms used as spawn anchors. Items will be placed at or near these positions.")]
    public Transform[] spawnPoints;

    [Tooltip("Small random offset from the spawn point to avoid perfect stacking.")]
    public float randomOffset = 0.25f;

    [Tooltip("Minimum distance between spawned items when avoiding overlap.")]
    public float minDistance = 0.5f;

    [Tooltip("If true, the spawner will try to avoid placing two items too close to each other.")]
    public bool avoidOverlap = true;

    // Spawn existing GameObjects (e.g., items removed from inventory) at random spawn points.
    public void SpawnItemsAtSpawnPoints(List<GameObject> items)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("ItemSpawner: No spawnPoints assigned.");
            return;
        }

        if (items == null || items.Count == 0)
        {
            Debug.Log("ItemSpawner: No items to spawn.");
            return;
        }

        // Shuffle spawn point indices to randomize assignment
        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++) indices.Add(i);
        for (int i = 0; i < indices.Count; i++)
        {
            int r = Random.Range(i, indices.Count);
            int tmp = indices[i]; indices[i] = indices[r]; indices[r] = tmp;
        }

        List<Vector3> usedPositions = new List<Vector3>();
        int spawnIndex = 0;

        foreach (var item in items)
        {
            if (item == null) continue;

            // If we ran out of spawn points, wrap around
            if (spawnIndex >= indices.Count) spawnIndex = 0;

            Transform anchor = spawnPoints[indices[spawnIndex]];
            spawnIndex++;

            Vector3 pos = anchor.position + new Vector3(
                Random.Range(-randomOffset, randomOffset),
                Random.Range(-randomOffset, randomOffset),
                Random.Range(-randomOffset, randomOffset)
            );

            // If avoidOverlap is enabled, try a few times to find a spot not too close
            if (avoidOverlap)
            {
                int attempts = 0;
                while (attempts < 10)
                {
                    bool tooClose = false;
                    foreach (var used in usedPositions)
                    {
                        if (Vector3.Distance(pos, used) < minDistance) { tooClose = true; break; }
                    }
                    if (!tooClose) break;
                    pos = anchor.position + new Vector3(
                        Random.Range(-randomOffset, randomOffset),
                        Random.Range(-randomOffset, randomOffset),
                        Random.Range(-randomOffset, randomOffset)
                    );
                    attempts++;
                }
            }

            item.transform.SetParent(null);
            item.transform.position = pos;
            item.SetActive(true);

            // enable physics if present
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.AddForce((Random.insideUnitSphere * 0.5f) + Vector3.up * Random.Range(0.6f, 1.2f), ForceMode.Impulse);
            }

            usedPositions.Add(pos);
            Debug.Log($"ItemSpawner: Spawned {item.name} at {pos}");
        }
    }

    // Convenience: spawn a number of random prefabs from a list
    public void SpawnRandomPrefabs(GameObject[] prefabs, int count)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("ItemSpawner: No prefabs provided.");
            return;
        }

        List<GameObject> created = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            // choose spawn point
            Transform anchor = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 pos = anchor.position + new Vector3(Random.Range(-randomOffset, randomOffset), 0f, Random.Range(-randomOffset, randomOffset));
            GameObject g = Instantiate(prefab, pos, Quaternion.identity);
            created.Add(g);
        }
        Debug.Log($"ItemSpawner: Instantiated {created.Count} prefabs.");
    }

    // Spawn random prefabs chosen from an ItemLibrary
    public void SpawnRandomFromLibrary(ItemLibrary library, int count, bool allowDuplicates = true)
    {
        if (library == null)
        {
            Debug.LogWarning("ItemSpawner: library is null.");
            return;
        }

        var prefabs = library.GetRandomPrefabs(count, allowDuplicates);
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("ItemSpawner: library returned no prefabs.");
            return;
        }

        SpawnRandomPrefabs(prefabs, prefabs.Length);
    }



    // Convenience: spawn a prefab directly at a list of world positions
    public void SpawnPrefabsAtPositions(GameObject[] prefabs, List<Vector3> positions)
    {
        if (prefabs == null || prefabs.Length == 0 || positions == null || positions.Count == 0)
        {
            Debug.LogWarning("ItemSpawner: missing prefabs or positions.");
            return;
        }
        foreach (var pos in positions)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Instantiate(prefab, pos + new Vector3(Random.Range(-randomOffset, randomOffset), 0f, Random.Range(-randomOffset, randomOffset)), Quaternion.identity);
        }
        Debug.Log($"ItemSpawner: Spawned {positions.Count} prefabs at provided positions.");
    }
}
