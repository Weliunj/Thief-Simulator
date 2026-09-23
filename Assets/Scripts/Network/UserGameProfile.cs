using System;
using System.Collections.Generic;

/// <summary>
/// Cấu trúc dữ liệu Hồ sơ người chơi lưu trữ trên Firebase Realtime Database
/// </summary>
[Serializable]
public class UserGameProfile
{
    public string uid = "";
    public string username = "Thief";
    public string email = "";
    public int cash = 0; // Tiền mặt (Cash)

    // Thuộc tính tương thích nếu cần
    public int gold { get => cash; set => cash = value; }

    // Dữ liệu Nhân vật
    public string selectedCharacterId = "char_01";
    public int selectedCharacterIndex = 0;
    public bool isMale = true;
    public List<string> unlockedCharacterIds = new List<string>() { "char_01" };

    // Dữ liệu Chapter đã mở khóa
    public int highestUnlockedChapter = 1;

    // Thời gian đăng nhập cuối cùng
    public string lastLoginTime = "";

    /// <summary>
    /// Thời gian lưu cuối cùng (Unix timestamp milliseconds) - dùng để so sánh phiên bản dữ liệu giữa Local và Cloud
    /// </summary>
    public long lastSaveTimestamp = 0;

    public UserGameProfile()
    {
        lastLoginTime = DateTime.UtcNow.ToString("o");
    }

    public UserGameProfile(string uid, string username, string email)
    {
        this.uid = uid;
        this.username = string.IsNullOrEmpty(username) ? "Thief_" + uid.Substring(0, Math.Min(5, uid.Length)) : username;
        this.email = email;
        this.cash = 0;
        this.selectedCharacterId = "char_01";
        this.selectedCharacterIndex = 0;
        this.isMale = true;
        this.unlockedCharacterIds = new List<string>() { "char_01" };
        this.highestUnlockedChapter = 1;
        this.lastLoginTime = DateTime.UtcNow.ToString("o");
        this.lastSaveTimestamp = GetCurrentTimestampMs();
    }

    /// <summary>
    /// Kiểm tra hồ sơ có tiến độ thực sự hay chỉ là save mới tạo (trắng)
    /// </summary>
    public bool HasMeaningfulProgress()
    {
        if (cash > 0) return true;
        if (highestUnlockedChapter > 1) return true;
        if (unlockedCharacterIds != null && unlockedCharacterIds.Count > 1) return true;
        if (selectedCharacterIndex > 0) return true;
        if (!string.IsNullOrEmpty(selectedCharacterId) && selectedCharacterId != "char_01") return true;
        return false;
    }

    /// <summary>
    /// Lấy Unix timestamp hiện tại (milliseconds)
    /// </summary>
    public static long GetCurrentTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

