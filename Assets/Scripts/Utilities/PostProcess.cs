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
        controller = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
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

        // Trọng lượng từ 25kg -> 75kg sẽ tương ứng tính Focal Length từ 0 -> 50
        float t = Mathf.InverseLerp(25f, 75f, currentWeight);
        float targetFocal = Mathf.Lerp(0f, 50f, t);

        _depthOfField.focalLength.value = Mathf.SmoothDamp(_depthOfField.focalLength.value, targetFocal, ref _dofVelocity, 0.1f);
    }
    void Died()
    {
        if (_vignette == null) return;
        _vignette.rounded.value = true;
        _vignette.smoothness.value = 1f;
        _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, 0.35f, ref _diedVelocity, 1.5f);
    }
}