using UnityEngine;
using StarterAssets;
public class Ladder : MonoBehaviour, IInteractable
{
    //Third Person Controller Reference
    private PlayerController PlayerController;
    private Range_Interaction ROI;
    private StarterAssetsInputs starterAssetsInputs;
    private MobileActionButtons mobileActions;

    public GameObject B;
    public GameObject A;
    //Bien cuc bo
    public bool isClimbingLadder = false;

    void Start()
    {
        PlayerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        if (PlayerController == null)
        {
            Debug.LogError("PlayerController not found on Player");
        }
        ROI = GetComponentInChildren<Range_Interaction>();
        if (ROI == null)
        {
            Debug.LogError("Range_Interaction script not found in children of " + gameObject.name);
        }
        starterAssetsInputs = PlayerController.GetComponent<StarterAssetsInputs>();
        mobileActions = FindFirstObjectByType<MobileActionButtons>();
    }

    // Update is called once per frame
    void Update()
    {
        bool interactInput = Input.GetKeyDown(KeyCode.E) || (mobileActions != null && mobileActions.interactPressed);

        if (interactInput && ROI.InRange && PlayerController.Grounded
        && !PlayerController.isClimbingLadder && !PlayerController.Crouching)
        {
            PlayerController.isClimbingLadder = true;
            isClimbingLadder = true;
            PlayerController.transform.position = A.transform.position;

            // Quay player theo hướng của B
            Vector3 directionToB = (B.transform.position - PlayerController.transform.position).normalized;
            directionToB.y = 0; // Giữ y không đổi để tránh nghiêng người lên/xuống
            PlayerController.transform.rotation = Quaternion.LookRotation(directionToB);
        }

        if (isClimbingLadder)
        {
            PlayerController.transform.position = Vector3.MoveTowards(PlayerController.transform.position, B.transform.position, 0.8f * Time.deltaTime);
        }

        if (Vector3.Distance(PlayerController.transform.position, B.transform.position) < 0.1f && PlayerController.isClimbingLadder)
        {
            PlayerController.isClimbingLadder = false;
            isClimbingLadder = false;
            starterAssetsInputs.jump = true;

        }
    }
    // Vẽ gizmos: đường từ A đến B và 2 sphere nhỏ ở đầu/đuôi
    private void OnDrawGizmos()
    {
        if (A == null || B == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(A.transform.position, B.transform.position);

        // spheres to show endpoints
        float s = 0.05f;
        Gizmos.DrawSphere(A.transform.position, s);
        Gizmos.DrawSphere(B.transform.position, s);
    }

    // =========================================================================
    //                       IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => "Ladder";
    public string GetActionPrompt() => "Climb";
    public int GetPrice() => 0;
    public int GetWeight() => 0;
    public bool IsLootItem() => false;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        if (player == null || isClimbingLadder || player.isClimbingLadder)
        {
            failReason = "";
            return false;
        }
        if (!player.Grounded)
        {
            failReason = "Must be grounded to climb";
            return false;
        }
        failReason = "";
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (player != null && !isClimbingLadder && !player.isClimbingLadder && player.Grounded)
        {
            player.isClimbingLadder = true;
            isClimbingLadder = true;
            player.transform.position = A.transform.position;

            Vector3 directionToB = (B.transform.position - player.transform.position).normalized;
            directionToB.y = 0;
            player.transform.rotation = Quaternion.LookRotation(directionToB);
        }
    }
}
