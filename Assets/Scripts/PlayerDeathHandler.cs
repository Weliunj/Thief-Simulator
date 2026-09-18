using System.Collections;
using UnityEngine;
using StarterAssets;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("⚙️ References")]
    public ThirdPersonController playerController;
    public PlayerManager playerManager;
    public Animator animator;

    [Tooltip("Mô hình 3D nhân vật (Geometry) - Đảm bảo luôn hiện khi chết")]
    public GameObject playerModel;

    [Header("🎥 Death Camera Settings")]
    [Tooltip("Target theo dõi của Camera (mặc định lấy CinemachineCameraTarget từ ThirdPersonController)")]
    public Transform cameraTarget;
    [Tooltip("Khoảng cách kéo lùi camera ra sau khi chết")]
    public float deathCameraDistanceOffset = 2.5f;
    [Tooltip("Độ cao nâng camera lên khi chết")]
    public float deathCameraHeightOffset = 1.2f;
    [Tooltip("Tốc độ làm mượt camera khi chết")]
    public float cameraLerpSpeed = 1.5f;
    [Tooltip("Offset tâm ngắm camera hướng vào cơ thể Player khi chết")]
    public Vector3 lookAtBodyOffset = new Vector3(0f, 0.5f, 0f);

    [Header("🔊 Audio & Effects")]
    [Tooltip("AudioSource 1: Âm thanh phát ngay khi Player chết")]
    public AudioSource deathAudioSource;
    
    [Tooltip("AudioSource 2: Còi cảnh sát (Police Siren)")]
    public AudioSource policeSirenAudioSource;
    [Tooltip("Hoặc kéo thả AudioClip còi cảnh sát vào đây nếu dùng chung AudioSource")]
    public AudioClip policeSirenClip;
    
    [Tooltip("Thời gian chờ trước khi còi cảnh sát hú (mặc định 3 giây)")]
    public float policeSirenDelay = 3.0f;

    [Tooltip("Đèn cảnh báo khi chết (optional)")]
    public GameObject warningLight;

    [HideInInspector] public bool isDeadProcessed = false;
    private Vector3 originalCameraTargetLocalPos;

    void Start()
    {
        if (playerController == null) playerController = GetComponent<ThirdPersonController>();
        if (animator == null && playerController != null) animator = playerController.GetComponent<Animator>();
        if (cameraTarget == null && playerController != null && playerController.CinemachineCameraTarget != null)
        {
            cameraTarget = playerController.CinemachineCameraTarget.transform;
        }

        if (cameraTarget != null)
        {
            originalCameraTargetLocalPos = cameraTarget.localPosition;
        }
    }

    void Update()
    {
        // Khi Player đã chết: liên tục khóa góc nhìn (LookAt) vào xác Player để không bao giờ bị mất hình
        if (isDeadProcessed && cameraTarget != null)
        {
            Vector3 bodyPos = transform.position + lookAtBodyOffset;
            Transform mainCam = Camera.main != null ? Camera.main.transform : null;
            Vector3 camPos = (mainCam != null) ? mainCam.position : cameraTarget.position;
            Vector3 dir = (bodyPos - camPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                cameraTarget.rotation = Quaternion.Slerp(cameraTarget.rotation, targetRot, Time.deltaTime * 6f);
            }
        }
    }

    /// <summary>
    /// Hàm công khai xử lý toàn bộ logic khi Player chết (Có thể gọi từ bất kỳ script nào).
    /// </summary>
    public void TriggerDeath()
    {
        if (isDeadProcessed) return;
        isDeadProcessed = true;

        if (playerManager != null)
        {
            playerManager.isDied = true;
            playerManager.currweight = 0;
        }

        // 1. Đảm bảo hiển thị Full Model Player (nếu trước đó đang ở POV 1)
        if (playerModel != null)
        {
            playerModel.SetActive(true);
        }

        // 2. Chạy Animation chết
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 3. Thả toàn bộ vật phẩm đang cầm xuống đất
        if (playerController != null)
        {
            playerController.DropItemsOnDeathPublic();
        }

        // 4. Kéo lùi Camera ra đằng sau xa hơn và mượt mà để nhìn toàn thân (Full Model)
        StartCoroutine(SmoothDeathCameraPullbackRoutine());

        // 5. Bật đèn cảnh báo (nếu có)
        if (warningLight != null)
        {
            warningLight.SetActive(true);
        }

        // 6. Phát âm thanh 1 (Kêu khi chết)
        PlayDeathSound();

        // 7. Khởi chạy đếm lùi 3s phát Police Siren
        StartCoroutine(PlayPoliceSirenRoutine(policeSirenDelay));

        // 8. Tắt va chạm giữa Player và NPC
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), true);

        Debug.Log($"[PlayerDeathHandler] Player đã chết. Camera kéo mượt ra sau và Lock-On vào xác Player.");
    }

    /// <summary>
    /// Coroutine kéo mượt Camera lùi ra sau và lên cao để nhìn rõ toàn thân Player
    /// </summary>
    private IEnumerator SmoothDeathCameraPullbackRoutine()
    {
        if (cameraTarget == null && playerController != null && playerController.CinemachineCameraTarget != null)
        {
            cameraTarget = playerController.CinemachineCameraTarget.transform;
        }

        if (cameraTarget == null) yield break;

        Vector3 startLocalPos = cameraTarget.localPosition;
        Vector3 targetLocalPos = originalCameraTargetLocalPos + new Vector3(0f, deathCameraHeightOffset, -deathCameraDistanceOffset);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * cameraLerpSpeed;
            cameraTarget.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, Mathf.SmoothStep(0f, 1f, elapsed));
            yield return null;
        }

        cameraTarget.localPosition = targetLocalPos;
    }

    /// <summary>
    /// Phát âm thanh kêu khi chết ban đầu
    /// </summary>
    public void PlayDeathSound()
    {
        if (deathAudioSource != null)
        {
            if (!deathAudioSource.gameObject.activeSelf) deathAudioSource.gameObject.SetActive(true);
            deathAudioSource.Play();
        }
    }

    /// <summary>
    /// Coroutine chờ delaySeconds rồi phát còi cảnh sát
    /// </summary>
    private IEnumerator PlayPoliceSirenRoutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (policeSirenAudioSource != null)
        {
            if (!policeSirenAudioSource.gameObject.activeSelf) policeSirenAudioSource.gameObject.SetActive(true);
            policeSirenAudioSource.loop = true;
            policeSirenAudioSource.Play();
            Debug.Log("[PlayerDeathHandler] Còi cảnh sát (Police Siren) bắt đầu hú!");
        }
        else if (deathAudioSource != null && policeSirenClip != null)
        {
            deathAudioSource.PlayOneShot(policeSirenClip);
            Debug.Log("[PlayerDeathHandler] Phát clip còi cảnh sát (Police Siren) qua deathAudioSource!");
        }
        else
        {
            Debug.LogWarning("[PlayerDeathHandler] Chưa gán policeSirenAudioSource hoặc policeSirenClip!");
        }
    }

    /// <summary>
    /// Hàm Reset trạng thái khi Replay game
    /// </summary>
    public void ResetDeathState()
    {
        isDeadProcessed = false;
        if (warningLight != null) warningLight.SetActive(false);
        if (policeSirenAudioSource != null) policeSirenAudioSource.Stop();
        if (cameraTarget != null) cameraTarget.localPosition = originalCameraTargetLocalPos;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), false);
    }
}
