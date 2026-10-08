using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý Bật/Tắt ngẫu nhiên (Random Toggle) các GameObject trong Scene (Rương Chest, Điểm trốn HideSpot, Cửa, Đèn...)
/// dựa theo Cấp độ khó (Easy, Normal, Hard):
/// - Hỗ trợ đồng bộ Seed ngẫu nhiên qua Multiplayer (mọi người chơi trong phòng thấy cùng rương/chỗ trốn được bật).
/// - Có thể cấu hình số lượng (Count) hoặc tỉ lệ % (Ratio) cần bật ngẫu nhiên cho từng nhóm đối tượng.
/// </summary>
public class SceneObjectRandomizer : MonoBehaviour
{
    [System.Serializable]
    public class DifficultySetting
    {
        [Tooltip("Chế độ chọn: Số lượng cố định (FixedCount) hoặc Tỉ lệ phần trăm (Percentage)")]
        public SelectionMode selectionMode = SelectionMode.FixedCount;

        [Tooltip("Số lượng đối tượng cần BẬT ngẫu nhiên trong danh sách (khi dùng FixedCount)")]
        public int activeCount = 5;

        [Range(0f, 1f)]
        [Tooltip("Tỉ lệ % đối tượng cần BẬT ngẫu nhiên (0.5 = 50%, 1.0 = Bật 100%) (khi dùng Percentage)")]
        public float activeRatio = 0.5f;
    }

    public enum SelectionMode
    {
        FixedCount,
        Percentage
    }

    [System.Serializable]
    public class RandomGroup
    {
        [Tooltip("Tên nhóm để phân biệt (VD: Chests, HideSpots, Safes, Doors...)")]
        public string groupName = "Chests";

        [Tooltip("Danh sách các GameObject trong Scene thuộc nhóm này")]
        public List<GameObject> targetObjects = new List<GameObject>();

        [Header("🎮 Cấu hình số lượng Bật theo Độ khó")]
        public DifficultySetting easySetting = new DifficultySetting { selectionMode = SelectionMode.FixedCount, activeCount = 8, activeRatio = 1.0f };
        public DifficultySetting normalSetting = new DifficultySetting { selectionMode = SelectionMode.FixedCount, activeCount = 5, activeRatio = 0.6f };
        public DifficultySetting hardSetting = new DifficultySetting { selectionMode = SelectionMode.FixedCount, activeCount = 3, activeRatio = 0.3f };

        public DifficultySetting GetSetting(GameDifficulty difficulty)
        {
            switch (difficulty)
            {
                case GameDifficulty.Easy: return easySetting;
                case GameDifficulty.Hard: return hardSetting;
                case GameDifficulty.Normal:
                default: return normalSetting;
            }
        }
    }

    [Header("📦 Danh sách các nhóm đối tượng cần Random Bật/Tắt")]
    public List<RandomGroup> groups = new List<RandomGroup>();

    [Header("⚙️ Settings")]
    [Tooltip("Tự động áp dụng ngay trong Awake (chạy trước Start của các script khác)")]
    public bool applyOnAwake = true;

    [Tooltip("Đảo ngược logic: Thay vì chọn những cái để BẬT, sẽ chọn những cái để TẮT")]
    public bool invertSelection = false;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyRandomization();
        }
    }

    /// <summary>
    /// Thực hiện xáo trộn ngẫu nhiên và bật/tắt các GameObject theo độ khó & seed đồng bộ
    /// </summary>
    [ContextMenu("Apply Randomization")]
    public void ApplyRandomization()
    {
        // 1. Đồng bộ Độ khó từ Fusion Session Properties (nếu đang trong phòng Online)
        if (FusionConnectionManager.Instance != null &&
            FusionConnectionManager.Instance.IsInGameplaySession &&
            FusionConnectionManager.Instance.currentRunner != null &&
            FusionConnectionManager.Instance.currentRunner.SessionInfo != null)
        {
            var session = FusionConnectionManager.Instance.currentRunner.SessionInfo;
            if (session.Properties != null && session.Properties.TryGetValue("diff", out var diffProp))
            {
                GameSession.SelectedDifficulty = (GameDifficulty)(int)diffProp;
            }
        }

        GameDifficulty difficulty = GameSession.SelectedDifficulty;

        // 2. Đồng bộ Seed ngẫu nhiên (nếu Online lấy chung Seed phòng của Host, Offline lấy ngẫu nhiên)
        int seed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode();
        if (FusionConnectionManager.Instance != null &&
            FusionConnectionManager.Instance.IsInGameplaySession &&
            FusionConnectionManager.Instance.currentRunner != null &&
            FusionConnectionManager.Instance.currentRunner.SessionInfo != null)
        {
            var session = FusionConnectionManager.Instance.currentRunner.SessionInfo;
            if (session.Properties != null && session.Properties.TryGetValue("seed", out var seedProp))
            {
                seed = (int)seedProp;
            }
        }

        // Tạo bộ sinh số ngẫu nhiên độc lập dựa trên Seed để không làm ảnh hưởng RNG khác
        System.Random rng = new System.Random(seed);

        if (groups == null || groups.Count == 0)
        {
            Debug.LogWarning("[SceneObjectRandomizer] Không có nhóm đối tượng nào (Groups is empty)!");
            return;
        }

        foreach (var group in groups)
        {
            if (group == null) continue;

            if (group.targetObjects == null || group.targetObjects.Count == 0)
            {
                Debug.LogWarning($"<color=orange>[SceneObjectRandomizer] Nhóm '{group.groupName}' có danh sách Target Objects rỗng!</color>");
                continue;
            }

            // Lọc danh sách hợp lệ (bỏ qua null)
            List<GameObject> validObjects = new List<GameObject>();
            foreach (var obj in group.targetObjects)
            {
                if (obj != null && !validObjects.Contains(obj))
                {
                    validObjects.Add(obj);
                }
            }

            int totalCount = validObjects.Count;
            if (totalCount == 0) continue;

            DifficultySetting setting = group.GetSetting(difficulty);
            int targetActiveCount = 0;

            if (setting.selectionMode == SelectionMode.FixedCount)
            {
                targetActiveCount = Mathf.Clamp(setting.activeCount, 0, totalCount);
            }
            else
            {
                targetActiveCount = Mathf.RoundToInt(totalCount * Mathf.Clamp01(setting.activeRatio));
            }

            // Fisher-Yates Shuffle danh sách bằng RNG đồng bộ
            List<GameObject> shuffled = new List<GameObject>(validObjects);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                GameObject temp = shuffled[i];
                shuffled[i] = shuffled[swapIndex];
                shuffled[swapIndex] = temp;
            }

            // Bật targetActiveCount phần tử đầu tiên, tắt các phần tử còn lại
            HashSet<GameObject> chosenToActivate = new HashSet<GameObject>();
            for (int i = 0; i < targetActiveCount; i++)
            {
                chosenToActivate.Add(shuffled[i]);
            }

            int actualActive = 0;
            int actualInactive = 0;

            foreach (var obj in validObjects)
            {
                bool shouldBeActive = chosenToActivate.Contains(obj);
                if (invertSelection) shouldBeActive = !shouldBeActive;

                obj.SetActive(shouldBeActive);

                if (shouldBeActive) actualActive++;
                else actualInactive++;
            }

            Debug.Log($"<color=cyan>[SceneObjectRandomizer] Nhóm '{group.groupName}' ({setting.selectionMode}) - Độ khó [{difficulty}]: Đã BẬT {actualActive}/{totalCount}, TẮT {actualInactive}/{totalCount}</color>");
        }
    }
}

