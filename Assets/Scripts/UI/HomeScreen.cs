using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreen : MonoBehaviour
{
    [Header("🔊 Audio")]
    public AudioSource[] audioSources; // audioSources[0]: Hover, audioSources[1]: Click

    [Header("🔘 Main Menu Buttons")]
    public Button playButton;
    public Button exitButton;
    public Button optionsButton;

    [Header("🚪 Panels Reference")]
    public GameObject mainMenuPanel;
    public GameObject settingPanel;
    public ChapterSelectManager chapterSelectManager;

    void Start()
    {
        // Tự động tìm AudioSources nếu chưa được kéo vào Inspector
        if (audioSources == null || audioSources.Length == 0)
        {
            audioSources = GetComponentsInChildren<AudioSource>(true);
            if (audioSources == null || audioSources.Length == 0)
            {
                audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }
        }

        // Khởi động: Bật Main Menu, tắt tất cả các panel khác
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (chapterSelectManager != null && chapterSelectManager.chapterSelectPanel != null)
        {
            chapterSelectManager.chapterSelectPanel.SetActive(false);
        }

        // Tự động tìm các Buttons nếu chưa gán
        if (playButton == null)
        {
            Transform p = transform.Find("PlayButton") ?? transform.Find("PlayBtn");
            if (p != null) playButton = p.GetComponent<Button>();
        }
        if (exitButton == null)
        {
            Transform e = transform.Find("ExitButton") ?? transform.Find("ExitBtn");
            if (e != null) exitButton = e.GetComponent<Button>();
        }
        if (optionsButton == null)
        {
            Transform o = transform.Find("OptionsButton") ?? transform.Find("OptionButton") ?? transform.Find("SettingButton") ?? transform.Find("SettingsBtn");
            if (o != null) optionsButton = o.GetComponent<Button>();
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(Play_Clicked);
            playButton.onClick.AddListener(Play_Clicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Exit_Clicked);
            exitButton.onClick.AddListener(Exit_Clicked);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(Options_Clicked);
            optionsButton.onClick.AddListener(Options_Clicked);
        }
    }

    public void Play_Clicked()
    {
        PlayClickSound();

        if (chapterSelectManager != null)
        {
            chapterSelectManager.OpenChapterSelect();
        }
        else
        {
            StartCoroutine(LoadSceneAfterDelay(1, 0.5f));
        }
    }

    public void Exit_Clicked()
    {
        PlayClickSound();
        Debug.Log("Exit Game");
        StartCoroutine(QuitAfterDelay(0.5f));
    }

    public void Options_Clicked()
    {
        PlayClickSound();
        Debug.Log("Options Clicked");
        
        // Đóng Main Menu trước đó và mở Setting
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            SettingsHUD hud = settingPanel.GetComponent<SettingsHUD>() ?? settingPanel.GetComponentInChildren<SettingsHUD>(true);
            if (hud != null)
            {
                hud.previousPanel = mainMenuPanel;
                hud.OpenPanel();
            }
        }
    }

    public void PlayClickSound()
    {
        if (audioSources != null && audioSources.Length > 0)
        {
            // Ưu tiên audioSources[1] (click), fallback về audioSources[0]
            if (audioSources.Length > 1 && audioSources[1] != null)
            {
                audioSources[1].Play();
            }
            else if (audioSources[0] != null)
            {
                audioSources[0].Play();
            }
        }
        else
        {
            var anyAudio = GetComponentInChildren<AudioSource>(true) ?? FindFirstObjectByType<AudioSource>();
            if (anyAudio != null)
            {
                anyAudio.Play();
            }
        }
    }

    private IEnumerator LoadSceneAfterDelay(int sceneIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
