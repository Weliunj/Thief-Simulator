using Fusion;
using UnityEngine;

/// <summary>
/// Đồng bộ trạng thái Cửa (Mở khóa / Đóng / Mở / Đang bẻ khóa) qua Photon Fusion
/// </summary>
public class NetworkDoorSync : NetworkBehaviour
{
    [Header("🚪 Networked Door State")]
    [Networked, OnChangedRender(nameof(OnDoorStateChanged))]
    public NetworkBool NetworkIsUnlocked { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnDoorStateChanged))]
    public NetworkBool NetworkIsOpen { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnLockpickingStateChanged))]
    public NetworkBool NetworkIsBeingLockpicked { get; set; } = false;

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
                NetworkIsBeingLockpicked = doorController.isBeingLockpicked;
            }
            else
            {
                doorController.isUnlocked = NetworkIsUnlocked;
                doorController.isBeingLockpicked = NetworkIsBeingLockpicked;
                doorController.SetDoorOpen(NetworkIsOpen, false);
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
            doorController.isBeingLockpicked = false;
            doorController.SetDoorOpen(true, false);
        }

        if (Object.HasStateAuthority)
        {
            NetworkIsUnlocked = true;
            NetworkIsOpen = true;
            NetworkIsBeingLockpicked = false;
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

    /// <summary>
    /// Đồng bộ trạng thái đang bẻ khóa (khóa độc quyền 1 người bẻ khóa)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcSyncSetLockpicking(bool isPicking)
    {
        if (doorController != null)
        {
            doorController.isBeingLockpicked = isPicking;
        }

        if (Object.HasStateAuthority)
        {
            NetworkIsBeingLockpicked = isPicking;
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

    private void OnLockpickingStateChanged()
    {
        if (doorController != null)
        {
            doorController.isBeingLockpicked = NetworkIsBeingLockpicked;
        }
    }
}
