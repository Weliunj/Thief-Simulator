using StarterAssets;
using UnityEngine;

public class Flashlight : MonoBehaviour
{
    private ThirdPersonController player;
    private
    Light flashlight;
    private bool toggleF;
    public AudioSource[] audioSources;

    public int range;
    public int intensity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindAnyObjectByType<ThirdPersonController>();
        flashlight = GetComponent<Light>();
        toggleF = false;
        flashlight.range = range;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.isDead)
        {
            flashlight.intensity = 0;
            return;
        }

        flashlight.range = range;
        if (Input.GetKeyDown(KeyCode.F))
        {
            toggleF = !toggleF;

            int type = toggleF ? 0 : 1;
            if(type == 0) { audioSources[0].Stop(); audioSources[0].PlayOneShot(audioSources[0].clip); }
            else { audioSources[1].Stop(); audioSources[1].PlayOneShot(audioSources[1].clip); }
        }

        int isOn = toggleF ? intensity :  0 ;
        flashlight.intensity = isOn;
    }
}
