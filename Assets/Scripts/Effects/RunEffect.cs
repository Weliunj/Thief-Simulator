using ithappy.Animals_FREE;
using StarterAssets;
using UnityEditor.Callbacks;
using UnityEngine;

public class RunEffect : MonoBehaviour
{
    public ThirdPersonController thirdPersonController;
    public GameObject Run_Effect;
    public float delayTime;
    private float _delaytime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if(Run_Effect == null)
        {
            return;
        }
        if (thirdPersonController.Grounded)
        {
            if(thirdPersonController.targetSpeed > 5)
            {
                Run_Effect.SetActive(true);
                _delaytime = delayTime;
            }
            else
            {
                if(_delaytime > 0)
                {
                    _delaytime -= Time.deltaTime;
                }
                else
                {
                    Run_Effect.SetActive(false);
                }
            }
        }
        else
        {
            if(_delaytime > 0)
                {
                    _delaytime -= Time.deltaTime;
                }
                else
                {
                    Run_Effect.SetActive(false);
                }
        }
    }
}
