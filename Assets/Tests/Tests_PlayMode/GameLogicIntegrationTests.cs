using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;
using UnityEngine.UI;

[TestFixture]
public class GameLogicIntegrationTests
{
    // --- HÀM HELPER (Chỉ giữ lại 1 phiên bản duy nhất cho mỗi chức năng) ---

    private PlayerManager CreatePlayerManager(int totalPoint = 400, int currPoint = 0)
    {
        var playerManager = ScriptableObject.CreateInstance<PlayerManager>();
        playerManager.totalpoint = totalPoint;
        playerManager.currpoint = currPoint;
        playerManager.MaxStamina = 10f;
        playerManager._stamina = playerManager.MaxStamina;
        playerManager.Maxweight = 100;
        playerManager.currweight = 0;
        playerManager.MaxTime = 300f;
        playerManager.currentTime = playerManager.MaxTime;
        playerManager.isDied = false;
        return playerManager;
    }

    private TextMeshProUGUI CreateTMPText(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }
        return go.GetComponent<TextMeshProUGUI>();
    }

    private Button CreateButtonWithText(string name, out TextMeshProUGUI text)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        text = CreateTMPText(name + "_Label", buttonGo.transform);
        return buttonGo.GetComponent<Button>();
    }

    private void AddCanvasGroup(GameObject go)
    {
        if (go != null && go.GetComponent<CanvasGroup>() == null)
        {
            go.AddComponent<CanvasGroup>();
        }
    }

    private UI_Manager CreateSafeUIManager(PlayerManager playerManager)
    {
        var uiGo = new GameObject("UI_Manager");
        var uiManager = uiGo.AddComponent<UI_Manager>();
        uiManager.playerManager = playerManager;

        uiManager.GuidePanel = new GameObject("GuidePanel");
        uiManager.diedPanel = new GameObject("DiedPanel");
        uiManager.WinPanel = new GameObject("WinPanel");
        uiManager.SolvePanel = new GameObject("SolvePanel");
        AddCanvasGroup(uiManager.SolvePanel);

        uiManager.alarm = new GameObject("Alarm").AddComponent<AudioSource>();

        uiManager.point = CreateTMPText("PointText", uiGo.transform);
        uiManager.targetPoint = CreateTMPText("TargetPoint", uiGo.transform);
        uiManager.currStamina = CreateTMPText("Stamina", uiGo.transform);
        uiManager.Stamina = CreateTMPText("MaxStamina", uiGo.transform);
        uiManager.currkg = CreateTMPText("Weight", uiGo.transform);
        uiManager.kg = CreateTMPText("MaxWeight", uiGo.transform);
        uiManager.timeText = CreateTMPText("Time", uiGo.transform);

        uiManager.progressSlider = new GameObject("Slider").AddComponent<Slider>();
        uiManager.progressText = CreateTMPText("ProgressText", uiGo.transform);

        uiManager.c = new TextMeshProUGUI[4];
        for (int i = 0; i < uiManager.c.Length; i++)
        {
            uiManager.c[i] = CreateTMPText("Ans" + i, uiGo.transform);
            uiManager.c[i].text = string.Empty;
        }

        return uiManager;
    }

    private void CreateRangeInteractionStub(GameObject parent)
    {
        var rangeGo = new GameObject("RangeInteraction");
        rangeGo.transform.SetParent(parent.transform, false);
        var range = rangeGo.AddComponent<Range_Interaction>();
        range.E_icon = new GameObject("EIcon");
        range.NameDisplay = new GameObject("NameDisplay", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        range.infItem = new GameObject("InfItem", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        var player = new GameObject("Player");
        player.tag = "Player";
        var camera = new GameObject("MainCamera");
        camera.tag = "MainCamera";
    }

    // --- CÁC BÀI TEST TÍCH HỢP ---

    [UnityTest]
    public IEnumerator TC_GL_01_ItemCollect_IncrementsPlayerPointsAndDestroysItem()
    {
        var playerManager = CreatePlayerManager(totalPoint: 100, currPoint: 0);

        var itemGo = new GameObject("TestItem");
        var item = itemGo.AddComponent<Item>();
        item.playerManagerl = playerManager;
        item.Price = 25f;

        var homeGo = new GameObject("Home");
        homeGo.tag = "home";
        var homeCollider = homeGo.AddComponent<BoxCollider>();

        itemGo.AddComponent<BoxCollider>().isTrigger = true;
        itemGo.AddComponent<Rigidbody>().isKinematic = true;

        item.OnTriggerEnter(homeCollider);
        yield return null;

        Assert.AreEqual(25, playerManager.currpoint, "Điểm phải tăng khi thu thập item.");
        Assert.IsTrue(item == null, "Item phải bị Destroy sau khi vào vùng home.");

        Object.DestroyImmediate(homeGo);
        Object.DestroyImmediate(playerManager);
    }

    [UnityTest]
    public IEnumerator TC_GL_02_UI_WinPanel_ShowsWhenTargetReached()
    {
        var playerManager = CreatePlayerManager(totalPoint: 400, currPoint: 400);

        var uiGo = new GameObject("UI_Manager");
        var uiManager = uiGo.AddComponent<UI_Manager>();
        uiManager.playerManager = playerManager;

        // Khởi tạo các Panel ảo
        uiManager.GuidePanel = new GameObject("GuidePanel");
        uiManager.diedPanel = new GameObject("DiedPanel");
        uiManager.WinPanel = new GameObject("WinPanel");
        uiManager.SolvePanel = new GameObject("SolvePanel");
        uiManager.alarm = new GameObject("Alarm").AddComponent<AudioSource>();

        // Cấu hình UI Text
        uiManager.point = CreateTMPText("PointText", uiGo.transform);
        uiManager.targetPoint = CreateTMPText("TargetPoint", uiGo.transform);
        uiManager.currStamina = CreateTMPText("Stamina", uiGo.transform);
        uiManager.Stamina = CreateTMPText("MaxStamina", uiGo.transform);
        uiManager.currkg = CreateTMPText("Weight", uiGo.transform);
        uiManager.kg = CreateTMPText("MaxWeight", uiGo.transform);
        uiManager.timeText = CreateTMPText("Time", uiGo.transform);

        uiManager.progressSlider = new GameObject("Slider").AddComponent<Slider>();
        uiManager.progressText = CreateTMPText("ProgressText", uiGo.transform);

        uiManager.c = new TextMeshProUGUI[4];
        for (int i = 0; i < 4; i++) uiManager.c[i] = CreateTMPText("Ans" + i, uiGo.transform);

        yield return null; // Chờ Update() của UI_Manager thực thi logic check win

        Assert.IsTrue(uiManager.WinPanel.activeSelf, "WinPanel phải hiện khi đủ điểm.");
        Assert.AreEqual("400", uiManager.point.text, "UI Point hiển thị sai giá trị.");

        Object.DestroyImmediate(uiGo);
        Object.DestroyImmediate(playerManager);
    }

    [UnityTest]
    public IEnumerator TC_GL_03_DoorMath_ThreeCorrectAnswers_DestroyDoorAfterDelay()
    {
        var uiManager = CreateSafeUIManager(CreatePlayerManager());

        var doorGo = new GameObject("TestDoor", typeof(AudioSource));
        CreateRangeInteractionStub(doorGo);
        var doorMath = doorGo.AddComponent<DoorMath>();
        doorMath.uiManager = uiManager;
        doorMath.requiredCorrectAnswers = 3;

        yield return null;

        doorMath.AnswerCorrect(); // 1/3
        doorMath.AnswerCorrect(); // 2/3
        doorMath.AnswerCorrect(); // 3/3
        
        Assert.AreEqual(3, doorMath.currentCorrectAnswers);
        
        // DoorMath có StartCoroutine(DestroyDoorAfterDelay()) đợi 1 giây
        yield return new WaitForSeconds(1.2f);

        Assert.IsTrue(doorMath == null, "Cửa phải bị Destroy sau khi hoàn thành câu hỏi.");
        
        Object.DestroyImmediate(uiManager.gameObject);
    }

    [UnityTest]
    public IEnumerator TC_GL_05_PlayerDeath_SetsIsDiedAndShowsDeathUI()
    {
        var playerManager = CreatePlayerManager();
        var uiManager = CreateSafeUIManager(playerManager);

        uiManager.diedPanel.SetActive(false);
        playerManager.isDied = true;
        
        yield return null; // Để Update() của UI_Manager chạy

        Assert.IsTrue(uiManager.diedPanel.activeSelf, "DiedPanel phải hiển thị khi isDied = true.");

        Object.DestroyImmediate(uiManager.gameObject);
        Object.DestroyImmediate(playerManager);
    }
}