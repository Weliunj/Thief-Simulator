using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenshotUtility : MonoBehaviour
{
    [Header("📷 Screenshot Settings")]
    [Tooltip("Phím tắt chụp ảnh nhanh trong Play Mode")]
    public KeyCode screenshotKey = KeyCode.F12;

    [Tooltip("Tỷ lệ độ phân giải (1: 100%, 2: 200%, 4: 400% độ phân giải siêu nét)")]
    public int superSize = 1;

    [Tooltip("Thư mục lưu ảnh (mặc định là thư mục 'Screenshots' trong dự án)")]
    public string folderName = "Screenshots";

    private static ScreenshotUtility _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    /// <summary>
    /// Hàm chụp ảnh màn hình có thể gọi từ bất kỳ đâu.
    /// Ví dụ: ScreenshotUtility.TakeScreenshot("Chapter1_Intro");
    /// </summary>
    /// <param name="customName">Tên tùy chỉnh cho file ảnh (để trống sẽ tự đặt tên theo ngày giờ)</param>
    /// <param name="scale">Tỷ lệ siêu phân giải (mặc định 1x)</param>
    public static string TakeScreenshot(string customName = "", int scale = 1)
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName;
        if (!string.IsNullOrEmpty(customName))
        {
            fileName = $"{customName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        }
        else
        {
            fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        }

        string fullPath = Path.Combine(folderPath, fileName);
        
        ScreenCapture.CaptureScreenshot(fullPath, scale);
        Debug.Log($"<color=cyan>📸 [Screenshot Saved]:</color> {fullPath}");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        return fullPath;
    }

#if UNITY_EDITOR
    [MenuItem("Tools/📷 Chụp ảnh màn hình (Screenshot) %&s")]
    public static void CaptureFromMenu()
    {
        string path = TakeScreenshot("EditorCap", 1);
        EditorUtility.RevealInFinder(path);
    }
#endif
}
