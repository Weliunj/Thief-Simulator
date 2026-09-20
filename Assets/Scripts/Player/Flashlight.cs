using StarterAssets;
using UnityEngine;

public class Flashlight : MonoBehaviour
{
    private PlayerController player;
    public PlayerManager playerManager;
    public MobileActionButtons mobileActions;

    private Light flashlight;
    private bool toggleF;
    public AudioSource[] audioSources;

    public int range;
    public int intensity;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        flashlight = GetComponent<Light>();
        toggleF = false;

        if (flashlight != null)
        {
            flashlight.range = range;
        }

        if (mobileActions == null)
        {
            mobileActions = FindFirstObjectByType<MobileActionButtons>();
        }
    }

    void Update()
    {
        if (playerManager != null && playerManager.isDied)
        {
            if (flashlight != null) flashlight.intensity = 0;
            return;
        }

        if (flashlight != null)
        {
            flashlight.range = range;
        }

        // Bật/tắt bằng phím F (PC) hoặc nút Flashlight trên Mobile HUD
        bool isTriggered = Input.GetKeyDown(KeyCode.F);
        if (mobileActions != null && mobileActions.flashlightPressed)
        {
            isTriggered = true;
        }

        if (isTriggered)
        {
            ToggleFlashlight();
        }

        if (flashlight != null)
        {
            flashlight.intensity = toggleF ? intensity : 0;
        }
    }

    public void ToggleFlashlight()
    {
        toggleF = !toggleF;

        int type = toggleF ? 0 : 1;
        if (audioSources != null && audioSources.Length > type && audioSources[type] != null)
        {
            audioSources[type].Stop();
            if (audioSources[type].clip != null)
            {
                audioSources[type].PlayOneShot(audioSources[type].clip);
            }
        }
    }
}
