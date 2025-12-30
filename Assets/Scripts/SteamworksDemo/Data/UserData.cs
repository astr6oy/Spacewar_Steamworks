using Steamworks;

namespace SteamworksDemo.Data
{
    /// <summary>
    /// Steam 사용자 정보 데이터 모델
    /// </summary>
    [System.Serializable]
    public class UserData
    {
        public CSteamID SteamId { get; set; }
        public string PersonaName { get; set; }
        public int SteamLevel { get; set; }
        public EPersonaState PersonaState { get; set; }
        public bool IsLoggedOn { get; set; }

        /// <summary>
        /// 온라인 상태를 한글 문자열로 반환
        /// </summary>
        public string GetPersonaStateText()
        {
            return PersonaState switch
            {
                EPersonaState.k_EPersonaStateOnline => "온라인",
                EPersonaState.k_EPersonaStateAway => "자리 비움",
                EPersonaState.k_EPersonaStateBusy => "다른 용무 중",
                EPersonaState.k_EPersonaStateSnooze => "대기 모드",
                EPersonaState.k_EPersonaStateLookingToTrade => "거래 희망",
                EPersonaState.k_EPersonaStateLookingToPlay => "게임 희망",
                EPersonaState.k_EPersonaStateOffline => "오프라인",
                _ => "알 수 없음"
            };
        }
    }
}
