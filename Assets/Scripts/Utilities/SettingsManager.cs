using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public float sensitivity = 2.0f;
    public int targetFPS = 60;
    public bool vSync = false;
    public float renderScale = 0.85f;
    public bool showFPSOnScreen = true;
    public bool showFPSLog = false;
}

public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SettingsManager");
                    _instance = go.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private string saveFilePath;
    private SettingsData settingsData = new SettingsData();

    public float Sensitivity => settingsData != null ? settingsData.sensitivity : 2.0f;
    public int TargetFPS => settingsData != null ? settingsData.targetFPS : 60;
    public bool VSync => settingsData != null ? settingsData.vSync : false;
    public float RenderScale => settingsData != null ? settingsData.renderScale : 0.85f;
    public bool ShowFPSOnScreen => settingsData != null ? settingsData.showFPSOnScreen : true;
    public bool ShowFPSLog => settingsData != null ? settingsData.showFPSLog : false;

    public static event Action<float> OnSensitivityChanged;
    public static event Action<SettingsData> OnSettingsChanged;

    // --- FPS Tracking Fields ---
    private float deltaTime = 0.0f;
    private float fpsTimer = 0.0f;
    private int frameCount = 0;
    private float currentFPS = 60.0f;
    private float currentFrameTimeMs = 16.6f;
    private string fpsDisplayText = "FPS: 60 (16.6 ms)";
    private GUIStyle fpsStyle;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSavePath();
            LoadSettings();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Tính toán FPS và cập nhật hiển thị đúng 1 giây 1 lần
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fpsTimer += Time.unscaledDeltaTime;
        frameCount++;

        if (fpsTimer >= 1.0f)
        {
            currentFPS = frameCount / fpsTimer;
            currentFrameTimeMs = deltaTime * 1000.0f;
            fpsDisplayText = $"FPS: {Mathf.RoundToInt(currentFPS)}";

            if (ShowFPSLog)
            {
                Debug.Log($"[FPS DEBUG] {fpsDisplayText}");
            }
            fpsTimer = 0.0f;
            frameCount = 0;
        }
    }

    private void OnGUI()
    {
        if (!ShowFPSOnScreen) return;

        if (fpsStyle == null)
        {
            fpsStyle = new GUIStyle();
            fpsStyle.alignment = TextAnchor.UpperLeft;
            fpsStyle.fontStyle = FontStyle.Bold;
            fpsStyle.normal.textColor = Color.white; // Chữ trắng cơ bản, không đổi màu
        }

        int w = Screen.width, h = Screen.height;
        Rect rect = new Rect(20, 20, w, h * 2 / 100);
        fpsStyle.fontSize = Mathf.Clamp(h * 2 / 50, 18, 50);

        // Hiển thị text cố định 1 giây cập nhật 1 lần
        GUI.Label(rect, fpsDisplayText, fpsStyle);
    }

    private void InitializeSavePath()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "game_settings.json");
    }

    public void LoadSettings()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            InitializeSavePath();
        }

        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                settingsData = JsonUtility.FromJson<SettingsData>(json);
                if (settingsData == null)
                {
                    settingsData = new SettingsData();
                }
                Debug.Log($"[SettingsManager] Loaded settings: Sens={settingsData.sensitivity}, FPS={settingsData.targetFPS}, ShowFPS={settingsData.showFPSOnScreen}");
            }
            else
            {
                settingsData = new SettingsData();
                SaveSettings();
                Debug.Log($"[SettingsManager] Created default settings: Sens={settingsData.sensitivity}, FPS={settingsData.targetFPS}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SettingsManager] Failed to load settings: {ex.Message}");
            settingsData = new SettingsData();
        }

        ApplyGraphicsSettings();
        OnSensitivityChanged?.Invoke(settingsData.sensitivity);
        OnSettingsChanged?.Invoke(settingsData);
    }

    public void SaveSettings()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            InitializeSavePath();
        }

        try
        {
            string json = JsonUtility.ToJson(settingsData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"[SettingsManager] Saved settings to JSON: {saveFilePath}\n{json}");
            ApplyGraphicsSettings();
            OnSensitivityChanged?.Invoke(settingsData.sensitivity);
            OnSettingsChanged?.Invoke(settingsData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SettingsManager] Failed to save settings: {ex.Message}");
        }
    }

    public void ApplyGraphicsSettings()
    {
        if (settingsData == null) return;

        // 1. Đồng bộ dọc (vSync)
        QualitySettings.vSyncCount = settingsData.vSync ? 1 : 0;

        // 2. Khóa FPS mục tiêu (30, 60, 90, 120...)
        Application.targetFrameRate = settingsData.targetFPS;

        // 3. Render Buffer Scale (Tối ưu độ phân giải 3D mà UI vẫn nét)
        if (settingsData.renderScale > 0.1f && settingsData.renderScale <= 1.0f)
        {
            ScalableBufferManager.ResizeBuffers(settingsData.renderScale, settingsData.renderScale);
        }
    }

    public void SetSensitivity(float value)
    {
        float snappedValue = Mathf.Round(value * 2f) / 2f;
        snappedValue = Mathf.Clamp(snappedValue, 1.0f, 10.0f);
        settingsData.sensitivity = snappedValue;
        SaveSettings();
    }

    public void SetTargetFPS(int fps)
    {
        settingsData.targetFPS = fps;
        SaveSettings();
    }

    public void SetShowFPSOnScreen(bool show)
    {
        settingsData.showFPSOnScreen = show;
        SaveSettings();
    }

    public void SetAllSettings(float sensitivity, int targetFPS, bool showFPSOnScreen)
    {
        float snappedValue = Mathf.Round(sensitivity * 2f) / 2f;
        settingsData.sensitivity = Mathf.Clamp(snappedValue, 1.0f, 10.0f);
        settingsData.targetFPS = targetFPS;
        settingsData.showFPSOnScreen = showFPSOnScreen;
        SaveSettings();
    }
}
