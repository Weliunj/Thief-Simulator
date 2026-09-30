#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;

public class TMPFontAutoReplacer : EditorWindow
{
    private TMP_FontAsset targetFontAsset;

    [MenuItem("Tools/Auto Fix Missing TMP Fonts")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontAutoReplacer>("Fix Missing TMP Fonts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto Replace Missing TMP Font Assets", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        targetFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset Thay Thế", targetFontAsset, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("1. Gán Font Này Cho Toàn Bộ Text Trong Scene Đang Mở", GUILayout.Height(35)))
        {
            if (targetFontAsset == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng kéo 1 Font Asset (SDF) vào ô trước!", "OK");
                return;
            }
            FixMissingFontsInActiveScene();
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("2. Gán Font Này Cho Toàn Bộ Prefab Trong Project", GUILayout.Height(35)))
        {
            if (targetFontAsset == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng kéo 1 Font Asset (SDF) vào ô trước!", "OK");
                return;
            }
            FixMissingFontsInAllPrefabs();
        }
    }

    private void FixMissingFontsInActiveScene()
    {
        var allText = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        Undo.RecordObjects(allText, "Fix Missing TMP Fonts");

        foreach (var txt in allText)
        {
            if (txt != null && (txt.font == null || txt.font.name.Contains("Missing")))
            {
                txt.font = targetFontAsset;
                EditorUtility.SetDirty(txt);
                count++;
            }
        }

        EditorUtility.DisplayDialog("Thành Công", $"Đã tự động sửa font cho {count} TextMeshPro trong Scene!", "OK");
    }

    private void FixMissingFontsInAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int count = 0;

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar("Đang quét Prefabs...", Path.GetFileName(path), (float)i / prefabGuids.Length);

                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab != null)
                {
                    bool modified = false;
                    var allText = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var txt in allText)
                    {
                        if (txt != null && (txt.font == null || txt.font.name.Contains("Missing")))
                        {
                            txt.font = targetFontAsset;
                            modified = true;
                            count++;
                        }
                    }

                    if (modified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    }

                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Thành Công", $"Đã tự động sửa font cho {count} TextMeshPro trong toàn bộ Prefabs!", "OK");
    }
}
#endif
