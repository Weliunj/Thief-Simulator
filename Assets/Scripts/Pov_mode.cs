using StarterAssets;
using TMPro;
using UnityEngine;

public class Pov_mode : MonoBehaviour
{
    ThirdPersonController player;
    public GameObject Pov3;
    public GameObject Pov1;
    public GameObject Geometry;
    public GameObject Flashlight1;
    public GameObject Flashlight3;
    private float cd = 3f;
    public bool Swap;
    void Start()
    {
        player = FindAnyObjectByType<ThirdPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if(cd >= 0)
        {
            cd -= Time.deltaTime;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Swap = !Swap;
                cd = 3f;
            }
        }

        if (player.isDead)
        {
            Swap = false;
        }

        if (Swap)
        {
            //1
            Pov1.SetActive(true);
            Pov3.SetActive(false);
            if(cd <= 1f)
            {
                Geometry.SetActive(false);
            }
            Flashlight1.SetActive(true);
            Flashlight3.SetActive(false);
        }
        else
        {   
            //3
            Pov1.SetActive(false);  
            Pov3.SetActive(true);
            Geometry.SetActive(true);
            
            Flashlight3.SetActive(true);
            Flashlight1.SetActive(false);
        }
    }
}
