using Fusion;
using UnityEngine;

/// <summary>
/// Đồng bộ trạng thái mở và bẻ khóa cho LockedContainerController trong chế độ nhiều người chơi Fusion Shared Mode.
/// </summary>
public class NetworkLockedContainerSync : NetworkBehaviour
{
    [Header("📦 Networked State")]
    [Networked, OnChangedRender(nameof(OnContainerUnlockedChanged))]
    public NetworkBool NetworkIsUnlocked { get; set; } = false;

    [Networked, OnChangedRender(nameof(OnLockpickingStateChanged))]
    public NetworkBool NetworkIsBeingLockpicked { get; set; } = false;

    public LockedContainerController containerController;

    public bool IsNetworkSpawned => Object != null && Object.IsValid && Runner != null && Runner.IsRunning;

    private void Awake()
    {
        if (containerController == null) containerController = GetComponent<LockedContainerController>();
    }

    public override void Spawned()
    {
        if (containerController != null)
        {
            if (Runner.IsServer || Runner.IsSharedModeMasterClient)
            {
                NetworkIsUnlocked = containerController.isUnlocked;
                NetworkIsBeingLockpicked = containerController.isBeingLockpicked;
            }
            else
            {
                containerController.isUnlocked = NetworkIsUnlocked;
                containerController.isBeingLockpicked = NetworkIsBeingLockpicked;
                if (NetworkIsUnlocked)
                {
                    containerController.ApplyOpenVisual(false);
                    containerController.SpawnLootItems();
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestUnlock()
    {
        NetworkIsUnlocked = true;
        NetworkIsBeingLockpicked = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestSetLockpicking(bool isPicking)
    {
        if (isPicking && NetworkIsBeingLockpicked)
        {
            Debug.LogWarning("[NetworkDoorSync] Bỏ qua RPC - đã có người lockpick!");
            // Có thể gửi RPC về client A báo "không được" để đóng minigame
            return;
        }
        NetworkIsBeingLockpicked = isPicking;
    }

    public enum AudioType { Hit = 0, Miss = 1, Victory = 2 }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcPlayAudio(AudioType audioType)
    {
        if (containerController == null) return;
        switch (audioType)
        {
            case AudioType.Hit: containerController.PlayLocalSound(containerController.hitSound); break;
            case AudioType.Miss: containerController.PlayLocalSound(containerController.missSound); break;
            case AudioType.Victory: containerController.PlayLocalSound(containerController.victorySound); break;
        }
    }

    private void OnContainerUnlockedChanged()
    {
        if (containerController != null)
        {
            containerController.isUnlocked = NetworkIsUnlocked;
            if (NetworkIsUnlocked)
            {
                containerController.ApplyOpenVisual(true);
                containerController.SpawnLootItems();
            }
        }
    }

    private void OnLockpickingStateChanged()
    {
        if (containerController != null)
        {
            containerController.isBeingLockpicked = NetworkIsBeingLockpicked;
        }
    }
}
