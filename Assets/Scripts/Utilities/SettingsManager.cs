using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public float sensitivity = 2.0f;
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

    public static event Action<float> OnSensitivityChanged;

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
                Debug.Log($"[SettingsManager] Loaded settings from JSON: Sensitivity = {settingsData.sensitivity}");
            }
            else
            {
                settingsData = new SettingsData();
                SaveSettings(); // Save default values
                Debug.Log($"[SettingsManager] Created default settings: Sensitivity = {settingsData.sensitivity}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SettingsManager] Failed to load settings: {ex.Message}");
            settingsData = new SettingsData();
        }

        OnSensitivityChanged?.Invoke(settingsData.sensitivity);
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
            OnSensitivityChanged?.Invoke(settingsData.sensitivity);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SettingsManager] Failed to save settings: {ex.Message}");
        }
    }

    public void SetSensitivity(float value)
    {
        // Clamp between 1.0 and 5.0 in steps of 0.5
        float snappedValue = Mathf.Round(value * 2f) / 2f;
        snappedValue = Mathf.Clamp(snappedValue, 1.0f, 5.0f);

        settingsData.sensitivity = snappedValue;
        SaveSettings();
    }
}
