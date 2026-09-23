using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class SettingsData
{
    public float sensitivity = 2.0f;
    public int targetFPS = 60;
    public bool vSync = false;
    public float renderScale = 1f;
    public bool showFPSOnScreen = true;
    public bool showFPSLog = false;
    public float bgmVolume = 0.8f; // 0.0 -> 1.0
    public float sfxVolume = 1.0f; // 0.0 -> 1.0
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
                _instance = FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
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

    [Header("🔊 Audio & Mixer References")]
    [Tooltip("AudioMixer chính của game (chứa 2 Group BGM và SFX)")]
    public AudioMixer mainAudioMixer;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    private string saveFilePath;
    private SettingsData settingsData = new SettingsData();

    public float Sensitivity => settingsData != null ? settingsData.sensitivity : 2.0f;
    public int TargetFPS => settingsData != null ? settingsData.targetFPS : 60;
    public bool VSync => settingsData != null ? settingsData.vSync : false;
    public float RenderScale => settingsData != null ? settingsData.renderScale : 0.85f;
    public bool ShowFPSOnScreen => settingsData != null ? settingsData.showFPSOnScreen : true;
    public bool ShowFPSLog => settingsData != null ? settingsData.showFPSLog : false;
    public float BGMVolume => settingsData != null ? settingsData.bgmVolume : 0.8f;
    public float SFXVolume => settingsData != null ? settingsData.sfxVolume : 1.0f;

    public static event Action<float> OnSensitivityChanged;
    public static event Action<float> OnBGMVolumeChanged;
    public static event Action<float> OnSFXVolumeChanged;
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
        // Nếu đang là con của GameObject khác (như GameManager), tự động tách ra Root để DontDestroyOnLoad hoạt động 100%
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSavePath();
            LoadSettings();
            EnsureMixerReferences();
        }
        else if (_instance != this)
        {
            // Nếu instance đang chạy chưa được gán mixer mà object mới này trong Scene có -> chuyển tham chiếu sang
            if (_instance.mainAudioMixer == null && mainAudioMixer != null)
            {
                _instance.mainAudioMixer = mainAudioMixer;
                _instance.bgmGroup = bgmGroup;
                _instance.sfxGroup = sfxGroup;
                _instance.ApplyAudioSettings();
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Tự động nạp 2 Group BGM / SFX từ mainAudioMixer nếu chưa được kéo vào Inspector
    /// </summary>
    public void EnsureMixerReferences()
    {
        if (mainAudioMixer != null)
        {
            if (bgmGroup == null)
            {
                AudioMixerGroup[] bgmGroups = mainAudioMixer.FindMatchingGroups("BGM");
                if (bgmGroups != null && bgmGroups.Length > 0)
                {
                    bgmGroup = bgmGroups[0];
                }
            }

            if (sfxGroup == null)
            {
                AudioMixerGroup[] sfxGroups = mainAudioMixer.FindMatchingGroups("SFX");
                if (sfxGroups != null && sfxGroups.Length > 0)
                {
                    sfxGroup = sfxGroups[0];
                }
            }
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
            fpsDisplayText = $"{Mathf.RoundToInt(currentFPS)}";

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
            fpsStyle.alignment = TextAnchor.LowerLeft;
            fpsStyle.fontStyle = FontStyle.Normal;
            fpsStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f); // Trắng trong suốt nhẹ, tinh tế
        }

        int w = Screen.width, h = Screen.height;
        // Kích thước nhỏ gọn (bằng 1 nửa trước đây), đặt ở góc dưới bên trái
        int fontSize = Mathf.Clamp(h / 70, 11, 20);
        fpsStyle.fontSize = fontSize;

        Rect rect = new Rect(15, h - fontSize - 12, 100, fontSize + 8);

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
        ApplyAudioSettings();
        OnSensitivityChanged?.Invoke(settingsData.sensitivity);
        OnBGMVolumeChanged?.Invoke(settingsData.bgmVolume);
        OnSFXVolumeChanged?.Invoke(settingsData.sfxVolume);
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
            ApplyAudioSettings();
            OnSensitivityChanged?.Invoke(settingsData.sensitivity);
            OnBGMVolumeChanged?.Invoke(settingsData.bgmVolume);
            OnSFXVolumeChanged?.Invoke(settingsData.sfxVolume);
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

    public void ApplyAudioSettings()
    {
        if (settingsData == null) return;

        EnsureMixerReferences();
        AutoRouteAllSceneAudioSources();
        SetBGMVolume(settingsData.bgmVolume, false);
        SetSFXVolume(settingsData.sfxVolume, false);
    }

    /// <summary>
    /// Tự động quét toàn bộ AudioSource trong Scene chưa gán Output để nối vào BGM/SFX Group
    /// </summary>
    public void AutoRouteAllSceneAudioSources()
    {
        EnsureMixerReferences();

        AudioSource[] allAudio = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allAudio == null || allAudio.Length == 0) return;

        foreach (var src in allAudio)
        {
            if (src == null || src.outputAudioMixerGroup != null) continue;

            string objName = src.gameObject.name.ToLower();
            if (src.loop || objName.Contains("bgm") || objName.Contains("music") || objName.Contains("background"))
            {
                if (bgmGroup != null)
                {
                    src.outputAudioMixerGroup = bgmGroup;
                }
            }
            else
            {
                if (sfxGroup != null)
                {
                    src.outputAudioMixerGroup = sfxGroup;
                }
            }
        }
    }

    /// <summary>
    /// Chuyển đổi mức âm lượng 0.0 -> 1.0 sang thang đo Decibel (-80dB -> 0dB) cho AudioMixer
    /// </summary>
    private float VolumeToDecibels(float volume)
    {
        if (volume <= 0.0001f) return -80f;
        return Mathf.Log10(Mathf.Clamp01(volume)) * 20f;
    }

    public void SetBGMVolume(float volume, bool save = true)
    {
        volume = Mathf.Clamp01(volume);
        settingsData.bgmVolume = volume;

        EnsureMixerReferences();

        if (mainAudioMixer != null)
        {
            float db = VolumeToDecibels(volume);
            bool ok = mainAudioMixer.SetFloat("BGMVolume", db);
            if (!ok)
            {
                Debug.LogWarning($"[SettingsManager] mainAudioMixer.SetFloat('BGMVolume', {db:F1} dB) trả về FALSE! Vui lòng kiểm tra Exposed Parameter 'BGMVolume' trong MainMixer!");
            }
            else
            {
                Debug.Log($"[SettingsManager] Đã cập nhật BGMVolume = {Mathf.RoundToInt(volume * 100f)}% ({db:F1} dB)");
            }
        }
        else
        {
            Debug.LogWarning("[SettingsManager] mainAudioMixer is NULL! Chưa thể gán âm lượng BGM.");
        }

        OnBGMVolumeChanged?.Invoke(volume);

        if (save) SaveSettings();
    }

    public void SetSFXVolume(float volume, bool save = true)
    {
        volume = Mathf.Clamp01(volume);
        settingsData.sfxVolume = volume;

        EnsureMixerReferences();

        if (mainAudioMixer != null)
        {
            float db = VolumeToDecibels(volume);
            bool ok = mainAudioMixer.SetFloat("SFXVolume", db);
            if (!ok)
            {
                Debug.LogWarning($"[SettingsManager] mainAudioMixer.SetFloat('SFXVolume', {db:F1} dB) trả về FALSE! Vui lòng kiểm tra Exposed Parameter 'SFXVolume' trong MainMixer!");
            }
            else
            {
                Debug.Log($"[SettingsManager] Đã cập nhật SFXVolume = {Mathf.RoundToInt(volume * 100f)}% ({db:F1} dB)");
            }
        }
        else
        {
            Debug.LogWarning("[SettingsManager] mainAudioMixer is NULL! Chưa thể gán âm lượng SFX.");
        }

        OnSFXVolumeChanged?.Invoke(volume);

        if (save) SaveSettings();
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

    public void SetAllSettings(float sensitivity, int targetFPS, bool showFPSOnScreen, float bgmVolume = -1f, float sfxVolume = -1f)
    {
        float snappedValue = Mathf.Round(sensitivity * 2f) / 2f;
        settingsData.sensitivity = Mathf.Clamp(snappedValue, 1.0f, 10.0f);
        settingsData.targetFPS = targetFPS;
        settingsData.showFPSOnScreen = showFPSOnScreen;

        if (bgmVolume >= 0f) settingsData.bgmVolume = Mathf.Clamp01(bgmVolume);
        if (sfxVolume >= 0f) settingsData.sfxVolume = Mathf.Clamp01(sfxVolume);

        SaveSettings();
    }
}
