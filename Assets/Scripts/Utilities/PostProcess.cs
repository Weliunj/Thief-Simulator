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

    [Header("🎯 Dynamic Resolution Settings (Làm mờ thay DoF & Tăng FPS)")]
    public bool enableDynamicResolution = true;
    [Range(0.2f, 1f)] public float baseResolutionScale = 0.6f;          // Độ nét khi bình thường
    [Range(0.05f, 1f)] public float heavyWeightResolutionScale = 0.3f;  // Độ nhòe khi mang đồ nặng (75kg)
    [Range(0.2f, 1f)] public float diedResolutionScale = 0.4f;         // Độ nhòe khi bị bắt / chết

    private Vignette _vignette;
    private float _vignetteVelocity;
    private float _diedVelocity;
    private float _currentResolutionScale = 0.85f;
    private float _resolutionVelocity;

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

        // 4. Tự động bật Post Processing & Allow Dynamic Resolution trên Camera URP
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.allowDynamicResolution = true;
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
            }
        }

        // 5. Khởi tạo Dynamic Resolution ban đầu
        _currentResolutionScale = baseResolutionScale;
        if (enableDynamicResolution)
        {
            ScalableBufferManager.ResizeBuffers(_currentResolutionScale, _currentResolutionScale);
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
            ApplyDynamicResolution(diedResolutionScale);
        }
        else
        {
            // 2. Trạng thái bình thường / Cúi người / Mang vác nặng
            HandleVignetteAndResolution();
        }
    }

    void HandleVignetteAndResolution()
    {
        // --- 1. VIGNETTE: Chỉ phụ thuộc vào Cúi người hoặc Đi bộ bình thường ---
        float targetVignette = controller.Crouching ? crouchVignette : defaultVignette;

        if (_vignette != null)
        {
            _vignette.rounded.value = false;
            _vignette.smoothness.value = 0.8f;
            _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, targetVignette, ref _vignetteVelocity, 0.15f);
        }

        // --- 2. DYNAMIC RESOLUTION: Giảm độ phân giải khi mang đồ nặng (25kg -> 75kg) ---
        float targetResolution = baseResolutionScale;
        if (controller.player != null)
        {
            float currentWeight = controller.player.currweight;
            float t = Mathf.InverseLerp(25f, 75f, currentWeight);
            targetResolution = Mathf.Lerp(baseResolutionScale, heavyWeightResolutionScale, t);
        }

        ApplyDynamicResolution(targetResolution);
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

    private void ApplyDynamicResolution(float targetScale)
    {
        if (!enableDynamicResolution) return;

        // Chuyển đổi độ phân giải mượt mà
        _currentResolutionScale = Mathf.SmoothDamp(_currentResolutionScale, targetScale, ref _resolutionVelocity, 0.2f);

        // 1. Dành cho URP (Hoạt động ngay lập tức 100% cả trong Unity Editor lẫn APK)
        var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = _currentResolutionScale;
        }

        // 2. Dành cho Dynamic Resolution buffer của Engine
        ScalableBufferManager.ResizeBuffers(_currentResolutionScale, _currentResolutionScale);
    }

    void OnDisable()
    {
        // Khôi phục lại độ phân giải gốc khi dừng game hoặc tắt script
        var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = baseResolutionScale;
        }
    }
}