using Steamworks;

namespace SteamworksDemo.Data
{
    /// <summary>
    /// 리더보드 항목 데이터 모델
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntryData
    {
        /// <summary>
        /// 순위
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 플레이어 Steam ID
        /// </summary>
        public CSteamID SteamId { get; set; }

        /// <summary>
        /// 플레이어 이름
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 점수
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 현재 사용자인지 여부
        /// </summary>
        public bool IsCurrentUser { get; set; }

        /// <summary>
        /// 추가 데이터 (선택적)
        /// </summary>
        public int[] Details { get; set; }
    }
}
