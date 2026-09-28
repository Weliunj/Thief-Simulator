using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcess : MonoBehaviour
{
    [Header("🎬 Volume & Controller")]
    public Volume myVolume;
    public StarterAssets.PlayerController controller;

    [Header("🌑 Vignette Settings")]
    [Range(0f, 1f)] public float defaultVignette = 0.2f;  // Viền tối khi đi bộ / đứng bình thường
    [Range(0f, 1f)] public float crouchVignette = 0.45f;  // Viền tối khi cúi người (Crouching)
    [Range(0f, 1f)] public float diedVignette = 0.35f;    // Viền tối khi chết (Died)

    private Vignette _vignette;
    private float _vignetteVelocity;
    private float _diedVelocity;

    void Start()
    {
        // 1. Tự động tìm Controller nếu chưa gán
        if (controller == null)
        {
            controller = FindAnyObjectByType<StarterAssets.PlayerController>();
        }

        // 2. Tự động tìm Volume trong Scene nếu chưa gán
        if (myVolume == null)
        {
            myVolume = FindFirstObjectByType<Volume>(FindObjectsInactive.Include);
        }

        // 3. Đảm bảo Volume luôn được bật (Active & Enabled)
        if (myVolume != null)
        {
            myVolume.gameObject.SetActive(true);
            myVolume.enabled = true;
            if (myVolume.profile != null)
            {
                myVolume.profile.TryGet(out _vignette);
            }
        }

        // 4. Tự động bật Post Processing trên Camera URP
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
            }
        }

        // 5. Đảm bảo áp dụng đúng RenderScale từ SettingsManager khi vào Scene
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyGraphicsSettings();
        }
    }

    void Update()
    {
        // Tự động tìm lại Controller nếu lúc Start nhân vật chưa kịp sinh ra
        if (controller == null)
        {
            controller = FindAnyObjectByType<StarterAssets.PlayerController>();
            if (controller == null) return;
        }

        // 1. Trạng thái chết (Died)
        if (controller.player != null && controller.player.isDied)
        {
            HandleDied();
        }
        else
        {
            // 2. Trạng thái bình thường / Cúi người
            HandleVignette();
        }
    }

    void HandleVignette()
    {
        // VIGNETTE: Chỉ phụ thuộc vào Cúi người hoặc Đi bộ bình thường
        float targetVignette = controller.Crouching ? crouchVignette : defaultVignette;

        if (_vignette != null)
        {
            _vignette.rounded.value = false;
            _vignette.smoothness.value = 0.8f;
            _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, targetVignette, ref _vignetteVelocity, 0.15f);
        }
    }

    void HandleDied()
    {
        if (_vignette != null)
        {
            _vignette.rounded.value = true;
            _vignette.smoothness.value = 0.8f;
            _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, diedVignette, ref _diedVelocity, 1.5f);
        }
    }
}