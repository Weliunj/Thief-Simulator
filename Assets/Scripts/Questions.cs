using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class QuestionData
{
    public string questionContent; // Nội dung câu hỏi
    public string correctAnswer;   // Đáp án đúng
    public string[] incorrectAnswers; // 3 đáp án sai
}

[CreateAssetMenu(fileName = "QuestionDataSO", menuName = "ScriptableObjects/Question List", order = 1)]
public class Questions : ScriptableObject
{
    // Danh sách tất cả câu hỏi
    public List<QuestionData> allQuestions = new List<QuestionData>();
}