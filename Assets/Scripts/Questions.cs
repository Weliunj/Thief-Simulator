using UnityEngine;
using System.Collections.Generic;

// Giữ nguyên Class dữ liệu của bạn
[System.Serializable]
public class QuestionData
{
    public string questionContent;
    public string correctAnswer;
    public string[] incorrectAnswers;

    public QuestionData(string q, string correct, string[] incorrect)
    {
        questionContent = q;
        correctAnswer = correct;
        incorrectAnswers = incorrect;
    }
}

[CreateAssetMenu(fileName = "QuestionDataSO", menuName = "ScriptableObjects/Question List", order = 1)]
public class Questions : ScriptableObject
{
    public List<QuestionData> allQuestions = new List<QuestionData>();

    // Hàm này giúp bạn đổ dữ liệu nhanh từ Inspector
    [ContextMenu("Đổ dữ liệu 50 câu đố")]
    public void InitializeQuestions()
    {
        allQuestions.Clear();

        // Thêm trực tiếp các đối tượng vào List
        allQuestions.Add(new QuestionData("Cái gì của mình nhưng toàn người khác dùng?", "Cái tên", new string[] { "Cái ví", "Cái áo", "Cái ghế" }));
        allQuestions.Add(new QuestionData("Con gì đập thì sống, không đập thì chết?", "Con tim", new string[] { "Con muỗi", "Con quay", "Con cá" }));
        allQuestions.Add(new QuestionData("Càng thiu càng ngon là cái gì?", "Giấc ngủ", new string[] { "Thức ăn", "Cá khô", "Nước mắm" }));
        allQuestions.Add(new QuestionData("Lịch nào dài nhất?", "Lịch sử", new string[] { "Lịch vạn niên", "Lịch âm", "Lịch treo tường" }));
        allQuestions.Add(new QuestionData("Quả gì không ăn được nhưng lại treo được?", "Quả bóng", new string[] { "Quả mít", "Quả cân", "Quả xoài" }));
        allQuestions.Add(new QuestionData("Cái gì chặt không đứt, bứt không rời?", "Nước", new string[] { "Sợi dây", "Cánh cửa", "Miếng thịt" }));
        allQuestions.Add(new QuestionData("Con gì không có xương sống mà vẫn đứng được?", "Con dốc", new string[] { "Con giun", "Con ốc sên", "Con rắn" }));
        allQuestions.Add(new QuestionData("Bỏ ngoài nướng trong, ăn ngoài bỏ trong là gì?", "Bắp ngô", new string[] { "Củ khoai", "Con gà", "Quả trứng" }));
        allQuestions.Add(new QuestionData("Con gì càng to lại càng nhỏ?", "Con cua", new string[] { "Con kiến", "Con voi", "Con muỗi" }));
        allQuestions.Add(new QuestionData("Cái gì có răng mà không có miệng?", "Cái lược", new string[] { "Cái cưa", "Con ma", "Cái nĩa" }));
        allQuestions.Add(new QuestionData("Sở thú bị cháy, con gì chạy ra đầu tiên?", "Con người", new string[] { "Con voi", "Con hổ", "Con khỉ" }));
        allQuestions.Add(new QuestionData("Cái gì có cổ nhưng không có đầu?", "Cái áo", new string[] { "Cái lọ", "Con cò", "Cái chai" }));
        allQuestions.Add(new QuestionData("Quần rộng nhất là quần gì?", "Quần đảo", new string[] { "Quần đùi", "Quần bò", "Quần tây" }));
        allQuestions.Add(new QuestionData("Cái gì luôn ở phía trước mà bạn không thấy?", "Tương lai", new string[] { "Cái mũi", "Mặt trời", "Cánh cửa" }));
        allQuestions.Add(new QuestionData("Con gì không biết đi nhưng lại biết bay?", "Con diều", new string[] { "Con chim", "Con muỗi", "Con ruồi" }));
        allQuestions.Add(new QuestionData("Nhà trắng ở đâu?", "Ở Mỹ", new string[] { "Ở giữa", "Ở trên", "Không tồn tại" }));
        allQuestions.Add(new QuestionData("Cái gì càng kéo càng ngắn?", "Điếu thuốc", new string[] { "Sợi dây", "Cây thước", "Con đường" }));
        allQuestions.Add(new QuestionData("Con gì có thể đi bộ bằng đầu?", "Con chấy", new string[] { "Con kiến", "Con rắn", "Con ốc" }));
        allQuestions.Add(new QuestionData("Cái gì có chân mà không biết đi?", "Cái bàn", new string[] { "Con vịt", "Cái áo", "Quả bóng" }));
        allQuestions.Add(new QuestionData("Vượt qua người thứ 2, bạn đứng thứ mấy?", "Thứ 2", new string[] { "Thứ 1", "Thứ 3", "Cuối cùng" }));
        allQuestions.Add(new QuestionData("Cái gì có lưỡi mà không có miệng?", "Con dao", new string[] { "Cái kéo", "Cái đĩa", "Cái thìa" }));
        allQuestions.Add(new QuestionData("Hoa gì luôn hướng về phía mặt trời?", "Hoa hướng dương", new string[] { "Hoa hồng", "Hoa huệ", "Hoa lan" }));
        allQuestions.Add(new QuestionData("Con đường dài nhất là đường nào?", "Đường đời", new string[] { "Đường bộ", "Đường sắt", "Đường mòn" }));
        allQuestions.Add(new QuestionData("Cái gì đánh cha mẹ, anh em, bạn bè?", "Bàn chải răng", new string[] { "Cái gậy", "Cây roi", "Lời nói" }));
        allQuestions.Add(new QuestionData("Nắng ba năm ta chưa hề bỏ bạn?", "Cái bóng", new string[] { "Đôi dép", "Cái nón", "Cái áo" }));
        allQuestions.Add(new QuestionData("Trái gì không thể cầm được?", "Trái tim", new string[] { "Trái bóng", "Trái mít", "Trái đất" }));
        allQuestions.Add(new QuestionData("Cái gì càng rửa càng bẩn?", "Nước", new string[] { "Cái bát", "Cái khăn", "Cái tay" }));
        allQuestions.Add(new QuestionData("Xe 15 tấn qua cầu 10 tấn không sập?", "Xe không chở gì", new string[] { "Cầu sắt", "Xe chạy nhanh", "Cầu mới" }));
        allQuestions.Add(new QuestionData("Bắn chết 1 con chim trong đàn 10 con?", "0 con", new string[] { "9 con", "1 con", "10 con" }));
        allQuestions.Add(new QuestionData("Núi nào bị chặt ra từng khúc?", "Thái Sơn", new string[] { "Everest", "Ngũ Hành", "Langbiang" }));
        allQuestions.Add(new QuestionData("Cây gì không lá, không hoa, không quả?", "Cột điện", new string[] { "Xương rồng", "Cây khô", "Cây thông" }));
        allQuestions.Add(new QuestionData("Cái gì Adam có 2 mà Eva chỉ có 1?", "Chữ A", new string[] { "Con mắt", "Cái chân", "Cái tai" }));
        allQuestions.Add(new QuestionData("Cái gì có lỗ hổng mà vẫn giữ được nước?", "Bọt biển", new string[] { "Cái rổ", "Cái ly", "Cái xô" }));
        allQuestions.Add(new QuestionData("Con mèo nào sợ chuột nhất?", "Doraemon", new string[] { "Mèo mướp", "Mèo tam thể", "Mèo đen" }));
        allQuestions.Add(new QuestionData("Tháng nào trong năm có 28 ngày?", "Tất cả", new string[] { "Tháng 2", "Tháng 1", "Tháng 12" }));
        allQuestions.Add(new QuestionData("Bệnh gì bác sĩ bó tay?", "Gãy tay", new string[] { "Bệnh tim", "Ung thư", "Đau đầu" }));
        allQuestions.Add(new QuestionData("Sông gì không có nước?", "Sông trên bản đồ", new string[] { "Sông Đà", "Sông Hồng", "Sông Cầu" }));
        allQuestions.Add(new QuestionData("Kiến gì không bao giờ ngủ?", "Kiến thức", new string[] { "Kiến lửa", "Kiến thợ", "Kiến gió" }));
        allQuestions.Add(new QuestionData("Con gì mang được cả miếng gỗ nhưng không mang được hòn sỏi?", "Con sông", new string[] { "Con kiến", "Con voi", "Con cá" }));
        allQuestions.Add(new QuestionData("Nơi nào có đường xá nhưng không có xe?", "Bản đồ", new string[] { "Sa mạc", "Rừng rậm", "Nghĩa địa" }));
        allQuestions.Add(new QuestionData("Cái gì không mượn mà trả?", "Lời chào", new string[] { "Tiền bạc", "Cuốn sách", "Cái nợ" }));
        allQuestions.Add(new QuestionData("Tay trái cầm được nhưng tay phải không cầm được?", "Khuỷu tay phải", new string[] { "Cái bút", "Cái bát", "Cục đá" }));
        allQuestions.Add(new QuestionData("Con gì vốn dĩ đã ác mồm ác miệng?", "Con cá sấu", new string[] { "Con hổ", "Con rắn", "Con sư tử" }));
        allQuestions.Add(new QuestionData("Cái gì không cánh mà bay?", "Thời gian", new string[] { "Con chim", "Máy bay", "Làn khói" }));
        allQuestions.Add(new QuestionData("Quả gì tên gọi như đau?", "Quả khổ qua", new string[] { "Quả ớt", "Quả chanh", "Quả me" }));
        allQuestions.Add(new QuestionData("Xã nào đông dân nhất?", "Xã hội", new string[] { "Xã nông thôn", "Xã miền núi", "Xã đảo" }));
        allQuestions.Add(new QuestionData("Tiền gì không dùng để mua đồ?", "Tiền sử", new string[] { "Tiền đô", "Tiền lẻ", "Tiền rách" }));
        allQuestions.Add(new QuestionData("Hạt gì dài nhất?", "Hạt mưa", new string[] { "Hạt gạo", "Hạt dưa", "Hạt đậu" }));
        allQuestions.Add(new QuestionData("Con gì càng đánh càng yêu?", "Con gái", new string[] { "Con chó", "Con mèo", "Con muỗi" }));
        allQuestions.Add(new QuestionData("Cái gì đi nằm, đứng nằm, nhưng nằm lại đứng?", "Bàn chân", new string[] { "Cái giường", "Cái ghế", "Cái tủ" }));

        Debug.Log("Đã đổ xong 50 câu hỏi!");
    }
}