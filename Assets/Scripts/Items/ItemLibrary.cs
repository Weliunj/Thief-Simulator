using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemLibrary", menuName = "Game/Item Library", order = 1)]
public class ItemLibrary : ScriptableObject
{
    [Tooltip("Optional manual list of prefabs. If empty, use CollectChildren to auto-fill from a GameObject in scene (via inspector helper).")]
    public List<GameObject> prefabs = new List<GameObject>();

    [Tooltip("If true, collection will include inactive children when using CollectFromTransform.")]
    public bool includeInactiveChildren = false;

    // Return a random prefab (or null if none)
    public GameObject GetRandomPrefab()
    {
        if (prefabs == null || prefabs.Count == 0) return null;
        return prefabs[Random.Range(0, prefabs.Count)];
    }

    // Return up to `count` random prefabs. If `allowDuplicates` is false, will attempt unique selection.
    public GameObject[] GetRandomPrefabs(int count, bool allowDuplicates = true)
    {
        if (prefabs == null || prefabs.Count == 0) return new GameObject[0];

        if (allowDuplicates)
        {
            GameObject[] arr = new GameObject[count];
            for (int i = 0; i < count; i++) arr[i] = GetRandomPrefab();
            return arr;
        }

        // unique selection
        int take = Mathf.Min(count, prefabs.Count);
        return prefabs.OrderBy(x => Random.value).Take(take).ToArray();
    }

    // Collect children of a Transform (useful in Editor to build library from a GameObject's children)
    public void CollectFromTransform(Transform root)
    {
        if (root == null) return;
        prefabs = new List<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactiveChildren))
        {
            if (child == root) continue;
            // add the root child GameObject as a prefab candidate
            if (!prefabs.Contains(child.gameObject)) prefabs.Add(child.gameObject);
        }
    }

    // Utility to remove null references
    public void CleanNulls()
    {
        if (prefabs == null) prefabs = new List<GameObject>();
        prefabs.RemoveAll(x => x == null);
    }
}
