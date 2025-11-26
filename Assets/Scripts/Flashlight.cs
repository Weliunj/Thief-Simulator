using UnityEngine;

public class Flashlight : MonoBehaviour
{
    Light flashlight;
    private bool toggleF;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flashlight = GetComponent<Light>();
        toggleF = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            toggleF = !toggleF;
        }

        int isOn = toggleF ? 250 :  0 ;
        flashlight.intensity = isOn;
    }
}
