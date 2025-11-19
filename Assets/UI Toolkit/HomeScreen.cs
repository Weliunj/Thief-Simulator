using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class HomeScreen : MonoBehaviour
{
    private void OnEnable()
    {
        VisualElement visualElement = GetComponent<UIDocument>().rootVisualElement;
        Button play = visualElement.Q<Button>("Play");
        Button options = visualElement.Q<Button>("Options");
        Button exit = visualElement.Q<Button>("Exit");
        /*
          Q: Chỉ trả về một phần tử duy nhất (hoặc null nếu không tìm thấy).
          Query: Thường được dùng để tìm nhiều phần tử.
        */

        play.clicked += Play_clicked;
        options.clicked += Options_clicked;
        exit.clicked += Exit_clicked;
    }

    private void Exit_clicked()
    {
        Debug.Log("Exit");
    }

    private void Options_clicked()
    {
        Debug.Log("Options");
    }

    private void Play_clicked()
    {
        SceneManager.LoadScene(1);
    }
}
