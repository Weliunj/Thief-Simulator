using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcess : MonoBehaviour
{
    public Volume myVolume;
    public StarterAssets.ThirdPersonController controller; 
    
    private Vignette _vignette;
    private DepthOfField _depthOfField;
    private ChromaticAberration _chromatic; // Sửa tên cho gọn

    // Các biến Velocity phải tách biệt hoàn toàn
    private float _vignetteVelocity;
    private float _dofVelocity;
    private float _diedVelocity;
    private float _sprintVelocity; // Thêm biến này cho Sprint

    void Start()
    {
        if (myVolume != null && myVolume.profile != null)
        {
            myVolume.profile.TryGet(out _vignette);
            myVolume.profile.TryGet(out _depthOfField);
            myVolume.profile.TryGet(out _chromatic); // Cách lấy đúng trong URP
        }
    }

    void Update()
    {
        if (controller == null) return;

        // Ưu tiên trạng thái Chết
        if (controller.player.isDied)
        {
            Died();
        }
        else
        {
            HandleVignette();
            HandleChromaticAberration(); // Chỉ chạy khi còn sống
        }
        
        HandleDepthOfField();
    }

    void HandleVignette()
    {
        if (_vignette == null) return;
        float targetVignette = controller.Crouching ? 0.45f : 0.2f;
        _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, targetVignette, ref _vignetteVelocity, 0.15f);
    }

    void HandleDepthOfField()
    {
        if (_depthOfField == null) return;
        float currentWeight = controller.player.currweight;
        float targetFocal = (currentWeight < 50f) ? 1f : Mathf.Lerp(30f, 130f, Mathf.InverseLerp(50f, 100f, currentWeight));

        _depthOfField.focalLength.value = Mathf.SmoothDamp(_depthOfField.focalLength.value, targetFocal, ref _dofVelocity, 0.1f);
    }

    void HandleChromaticAberration()
    {
        if (_chromatic == null) return;

        // Kiểm tra xem có đang chạy nhanh (Sprint) không
        // Lưu ý: controller.targetSpeed là float, nên ta so sánh với MoveSpeed
        bool isSprinting = controller.targetSpeed > controller.player.MoveSpeed + 0.1f;
        float targetIntensity = isSprinting ? 0.25f : 0f;

        _chromatic.intensity.value = Mathf.SmoothDamp(
            _chromatic.intensity.value, 
            targetIntensity, 
            ref _sprintVelocity, 
            0.2f // Thời gian làm mượt khi bắt đầu chạy
        );
    }

    void Died()
    {
        if (_vignette == null) return;
        _vignette.rounded.value = true;
        _vignette.smoothness.value = 1f;
        _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, 0.35f, ref _diedVelocity, 1.5f);
    }
}