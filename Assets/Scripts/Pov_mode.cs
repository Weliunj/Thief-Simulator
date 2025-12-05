using StarterAssets;
using TMPro;
using UnityEngine;

public class Pov_mode : MonoBehaviour
{
    ThirdPersonController player;
    public GameObject Pov1;
    public GameObject Pov3;
    public GameObject Geometry;
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
            Pov1.SetActive(false);
            Pov3.SetActive(true);
            if(cd <= 1f)
            {
                Geometry.SetActive(false);
            }
        }
        else
        {
            Pov1.SetActive(true);
            Geometry.SetActive(true);
            Pov3.SetActive(false);
        }
    }
}
