using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [Header("📊 FPS Debug Settings")]
    public bool showFPSLog = true;
    public bool showFPSOnScreen = true; // Hiển thị góc màn hình khi test APK trên Android

    private float deltaTime = 0.0f;
    private float fpsTimer = 0.0f;
    private int frameCount = 0;
    private float currentFPS = 0.0f;

    void Awake()
    {
        // Tắt đồng bộ dọc để tự quyết định FPS
        QualitySettings.vSyncCount = 0; 

        // Khóa mục tiêu ở 60 FPS (hoặc 90, 120 nếu muốn)
        Application.targetFrameRate = 60; 

        // Giảm bớt 15% độ phân giải 3D để nhẹ máy (UI vẫn nét)
        ScalableBufferManager.ResizeBuffers(0.85f, 0.85f);
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fpsTimer += Time.unscaledDeltaTime;
        frameCount++;

        // Debug Log FPS mỗi 1 giây
        if (fpsTimer >= 1.0f)
        {
            currentFPS = frameCount / fpsTimer;
            if (showFPSLog)
            {
                Debug.Log($"[FPS DEBUG] FPS: {Mathf.RoundToInt(currentFPS)} | FrameTime: {(deltaTime * 1000.0f):F1} ms");
            }
            fpsTimer = 0.0f;
            frameCount = 0;
        }
    }

    private GUIStyle _fpsStyle;

    void OnGUI()
    {
        if (!showFPSOnScreen) return;

        if (_fpsStyle == null)
        {
            _fpsStyle = new GUIStyle();
            _fpsStyle.alignment = TextAnchor.UpperLeft;
            _fpsStyle.fontStyle = FontStyle.Bold;
        }

        int w = Screen.width, h = Screen.height;
        Rect rect = new Rect(20, 20, w, h * 2 / 100);
        _fpsStyle.fontSize = Mathf.Clamp(h * 2 / 50, 20, 60);
        
        // Đổi màu theo mức độ mượt: Xanh >= 45 FPS, Vàng >= 30 FPS, Đỏ < 30 FPS
        _fpsStyle.normal.textColor = currentFPS >= 45 ? Color.green : (currentFPS >= 30 ? Color.yellow : Color.red);

        float msec = deltaTime * 1000.0f;
        string text = string.Format("FPS: {0:0.} ({1:0.0} ms)", currentFPS, msec);
        GUI.Label(rect, text, _fpsStyle);
    }
}