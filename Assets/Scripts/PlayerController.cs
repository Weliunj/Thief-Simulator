using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    float moveX;
    float moveZ;
    public float speed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        moveX = Input.GetAxisRaw("Horizontal");
        moveZ = Input.GetAxisRaw("Vertical");

        Vector3 direc = new Vector3(moveX, rb.linearVelocity.y, moveZ).normalized;
        rb.linearVelocity = direc * speed ;
    }
}
