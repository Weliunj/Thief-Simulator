using UnityEngine;

public class Range_Interaction : MonoBehaviour
{
    public GameObject Pivot;
    public GameObject InteractableObject;
    public float Radius;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Physics.CheckSphere(Pivot.transform.position, Radius, LayerMask.GetMask("Player")))
        {
            Debug.Log("Object in Range");
        
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Pivot.transform.position, Radius);
    }
}
