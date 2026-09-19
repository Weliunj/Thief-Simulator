using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreen : MonoBehaviour
{
    [Header("🔊 Audio")]
    public AudioSource[] audioSources; // audioSources[0]: Hover, audioSources[1]: Click

    [Header("🔘 UI Buttons (Optional - can also assign OnClick in Inspector)")]
    public Button playButton;
    public Button exitButton;

    void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(Play_Clicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Exit_Clicked);
        }
    }

    public void Play_Clicked()
    {
        if (audioSources != null && audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].Play();
        }
        StartCoroutine(LoadSceneAfterDelay(0.5f));
    }

    public void Exit_Clicked()
    {
        if (audioSources != null && audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].Play();
        }
        Debug.Log("Exit Game");
        StartCoroutine(QuitAfterDelay(0.5f));
    }

    public void Options_Clicked()
    {
        if (audioSources != null && audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].Play();
        }
        Debug.Log("Options");
    }

    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(1);
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
