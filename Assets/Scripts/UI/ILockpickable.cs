/// <summary>
/// Interface cho các đối tượng có thể bẻ khóa qua LockpickMinigame (DoorController, LockedContainerController,...)
/// </summary>
public interface ILockpickable
{
    void PlayHitSound();
    void PlayMissSound();
    void CancelLockpicking();
}
