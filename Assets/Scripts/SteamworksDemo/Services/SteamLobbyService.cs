using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;
using SteamworksDemo.Data;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam 로비 서비스
    /// 로비 생성, 검색, 참가, 데이터 관리
    /// Callback + CallResult 패턴 사용
    /// </summary>
    public class SteamLobbyService : MonoBehaviour, ISteamService
    {
        public static SteamLobbyService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 현재 로비 정보
        public CSteamID CurrentLobbyId { get; private set; }
        public LobbyData CurrentLobby { get; private set; }
        public bool IsInLobby => CurrentLobbyId.IsValid();

        // 검색된 로비 목록
        private readonly List<LobbyData> _availableLobbies = new List<LobbyData>();
        public IReadOnlyList<LobbyData> AvailableLobbies => _availableLobbies;

        // Callbacks (Steam에서 자동 호출)
        private Callback<LobbyEnter_t> _lobbyEnterCallback;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdateCallback;
        private Callback<LobbyDataUpdate_t> _lobbyDataUpdateCallback;
        private Callback<LobbyChatMsg_t> _lobbyChatMsgCallback;

        // CallResults (비동기 요청)
        private CallResult<LobbyCreated_t> _lobbyCreatedCallResult;
        private CallResult<LobbyMatchList_t> _lobbyMatchListCallResult;

        // 외부 콜백
        private Action<bool, CSteamID> _onLobbyCreated;
        private Action<List<LobbyData>> _onLobbiesFound;

        // 로비 설정
        public const int DefaultMaxMembers = 4;

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
            if (!SteamManager.CheckInitialized("LobbyService")) return;

            // Callbacks 등록
            _lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            _lobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
            _lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);

            // CallResults 초기화
            _lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);

            IsInitialized = true;
            SteamLogger.Log("Lobby", "LobbyService 초기화 완료");
        }

        /// <summary>
        /// 새 로비 생성
        /// </summary>
        public void CreateLobby(ELobbyType lobbyType, int maxMembers, Action<bool, CSteamID> callback = null)
        {
            if (!SteamManager.CheckInitialized("LobbyService"))
            {
                callback?.Invoke(false, CSteamID.Nil);
                return;
            }

            // 이미 로비에 있으면 나가기
            if (IsInLobby)
            {
                LeaveLobby();
            }

            _onLobbyCreated = callback;

            SteamAPICall_t apiCall = SteamMatchmaking.CreateLobby(lobbyType, maxMembers);
            _lobbyCreatedCallResult.Set(apiCall);

            SteamLogger.Log("Lobby", $"로비 생성 요청 (타입: {lobbyType}, 최대: {maxMembers}명)");
        }

        /// <summary>
        /// 공개 로비로 생성 (편의 메서드)
        /// </summary>
        public void CreatePublicLobby(int maxMembers = DefaultMaxMembers, Action<bool, CSteamID> callback = null)
        {
            CreateLobby(ELobbyType.k_ELobbyTypePublic, maxMembers, callback);
        }

        /// <summary>
        /// 친구 전용 로비로 생성 (편의 메서드)
        /// </summary>
        public void CreateFriendsOnlyLobby(int maxMembers = DefaultMaxMembers, Action<bool, CSteamID> callback = null)
        {
            CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers, callback);
        }

        /// <summary>
        /// 로비 검색
        /// </summary>
        public void SearchLobbies(Action<List<LobbyData>> callback = null)
        {
            if (!SteamManager.CheckInitialized("LobbyService"))
            {
                callback?.Invoke(new List<LobbyData>());
                return;
            }

            _onLobbiesFound = callback;

            // 필터 설정 (전세계 검색)
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

            SteamAPICall_t apiCall = SteamMatchmaking.RequestLobbyList();
            _lobbyMatchListCallResult.Set(apiCall);

            SteamLogger.Log("Lobby", "로비 검색 시작");
        }

        /// <summary>
        /// 특정 로비에 참가
        /// </summary>
        public void JoinLobby(CSteamID lobbyId)
        {
            if (!SteamManager.CheckInitialized("LobbyService")) return;

            // 이미 로비에 있으면 나가기
            if (IsInLobby)
            {
                LeaveLobby();
            }

            SteamMatchmaking.JoinLobby(lobbyId);
            SteamLogger.Log("Lobby", $"로비 참가 요청: {lobbyId}");
        }

        /// <summary>
        /// 현재 로비 나가기
        /// </summary>
        public void LeaveLobby()
        {
            if (!IsInLobby) return;

            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
            SteamLogger.Log("Lobby", $"로비 퇴장: {CurrentLobbyId}");

            CurrentLobbyId = CSteamID.Nil;
            CurrentLobby = null;

            SteamEventBus.RaiseLobbyLeft();
        }

        /// <summary>
        /// 로비 데이터 설정 (소유자만)
        /// </summary>
        public bool SetLobbyData(string key, string value)
        {
            if (!IsInLobby) return false;

            bool success = SteamMatchmaking.SetLobbyData(CurrentLobbyId, key, value);
            SteamLogger.LogResult("Lobby", $"로비 데이터 설정: {key}={value}", success);

            return success;
        }

        /// <summary>
        /// 로비 데이터 가져오기
        /// </summary>
        public string GetLobbyData(string key)
        {
            if (!IsInLobby) return "";
            return SteamMatchmaking.GetLobbyData(CurrentLobbyId, key);
        }

        /// <summary>
        /// 로비 이름 설정
        /// </summary>
        public bool SetLobbyName(string name)
        {
            return SetLobbyData("name", name);
        }

        /// <summary>
        /// 로비 채팅 메시지 전송
        /// </summary>
        public bool SendChatMessage(string message)
        {
            if (!IsInLobby) return false;

            byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(message);
            bool success = SteamMatchmaking.SendLobbyChatMsg(CurrentLobbyId, msgBytes, msgBytes.Length);

            SteamLogger.LogResult("Lobby", "채팅 메시지 전송", success);
            return success;
        }

        /// <summary>
        /// 로비 멤버 목록 가져오기
        /// </summary>
        public List<LobbyMemberData> GetLobbyMembers()
        {
            var members = new List<LobbyMemberData>();
            if (!IsInLobby) return members;

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyId);
            CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(CurrentLobbyId);

            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobbyId, i);
                members.Add(new LobbyMemberData
                {
                    SteamId = memberId,
                    PersonaName = SteamFriends.GetFriendPersonaName(memberId),
                    IsOwner = memberId == ownerId
                });
            }

            return members;
        }

        #region Callback Handlers

        private void OnLobbyCreated(LobbyCreated_t result, bool bIOFailure)
        {
            if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
            {
                SteamLogger.LogError("Lobby", $"로비 생성 실패: {result.m_eResult}");
                _onLobbyCreated?.Invoke(false, CSteamID.Nil);
                return;
            }

            CurrentLobbyId = new CSteamID(result.m_ulSteamIDLobby);
            UpdateCurrentLobbyData();

            SteamLogger.Log("Lobby", $"로비 생성 성공: {CurrentLobbyId}");
            SteamEventBus.RaiseLobbyCreated(CurrentLobbyId);

            _onLobbyCreated?.Invoke(true, CurrentLobbyId);
        }

        private void OnLobbyMatchList(LobbyMatchList_t result, bool bIOFailure)
        {
            _availableLobbies.Clear();

            if (bIOFailure)
            {
                SteamLogger.LogError("Lobby", "로비 검색 실패");
                _onLobbiesFound?.Invoke(_availableLobbies);
                return;
            }

            uint count = result.m_nLobbiesMatching;
            SteamLogger.Log("Lobby", $"{count}개 로비 발견");

            for (int i = 0; i < count; i++)
            {
                CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                LobbyData lobby = GetLobbyInfo(lobbyId);
                _availableLobbies.Add(lobby);
            }

            _onLobbiesFound?.Invoke(_availableLobbies);
        }

        private void OnLobbyEnter(LobbyEnter_t result)
        {
            EChatRoomEnterResponse response = (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;

            if (response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                SteamLogger.LogError("Lobby", $"로비 입장 실패: {response}");
                return;
            }

            CurrentLobbyId = new CSteamID(result.m_ulSteamIDLobby);
            UpdateCurrentLobbyData();

            SteamLogger.Log("Lobby", $"로비 입장 완료: {CurrentLobbyId}");
            SteamEventBus.RaiseLobbyEntered(CurrentLobbyId);
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t result)
        {
            CSteamID changedUser = new CSteamID(result.m_ulSteamIDUserChanged);
            EChatMemberStateChange stateChange = (EChatMemberStateChange)result.m_rgfChatMemberStateChange;

            string userName = SteamFriends.GetFriendPersonaName(changedUser);

            if ((stateChange & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                SteamLogger.Log("Lobby", $"멤버 입장: {userName}");
                SteamEventBus.RaiseLobbyMemberJoined(changedUser);
            }

            if ((stateChange & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0 ||
                (stateChange & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0)
            {
                SteamLogger.Log("Lobby", $"멤버 퇴장: {userName}");
                SteamEventBus.RaiseLobbyMemberLeft(changedUser);
            }

            UpdateCurrentLobbyData();
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t result)
        {
            if (new CSteamID(result.m_ulSteamIDLobby) == CurrentLobbyId)
            {
                UpdateCurrentLobbyData();
                SteamEventBus.RaiseLobbyDataUpdated();
            }
        }

        private void OnLobbyChatMsg(LobbyChatMsg_t result)
        {
            // 채팅 메시지 수신
            byte[] msgBuffer = new byte[4096];
            CSteamID sender;
            EChatEntryType chatType;

            int msgLength = SteamMatchmaking.GetLobbyChatEntry(
                new CSteamID(result.m_ulSteamIDLobby),
                (int)result.m_iChatID,
                out sender,
                msgBuffer,
                msgBuffer.Length,
                out chatType
            );

            if (msgLength > 0)
            {
                string message = System.Text.Encoding.UTF8.GetString(msgBuffer, 0, msgLength);
                string senderName = SteamFriends.GetFriendPersonaName(sender);
                SteamLogger.Log("Lobby", $"[채팅] {senderName}: {message}");
            }
        }

        #endregion

        /// <summary>
        /// 로비 정보 가져오기
        /// </summary>
        private LobbyData GetLobbyInfo(CSteamID lobbyId)
        {
            return new LobbyData
            {
                LobbyId = lobbyId,
                OwnerId = SteamMatchmaking.GetLobbyOwner(lobbyId),
                MaxMembers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId),
                CurrentMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyId),
                Name = SteamMatchmaking.GetLobbyData(lobbyId, "name")
            };
        }

        /// <summary>
        /// 현재 로비 데이터 갱신
        /// </summary>
        private void UpdateCurrentLobbyData()
        {
            if (!CurrentLobbyId.IsValid()) return;

            CurrentLobby = GetLobbyInfo(CurrentLobbyId);

            // 멤버 목록 갱신
            CurrentLobby.Members.Clear();
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyId);
            for (int i = 0; i < memberCount; i++)
            {
                CurrentLobby.Members.Add(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobbyId, i));
            }
        }

        public void Shutdown()
        {
            if (IsInLobby)
            {
                LeaveLobby();
            }

            _availableLobbies.Clear();
            IsInitialized = false;
            SteamLogger.Log("Lobby", "LobbyService 종료");
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
