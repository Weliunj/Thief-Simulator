using StarterAssets;
using UnityEngine;

/// <summary>
/// Quản lý Đèn pin (Flashlight):
/// - Là Special Item trong túi đồ / Hotbar
/// - Khi cầm trên tay: Bấm nút Interact (hoặc phím F) để Bật/Tắt đèn pin
/// - Trang bị 3D Spatial Audio chuẩn hóa (Turn On Sound, Turn Off Sound), sẵn sàng cho Multiplayer/RPC
/// - Tự động thiết lập góc ngửa lên / cúi xuống bám theo Camera (followCameraPitch)
/// </summary>
public class FlashlightController : MonoBehaviour, IInteractable, IHeldInteractable
{
    [Header("🔦 Flashlight Light Settings")]
    [Tooltip("Component Light phát sáng (tự động tìm trong children nếu để trống)")]
    public Light flashlightLight;

    [Tooltip("Cường độ ánh sáng khi bật (Intensity)")]
    public float lightIntensity = 2.5f;

    [Tooltip("Khoảng cách chiếu xa (Range)")]
    public float lightRange = 25f;

    [Tooltip("Góc chiếu tỏa của chùm sáng (Spot Angle)")]
    public float spotAngle = 55f;

    [Tooltip("Màu sắc của ánh sáng")]
    public Color lightColor = new Color(1f, 0.98f, 0.92f); // Ánh sáng vàng nhạt tự nhiên

    [Tooltip("Trạng thái bật/tắt ban đầu")]
    public bool isOn = false;

    [Header("📐 Light Orientation & Offsets (Hướng chiếu sáng)")]
    [Tooltip("Vị trí tương đối của điểm phát sáng so với đèn pin")]
    public Vector3 lightLocalOffset = new Vector3(0f, 0f, 0.2f);

    [Tooltip("Góc xoay điều chỉnh chùm sáng (Ví dụ: (90,0,0) nếu nòng 3D Model bị ngửa lên trục Y)")]
    public Vector3 lightLocalRotation = Vector3.zero;

    [Header("🔊 3D Spatial Audio (Âm thanh phát tại đèn pin)")]
    public AudioSource audioSource;

    [Tooltip("Âm thanh khi BẬT đèn pin (Click On)")]
    public AudioClip turnOnSound;

    [Tooltip("Âm thanh khi TẮT đèn pin (Click Off)")]
    public AudioClip turnOffSound;

    private Item itemComp;

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

        // 1. Tự động tìm / tạo Light child
        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>(true);
            if (flashlightLight == null)
            {
                GameObject lightObj = new GameObject("FlashlightLight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = lightLocalOffset;
                lightObj.transform.localRotation = Quaternion.Euler(lightLocalRotation);
                flashlightLight = lightObj.AddComponent<Light>();
            }
        }

        if (flashlightLight != null)
        {
            flashlightLight.type = LightType.Spot;
            flashlightLight.range = lightRange;
            flashlightLight.spotAngle = spotAngle;
            flashlightLight.color = lightColor;

            // Đảm bảo offset và góc xoay hướng chiếu được áp dụng đúng
            if (flashlightLight.transform != transform)
            {
                flashlightLight.transform.localPosition = lightLocalOffset;
                flashlightLight.transform.localRotation = Quaternion.Euler(lightLocalRotation);
            }
        }

        // 2. Tự động thiết lập AudioSource 3D
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
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

        // 3. Đảm bảo Item component tự động bật followCameraPitch
        if (itemComp != null)
        {
            itemComp.followCameraPitch = true;
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái Bật / Tắt đèn pin
    /// </summary>
    public void ToggleFlashlight()
    {
        SetFlashlightState(!isOn);
    }

    /// <summary>
    /// Thiết lập trạng thái Bật / Tắt và đồng bộ qua mạng Photon Fusion
    /// </summary>
    public void SetFlashlightState(bool enable)
    {
        var localSync = NetworkItemSync.GetLocalPlayerSync();
        if (localSync != null && localSync.Runner != null && localSync.Runner.IsRunning)
        {
            NetworkItemSync.SyncFlashlightState(gameObject, enable);
        }
        else
        {
            ApplyFlashlightVisualAndAudio(enable);
        }
    }

    /// <summary>
    /// Áp dụng ánh sáng và phát âm thanh Bật/Tắt (được gọi từ RPC mạng hoặc cục bộ)
    /// </summary>
    public void ApplyFlashlightVisualAndAudio(bool enable)
    {
        isOn = enable;
        UpdateLightState();

        // Phát âm thanh 3D tại vị trí Đèn pin
        AudioClip clipToPlay = isOn ? turnOnSound : turnOffSound;
        if (clipToPlay == null) clipToPlay = turnOnSound ?? turnOffSound;

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void UpdateLightState()
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isOn;
            flashlightLight.intensity = isOn ? lightIntensity : 0f;
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

    public string GetInteractableName() => (itemComp != null) ? itemComp.GetInteractableName() : "Flashlight";
    public string GetActionPrompt() => "Take Flashlight";
    public int GetPrice() => (itemComp != null) ? itemComp.GetPrice() : 50;
    public int GetWeight() => (itemComp != null) ? itemComp.GetWeight() : 1;
    public bool IsLootItem() => true;
    public string GetDescription() => (itemComp != null) ? itemComp.GetDescription() : "A portable flashlight to illuminate dark areas.";
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
