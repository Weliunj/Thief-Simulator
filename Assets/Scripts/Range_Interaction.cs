using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class Range_Interaction : MonoBehaviour
{
    //Offset Pivot and Radius for Range Check
    public GameObject Pivot;
    public float Radius;
    public bool InRange;

    //Lock
    private GameObject Cam_LockOn;
    public GameObject InteractableObject;

    //Third Person Controller Reference
    private ThirdPersonController thirdPersonController;
    void Start()
    {
        InteractableObject.SetActive(false);
        thirdPersonController = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
        Cam_LockOn = GameObject.FindGameObjectWithTag("MainCamera");
        if(thirdPersonController == null)
        {
            Debug.LogError("ThirdPersonController not found on Player");
        }
    }

    void Update()
    {
        //Player in Range Check
        if(Physics.CheckSphere(Pivot.transform.position, Radius, LayerMask.GetMask("Player")))
        {   InRange = true;}
        else
        {   InRange = false;}

        if(InRange)
        {
            E_Interact();
            LookAtObject();
        }
        else
        {
            InteractableObject.SetActive(false);
            
        }
    }

    public void E_Interact()
    {
        InteractableObject.SetActive(true);
        
    }
    public void LookAtObject()
    {
        Vector3 direc = Cam_LockOn.transform.position - thirdPersonController.transform.position;
        InteractableObject.transform.rotation = Quaternion.LookRotation(direc);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Pivot.transform.position, Radius);
    }
}
