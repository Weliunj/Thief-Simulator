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
        if(ROI.InRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Picked up " + gameObject.name);
            gameObject.SetActive(false);
        }
        
    }
}
