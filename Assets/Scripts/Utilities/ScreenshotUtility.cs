using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenshotUtility : MonoBehaviour
{
    [Header("📷 Camera & Screenshot Settings")]
    [Tooltip("Camera dùng để chụp ảnh. Để trống sẽ tự động lấy Main Camera")]
    public Camera photoCamera;

    [Tooltip("Phím tắt chụp ảnh nhanh trong Play Mode")]
    public KeyCode screenshotKey = KeyCode.F12;

    [Tooltip("Ẩn giao diện UI (HUD, nút bấm, stamina...) khi chụp ảnh")]
    public bool hideUI = true;

    [Header("📐 Photo Resolution")]
    public int imageWidth = 1920;
    public int imageHeight = 1080;

    [Tooltip("Tự động mở thư mục ảnh trong Windows Explorer sau khi chụp")]
    public bool openFolderAfterCapture = false;

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

    void Start()
    {
        if (photoCamera == null)
        {
            photoCamera = Camera.main;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot(photoCamera, "", imageWidth, imageHeight, hideUI, openFolderAfterCapture);
        }
    }

    /// <summary>
    /// Hàm chụp ảnh màn hình bằng Camera chuyên dụng (có thể ẩn UI, tùy chỉnh độ phân giải).
    /// </summary>
    /// <param name="cam">Camera dùng để chụp (null = Main Camera)</param>
    /// <param name="customName">Tên file ảnh tùy chỉnh</param>
    /// <param name="width">Chiều rộng ảnh (pixel)</param>
    /// <param name="height">Chiều cao ảnh (pixel)</param>
    /// <param name="hideUI">Có ẩn UI khi chụp không</param>
    /// <param name="openFolder">Mở thư mục chứa ảnh sau khi chụp</param>
    /// <returns>Đường dẫn file ảnh vừa lưu</returns>
    public static string TakeScreenshot(Camera cam = null, string customName = "", int width = 1920, int height = 1080, bool hideUI = true, bool openFolder = true)
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            Debug.LogError("❌ [ScreenshotUtility]: Không tìm thấy Camera nào để chụp ảnh!");
            return null;
        }

        // 1. Tạo thư mục Assets/Screenshots nếu chưa có
        string folderPath = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 2. Tạo tên file theo thời gian
        string fileName = string.IsNullOrEmpty(customName)
            ? $"Photo_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png"
            : $"{customName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";

        string fullPath = Path.Combine(folderPath, fileName);

        // 3. Tạm thời ẩn Layer UI nếu hideUI = true
        int originalCullingMask = cam.cullingMask;
        int uiLayer = LayerMask.NameToLayer("UI");

        if (hideUI && uiLayer != -1)
        {
            cam.cullingMask &= ~(1 << uiLayer);
        }

        // 4. Render khung cảnh từ Camera ra RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousRT = cam.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 5. Khôi phục lại cấu hình Camera & RenderTexture cũ
        cam.targetTexture = previousRT;
        RenderTexture.active = previousActive;
        cam.cullingMask = originalCullingMask;
        DestroyImmediate(rt);

        // 6. Ghi dữ liệu file PNG xuống đĩa
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        DestroyImmediate(screenShot);

        Debug.Log($"<color=green>📸 [Camera Photo Saved]:</color> {fullPath}");

#if UNITY_EDITOR
        // Refresh để ảnh xuất hiện ngay trong bảng Project Window của Unity
        AssetDatabase.Refresh();

        if (openFolder)
        {
            EditorUtility.RevealInFinder(fullPath);
        }
#endif

        return fullPath;
    }

#if UNITY_EDITOR
    [MenuItem("Tools/📷 Chụp ảnh bằng Camera (Photo Mode) %&s")]
    public static void CaptureFromMenu()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        TakeScreenshot(cam, "CameraCap", 1920, 1080, true, false);
    }
#endif
}
