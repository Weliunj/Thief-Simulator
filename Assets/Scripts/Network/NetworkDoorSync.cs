using Fusion;
using UnityEngine;

public class NetworkDoorSync : NetworkBehaviour
{
    [Header("🚪 Networked Door State")]
    [Networked, OnChangedRender(nameof(OnDoorUnlockedChanged))]
    public NetworkBool NetworkIsUnlocked { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnDoorOpenChanged))]
    public NetworkBool NetworkIsOpen { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnLockpickingStateChanged))]
    public NetworkBool NetworkIsBeingLockpicked { get; set; } = false;

    // Biến lưu số lượng người chơi đang đứng gần cửa
    [Networked]
    public int PlayersNearbyCount { get; set; } = 0;

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
                PlayersNearbyCount = 0;
            }
            else
            {
                doorController.isUnlocked = NetworkIsUnlocked;
                doorController.isBeingLockpicked = NetworkIsBeingLockpicked;
                doorController.ApplyDoorVisual(NetworkIsOpen, false);
            }
        }
    }

    // RPC để bất kỳ máy nào cũng có thể báo danh "Tôi đang ở gần cửa" hoặc "Tôi đã đi ra xa"
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcUpdatePlayerPresence(PlayerRef player, bool isInside)
    {
        if (isInside)
        {
            PlayersNearbyCount = Mathf.Max(1, PlayersNearbyCount + 1);
        }
        else
        {
            PlayersNearbyCount = Mathf.Max(0, PlayersNearbyCount - 1);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestUnlockDoor()
    {
        NetworkIsUnlocked = true;
        NetworkIsOpen = true;
        NetworkIsBeingLockpicked = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestSetDoorOpen(bool open)
    {
        NetworkIsOpen = open;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestSetLockpicking(bool isPicking)
    {
        NetworkIsBeingLockpicked = isPicking;
    }

    public enum DoorAudioType { Hit = 0, Miss = 1, Victory = 2 }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcPlayDoorAudio(DoorAudioType audioType)
    {
        if (doorController == null) return;
        switch (audioType)
        {
            case DoorAudioType.Hit: doorController.PlayLocalSound(doorController.hitSound); break;
            case DoorAudioType.Miss: doorController.PlayLocalSound(doorController.missSound); break;
            case DoorAudioType.Victory: doorController.PlayLocalSound(doorController.victorySound); break;
        }
    }

    private void OnDoorUnlockedChanged()
    {
        if (doorController != null) doorController.isUnlocked = NetworkIsUnlocked;
    }

    private void OnDoorOpenChanged()
    {
        if (doorController != null) doorController.ApplyDoorVisual(NetworkIsOpen, true);
    }

    private void OnLockpickingStateChanged()
    {
        if (doorController != null) doorController.isBeingLockpicked = NetworkIsBeingLockpicked;
    }
}