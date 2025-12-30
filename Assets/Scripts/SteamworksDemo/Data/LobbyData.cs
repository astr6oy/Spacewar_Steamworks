using System.Collections.Generic;
using Steamworks;

namespace SteamworksDemo.Data
{
    /// <summary>
    /// 로비 데이터 모델
    /// </summary>
    [System.Serializable]
    public class LobbyData
    {
        /// <summary>
        /// 로비 Steam ID
        /// </summary>
        public CSteamID LobbyId { get; set; }

        /// <summary>
        /// 로비 소유자 Steam ID
        /// </summary>
        public CSteamID OwnerId { get; set; }

        /// <summary>
        /// 최대 인원
        /// </summary>
        public int MaxMembers { get; set; }

        /// <summary>
        /// 현재 인원
        /// </summary>
        public int CurrentMembers { get; set; }

        /// <summary>
        /// 멤버 목록
        /// </summary>
        public List<CSteamID> Members { get; set; } = new List<CSteamID>();

        /// <summary>
        /// 로비 메타데이터
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 로비 이름 (메타데이터에서 가져옴)
        /// </summary>
        public string Name
        {
            get => Metadata.ContainsKey("name") ? Metadata["name"] : $"Lobby {LobbyId}";
            set => Metadata["name"] = value;
        }

        /// <summary>
        /// 현재 사용자가 소유자인지 확인
        /// </summary>
        public bool IsOwner(CSteamID userId)
        {
            return OwnerId == userId;
        }

        /// <summary>
        /// 빈 자리 수
        /// </summary>
        public int AvailableSlots => MaxMembers - CurrentMembers;

        /// <summary>
        /// 로비가 가득 찼는지 확인
        /// </summary>
        public bool IsFull => CurrentMembers >= MaxMembers;
    }

    /// <summary>
    /// 로비 멤버 데이터
    /// </summary>
    [System.Serializable]
    public class LobbyMemberData
    {
        public CSteamID SteamId { get; set; }
        public string PersonaName { get; set; }
        public bool IsOwner { get; set; }
        public bool IsReady { get; set; }
    }
}
