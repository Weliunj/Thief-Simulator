using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Tự động đảm bảo EventSystem và Canvas trong Scene hoạt động 100% với Chuột trên PC (Game View, Standalone EXE)
/// lẫn Cảm ứng trên Mobile (Simulator, Android, iOS).
/// 
/// Tự động:
/// 1. Mở khóa con trỏ chuột trong Menu/UI (Cursor.lockState = None, Cursor.visible = true).
/// 2. Cấu hình InputSystemUIInputModule với Default Actions để click chuột và touch hoạt động trơn tru.
/// 3. Đảm bảo Canvas có GraphicRaycaster và Camera hợp lệ.
/// </summary>
[DefaultExecutionOrder(-500)]
public class UIEventSystemFixer : MonoBehaviour
{
    [Header("⚙️ Settings")]
    [Tooltip("Tự động mở khóa chuột khi script này khởi chạy (rất cần cho HomeMenu, UI screens)")]
    public bool unlockCursorOnStart = true;

    [Tooltip("Tự động cấu hình default actions cho InputSystemUIInputModule")]
    public bool fixInputModule = true;

    [Tooltip("Tự động chuyển Canvas sang Screen Space - Overlay nếu thiếu Camera")]
    public bool ensureCanvasCamera = true;

    private void Awake()
    {
        ApplyCursorState();
        EnsureEventSystem();
        EnsureCanvasSetup();
    }

    private void Start()
    {
        ApplyCursorState();
        EnsureEventSystem();
    }

    private void OnEnable()
    {
        ApplyCursorState();
    }

    public void ApplyCursorState()
    {
        if (unlockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Đảm bảo trong Scene luôn có 1 EventSystem hoàn chỉnh, hỗ trợ cả New Input System và Old Input Manager
    /// </summary>
    public static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        }

        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
        }

        if (!eventSystem.gameObject.activeSelf)
        {
            eventSystem.gameObject.SetActive(true);
        }

        if (!eventSystem.enabled)
        {
            eventSystem.enabled = true;
        }

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            // Xóa StandaloneInputModule cũ nếu có để tránh conflict
            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                DestroyImmediate(oldModule);
            }

            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputSystemModule.AssignDefaultActions();
        }
        else
        {
            // Đảm bảo module đã được enable và có default actions
            if (!inputSystemModule.enabled) inputSystemModule.enabled = true;
            if (inputSystemModule.actionsAsset == null)
            {
                inputSystemModule.AssignDefaultActions();
            }
        }
#else
        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule == null)
        {
            standaloneModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
        if (!standaloneModule.enabled) standaloneModule.enabled = true;
#endif
    }

    /// <summary>
    /// Kiểm tra các Canvas trong Scene để đảm bảo nhận được Raycast chuột
    /// </summary>
    private void EnsureCanvasSetup()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;

            // Đảm bảo có GraphicRaycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // Nếu Canvas là ScreenSpaceCamera nhưng không có camera, fallback sang Overlay hoặc Camera.main
            if (ensureCanvasCamera && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (canvas.worldCamera == null)
                {
                    canvas.worldCamera = Camera.main;
                    if (canvas.worldCamera == null)
                    {
                        canvas.worldCamera = FindFirstObjectByType<Camera>();
                    }

                    // Nếu vẫn không có camera, chuyển sang ScreenSpaceOverlay để đảm bảo 100% click được
                    if (canvas.worldCamera == null)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                }
            }
        }
    }
}
