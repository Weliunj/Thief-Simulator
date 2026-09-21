using UnityEngine;

/// <summary>
/// Quản lý dữ liệu phiên chơi (Game Session) truyền xuyên suốt qua các Scene:
/// - Lưu Chapter được chọn từ Menu: GameSession.SelectedChapter
/// - Lưu Chapter tiếp theo: GameSession.NextChapter
/// - Lưu Nhân vật được chọn: GameSession.SelectedPlayer
/// </summary>
public static class GameSession
{
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
