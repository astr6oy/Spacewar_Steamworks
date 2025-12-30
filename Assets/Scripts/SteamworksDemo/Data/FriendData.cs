using Steamworks;

namespace SteamworksDemo.Data
{
    /// <summary>
    /// Steam 친구 정보 데이터 모델
    /// </summary>
    [System.Serializable]
    public class FriendData
    {
        /// <summary>
        /// 친구의 Steam ID
        /// </summary>
        public CSteamID SteamId { get; set; }

        /// <summary>
        /// 닉네임
        /// </summary>
        public string PersonaName { get; set; }

        /// <summary>
        /// 온라인 상태
        /// </summary>
        public EPersonaState PersonaState { get; set; }

        /// <summary>
        /// 현재 같은 게임을 플레이 중인지
        /// </summary>
        public bool IsPlayingThisGame { get; set; }

        /// <summary>
        /// 현재 플레이 중인 게임 이름 (다른 게임인 경우)
        /// </summary>
        public string CurrentGameName { get; set; }

        /// <summary>
        /// 온라인 상태 한글 텍스트
        /// </summary>
        public string GetStateText()
        {
            return PersonaState switch
            {
                EPersonaState.k_EPersonaStateOnline => "온라인",
                EPersonaState.k_EPersonaStateAway => "자리 비움",
                EPersonaState.k_EPersonaStateBusy => "다른 용무 중",
                EPersonaState.k_EPersonaStateSnooze => "잠자기 모드",
                EPersonaState.k_EPersonaStateLookingToTrade => "거래 희망",
                EPersonaState.k_EPersonaStateLookingToPlay => "게임 희망",
                EPersonaState.k_EPersonaStateOffline => "오프라인",
                _ => "알 수 없음"
            };
        }

        /// <summary>
        /// 온라인 상태인지 확인
        /// </summary>
        public bool IsOnline()
        {
            return PersonaState != EPersonaState.k_EPersonaStateOffline;
        }
    }
}
