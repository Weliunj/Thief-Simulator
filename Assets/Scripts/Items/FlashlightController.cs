using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Quản lý Đèn pin / Bật lửa / Đèn cầm tay (Flashlight / Zipper Light):
/// - Là Special Item trong túi đồ / Hotbar
/// - Khi cầm trên tay: Bấm nút Interact (hoặc phím F) để Bật/Tắt đèn
/// - Hỗ trợ cả Spot Light, Point Light hoặc danh sách nhiều Light con
/// - Chỉ phát âm thanh nếu AudioClip tương ứng được gán (không bị phát nhầm khi thiếu Turn Off)
/// - Tự động thiết lập góc ngửa lên / cúi xuống bám theo Camera (followCameraPitch)
/// </summary>
public class FlashlightController : MonoBehaviour, IInteractable, IHeldInteractable
{
    [Header("🔦 Light Sources Settings")]
    [Tooltip("Component Light chính (để trống sẽ tự tìm trong children hoặc mảng lights)")]
    public Light flashlightLight;

    [Tooltip("Danh sách các Light con cần bật/tắt đồng thời (hỗ trợ nhiều Spot/Point Light)")]
    public Light[] additionalLights;

    [Tooltip("Nếu tích chọn, code sẽ tự động ghi đè Range, Angle, Color theo cài đặt bên dưới. Nếu bỏ chọn, giữ nguyên thiết lập có sẵn trên Prefab.")]
    public bool overrideLightProperties = false;

    [Tooltip("Cường độ ánh sáng khi bật (Intensity)")]
    public float lightIntensity = 2.5f;

    [Tooltip("Khoảng cách chiếu xa (Range)")]
    public float lightRange = 25f;

    [Tooltip("Góc chiếu tỏa của chùm sáng (Spot Angle - chỉ áp dụng cho Spot Light)")]
    public float spotAngle = 55f;

    [Tooltip("Màu sắc của ánh sáng")]
    public Color lightColor = new Color(1f, 0.98f, 0.92f);

    [Tooltip("Trạng thái bật/tắt ban đầu")]
    public bool isOn = false;

    [Header("📐 Light Transform Overrides (Chỉ dùng nếu tự tạo Light mới khi thiếu)")]
    public Vector3 lightLocalOffset = new Vector3(0f, 0f, 0.2f);
    public Vector3 lightLocalRotation = Vector3.zero;

    [Header("🔊 3D Spatial Audio (Âm thanh phát tại vật phẩm)")]
    public AudioSource audioSource;

    [Tooltip("Âm thanh khi BẬT (Click On / Quẹt lửa). Để trống nếu không dùng.")]
    public AudioClip turnOnSound;

    [Tooltip("Âm thanh khi TẮT (Click Off / Dập lửa). Để trống nếu không dùng.")]
    public AudioClip turnOffSound;

    private Item itemComp;
    private readonly List<Light> allManagedLights = new List<Light>();

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();
        UpdateLightState();
    }

    private void InitializeComponents()
    {
        if (itemComp == null) itemComp = GetComponent<Item>();

        allManagedLights.Clear();

        // 1. Tìm hoặc thu thập tất cả Light có sẵn
        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>(true);
        }

        if (flashlightLight != null && !allManagedLights.Contains(flashlightLight))
        {
            allManagedLights.Add(flashlightLight);
        }

        if (additionalLights != null)
        {
            foreach (var l in additionalLights)
            {
                if (l != null && !allManagedLights.Contains(l))
                {
                    allManagedLights.Add(l);
                }
            }
        }

        // Nếu hoàn toàn không có Light nào, tự tạo 1 Spot Light mặc định
        if (allManagedLights.Count == 0)
        {
            GameObject lightObj = new GameObject("FlashlightLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = lightLocalOffset;
            lightObj.transform.localRotation = Quaternion.Euler(lightLocalRotation);
            flashlightLight = lightObj.AddComponent<Light>();
            flashlightLight.type = LightType.Spot;
            allManagedLights.Add(flashlightLight);
        }

        // Tùy chỉnh thuộc tính nếu được yêu cầu
        if (overrideLightProperties)
        {
            foreach (var l in allManagedLights)
            {
                if (l == null) continue;
                l.range = lightRange;
                l.color = lightColor;
                if (l.type == LightType.Spot)
                {
                    l.spotAngle = spotAngle;
                }
            }
        }

        // 2. Tự động thiết lập AudioSource 3D
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (turnOnSound != null || turnOffSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Spatial Sound
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 20f;
            audioSource.playOnAwake = false;
        }

        if (audioSource != null && audioSource.outputAudioMixerGroup == null && SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            audioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
        }
    }


    private float lastToggleTime = -1f;
    private const float TOGGLE_COOLDOWN = 0.2f;

    /// <summary>
    /// Chuyển đổi trạng thái Bật / Tắt đèn
    /// </summary>
    public void ToggleFlashlight()
    {
        if (Time.time - lastToggleTime < TOGGLE_COOLDOWN)
        {
            return; // Chặn spam click / gọi 2 lần trong cùng frame
        }
        lastToggleTime = Time.time;

        SetFlashlightState(!isOn);
    }

    /// <summary>
    /// Thiết lập trạng thái Bật / Tắt và đồng bộ qua mạng Photon Fusion
    /// </summary>
    public void SetFlashlightState(bool enable)
    {
        // 1. Áp dụng ngay trên máy cục bộ
        ApplyFlashlightVisualAndAudio(enable);

        // 2. Nếu đang trong phòng Online, gửi RPC đồng bộ cho những người chơi khác
        var localSync = NetworkItemSync.GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            NetworkItemSync.SyncFlashlightState(gameObject, enable);
        }
    }

    /// <summary>
    /// Áp dụng ánh sáng và phát âm thanh Bật/Tắt (được gọi từ RPC mạng hoặc cục bộ)
    /// </summary>
    public void ApplyFlashlightVisualAndAudio(bool enable)
    {
        bool stateChanged = (isOn != enable);
        isOn = enable;
        UpdateLightState();

        // Chỉ phát âm thanh nếu trạng thái thực sự thay đổi và clip tương ứng được gán
        if (stateChanged)
        {
            AudioClip clipToPlay = isOn ? turnOnSound : turnOffSound;
            if (clipToPlay != null && audioSource != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }
    }


    private void UpdateLightState()
    {
        if (allManagedLights.Count == 0)
        {
            InitializeComponents();
        }

        foreach (var l in allManagedLights)
        {
            if (l == null) continue;
            l.enabled = isOn;
            if (overrideLightProperties)
            {
                l.intensity = isOn ? lightIntensity : 0f;
            }
        }
    }

    // =========================================================================
    //                    IHELDINTERACTABLE IMPLEMENTATION (Khi cầm trên tay)
    // =========================================================================

    public string GetHeldActionPrompt() => isOn ? "Turn Off" : "Turn On";

    public Sprite GetHeldActionIcon() => (itemComp != null) ? itemComp.GetIcon() : null;

    public bool CanInteractWhileHeld() => true;

    public void OnHeldInteract(PlayerController player)
    {
        ToggleFlashlight();
    }

    // =========================================================================
    //                    IINTERACTABLE IMPLEMENTATION (Khi dưới đất)
    // =========================================================================

    public string GetInteractableName() => (itemComp != null) ? itemComp.GetInteractableName() : "Light Item";
    public string GetActionPrompt() => "Take " + GetInteractableName();
    public int GetPrice() => (itemComp != null) ? itemComp.GetPrice() : 50;
    public int GetWeight() => (itemComp != null) ? itemComp.GetWeight() : 1;
    public bool IsLootItem() => true;
    public string GetDescription() => (itemComp != null) ? itemComp.GetDescription() : "A portable light source.";
    public Sprite GetIcon() => (itemComp != null) ? itemComp.GetIcon() : null;
    public ItemRarity GetRarity() => (itemComp != null) ? itemComp.GetRarity() : ItemRarity.Common;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        failReason = "";
        return true;
    }

    public void Interact(PlayerController player)
    {
        // Khi ở dưới đất: Nhặt vào túi đồ
        if (player != null && player.inventory != null)
        {
            player.inventory.TryPickupItem(gameObject, GetWeight());
        }
    }
}

