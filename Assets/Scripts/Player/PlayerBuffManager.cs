using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Định nghĩa tất cả các loại hiệu ứng Potion / Buff trong game.
/// Dễ dàng mở rộng cho các loại Potion khác như JumpBoost, NightVision, StaminaRecovery, Invisibility...
/// </summary>
public enum BuffType
{
    SpeedBoost,
    JumpBoost,
    NightVision,
    StaminaRecovery,
    Invisibility
}

/// <summary>
/// Quản lý toàn bộ hiệu ứng Buff đang áp dụng trên Nhân vật:
/// - Cho phép các cấp độ khác nhau (Speed I và Speed II) tồn tại ĐỘC LẬP cùng lúc (uống cả 2 được cộng dồn hệ số nhân tốc độ).
/// - HUD Màn hình (effectStatusPanel): Dùng hudEffectItemPrefab (chứa Name, Duration đếm ngược, ItemImg).
/// - Trên đầu Player (LocalCanvas/Effect): Dùng overheadEffectItemPrefab (chỉ có ItemImg tròn nhỏ, ẩn Name/Duration).
/// - Nhấp nháy CanvasGroup khi còn 5 giây cuối.
/// - Tự động xóa UI và khôi phục chỉ số khi hết thời gian.
/// - Hỗ trợ phát âm thanh uống thuốc 3D Spatial Audio đồng bộ qua mạng.
/// </summary>
public class PlayerBuffManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [Tooltip("Prefab hiển thị trên Màn hình HUD (chứa Name, Remaining time, ItemImg)")]
    public GameObject hudEffectItemPrefab;

    [Tooltip("Prefab hiển thị trên Đầu nhân vật (chỉ chứa ItemImg nhỏ gọn)")]
    public GameObject overheadEffectItemPrefab;

    [Header("UI Containers")]
    [Tooltip("Panel chứa icon buff trên HUD màn hình (HUD/effectStatusPanel)")]
    public Transform hudEffectContainer;

    [Tooltip("Transform chứa icon buff trên đầu (LocalCanvas/Effect)")]
    public Transform overheadEffectContainer;

    [Header("🔊 Audio Settings")]
    public AudioSource audioSource;
    public AudioClip defaultDrinkSound;

    private PlayerController _player;
    private PlayerStats _stats;
    private NetworkPlayerSync _netSync;

    // Lưu các coroutine buff đang chạy theo BuffKey duy nhất (ví dụ: "SpeedBoost_I", "SpeedBoost_II")
    private readonly Dictionary<string, Coroutine> _runningBuffCoroutines = new Dictionary<string, Coroutine>();

    // Lưu các modifier tốc độ đang hoạt động theo BuffKey
    private readonly Dictionary<string, float> _speedMultipliers = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _jumpMultipliers = new Dictionary<string, float>();

    // Lưu GameObject UI cũ theo BuffKey để dọn dẹp khi refresh cùng 1 cấp
    private readonly Dictionary<string, GameObject> _activeHudUIs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _activeOverheadUIs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _stats = GetComponent<PlayerStats>() ?? (_player != null ? _player.stats : null);
        _netSync = GetComponent<NetworkPlayerSync>();

        EnsureAudioSource();
        LocateUIContainers();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Spatial Audio
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 20f;
            audioSource.playOnAwake = false;
        }

        if (audioSource != null && audioSource.outputAudioMixerGroup == null && SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            audioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
        }
    }

    private void Start()
    {
        LocateUIContainers();
    }

    /// <summary>
    /// Tự động tìm kiếm các Panel UI và nạp Prefab nếu chưa được gán
    /// </summary>
    public void LocateUIContainers()
    {
        // 1. Tự động nạp Prefabs nếu chưa gán
        if (hudEffectItemPrefab == null)
        {
            hudEffectItemPrefab = Resources.Load<GameObject>("HUD/effectItem") ?? Resources.Load<GameObject>("effectItem");
        }
        if (overheadEffectItemPrefab == null)
        {
            overheadEffectItemPrefab = Resources.Load<GameObject>("HUD/localEffectItem") ?? Resources.Load<GameObject>("localEffectItem");
        }

        // 2. Tìm effectStatusPanel trên PlayerUI
        if (hudEffectContainer == null)
        {
            var uiManager = FindFirstObjectByType<UI_Manager>();
            if (uiManager != null)
            {
                var panel = uiManager.transform.Find("effectStatusPanel") ??
                            uiManager.transform.Find("HUD/effectStatusPanel") ??
                            uiManager.transform.Find("Canvas/effectStatusPanel");
                if (panel != null) hudEffectContainer = panel;
            }

            if (hudEffectContainer == null)
            {
                var found = GameObject.Find("effectStatusPanel");
                if (found != null) hudEffectContainer = found.transform;
            }
        }

        // 3. Tìm LocalCanvas/Effect trên đầu Player
        if (overheadEffectContainer == null)
        {
            var effectTrans = transform.Find("LocalCanvas/Effect") ??
                              transform.Find("Overhead/LocalCanvas/Effect") ??
                              transform.Find("LocalCanvas");
            if (effectTrans != null)
            {
                var eff = effectTrans.Find("Effect");
                overheadEffectContainer = eff != null ? eff : effectTrans;
            }
        }
    }

    /// <summary>
    /// Áp dụng Buff cho người chơi cục bộ: Speed I và Speed II có buffKey riêng nên tạo 2 status độc lập và cộng dồn hệ số
    /// </summary>
    public void ApplyBuff(BuffType type, string buffName, string tier, float duration, float multiplier, Sprite icon)
    {
        string buffKey = $"{type}_{tier}";

        if (_runningBuffCoroutines.TryGetValue(buffKey, out Coroutine existingRoutine))
        {
            if (existingRoutine != null) StopCoroutine(existingRoutine);
            _runningBuffCoroutines.Remove(buffKey);
        }

        _runningBuffCoroutines[buffKey] = StartCoroutine(BuffLifecycleRoutine(buffKey, type, buffName, tier, duration, multiplier, icon, false));
    }

    /// <summary>
    /// Áp dụng Buff nhận từ mạng (RPC) cho Remote Player (Hiển thị icon riêng trên đầu cho từng cấp)
    /// </summary>
    public void ApplyRemoteOverheadBuff(BuffType type, string buffName, string tier, float duration, Sprite icon)
    {
        string buffKey = $"{type}_{tier}";

        if (_runningBuffCoroutines.TryGetValue(buffKey, out Coroutine existingRoutine))
        {
            if (existingRoutine != null) StopCoroutine(existingRoutine);
            _runningBuffCoroutines.Remove(buffKey);
        }

        _runningBuffCoroutines[buffKey] = StartCoroutine(BuffLifecycleRoutine(buffKey, type, buffName, tier, duration, 1.0f, icon, true));
    }

    /// <summary>
    /// Phát âm thanh uống thuốc 3D Spatial Audio thông qua AudioSource của Player (có gán AudioMixer SFX)
    /// </summary>
    public void PlayDrinkAudio(AudioClip customClip = null)
    {
        EnsureAudioSource();

        AudioClip clipToPlay = customClip != null ? customClip : defaultDrinkSound;
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private IEnumerator BuffLifecycleRoutine(string buffKey, BuffType type, string buffName, string tier, float duration, float multiplier, Sprite icon, bool isRemoteOnly)
    {
        LocateUIContainers();

        GameObject hudUI = null;
        GameObject overheadUI = null;
        TMP_Text hudRemainingText = null;

        bool isLocal = (_netSync == null || _netSync.IsLocalPlayer);

        // 1. Tạo UI trên Màn hình HUD (Dùng effectItemPrefab: có Tên + Duration đếm giây)
        if (!isRemoteOnly && isLocal && hudEffectContainer != null)
        {
            if (_activeHudUIs.TryGetValue(buffKey, out GameObject oldHud) && oldHud != null)
            {
                Destroy(oldHud);
            }

            GameObject prefabToUse = hudEffectItemPrefab != null ? hudEffectItemPrefab : overheadEffectItemPrefab;
            if (prefabToUse != null)
            {
                hudUI = Instantiate(prefabToUse, hudEffectContainer);
                hudUI.name = $"HUD_Buff_{buffKey}";
                SetupHudEffectItemVisual(hudUI, buffName, tier, duration, icon, out hudRemainingText);
                _activeHudUIs[buffKey] = hudUI;
            }
        }

        // 2. Tạo UI trên Đầu Player (Dùng overheadEffectItemPrefab / localEffectItem: CHỈ CÓ ICON ẢNH)
        if (overheadEffectContainer != null)
        {
            if (_activeOverheadUIs.TryGetValue(buffKey, out GameObject oldOverhead) && oldOverhead != null)
            {
                Destroy(oldOverhead);
            }

            GameObject prefabToUse = overheadEffectItemPrefab != null ? overheadEffectItemPrefab : hudEffectItemPrefab;
            if (prefabToUse != null)
            {
                overheadUI = Instantiate(prefabToUse, overheadEffectContainer);
                overheadUI.name = $"Overhead_Buff_{buffKey}";
                SetupOverheadEffectItemVisual(overheadUI, icon);
                _activeOverheadUIs[buffKey] = overheadUI;

                // Ẩn icon trên đầu đối với chính bản thân Local Player để không che mắt
                if (isLocal && !isRemoteOnly && (_netSync == null || _netSync.IsLocalPlayer))
                {
                    overheadUI.SetActive(false);
                }
                else
                {
                    overheadUI.SetActive(true);
                }
            }
        }

        // 3. Kích hoạt hiệu ứng chỉ số logic (Nếu không phải remote visual)
        if (!isRemoteOnly)
        {
            ApplyStatEffect(buffKey, type, multiplier);
        }

        // 4. Vòng lặp đếm ngược thời gian + Cập nhật số giây còn lại + Nhấp nháy khi còn <= 5s
        float remaining = duration;
        CanvasGroup hudCg = hudUI != null ? hudUI.GetComponent<CanvasGroup>() : null;
        CanvasGroup overheadCg = overheadUI != null ? overheadUI.GetComponent<CanvasGroup>() : null;

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;

            // Cập nhật số giây đếm ngược trên HUD (VD: "15s")
            if (hudRemainingText != null)
            {
                hudRemainingText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, remaining))}s";
            }

            if (remaining <= 5.0f)
            {
                // Nhấp nháy mượt mà với sin wave (alpha từ 0.2 đến 1.0)
                float blinkAlpha = Mathf.PingPong(Time.time * 4f, 0.8f) + 0.2f;

                if (hudCg != null) hudCg.alpha = blinkAlpha;
                if (overheadCg != null) overheadCg.alpha = blinkAlpha;
            }

            yield return null;
        }

        // 5. Kết thúc hiệu ứng: Khôi phục chỉ số & Hủy UI
        if (!isRemoteOnly)
        {
            RemoveStatEffect(buffKey, type);
        }

        if (hudUI != null)
        {
            if (_activeHudUIs.TryGetValue(buffKey, out GameObject currentHud) && currentHud == hudUI)
            {
                _activeHudUIs.Remove(buffKey);
            }
            Destroy(hudUI);
        }
        if (overheadUI != null)
        {
            if (_activeOverheadUIs.TryGetValue(buffKey, out GameObject currentOverhead) && currentOverhead == overheadUI)
            {
                _activeOverheadUIs.Remove(buffKey);
            }
            Destroy(overheadUI);
        }

        _runningBuffCoroutines.Remove(buffKey);
    }

    private void ApplyStatEffect(string buffKey, BuffType type, float multiplier)
    {
        switch (type)
        {
            case BuffType.SpeedBoost:
                _speedMultipliers[buffKey] = multiplier;
                RecalculateCurrentSpeeds();
                break;

            case BuffType.JumpBoost:
                _jumpMultipliers[buffKey] = multiplier;
                if (_player != null)
                {
                    _player.JumpHeight *= multiplier;
                }
                break;

            case BuffType.NightVision:
                break;
        }
    }

    private void RemoveStatEffect(string buffKey, BuffType type)
    {
        switch (type)
        {
            case BuffType.SpeedBoost:
                _speedMultipliers.Remove(buffKey);
                RecalculateCurrentSpeeds();
                break;

            case BuffType.JumpBoost:
                if (_jumpMultipliers.TryGetValue(buffKey, out float mult))
                {
                    if (_player != null && mult > 0.01f)
                    {
                        _player.JumpHeight /= mult;
                    }
                    _jumpMultipliers.Remove(buffKey);
                }
                break;

            case BuffType.NightVision:
                break;
        }
    }

    /// <summary>
    /// Lấy tổng hệ số nhân tốc độ từ TẤT CẢ các Buff đang hoạt động (Ví dụ: Speed I x 1.3 và Speed II x 1.6 -> x 2.08)
    /// </summary>
    public float GetTotalSpeedMultiplier()
    {
        float totalSpeedMult = 1.0f;
        foreach (var m in _speedMultipliers.Values)
        {
            totalSpeedMult *= m;
        }
        return totalSpeedMult;
    }

    private void RecalculateCurrentSpeeds()
    {
        if (_stats != null)
        {
            _stats.CalculateWeightSpeedPenalty();
        }
    }

    /// <summary>
    /// Thiết lập UI đầy đủ cho HUD Màn hình (Name, Remaining Text, Item Icon)
    /// </summary>
    private void SetupHudEffectItemVisual(GameObject itemObj, string buffName, string tier, float duration, Sprite icon, out TMP_Text remainingText)
    {
        remainingText = null;
        if (itemObj == null) return;

        var cg = itemObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = itemObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Gán Tên hiệu ứng (VD: "Speed I" hoặc "Speed II")
        var nameText = itemObj.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(tier) ? buffName : $"{buffName} {tier}";
        }

        // Lấy Text hiển thị thời gian còn lại (VD: "15s")
        remainingText = itemObj.transform.Find("Remaining")?.GetComponent<TMP_Text>();
        if (remainingText != null)
        {
            remainingText.text = $"{Mathf.CeilToInt(duration)}s";
        }

        // Gán Sprite Icon
        SetIconImage(itemObj, icon);
    }

    /// <summary>
    /// Thiết lập UI nhỏ gọn trên đầu (Overhead): Chỉ có Icon, ẩn Name và Remaining nếu có
    /// </summary>
    private void SetupOverheadEffectItemVisual(GameObject itemObj, Sprite icon)
    {
        if (itemObj == null) return;

        var cg = itemObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = itemObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Nếu vô tình truyền prefab có Name/Remaining thì ẩn đi để trên đầu chỉ thấy icon
        var nameObj = itemObj.transform.Find("Name");
        if (nameObj != null) nameObj.gameObject.SetActive(false);

        var remainingObj = itemObj.transform.Find("Remaining");
        if (remainingObj != null) remainingObj.gameObject.SetActive(false);

        // Gán Sprite Icon
        SetIconImage(itemObj, icon);
    }

    private void SetIconImage(GameObject itemObj, Sprite icon)
    {
        if (itemObj == null || icon == null) return;

        Image targetImg = null;
        Transform mask = itemObj.transform.Find("MaskImg");
        if (mask != null)
        {
            Transform imgTrans = mask.Find("ItemImg");
            if (imgTrans != null) targetImg = imgTrans.GetComponent<Image>();
        }

        if (targetImg == null)
        {
            var allImgs = itemObj.GetComponentsInChildren<Image>(true);
            foreach (var img in allImgs)
            {
                if (img.gameObject.name.Equals("ItemImg", StringComparison.OrdinalIgnoreCase))
                {
                    targetImg = img;
                    break;
                }
            }
        }

        if (targetImg != null)
        {
            targetImg.sprite = icon;
            targetImg.enabled = true;
        }
    }
}
