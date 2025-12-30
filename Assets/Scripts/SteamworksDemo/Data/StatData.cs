namespace SteamworksDemo.Data
{
    /// <summary>
    /// Stats 데이터 모델
    /// SpaceWar에서 사용하는 통계 정보
    /// </summary>
    [System.Serializable]
    public class StatData
    {
        /// <summary>
        /// Stat API 이름
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 표시용 이름
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Stat 타입 (int 또는 float)
        /// </summary>
        public StatType Type { get; set; }

        /// <summary>
        /// 정수 값 (INT 타입인 경우)
        /// </summary>
        public int IntValue { get; set; }

        /// <summary>
        /// 실수 값 (FLOAT 타입인 경우)
        /// </summary>
        public float FloatValue { get; set; }

        /// <summary>
        /// 최대값 (게이지 표시용)
        /// </summary>
        public float MaxValue { get; set; } = 100f;

        /// <summary>
        /// 값을 문자열로 반환
        /// </summary>
        public string GetValueText()
        {
            return Type == StatType.Int
                ? IntValue.ToString()
                : FloatValue.ToString("F1");
        }

        /// <summary>
        /// 진행률 (0~1) 반환
        /// </summary>
        public float GetProgress()
        {
            float value = Type == StatType.Int ? IntValue : FloatValue;
            return MaxValue > 0 ? UnityEngine.Mathf.Clamp01(value / MaxValue) : 0f;
        }
    }

    public enum StatType
    {
        Int,
        Float
    }
}
