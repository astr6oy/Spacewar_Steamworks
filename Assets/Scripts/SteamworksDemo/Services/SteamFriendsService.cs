using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;
using SteamworksDemo.Data;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam 친구 서비스
    /// 친구 목록 조회, 상태 확인, 게임 초대 등
    /// </summary>
    public class SteamFriendsService : MonoBehaviour, ISteamService
    {
        public static SteamFriendsService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 친구 목록
        private readonly List<FriendData> _friends = new List<FriendData>();
        public IReadOnlyList<FriendData> Friends => _friends;

        // 온라인 친구 수
        public int OnlineFriendsCount => _friends.FindAll(f => f.IsOnline()).Count;
        public int TotalFriendsCount => _friends.Count;

        // Callbacks
        private Callback<PersonaStateChange_t> _personaStateChangeCallback;
        private Callback<GameRichPresenceJoinRequested_t> _gameRichPresenceCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            if (!SteamManager.CheckInitialized("FriendsService")) return;

            // Callback 등록
            _personaStateChangeCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
            _gameRichPresenceCallback = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);

            // 친구 목록 로드
            LoadFriendsList();

            IsInitialized = true;
            SteamLogger.Log("Friends", "FriendsService 초기화 완료");
        }

        /// <summary>
        /// 친구 목록 로드
        /// </summary>
        public void LoadFriendsList()
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return;

            _friends.Clear();

            // 친구 수 가져오기
            int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

            for (int i = 0; i < friendCount; i++)
            {
                CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                FriendData friend = GetFriendData(friendId);
                _friends.Add(friend);
            }

            // 온라인 상태 순으로 정렬
            _friends.Sort((a, b) =>
            {
                // 1. 같은 게임 플레이 중
                if (a.IsPlayingThisGame != b.IsPlayingThisGame)
                    return a.IsPlayingThisGame ? -1 : 1;

                // 2. 온라인 상태
                if (a.IsOnline() != b.IsOnline())
                    return a.IsOnline() ? -1 : 1;

                // 3. 이름순
                return string.Compare(a.PersonaName, b.PersonaName);
            });

            SteamLogger.Log("Friends", $"친구 목록 로드 완료: {_friends.Count}명 (온라인: {OnlineFriendsCount})");
        }

        /// <summary>
        /// 특정 친구 정보 가져오기
        /// </summary>
        private FriendData GetFriendData(CSteamID friendId)
        {
            var friend = new FriendData
            {
                SteamId = friendId,
                PersonaName = SteamFriends.GetFriendPersonaName(friendId),
                PersonaState = SteamFriends.GetFriendPersonaState(friendId)
            };

            // 현재 플레이 중인 게임 확인
            if (SteamFriends.GetFriendGamePlayed(friendId, out FriendGameInfo_t gameInfo))
            {
                friend.IsPlayingThisGame = gameInfo.m_gameID.AppID() == SteamManager.Instance.AppId;

                if (!friend.IsPlayingThisGame && gameInfo.m_gameID.IsValid())
                {
                    // 다른 게임 플레이 중
                    friend.CurrentGameName = "게임 중";
                }
            }

            return friend;
        }

        /// <summary>
        /// 친구에게 게임 초대 보내기
        /// </summary>
        public bool InviteFriend(CSteamID friendId)
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return false;

            bool success = SteamFriends.InviteUserToGame(friendId, "");
            SteamLogger.LogResult("Friends", $"친구 초대: {SteamFriends.GetFriendPersonaName(friendId)}", success);

            return success;
        }

        /// <summary>
        /// Steam 오버레이로 친구 프로필 열기
        /// </summary>
        public void OpenFriendProfile(CSteamID friendId)
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return;

            SteamFriends.ActivateGameOverlayToUser("steamid", friendId);
            SteamLogger.Log("Friends", $"친구 프로필 열기: {SteamFriends.GetFriendPersonaName(friendId)}");
        }

        /// <summary>
        /// Steam 오버레이로 친구와 채팅 열기
        /// </summary>
        public void OpenFriendChat(CSteamID friendId)
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return;

            SteamFriends.ActivateGameOverlayToUser("chat", friendId);
            SteamLogger.Log("Friends", $"친구 채팅 열기: {SteamFriends.GetFriendPersonaName(friendId)}");
        }

        /// <summary>
        /// 친구 추가 오버레이 열기
        /// </summary>
        public void OpenAddFriendOverlay()
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return;

            SteamFriends.ActivateGameOverlay("friends");
            SteamLogger.Log("Friends", "친구 목록 오버레이 열기");
        }

        /// <summary>
        /// Rich Presence 설정 (친구에게 보이는 게임 상태)
        /// </summary>
        public bool SetRichPresence(string key, string value)
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return false;

            bool success = SteamFriends.SetRichPresence(key, value);
            SteamLogger.LogResult("Friends", $"Rich Presence 설정: {key}={value}", success);

            return success;
        }

        /// <summary>
        /// Rich Presence 초기화
        /// </summary>
        public void ClearRichPresence()
        {
            if (!SteamManager.CheckInitialized("FriendsService")) return;

            SteamFriends.ClearRichPresence();
            SteamLogger.Log("Friends", "Rich Presence 초기화");
        }

        /// <summary>
        /// 특정 친구 정보 새로고침
        /// </summary>
        public void RefreshFriend(CSteamID friendId)
        {
            var existingIndex = _friends.FindIndex(f => f.SteamId == friendId);
            if (existingIndex >= 0)
            {
                _friends[existingIndex] = GetFriendData(friendId);
                SteamEventBus.RaiseFriendStateChanged(friendId);
            }
        }

        #region Callbacks

        private void OnPersonaStateChange(PersonaStateChange_t result)
        {
            CSteamID changedId = new CSteamID(result.m_ulSteamID);

            // 친구 목록에 있는 경우에만 처리
            var friend = _friends.Find(f => f.SteamId == changedId);
            if (friend != null)
            {
                RefreshFriend(changedId);
                SteamLogger.Log("Friends", $"친구 상태 변경: {friend.PersonaName}");
            }
        }

        private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t result)
        {
            string friendName = SteamFriends.GetFriendPersonaName(result.m_steamIDFriend);
            SteamLogger.Log("Friends", $"게임 참가 요청: {friendName} - {result.m_rgchConnect}");

            // TODO: 게임 참가 처리
        }

        #endregion

        public void Shutdown()
        {
            ClearRichPresence();
            _friends.Clear();
            IsInitialized = false;
            SteamLogger.Log("Friends", "FriendsService 종료");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
