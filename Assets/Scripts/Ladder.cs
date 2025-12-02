using UnityEngine;
using StarterAssets;
public class Ladder : MonoBehaviour
{
    //Third Person Controller Reference
    private ThirdPersonController thirdPersonController;
    private Range_Interaction ROI;
    private StarterAssetsInputs starterAssetsInputs;
    
    public GameObject B;
    public GameObject A;
    //Bien cuc bo
    public bool isClimbingLadder = false;
    
    void Start()
    {
        thirdPersonController = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
        if(thirdPersonController == null)
        {
            Debug.LogError("ThirdPersonController not found on Player");
        }
        ROI = GetComponentInChildren<Range_Interaction>();
        if(ROI == null)
        {
            Debug.LogError("Range_Interaction script not found in children of " + gameObject.name);
        }
        starterAssetsInputs = thirdPersonController.GetComponent<StarterAssetsInputs>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E) && ROI.InRange && thirdPersonController.Grounded
        && !thirdPersonController.isClimbingLadder  && !thirdPersonController.Crouching)
        {
            thirdPersonController.isClimbingLadder = true;
            isClimbingLadder = true;
            thirdPersonController.transform.position = A.transform.position;

            // Quay player theo hướng của B
            Vector3 directionToB = (B.transform.position - thirdPersonController.transform.position).normalized;
            directionToB.y = 0;
            thirdPersonController.transform.rotation = Quaternion.LookRotation(directionToB);
        }

        if(isClimbingLadder)
        {
            thirdPersonController.transform.position = Vector3.MoveTowards(thirdPersonController.transform.position, B.transform.position, 0.6f * Time.deltaTime);
        }

        if(Vector3.Distance(thirdPersonController.transform.position, B.transform.position) < 0.1f && thirdPersonController.isClimbingLadder)
        {
            thirdPersonController.isClimbingLadder = false;
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
}
