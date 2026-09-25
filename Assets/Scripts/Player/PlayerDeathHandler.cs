using System.Collections;
using UnityEngine;
using StarterAssets;

/// <summary>
/// Quản lý trạng thái tử vong và hồi sinh (Death & Respawn) của người chơi:
/// - Khi bị bắt: Kích hoạt Trigger/Animation "Die", thả rơi toàn bộ đồ trong túi (văng ra sàn).
/// - Bật đèn cảnh báo (dieLight / warningLight).
/// - Bộ đếm hồi sinh 10 giây (Respawn Cooldown):
///   + Từ 0s -> 5s: Chạy animation chết, camera kéo lùi quan sát toàn cảnh.
///   + Từ giây thứ 5 (khi còn 5s cuối): Bắt đầu phát âm thanh còi cảnh sát (Police Siren), to dần (Fade In) ở đầu và nhỏ dần (Fade Out) ở cuối.
///   + Hết 10s: Tắt còi cảnh sát, tắt dieLight, dịch chuyển Player về điểm xuất phát (Spawn Point) và khôi phục trạng thái hoạt động bình thường.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("⚙️ References")]
    public PlayerController playerController;
    public PlayerStats playerStats;
    public PlayerStats playerManager => playerStats;
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

    [Header("⏱️ Respawn Settings")]
    [Tooltip("Thời gian chờ đếm ngược để hồi sinh (mặc định 10 giây)")]
    public float respawnCooldown = 10.0f;

    [Header("💡 Lights & Visuals")]
    [Tooltip("Đèn cảnh báo chết (dieLight / warningLight)")]
    public GameObject dieLight;
    public GameObject warningLight;

    [Header("🔊 Audio & Effects")]
    [Tooltip("AudioSource 1: Âm thanh phát ngay khi Player chết")]
    public AudioSource deathAudioSource;

    [Tooltip("AudioSource 2: Còi cảnh sát (Police Siren)")]
    public AudioSource policeSirenAudioSource;
    [Tooltip("Hoặc kéo thả AudioClip còi cảnh sát vào đây nếu dùng chung AudioSource")]
    public AudioClip policeSirenClip;

    [HideInInspector] public bool isDeadProcessed = false;

    private Vector3 originalCameraTargetLocalPos;
    private Vector3 _initialSpawnPos;
    private Quaternion _initialSpawnRot;
    private Coroutine _respawnCoroutine;

    void Start()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerStats == null && playerController != null) playerStats = playerController.stats;
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (animator == null && playerController != null) animator = playerController.GetComponent<Animator>();
        if (cameraTarget == null && playerController != null && playerController.CinemachineCameraTarget != null)
        {
            cameraTarget = playerController.CinemachineCameraTarget.transform;
        }

        if (playerModel == null)
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null) playerModel = smr.gameObject;
        }

        if (cameraTarget != null)
        {
            originalCameraTargetLocalPos = cameraTarget.localPosition;
        }

        // Lưu vị trí spawn ban đầu của Player
        _initialSpawnPos = transform.position;
        _initialSpawnRot = transform.rotation;

        // Tự động tìm kiếm dieLight / warningLight nếu chưa gán
        if (dieLight == null)
        {
            Transform t = transform.Find("DieLight") ?? transform.Find("dieLight") ?? transform.Find("WarningLight") ?? transform.Find("warningLight");
            if (t != null) dieLight = t.gameObject;
        }
        if (dieLight == null && warningLight != null) dieLight = warningLight;
        if (warningLight == null && dieLight != null) warningLight = dieLight;

        SetDieLight(false);

        // Kết nối AudioSource với SFX Mixer Group trong Settings và cấu hình 3D Spatial Audio
        if (policeSirenAudioSource != null)
        {
            policeSirenAudioSource.spatialBlend = 1.0f; // 3D Sound để các người chơi khác nghe thấy từ vị trí người chết
            policeSirenAudioSource.minDistance = 1.0f;
            policeSirenAudioSource.maxDistance = 30f;
            policeSirenAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        if (SettingsManager.Instance != null && SettingsManager.Instance.sfxGroup != null)
        {
            if (deathAudioSource != null && deathAudioSource.outputAudioMixerGroup == null)
            {
                deathAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
            }
            if (policeSirenAudioSource != null && policeSirenAudioSource.outputAudioMixerGroup == null)
            {
                policeSirenAudioSource.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
            }
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

    [Header("🛡️ Respawn Immunity")]
    [Tooltip("Thời gian bất tử (không thể bị bắt lại) ngay sau khi vừa hồi sinh")]
    public float spawnImmunityDuration = 4.0f;
    [HideInInspector] public float lastRespawnTime = -10f;
    public bool IsInvulnerable => Time.time < lastRespawnTime + spawnImmunityDuration;

    /// <summary>
    /// Bật hoặc tắt đèn cảnh báo DieLight
    /// </summary>
    public void SetDieLight(bool active)
    {
        if (dieLight != null) dieLight.SetActive(active);
        if (warningLight != null && warningLight != dieLight) warningLight.SetActive(active);
    }

    /// <summary>
    /// Hàm công khai kích hoạt cái chết của Player (Tự động phát RPC cho toàn bộ phòng nếu chơi Online)
    /// </summary>
    public void TriggerDeath(string caughtBy = "Guard")
    {
        if (isDeadProcessed || IsInvulnerable) return;

        var netSync = GetComponent<NetworkPlayerSync>();
        if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
        {
            netSync.RpcTriggerPlayerDeath(caughtBy);
        }
        else
        {
            ExecuteDeath(caughtBy, true);
        }
    }

    /// <summary>
    /// Thực thi cái chết trên từng máy (Được gọi từ RPC Mạng hoặc Cục bộ)
    /// </summary>
    public void ExecuteDeath(string caughtBy, bool isLocal)
    {
        if (isDeadProcessed || IsInvulnerable) return;
        isDeadProcessed = true;

        if (isLocal && playerStats != null)
        {
            playerStats.isDied = true;
            playerStats.currweight = 0;
        }

        // 1. Đảm bảo hiển thị Full Model Player (bật lại tất cả SkinnedMeshRenderer và Renderer của chính mình)
        if (playerModel != null)
        {
            playerModel.SetActive(true);
        }

        var netSync = GetComponent<NetworkPlayerSync>();
        if (netSync != null)
        {
            netSync.SetLocalMeshVisibility(true);
        }

        var allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRenderers)
        {
            if (r != null)
            {
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        // 2. Chạy Animation chết trên máy
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 3. Thả toàn bộ vật phẩm đang cầm xuống đất kèm lực đẩy vật lý (Chỉ Local Player thực hiện để tránh lặp RPC)
        if (isLocal)
        {
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv == null && playerController != null) inv = playerController.inventory;
            if (inv != null)
            {
                inv.DropAllItemsOnDeath();
            }
        }

        // 4. Phát thông báo Status trên màn hình ("Player got caught by ...")
        string pName = "Player";
        if (netSync != null && netSync.Object != null && netSync.Object.IsValid && !string.IsNullOrEmpty(netSync.NetworkPlayerName.ToString()))
        {
            pName = netSync.NetworkPlayerName.ToString();
        }
        else if (FirebaseDataService.Instance != null && FirebaseDataService.Instance.CurrentUserProfile != null && !string.IsNullOrEmpty(FirebaseDataService.Instance.CurrentUserProfile.username))
        {
            pName = FirebaseDataService.Instance.CurrentUserProfile.username;
        }
        else
        {
            pName = PlayerPrefs.GetString("PlayerNickname", "Player");
        }

        string deathNotice = $"{pName} got caught by {caughtBy}!";
        GameStatusHUD.Show(deathNotice);

        // 5. Ẩn Main HUD và kéo lùi Camera ra đằng sau xa hơn (Chỉ cho chính người chết)
        if (isLocal)
        {
            UI_Manager ui = FindFirstObjectByType<UI_Manager>(FindObjectsInactive.Include);
            if (ui != null)
            {
                ui.SetMainHUDActive(false);
            }

            StartCoroutine(SmoothDeathCameraPullbackRoutine());
        }

        // 6. Bật đèn cảnh báo DieLight
        SetDieLight(true);

        // 7. Phát âm thanh 1 (Kêu khi chết)
        PlayDeathSound();

        // 8. Tắt va chạm giữa Player và NPC
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), true);

        // 9. Bắt đầu chuỗi đếm ngược 10s hồi sinh và còi cảnh sát từ giây thứ 5
        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnSequenceRoutine(isLocal));

        Debug.Log($"[PlayerDeathHandler] {pName} đã bị bắt bởi {caughtBy}. Bắt đầu đếm ngược hồi sinh {respawnCooldown}s!");
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
    /// Chuỗi đếm ngược 10 giây hồi sinh:
    /// - Từ 0s -> 5s: Player nằm gục, camera pull-back.
    /// - Từ giây thứ 5 (5s còn lại): Còi cảnh sát bắt đầu phát với Fade In (to dần) và kết thúc với Fade Out (nhỏ dần).
    /// - Hết 10s: Tắt còi cảnh sát, tắt dieLight, hồi sinh đưa về Spawn Point.
    /// </summary>
    private IEnumerator RespawnSequenceRoutine(bool isLocal)
    {
        float totalDuration = Mathf.Max(5f, respawnCooldown); // 10s
        float sirenStartTime = 5.0f; // Bắt đầu còi cảnh sát từ giây thứ 5
        float sirenFadeInDuration = 1.0f; // To dần trong 1.0s
        float sirenFadeOutDuration = 1.5f; // Nhỏ dần trong 1.5s cuối
        float sirenEndTime = totalDuration; // Kết thúc ở giây thứ 10

        float elapsed = 0f;
        bool sirenStarted = false;

        AudioSource sirenSource = policeSirenAudioSource != null ? policeSirenAudioSource : deathAudioSource;
        float baseVolume = 1.0f;

        if (sirenSource != null && sirenSource.volume > 0f)
        {
            baseVolume = sirenSource.volume;
        }

        bool blackFadeStarted = false;

        while (elapsed < totalDuration)
        {
            yield return null;
            elapsed += Time.deltaTime;

            // Bắt đầu còi cảnh sát từ giây thứ 5
            if (elapsed >= sirenStartTime && !sirenStarted)
            {
                sirenStarted = true;
                if (sirenSource != null)
                {
                    sirenSource.volume = 0f;
                    if (!sirenSource.gameObject.activeSelf) sirenSource.gameObject.SetActive(true);

                    if (policeSirenAudioSource != null)
                    {
                        policeSirenAudioSource.loop = true;
                        policeSirenAudioSource.Play();
                    }
                    else if (policeSirenClip != null)
                    {
                        sirenSource.clip = policeSirenClip;
                        sirenSource.loop = true;
                        sirenSource.Play();
                    }
                    Debug.Log("[PlayerDeathHandler] Còi cảnh sát (Police Siren) bắt đầu hú từ giây thứ 5!");
                }
            }

            // Xử lý Fade In (To dần ở đầu) và Fade Out (Nhỏ dần ở cuối)
            if (sirenStarted && sirenSource != null && sirenSource.isPlaying)
            {
                if (elapsed < sirenStartTime + sirenFadeInDuration)
                {
                    // Fade In: từ 5.0s -> 6.0s (0 -> baseVolume)
                    float progress = Mathf.Clamp01((elapsed - sirenStartTime) / sirenFadeInDuration);
                    sirenSource.volume = Mathf.Lerp(0f, baseVolume, progress);
                }
                else if (elapsed >= sirenEndTime - sirenFadeOutDuration)
                {
                    // Fade Out: từ 8.5s -> 10.0s (baseVolume -> 0)
                    float progress = Mathf.Clamp01((elapsed - (sirenEndTime - sirenFadeOutDuration)) / sirenFadeOutDuration);
                    sirenSource.volume = Mathf.Lerp(baseVolume, 0f, progress);
                }
                else
                {
                    sirenSource.volume = baseVolume;
                }
            }

            // Rõ dần màn hình đen (Fade to Black) khi còn 1.5 giây cuối của chuỗi hồi sinh
            if (isLocal && !blackFadeStarted && elapsed >= sirenEndTime - sirenFadeOutDuration)
            {
                blackFadeStarted = true;
                ScreenFader.FadeToBlack(sirenFadeOutDuration);
            }
        }

        // Tắt còi cảnh sát hoàn toàn
        if (sirenSource != null)
        {
            sirenSource.Stop();
            sirenSource.volume = baseVolume;
        }

        // Tắt đèn cảnh báo / DieLight
        SetDieLight(false);

        // Hồi sinh người chơi và đưa về điểm xuất phát
        RespawnPlayer(isLocal);

    }

    /// <summary>
    /// Hồi sinh người chơi và đưa về điểm xuất phát ban đầu
    /// </summary>
    public void RespawnPlayer(bool isLocal)
    {
        // 1. Tìm vị trí Spawn thích hợp
        Vector3 targetSpawnPos = _initialSpawnPos;
        Quaternion targetSpawnRot = _initialSpawnRot;

        var spawner = ScenePlayerSpawner.Instance != null ? ScenePlayerSpawner.Instance : FindFirstObjectByType<ScenePlayerSpawner>();
        if (spawner != null && spawner.spawnPoints != null && spawner.spawnPoints.Count > 0)
        {
            var netSync = GetComponent<NetworkPlayerSync>();
            int idx = 0;
            if (netSync != null && netSync.Object != null && netSync.Object.IsValid)
            {
                idx = Mathf.Abs(netSync.Object.InputAuthority.PlayerId) % spawner.spawnPoints.Count;
            }
            else if (netSync != null && netSync.Runner != null && netSync.Runner.IsRunning)
            {
                idx = Mathf.Abs(netSync.Runner.LocalPlayer.PlayerId) % spawner.spawnPoints.Count;
            }

            if (idx < spawner.spawnPoints.Count && spawner.spawnPoints[idx] != null)
            {
                targetSpawnPos = spawner.spawnPoints[idx].position;
                targetSpawnRot = spawner.spawnPoints[idx].rotation;
            }
        }

        // 2. Di chuyển Player về vị trí Spawn (Tắt tạm CharacterController để không bị kẹt va chạm)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.position = targetSpawnPos;
        transform.rotation = targetSpawnRot;

        if (cc != null) cc.enabled = true;

        // 3. Khôi phục Animation về trạng thái Idle bình thường
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.Rebind();
            animator.Update(0f);
            animator.Play("Idle", 0, 0f);
        }

        // 4. Khôi phục Camera Target
        if (cameraTarget != null)
        {
            cameraTarget.localPosition = originalCameraTargetLocalPos;
        }

        // 5. Khôi phục PlayerStats
        if (playerStats != null)
        {
            playerStats.isDied = false;
            playerStats.currentStamina = playerStats.maxStamina;
            playerStats.currweight = 0;
        }

        // 6. Bật lại va chạm với NPC
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), false);

        // 7. Reset cờ trạng thái chết và kích hoạt thời gian miễn nhiễm
        isDeadProcessed = false;
        lastRespawnTime = Time.time;

        // 8. Nếu là Local Player và đang chơi FPV, khôi phục lại ẩn mesh, bật lại MainHUD và mờ dần màn đen
        if (isLocal)
        {
            var netSync = GetComponent<NetworkPlayerSync>();
            if (netSync != null)
            {
                netSync.SetLocalMeshVisibility(false);
            }

            UI_Manager ui = FindFirstObjectByType<UI_Manager>(FindObjectsInactive.Include);
            if (ui != null && !ui.isPaused && !UI_Manager.isSolving)
            {
                ui.SetMainHUDActive(true);
            }

            ScreenFader.FadeFromBlack(0.8f);
            GameStatusHUD.Show("Respawned! Continue your heist...", 3f);
        }

        Debug.Log($"<color=green>[PlayerDeathHandler] Player đã hồi sinh thành công tại vị trí {targetSpawnPos}!</color>");
    }

    /// <summary>
    /// Hàm Reset trạng thái khi tải lại trận hoặc đổi Scene
    /// </summary>
    public void ResetDeathState()
    {
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }

        isDeadProcessed = false;
        SetDieLight(false);

        if (policeSirenAudioSource != null) policeSirenAudioSource.Stop();
        if (deathAudioSource != null) deathAudioSource.Stop();
        if (cameraTarget != null) cameraTarget.localPosition = originalCameraTargetLocalPos;

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Npc"), false);
    }
}
