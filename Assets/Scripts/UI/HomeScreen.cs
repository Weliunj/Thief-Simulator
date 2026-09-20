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
        // Khởi động: Bật Main Menu, tắt tất cả các panel khác
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (chapterSelectManager != null && chapterSelectManager.chapterSelectPanel != null)
        {
            chapterSelectManager.chapterSelectPanel.SetActive(false);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(Play_Clicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Exit_Clicked);
        }

        if (optionsButton != null)
        {
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
        if (settingPanel != null) settingPanel.SetActive(true);
    }

    private void PlayClickSound()
    {
        if (audioSources != null && audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].Play();
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
