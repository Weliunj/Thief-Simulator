using System.Collections;
using StarterAssets;
using UnityEngine;

public class LockpickDoor : MonoBehaviour, IInteractable
{
    [Header("🚪 Lock Settings")]
    public string doorName = "Wooden Locked Door";
    public int rewardPoints = 10;
    public bool destroyOnUnlock = true;
    public GameObject doorModel; // Door model (if hiding mesh instead of destroying entire GameObject)

    [Header("🔊 Audio")]
    public AudioSource audioSource;
    public AudioClip unlockSound;

    [Header("📍 Interaction Settings")]
    public float interactRadius = 3f;
    public Transform interactPivot;

    [HideInInspector] public bool isUnlocked = false;
    private bool playerInRange = false;
    private GameObject playerObj;
    private UI_Manager uiManager;
    private MobileActionButtons mobileActions;

    void Start()
    {
        if (interactPivot == null) interactPivot = transform;
        playerObj = GameObject.FindGameObjectWithTag("Player");
        uiManager = FindFirstObjectByType<UI_Manager>();
        mobileActions = FindFirstObjectByType<MobileActionButtons>();
    }

    void Update()
    {
        if (isUnlocked) return;
        if (UI_Manager.isSolving) return;

        CheckPlayerDistance();

        bool isInteractInput = Input.GetKeyDown(KeyCode.E) || (mobileActions != null && mobileActions.interactPressed);
        if (playerInRange && isInteractInput)
        {
            StartLockpicking();
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerObj == null) return;
        float dist = Vector3.Distance(interactPivot.position, playerObj.transform.position);
        playerInRange = (dist <= interactRadius);
    }

    public void StartLockpicking()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();

        if (uiManager != null)
        {
            uiManager.StartLockpicking(this);
        }
        else
        {
            Debug.LogError("UI_Manager not found in Scene!");
        }
    }

    public void OnUnlockSuccess()
    {
        isUnlocked = true;
        Debug.Log($"<color=green>Successfully unlocked door: {doorName}!</color>");

        if (uiManager != null && uiManager.playerManager != null)
        {
            uiManager.playerManager.currpoint += rewardPoints;
            Debug.Log($"Added {rewardPoints} points. Current total: {uiManager.playerManager.currpoint}");
        }

        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        if (destroyOnUnlock)
        {
            StartCoroutine(DestroyDoorWithDelay(1.0f));
        }
    }

    private IEnumerator DestroyDoorWithDelay(float delay)
    {
        if (audioSource != null && unlockSound != null)
        {
            delay = Mathf.Max(delay, unlockSound.length);
        }

        // Disable colliders immediately so player can walk through without waiting
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        // Hide door visuals immediately
        if (doorModel != null)
        {
            doorModel.SetActive(false);
        }
        else
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }

        // Wait for audio playback to finish
        yield return new WaitForSeconds(delay);

        if (doorModel != null)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    [Header("⚠️ Alarm & NPC Alert Settings")]
    public float callRange = 20f;

    public void OnUnlockFailed()
    {
        AnswerFailed();
    }

    public void AnswerFailed()
    {
        if (audioSource != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        Debug.Log($"Trò chơi thất bại! Đang kêu gọi AdultNPC trong phạm vi {callRange}m.");

        // 1. Tìm tất cả colliders trong phạm vi callRange
        Collider[] colliders = Physics.OverlapSphere(transform.position, callRange);

        int adultCount = 0;

        foreach (var collider in colliders)
        {
            // 2. Kiểm tra Tag "adult"
            if (collider.CompareTag("adult"))
            {
                AI_Move_NavMesh adultNpc = collider.GetComponent<AI_Move_NavMesh>();

                if (adultNpc != null)
                {
                    // 3. Kích hoạt chế độ đuổi (chase) trên AdultNPC
                    adultNpc.PlayDetectionSound();
                    adultNpc.HandleChaseMusic(true);
                    adultNpc.targetDetected = true;

                    // Thiết lập thời gian theo đuổi ngẫu nhiên
                    adultNpc.chaseDuration = Random.Range(
                        adultNpc.chaseDurationPublic.x,
                        adultNpc.chaseDurationPublic.y);

                    adultCount++;
                    Debug.Log($"Kích hoạt chase trên NPC: {collider.gameObject.name}");
                }
            }
        }

        if (adultCount == 0)
        {
            Debug.Log("Không tìm thấy AdultNPC nào trong phạm vi.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Transform p = (interactPivot != null) ? interactPivot : transform;
        Gizmos.DrawWireSphere(p.position, interactRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p.position, callRange);
    }

    // =========================================================================
    //                       IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => string.IsNullOrEmpty(doorName) ? "Locked Door" : doorName;
    public string GetActionPrompt() => "Pick Lock";
    public int GetPrice() => 0;
    public int GetWeight() => 0;
    public bool IsLootItem() => false;
    public string GetDescription() => "A locked door. Requires lockpicking to open.";
    public Sprite GetIcon() => null;
    public ItemRarity GetRarity() => ItemRarity.Common;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        if (isUnlocked)
        {
            failReason = "Already Unlocked";
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
        if (!isUnlocked && !UI_Manager.isSolving)
        {
            StartLockpicking();
        }
    }
}
