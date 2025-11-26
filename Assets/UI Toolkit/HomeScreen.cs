using System;
using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class HomeScreen : MonoBehaviour
{
    public AudioSource[] audioSources;
    VisualElement visualElement;
    Button play;
    Button options;
    Button exit;
    VisualElement Charac;
    private void OnEnable()
    {
         visualElement = GetComponent<UIDocument>().rootVisualElement;
         play = visualElement.Q<Button>("Play");
         options = visualElement.Q<Button>("Options");
         exit = visualElement.Q<Button>("Exit");
         Charac = visualElement.Q<Image>("Char"); //anh player man hinh chinh
        /*
          Q: Chỉ trả về một phần tử duy nhất (hoặc null nếu không tìm thấy).
          Query: Thường được dùng để tìm nhiều phần tử.
        */

        play.clicked += Play_clicked;
        options.clicked += Options_clicked;
        exit.clicked += Exit_clicked;


        RegisterHoverEvents(play);
        RegisterHoverEvents(options);
        RegisterHoverEvents(exit);
    }
    private void RegisterHoverEvents(Button button)
    {
        if (button != null)
        {
            // Đăng ký sự kiện khi chuột đi vào (phát âm thanh Hover)
            button.RegisterCallback<PointerEnterEvent>(evt =>
            {
                // Kiểm tra xem AudioSource có tồn tại và đã có AudioClip không
                if (audioSources.Length > 1 && audioSources[0].clip != null)
                {
                    // Phát âm thanh hover (audioSources[1])
                    audioSources[0].PlayOneShot(audioSources[0].clip);
                }
            });

            // Bạn có thể đăng ký PointerLeaveEvent nếu cần logic khi rời chuột
        }
    }
    private void Exit_clicked()
    {
        if (audioSources.Length > 0) audioSources[1].Play();
        Debug.Log("Exit");
    }

    private void Options_clicked()
    {
        if (audioSources.Length > 0) audioSources[1].Play();
        Debug.Log("Options");
    }

    private void Play_clicked()
    {
        if (audioSources.Length > 0) audioSources[1].Play();
        StartCoroutine(LoadSceneAfterDelay(1));
    }
    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(1);
    }
}
