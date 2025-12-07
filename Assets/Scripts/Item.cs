using UnityEngine;

public class Item : MonoBehaviour
{
    public float Price = 0f;
    public int kg = 0;
    private Range_Interaction ROI;
    void Start()
    {
        ROI = GetComponentInChildren<Range_Interaction>();
    }

    void Update()
    {
        
    }
}
