using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioTest
{
    private IEnumerator LoadTestScene(string sceneName = "Lv1")
    {
        SceneManager.LoadScene(sceneName);
        yield return new WaitForSeconds(1.5f); 
    }


    [UnityTest]
    public IEnumerator TestBGVolume_SliderChangesAudioSource()
    {
        yield return LoadTestScene("HomeMenu");

        GameObject audioManager = GameObject.Find("AudioManager");
        Assert.IsNotNull(audioManager, "LỖI: Không tìm thấy GameObject 'AudioManager'!");

        Transform pressTransform = audioManager.transform.Find("Press");
        Assert.IsNotNull(pressTransform, "LỖI: Không tìm thấy Object con 'Press'!");
        
        AudioSource audioSource = pressTransform.GetComponent<AudioSource>();
        Assert.IsNotNull(audioSource, "LỖI: Không tìm thấy AudioSource trên Object 'Press'!");

        float testValue = 0.5f;
        audioSource.volume = testValue;
        yield return null;

        Assert.AreEqual(testValue, audioSource.volume, 0.01f, 
            $"Lỗi: Volume của AudioSource ({audioSource.volume}) không khớp với giá trị gán ({testValue})");
    }

    [UnityTest]
    public IEnumerator TC_AUD_02_AlarmLoops_WhenTimeRunsOut()
    {
        yield return LoadTestScene("Lv1");

        var ui = Object.FindFirstObjectByType<UI_Manager>();
        Assert.IsNotNull(ui, "LỖI: Không tìm thấy UI_Manager trong Scene!");
        Assert.IsNotNull(ui.playerManager, "LỖI: PlayerManager chưa được gán vào UI_Manager!");
        Assert.IsNotNull(ui.alarm, "LỖI: AudioSource 'alarm' chưa được gán vào UI_Manager!");

        ui.playerManager.currentTime = 0f;
        yield return null;

        Assert.IsTrue(ui.alarm.isPlaying, "Lỗi: Chuông báo động không kêu khi hết giờ!");
        Assert.IsTrue(ui.alarm.loop, "Lỗi: Chuông báo động không ở chế độ lặp (loop)!");
    }

    [UnityTest]
    public IEnumerator TC_AUD_04_AI_Detects_Player_And_Plays_Sound()
    {
        yield return LoadTestScene("Lv1");

        var ai = Object.FindFirstObjectByType<AI_Move_NavMesh>();
        var player = Object.FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        
        Assert.IsNotNull(ai, "LỖI: Không tìm thấy AI trong Scene!");
        Assert.IsNotNull(player, "LỖI: Không tìm thấy Player trong Scene!");

        player.transform.position = ai.transform.position + ai.transform.forward * 2f;
        player.player.isDied = false;
        yield return new WaitForSeconds(0.5f);

        Assert.IsTrue(ai.targetDetected, "Lỗi: AI không phát hiện được Player dù đã đứng trước mặt!");
        Assert.IsTrue(ai.audioSources[1].isPlaying, "Lỗi: AI phát hiện Player nhưng không phát âm thanh Detection!");
        Assert.IsTrue(ai.audioSources[2].isPlaying, "Lỗi: Nhạc Chase không tự động phát khi phát hiện mục tiêu!");
        Assert.IsTrue(AI_Move_NavMesh.isChaseMusicPlaying, "Lỗi: Biến Static isChaseMusicPlaying không được cập nhật!");
    }

    [UnityTest]
    public IEnumerator TC_AUD_05_ChaseMusic_Starts_On_Detection()
    {
        yield return LoadTestScene("Lv1");

        var ai = Object.FindFirstObjectByType<AI_Move_NavMesh>();
        Assert.IsNotNull(ai, "LỖI: Không tìm thấy AI trong Scene!");
        Assert.IsNotNull(ai.audioSources[2], "LỖI: Chưa gán nhạc Chase vào audioSources[2]!");
        ai.HandleChaseMusic(true);
        yield return null;

        Assert.IsTrue(ai.audioSources[2].isPlaying, "Lỗi: Nhạc Chase không phát khi AI đuổi bắt!");
        Assert.IsTrue(AI_Move_NavMesh.isChaseMusicPlaying, "Lỗi: Biến static 'isChaseMusicPlaying' không cập nhật thành True!");
    }

    [UnityTest]
    public IEnumerator TC_ANI_02_DieTrigger_ActivatesOnDeath()
    {
        yield return LoadTestScene("Lv1");
        var player = Object.FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        Assert.IsNotNull(player, "LỖI: Không tìm thấy Player (ThirdPersonController) trong Scene!");
        Assert.IsNotNull(player.player, "LỖI: PlayerManager chưa được gán vào Player!");
        Assert.IsNotNull(player.lightD, "LỖI: Object 'lightD' chưa được gán vào Player!");

        player.player.isDied = true;
        yield return new WaitForSeconds(1.0f);
        Assert.IsTrue(player.lightD.activeSelf, "Lỗi: Hiệu ứng đèn chết (lightD) chưa được bật!");
    }
}