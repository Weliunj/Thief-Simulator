using Unity.VisualScripting;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float Price = 0f;
    public int kg = 0;
    private Range_Interaction ROI;
    public PlayerManager playerManagerl;
    void Start()
    {
        ROI = GetComponentInChildren<Range_Interaction>();
    }

    void Update()
    {
        
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("home"))
        {
            playerManagerl.currpoint += (int)Price;
            Debug.Log($"Curr: {playerManagerl.currpoint} / {playerManagerl.totalpoint}");
            Destroy(gameObject);
        }
    }

}
