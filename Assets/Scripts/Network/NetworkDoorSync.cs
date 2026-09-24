using Fusion;
using UnityEngine;

/// <summary>
/// Đồng bộ trạng thái Cửa (Mở khóa / Đóng / Mở) qua Photon Fusion
/// </summary>
public class NetworkDoorSync : NetworkBehaviour
{
    [Header("🚪 Networked Door State")]
    [Networked, OnChangedRender(nameof(OnDoorStateChanged))]
    public NetworkBool NetworkIsUnlocked { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnDoorStateChanged))]
    public NetworkBool NetworkIsOpen { get; set; } = false;

    public DoorController doorController;

    private void Awake()
    {
        if (doorController == null) doorController = GetComponent<DoorController>();
    }

    public override void Spawned()
    {
        if (doorController != null)
        {
            if (Runner.IsServer || Runner.IsSharedModeMasterClient)
            {
                NetworkIsUnlocked = doorController.isUnlocked;
                NetworkIsOpen = doorController.isOpen;
            }
            else
            {
                doorController.isUnlocked = NetworkIsUnlocked;
                doorController.SetDoorOpen(NetworkIsOpen);
            }
        }
    }

    /// <summary>
    /// Gửi yêu cầu mở khóa cửa tới toàn bộ phòng
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSyncUnlockDoor()
    {
        if (doorController != null)
        {
            doorController.isUnlocked = true;
            doorController.SetDoorOpen(true, false);
        }

        if (Object.HasStateAuthority)
        {
            NetworkIsUnlocked = true;
            NetworkIsOpen = true;
        }
    }

    /// <summary>
    /// Gửi yêu cầu đóng/mở cửa tới toàn bộ phòng
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSyncSetDoorOpen(bool open)
    {
        if (doorController != null)
        {
            doorController.SetDoorOpen(open, false);
        }

        if (Object.HasStateAuthority)
        {
            NetworkIsOpen = open;
        }
    }

    private void OnDoorStateChanged()
    {
        if (doorController != null)
        {
            doorController.isUnlocked = NetworkIsUnlocked;
            doorController.SetDoorOpen(NetworkIsOpen, false);
        }
    }
}
