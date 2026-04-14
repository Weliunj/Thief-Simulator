using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameLogic_PlayMode_Tests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Load scene trước khi test
        SceneManager.LoadScene("Lv1"); 
        yield return new WaitForSeconds(0.5f); // Đợi scene khởi tạo
    }
    
    [UnityTest]
    public IEnumerator TC_HUD_01_PointUpdate_ReflectsInUI()
    {
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
        var ui = Object.FindFirstObjectByType<UI_Manager>();
        ui.playerManager.currpoint = 50; 
        yield return null; 
        
        Assert.AreEqual("50", ui.point.text, "UI Point không cập nhật đúng giá trị hiển thị!");
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator TC_SET_03_ProgressSlider_IncreasesOnCorrectAnswer()
    {

        var ui = Object.FindFirstObjectByType<UI_Manager>();
        ui.UpdateProgressUI(2);
        
        yield return null;
        
        Assert.AreEqual(2f, ui.progressSlider.value, "Slider tiến độ không cập nhật đúng");
        Assert.AreEqual("2/3", ui.progressText.text, "Text tiến độ không hiển thị 2/3");
    }
}