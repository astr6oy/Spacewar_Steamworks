namespace SteamworksDemo.Data
{
    /// <summary>
    /// 도전과제 데이터 모델
    /// Steam에서 가져온 업적 정보 저장
    /// </summary>
    [System.Serializable]
    public class AchievementData
    {
        /// <summary>
        /// Steam에 등록된 Achievement API 이름
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 표시용 이름
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 도전과제 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 해제 여부
        /// </summary>
        public bool IsUnlocked { get; set; }

        /// <summary>
        /// 해제 시간 (Unix timestamp)
        /// </summary>
        public uint UnlockTime { get; set; }

        /// <summary>
        /// 숨겨진 도전과제 여부
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// 아이콘 인덱스 (기본 도형 색상 지정용)
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// 해제 시간을 읽기 쉬운 형태로 반환
        /// </summary>
        public string GetUnlockTimeText()
        {
            if (!IsUnlocked || UnlockTime == 0) return "";

            var dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
                .AddSeconds(UnlockTime)
                .ToLocalTime();

            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
