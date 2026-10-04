public enum GameDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

/// <summary>
/// Quản lý dữ liệu phiên chơi (Game Session) truyền xuyên suốt qua các Scene:
/// - Lưu Chapter được chọn từ Menu: GameSession.SelectedChapter
/// - Lưu Độ khó được chọn: GameSession.SelectedDifficulty
/// - Lưu Chapter tiếp theo: GameSession.NextChapter
/// - Lưu Nhân vật được chọn: GameSession.SelectedPlayer
/// </summary>
public static class GameSession
{
    /// <summary>
    /// Độ khó được chọn cho ván chơi (Easy, Normal, Hard)
    /// </summary>
    public static GameDifficulty SelectedDifficulty { get; set; } = GameDifficulty.Normal;

    /// <summary>
    /// Cho phép kích hoạt thêm các NPC đặc biệt (Special NPCs) bất kể độ khó
    /// </summary>
    public static bool EnableSpecialNPCs { get; set; } = true;

    /// <summary>
    /// Chapter đang được chọn để chơi trong trận
    /// </summary>
    public static ChapterSO SelectedChapter { get; set; }

    /// <summary>
    /// Chapter tiếp theo sau khi hoàn thành Chapter hiện tại
    /// </summary>
    public static ChapterSO NextChapter { get; set; }

    /// <summary>
    /// Nhân vật (PlayerSO) đang được chọn
    /// </summary>
    public static PlayerSO SelectedPlayer { get; set; }

    /// <summary>
    /// Giới tính của nhân vật đang chọn (true = Male, false = Female)
    /// </summary>
    public static bool IsMale { get; set; } = true;

    /// <summary>
    /// Index của Skin / Texture đang chọn
    /// </summary>
    public static int SelectedTextureIndex { get; set; } = 0;

    /// <summary>
    /// Thiết lập nhanh dữ liệu phiên chơi
    /// </summary>
    public static void SetSession(ChapterSO chapter, ChapterSO next = null, PlayerSO player = null)
    {
        SelectedChapter = chapter;
        NextChapter = next;
        if (player != null)
        {
            SelectedPlayer = player;
        }
    }

    /// <summary>
    /// Xóa session khi quay về Main Menu
    /// </summary>
    public static void ClearSession()
    {
        SelectedChapter = null;
        NextChapter = null;
        SelectedPlayer = null;
    }
}
