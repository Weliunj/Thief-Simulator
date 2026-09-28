using StarterAssets;
using UnityEngine;

/// <summary>
/// Quản lý điểm trốn (Tủ trốn / Wardrobe / Locker).
/// Triển khai giao diện IInteractable để tương tác qua hệ thống ItemInfoHUD / nút Interact.
/// Hỗ trợ đồng bộ Online qua NetworkHidingSpotSync.
/// </summary>
public class HidingSpotController : MonoBehaviour, IInteractable
{
    [Header("🚪 Display & Prompts")]
    public string spotName = "Wardrobe";
    public string actionPrompt = "Hide";
    [TextArea(2, 4)]
    public string description = "Hide inside to evade patrolling NPCs.";
    public Sprite spotIcon;

    [Header("📍 Positioning Points")]
    [Tooltip("Vị trí và hướng quay của Player khi đứng trốn bên trong tủ")]
    public Transform insidePoint;

    [Tooltip("Vị trí và hướng quay của Player khi bước ra ngoài tủ")]
    public Transform exitPoint;

    [Header("🚪 Locker Doors (Cánh cửa hé khi trốn)")]
    public Transform leftDoor;
    public Transform rightDoor;
    public Vector3 leftDoorOpenAngle = new Vector3(0f, -15f, 0f);
    public Vector3 rightDoorOpenAngle = new Vector3(0f, 15f, 0f);
    public float doorSpeed = 5.0f;

    [Header("🖥️ UI Settings")]
    [Tooltip("Tự động ẩn MainHUD (Stamina, Cân nặng, Phím Mobile...) khi đang trốn trong tủ để tạo không gian nhìn qua khe cửa")]
    public bool hideMainHUDWhileHiding = true;

    [Header("🔊 3D Spatial Audio")]
    public AudioSource audioSource;
    public AudioClip doorOpenCreakClip;
    public AudioClip doorCloseCreakClip;

    [Header("📊 Runtime State")]
    public bool isOccupied = false;
    public PlayerController currentHidingPlayer;

    private Quaternion leftClosedLocalRot;
    private Quaternion rightClosedLocalRot;
    private NetworkHidingSpotSync netSync;

    void Awake()
    {
        InitializeAudio();
        netSync = GetComponent<NetworkHidingSpotSync>();

        if (leftDoor != null) leftClosedLocalRot = leftDoor.localRotation;
        if (rightDoor != null) rightClosedLocalRot = rightDoor.localRotation;

        // Tự động tạo insidePoint / exitPoint nếu chưa kéo vào Inspector
        if (insidePoint == null)
        {
            GameObject inside = new GameObject("InsidePoint");
            inside.transform.SetParent(transform);
            inside.transform.localPosition = new Vector3(0f, 0f, 0f);
            inside.transform.localRotation = Quaternion.identity;
            insidePoint = inside.transform;
        }

        if (exitPoint == null)
        {
            GameObject exit = new GameObject("ExitPoint");
            exit.transform.SetParent(transform);
            exit.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            exit.transform.localRotation = Quaternion.identity;
            exitPoint = exit.transform;
        }
    }

    private void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.spatialBlend = 1.0f;
        audioSource.playOnAwake = false;
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 15.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void Update()
    {
        UpdateDoorVisual();
    }

    /// <summary>
    /// Cập nhật xoay cánh cửa tủ hé ra khi có người trốn và đóng kín khi trống.
    /// </summary>
    private void UpdateDoorVisual()
    {
        bool occupied = isOccupied;
        if (netSync != null && netSync.IsNetworkSpawned)
        {
            occupied = netSync.NetworkIsOccupied;
        }

        if (leftDoor != null)
        {
            Quaternion targetRot = occupied ? leftClosedLocalRot * Quaternion.Euler(leftDoorOpenAngle) : leftClosedLocalRot;
            leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, targetRot, Time.deltaTime * doorSpeed);
        }

        if (rightDoor != null)
        {
            Quaternion targetRot = occupied ? rightClosedLocalRot * Quaternion.Euler(rightDoorOpenAngle) : rightClosedLocalRot;
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, targetRot, Time.deltaTime * doorSpeed);
        }
    }

    // =========================================================================
    //                        IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => spotName;
    public string GetActionPrompt() => actionPrompt;
    public int GetPrice() => 0;
    public int GetWeight() => 0;
    public bool IsLootItem() => false;
    public string GetDescription() => description;
    public Sprite GetIcon() => spotIcon;
    public ItemRarity GetRarity() => ItemRarity.Common;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        bool effectiveOccupied = (netSync != null && netSync.IsNetworkSpawned)
            ? (bool)netSync.NetworkIsOccupied
            : isOccupied;

        Debug.Log($"[CanInteract] netOcc={netSync?.NetworkIsOccupied}, localOcc={isOccupied}, effective={effectiveOccupied}");

        if (effectiveOccupied)
        {
            failReason = "Tủ đang có người trốn!";
            return false;
        }

        if (UI_Manager.isSolving)
        {
            failReason = "";
            return false;
        }

        failReason = "";
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player == null) return;

        // ⭐ Dùng isOccupied local (đã sync qua OnOccupiedChanged), tránh lag network
        if (isOccupied) return;

        Debug.Log($"[Interact] OK - gửi RpcRequestEnter, isOccupied={isOccupied}, netOcc={netSync?.NetworkIsOccupied}");

        if (netSync != null && netSync.IsNetworkSpawned)
        {
            var netObj = player.GetComponent<Fusion.NetworkObject>();
            if (netObj != null)
            {
                netSync.RpcRequestEnter(netObj.Id);
                return;
            }
        }

        // Chế độ Offline / Singleplayer
        EnterHiding(player);
    }

    private bool IsLocalPlayer(PlayerController player)
    {
        if (player == null) return false;
        var netObj = player.GetComponent<Fusion.NetworkObject>();
        if (netObj == null || !netObj.IsValid) return true;
        return netObj.HasStateAuthority || netObj.HasInputAuthority;
    }

    /// <summary>
    /// Thực hiện đưa Player vào bên trong tủ, khóa di chuyển và căn chỉnh Camera.
    /// </summary>
    public void EnterHiding(PlayerController player)
    {
        Debug.Log($"[EnterHiding] CALLED! StackTrace:\n{System.Environment.StackTrace}");
        if (player == null) return;
        if (isOccupied && currentHidingPlayer != null && currentHidingPlayer != player) return;

        isOccupied = true;
        currentHidingPlayer = player;
        player.currentHidingSpot = this;

        // Phát âm thanh mở cửa hé
        PlaySound(doorOpenCreakClip);

        // Đặt vị trí Player vào bên trong tủ và đồng bộ Physics
        var charController = player.GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;

        player.transform.position = insidePoint.position;
        player.transform.rotation = insidePoint.rotation;

        var ntEnter = player.GetComponent<Fusion.NetworkTransform>();
        if (ntEnter != null && ntEnter.Object != null && ntEnter.Object.IsValid && ntEnter.Runner != null && ntEnter.Runner.IsRunning)
        {
            try
            {
                ntEnter.Teleport(insidePoint.position, insidePoint.rotation);
            }
            catch { }
        }

        Physics.SyncTransforms();

        if (charController != null) charController.enabled = true;

        // Kích hoạt trạng thái Hiding trên PlayerController
        player.isHiding = true;

        // ⭐ Block interaction để tránh EnterHiding chạy 2 lần liên tiếp
        PlayerInteraction.SetInteractionCooldown(0.3f);

        if (player._input != null)
        {
            player._input.jump = false;
        }

        // ⭐ Đồng bộ lên mạng
        var netPlayerSyncEnter = player.GetComponent<NetworkPlayerSync>();
        if (netPlayerSyncEnter != null && netPlayerSyncEnter.Object != null 
            && netPlayerSyncEnter.Object.IsValid && netPlayerSyncEnter.Object.HasStateAuthority)
        {
            netPlayerSyncEnter.SetHiding(true);
        }

        // Cố định hướng nhìn Camera nhìn thẳng ra hướng cửa tủ (khóa hoàn toàn góc quay)
        float facingYaw = insidePoint.eulerAngles.y;
        player.SetHidingCameraFacing(facingYaw);

        // Tắt item đang cầm trên tay (như khi leo thang) để rảnh tay và không bị bật/tắt đèn pin khi đang trốn
        if (player.hotbarManager != null)
        {
            player.hotbarManager.DeselectAll();
        }
        else if (player.heldItem != null)
        {
            foreach (var item in player.heldItem)
            {
                if (item != null) item.SetActive(false);
            }
        }

        // Hiển thị UI nút thoát nếu đây là local player
        if (IsLocalPlayer(player))
        {
            HidingExitHUD.Instance?.Show(this, player);
            if (hideMainHUDWhileHiding && UI_Manager.Instance != null)
            {
                UI_Manager.Instance.SetMainHUDActive(false);
            }
        }
    }

    /// <summary>
    /// Thực hiện đưa Player ra ngoài tủ, mở khóa di chuyển và giải phóng tủ.
    /// </summary>
    public void ExitHiding(PlayerController player)
    {
        Debug.Log($"[ExitHiding] ENTER player={player?.name}, isHiding={player?.isHiding}, currentHidingPlayer={currentHidingPlayer?.name}");
        if (player == null) player = currentHidingPlayer;
        if (player == null) { Debug.LogWarning("[ExitHiding] ABORT - player null!"); return; }
        Debug.Log($"[ExitHiding] Moving to {exitPoint.position}, isLocalPlayer={IsLocalPlayer(player)}");

        isOccupied = false;
        currentHidingPlayer = null;
        player.currentHidingSpot = null;

        // Phát âm thanh đóng cửa
        PlaySound(doorCloseCreakClip);

        // Đặt vị trí Player ra trước tủ và đồng bộ Physics
        var charController = player.GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;

        player.transform.position = exitPoint.position;
        player.transform.rotation = exitPoint.rotation;

        Physics.SyncTransforms();

        if (charController != null) charController.enabled = true;

        // ⭐ THÊM: Force NetworkTransform teleport (chống snapback)
        var netTransform = player.GetComponent<Fusion.NetworkTransform>();
        if (netTransform != null && netTransform.Object != null 
            && netTransform.Object.IsValid && netTransform.Object.HasStateAuthority)
        {
            try
            {
                netTransform.Teleport(exitPoint.position, exitPoint.rotation);
                Debug.Log($"[ExitHiding] NetworkTransform.Teleport called → {exitPoint.position}");
            }
            catch { }
        }

        // Tắt trạng thái Hiding
        player.isHiding = false;

        // ⭐ CHẶN RE-ENTRY CÙNG FRAME: block PlayerInteraction trong 0.5s
        PlayerInteraction.SetInteractionCooldown(0.5f);

        // ⭐ Xóa trạng thái input đang giữ để không trigger lại
        if (player._input != null)
        {
            player._input.jump = false;
        }

        // ⭐ Đồng bộ lên mạng
        var netPlayerSyncExit = player.GetComponent<NetworkPlayerSync>();
        if (netPlayerSyncExit != null && netPlayerSyncExit.Object != null 
            && netPlayerSyncExit.Object.IsValid && netPlayerSyncExit.Object.HasStateAuthority)
        {
            netPlayerSyncExit.SetHiding(false);
        }

        Debug.Log($"[ExitHiding] DONE - player.isHiding={player.isHiding}, pos={player.transform.position}");
        var cc = player.GetComponent<CharacterController>();
        Debug.Log($"[ExitHiding] CC.enabled={cc?.enabled}");
        StartCoroutine(CheckPos(player));

        // Ẩn UI nút thoát và khôi phục MainHUD
        if (IsLocalPlayer(player))
        {
            HidingExitHUD.Instance?.Hide();
            if (hideMainHUDWhileHiding && UI_Manager.Instance != null)
            {
                UI_Manager.Instance.SetMainHUDActive(true);
            }
        }
    }

    /// <summary>
    /// Yêu cầu thoát tủ (được gọi từ nút UI hoặc phím nóng của người đang trốn).
    /// </summary>
    public void RequestExit(PlayerController player)
    {
        Debug.Log($"[RequestExit] player={player?.name}, currentHidingPlayer={currentHidingPlayer?.name}, netSync={netSync}, IsNetSpawned={netSync?.IsNetworkSpawned}");
        if (player == null) player = currentHidingPlayer;

        if (netSync != null && netSync.IsNetworkSpawned)
        {
            Fusion.NetworkId pId = default;
            if (player != null)
            {
                var netObj = player.GetComponent<Fusion.NetworkObject>();
                if (netObj != null) pId = netObj.Id;
            }
            netSync.RpcRequestExit(pId);
            return;
        }

        // Thực thi thoát ngay lập tức trên máy cục bộ
        ExitHiding(player);
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private System.Collections.IEnumerator CheckPos(PlayerController p)
    {
        yield return null;
        if (p == null) yield break;
        Debug.Log($"[+1 frame] pos={p.transform.position}, isHiding={p.isHiding}, " +
                  $"distToInside={Vector3.Distance(p.transform.position, insidePoint.position):F2}");
        yield return new WaitForSeconds(1f);
        if (p == null) yield break;
        Debug.Log($"[+1s] pos={p.transform.position}, isHiding={p.isHiding}");
    }
}
