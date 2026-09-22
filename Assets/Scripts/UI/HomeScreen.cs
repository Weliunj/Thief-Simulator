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
    public Button characterButton;

    [Header("🚪 Panels Reference")]
    public GameObject mainMenuPanel;
    public GameObject settingPanel;
    public GameObject characterSelectPanel;
    public ChapterSelectManager chapterSelectManager;

    private void Awake()
    {
        // Đảm bảo trong HomeMenu chỉ có đúng 1 AudioListener duy nhất
        ScenePlayerSpawner.EnsureAudioListener();

        // Luôn mở khóa chuột khi vào màn hình Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tự động đảm bảo EventSystem hoạt động
        if (FindFirstObjectByType<UIEventSystemFixer>() == null)
        {
            gameObject.AddComponent<UIEventSystemFixer>();
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tự động tìm AudioSources nếu chưa được kéo vào Inspector
        if (audioSources == null || audioSources.Length == 0)
        {
            audioSources = GetComponentsInChildren<AudioSource>(true);
            if (audioSources == null || audioSources.Length == 0)
            {
                audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }
        }

        // Tự động gán Mixer Group (BGM cho nhạc lặp, SFX cho tiếng click)
        if (audioSources != null && SettingsManager.Instance != null)
        {
            foreach (var src in audioSources)
            {
                if (src == null || src.outputAudioMixerGroup != null) continue;

                string objName = src.gameObject.name.ToLower();
                if (src.loop || objName.Contains("bgm") || objName.Contains("music"))
                {
                    src.outputAudioMixerGroup = SettingsManager.Instance.bgmGroup;
                }
                else
                {
                    src.outputAudioMixerGroup = SettingsManager.Instance.sfxGroup;
                }
            }
        }

        // Khởi động: Bật Main Menu, tắt tất cả các panel khác
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
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
        if (characterButton == null)
        {
            Transform c = transform.Find("CharacterButton") ?? transform.Find("CharacterBtn") ?? transform.Find("CharBtn") ?? transform.Find("SkinBtn");
            if (c != null) characterButton = c.GetComponent<Button>();
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

        if (characterButton != null)
        {
            characterButton.onClick.RemoveListener(Character_Clicked);
            characterButton.onClick.AddListener(Character_Clicked);
        }
    }

    public void Character_Clicked()
    {
        PlayClickSound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
            CharacterSelectionHUD hud = characterSelectPanel.GetComponent<CharacterSelectionHUD>() ?? characterSelectPanel.GetComponentInChildren<CharacterSelectionHUD>(true);
            if (hud != null)
            {
                hud.previousPanel = mainMenuPanel;
            }
        }
    }

    public void CloseCharacterSelect()
    {
        PlayClickSound();
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
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
