using UnityEngine;

public class AI_Move : MonoBehaviour
{
    private Animator animator;
    public GameObject Sub_corner;
    public GameObject Add_Corner;
    public int X;
    public int Z;
    public Vector2 Target;
    public float TimeMToT;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }


    void Update()
    {
        
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        //Add
        Gizmos.DrawLine(Add_Corner.transform.position, new Vector3(X + Add_Corner.transform.position.x, Add_Corner.transform.position.y, Add_Corner.transform.position.z));
        Gizmos.DrawLine(Add_Corner.transform.position, new Vector3(Add_Corner.transform.position.x, Add_Corner.transform.position.y, Z + Add_Corner.transform.position.z));
        //Sub
        Gizmos.DrawLine(Sub_corner.transform.position, new Vector3((X * -1) + Sub_corner.transform.position.x, Sub_corner.transform.position.y, Sub_corner.transform.position.z));
        Gizmos.DrawLine(Sub_corner.transform.position, new Vector3(Sub_corner.transform.position.x, Sub_corner.transform.position.y, (-1*Z) + Sub_corner.transform.position.z));
    }
}
