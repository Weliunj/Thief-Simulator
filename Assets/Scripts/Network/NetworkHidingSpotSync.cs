using Fusion;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Đồng bộ trạng thái điểm trốn (Tủ trốn / Wardrobe) trên Photon Fusion Multiplayer.
/// Đảm bảo cơ chế ĐỘC QUYỀN: Chỉ duy nhất 1 người chơi được trốn tại 1 thời điểm.
/// </summary>
public class NetworkHidingSpotSync : NetworkBehaviour
{
    [Header("📦 Networked Hiding State")]
    [Networked, OnChangedRender(nameof(OnOccupiedChanged))]
    public NetworkBool NetworkIsOccupied { get; set; } = false;

    [Networked]
    public NetworkId OccupantNetworkId { get; set; } = default;

    public HidingSpotController spotController;

    public bool IsNetworkSpawned => Object != null && Object.IsValid && Runner != null && Runner.IsRunning;

    private void Awake()
    {
        if (spotController == null) spotController = GetComponent<HidingSpotController>();
    }

    public override void Spawned()
    {
        if (spotController != null)
        {
            if (Runner.IsServer || Runner.IsSharedModeMasterClient)
            {
                NetworkIsOccupied = spotController.isOccupied;
                OccupantNetworkId = default;
            }
            else
            {
                spotController.isOccupied = NetworkIsOccupied;
            }
        }
    }

    /// <summary>
    /// Gửi yêu cầu vào trốn trong tủ tới Host / StateAuthority.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestEnter(NetworkId playerNetId)
    {
        // Kiểm tra độc quyền: Nếu đã có người trốn thì từ chối
        if (NetworkIsOccupied)
        {
            return;
        }

        NetworkIsOccupied = true;
        OccupantNetworkId = playerNetId;

        // Báo cho toàn bộ Client thực thi đưa Player vào tủ
        RpcConfirmEnter(playerNetId);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcConfirmEnter(NetworkId playerNetId)
    {
        if (spotController == null) return;

        // Chỉ chặn RPC đến trễ sau khi đã Exit
        if (!NetworkIsOccupied)
        {
            Debug.LogWarning("[RpcConfirmEnter] BỎ QUA - đã exit trước khi RPC tới!");
            return;
        }

        if (Runner != null && Runner.TryFindObject(playerNetId, out NetworkObject playerNetObj))
        {
            var pc = playerNetObj.GetComponent<PlayerController>() ?? playerNetObj.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                // Chỉ bỏ qua nếu player ĐÃ hiding (tránh gọi EnterHiding trùng)
                if (pc.isHiding) return;
                
                spotController.EnterHiding(pc);
            }
        }
    }

    /// <summary>
    /// Gửi yêu cầu thoát khỏi tủ tới Host / StateAuthority.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestExit(NetworkId playerNetId = default)
    {
        NetworkId exitingPlayerId = (playerNetId != default) ? playerNetId : OccupantNetworkId;

        NetworkIsOccupied = false;
        OccupantNetworkId = default;

        // Báo cho toàn bộ Client thực thi đưa Player ra ngoài
        RpcConfirmExit(exitingPlayerId);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcConfirmExit(NetworkId playerNetId)
    {
        if (spotController == null) return;

        if (playerNetId != default && Runner != null && Runner.TryFindObject(playerNetId, out NetworkObject playerNetObj))
        {
            var pc = playerNetObj.GetComponent<PlayerController>() ?? playerNetObj.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                spotController.ExitHiding(pc);
                return;
            }
        }

        // Dự phòng nếu không tìm thấy NetworkObject
        spotController.ExitHiding(null);
    }

    private void OnOccupiedChanged()
    {
        Debug.Log($"[OnOccupiedChanged] NetworkIsOccupied={NetworkIsOccupied}");
        if (spotController != null)
        {
            spotController.isOccupied = NetworkIsOccupied;
        }
    }
}
