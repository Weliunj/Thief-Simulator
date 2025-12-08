using StarterAssets;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class Range_Interaction : MonoBehaviour
{
    //Offset Pivot and Radius for Range Check
    public GameObject Pivot;
    public float Radius;
    public bool InRange;

    [Header("Lock")]
    private GameObject target;
    public GameObject Center;
    public GameObject E_icon;
    public TextMeshPro Name;
    public Color Colorname;

    //Third Person Controller Reference
    private ThirdPersonController thirdPersonController;
    void Start()
    {
        E_icon.SetActive(false);
        if (Name != null)
        {

            Name.text = transform.parent.gameObject.name;            
            // 3. Áp dụng gradient
            Name.color = Colorname;
        }
        else
        {
            Debug.LogError("TextMeshPro component (Name) not found in children or not assigned.");
        }


        thirdPersonController = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();
        target = GameObject.FindGameObjectWithTag("MainCamera");
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
            E_icon.SetActive(false);
        }
    }

    public void E_Interact()
    {
        E_icon.SetActive(true);
    }
    public void LookAtObject()
    {
        Vector3 direc = target.transform.position - Center.transform.position;
        Center.transform.rotation = Quaternion.LookRotation(direc);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Pivot.transform.position, Radius);
    }
}
