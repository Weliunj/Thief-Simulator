using UnityEngine;

public class Item : MonoBehaviour
{
    private Range_Interaction ROI;
    void Start()
    {
        ROI = GetComponentInChildren<Range_Interaction>();
    }

    void Update()
    {
        
    }
}
