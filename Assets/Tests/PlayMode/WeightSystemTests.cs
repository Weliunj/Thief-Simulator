using NUnit.Framework;
using UnityEngine;

public class WeightSystemTests
{
    // Giả lập lại các thông số bạn đang dùng trong PlayerManager và Controller
    private float moveSpeed = 2.0f;
    private int maxWeight = 100;

    // Hàm này mô phỏng y hệt logic trong file ThirdPersonController.cs của bạn
    private float CalculateSpeedLogic(int currentWeight)
    {
        // Copy y hệt công thức từ code bạn gửi
        float weightRatio = (float)currentWeight / (float)maxWeight;
        float minMoveSpeed = moveSpeed / 4f;

        weightRatio = Mathf.Clamp01(weightRatio);

        // Trả về kết quả sau khi Lerp
        return Mathf.Lerp(moveSpeed, minMoveSpeed, weightRatio);
    }

    [Test]
    public void Test_WeightZero_ReturnsMaxSpeed()
    {
        // Sắp xếp: Mang 0kg
        int weight = 0;

        // Hành động: Tính toán
        float finalSpeed = CalculateSpeedLogic(weight);

        // Kiểm tra: Phải bằng 2.0f
        Assert.AreEqual(2.0f, finalSpeed, 0.01f);
    }

    [Test]
    public void Test_WeightMax_ReturnsMinSpeed()
    {
        // Sắp xếp: Mang 100kg (Max)
        int weight = 100;

        // Hành động
        float finalSpeed = CalculateSpeedLogic(weight);

        // Kiểm tra: Phải bằng 2.0 / 4 = 0.5f
        Assert.AreEqual(0.5f, finalSpeed, 0.01f);
        //Delta: khoang chenh lech cho phep
    }

    [Test]
    public void Test_WeightOverLimit_StillReturnsMinSpeed()
    {
        // Sắp xếp: Mang 150kg (Vượt giới hạn)
        int weight = 150;

        // Hành động
        float finalSpeed = CalculateSpeedLogic(weight);

        // Kiểm tra: Vì có Clamp01 nên tốc độ vẫn phải là 0.5f, không được thấp hơn
        Assert.AreEqual(0.5f, finalSpeed, 0.01f);
    }
}