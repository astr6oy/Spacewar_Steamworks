using System;
using Steamworks;

namespace SteamworksDemo.Core
{
    /// <summary>
    /// Steam 이벤트 전역 버스
    /// 각 서비스에서 발생하는 이벤트를 구독자에게 전달
    /// UI와 서비스 간의 느슨한 결합 유지
    /// </summary>
    public static class SteamEventBus
    {
        // ===== Steam 초기화 관련 =====
        public static event Action OnSteamInitialized;
        public static event Action OnSteamShutdown;

        // ===== 사용자 관련 =====
        public static event Action<CSteamID> OnUserInfoChanged;

        // ===== Stats & Achievements 관련 =====
        public static event Action OnStatsReceived;
        public static event Action<bool> OnStatsStored;
        public static event Action<string> OnAchievementUnlocked;

        // ===== Friends 관련 =====
        public static event Action<CSteamID> OnFriendStateChanged;
        public static event Action OnFriendsListChanged;

        // ===== Lobby 관련 =====
        public static event Action<CSteamID> OnLobbyCreated;
        public static event Action<CSteamID> OnLobbyEntered;
        public static event Action OnLobbyLeft;
        public static event Action<CSteamID> OnLobbyMemberJoined;
        public static event Action<CSteamID> OnLobbyMemberLeft;
        public static event Action OnLobbyDataUpdated;

        // ===== P2P 관련 =====
        public static event Action<CSteamID> OnP2PSessionRequested;
        public static event Action<CSteamID, byte[]> OnP2PPacketReceived;
        public static event Action<CSteamID> OnP2PSessionClosed;

        // ===== Cloud 관련 =====
        public static event Action<string> OnCloudFileWritten;
        public static event Action<string> OnCloudFileDeleted;

        // ===== Inventory 관련 =====
        public static event Action OnInventoryUpdated;

        // ===== 이벤트 발행 메서드 =====

        public static void RaiseSteamInitialized()
        {
            SteamLogger.Log("EventBus", "Steam 초기화 이벤트 발생");
            OnSteamInitialized?.Invoke();
        }

        public static void RaiseSteamShutdown()
        {
            SteamLogger.Log("EventBus", "Steam 종료 이벤트 발생");
            OnSteamShutdown?.Invoke();
        }

        public static void RaiseUserInfoChanged(CSteamID userId)
        {
            OnUserInfoChanged?.Invoke(userId);
        }

        public static void RaiseStatsReceived()
        {
            SteamLogger.Log("EventBus", "Stats 수신 이벤트 발생");
            OnStatsReceived?.Invoke();
        }

        public static void RaiseStatsStored(bool success)
        {
            SteamLogger.Log("EventBus", $"Stats 저장 이벤트 발생 (성공: {success})");
            OnStatsStored?.Invoke(success);
        }

        public static void RaiseAchievementUnlocked(string achievementId)
        {
            SteamLogger.Log("EventBus", $"Achievement 해제 이벤트: {achievementId}");
            OnAchievementUnlocked?.Invoke(achievementId);
        }

        public static void RaiseFriendStateChanged(CSteamID friendId)
        {
            OnFriendStateChanged?.Invoke(friendId);
        }

        public static void RaiseFriendsListChanged()
        {
            OnFriendsListChanged?.Invoke();
        }

        public static void RaiseLobbyCreated(CSteamID lobbyId)
        {
            SteamLogger.Log("EventBus", $"Lobby 생성 이벤트: {lobbyId}");
            OnLobbyCreated?.Invoke(lobbyId);
        }

        public static void RaiseLobbyEntered(CSteamID lobbyId)
        {
            SteamLogger.Log("EventBus", $"Lobby 입장 이벤트: {lobbyId}");
            OnLobbyEntered?.Invoke(lobbyId);
        }

        public static void RaiseLobbyLeft()
        {
            SteamLogger.Log("EventBus", "Lobby 퇴장 이벤트");
            OnLobbyLeft?.Invoke();
        }

        public static void RaiseLobbyMemberJoined(CSteamID memberId)
        {
            SteamLogger.Log("EventBus", $"Lobby 멤버 입장: {memberId}");
            OnLobbyMemberJoined?.Invoke(memberId);
        }

        public static void RaiseLobbyMemberLeft(CSteamID memberId)
        {
            SteamLogger.Log("EventBus", $"Lobby 멤버 퇴장: {memberId}");
            OnLobbyMemberLeft?.Invoke(memberId);
        }

        public static void RaiseLobbyDataUpdated()
        {
            OnLobbyDataUpdated?.Invoke();
        }

        public static void RaiseP2PSessionRequested(CSteamID remoteId)
        {
            SteamLogger.Log("EventBus", $"P2P 세션 요청: {remoteId}");
            OnP2PSessionRequested?.Invoke(remoteId);
        }

        public static void RaiseP2PPacketReceived(CSteamID senderId, byte[] data)
        {
            OnP2PPacketReceived?.Invoke(senderId, data);
        }

        public static void RaiseP2PSessionClosed(CSteamID remoteId)
        {
            SteamLogger.Log("EventBus", $"P2P 세션 종료: {remoteId}");
            OnP2PSessionClosed?.Invoke(remoteId);
        }

        public static void RaiseCloudFileWritten(string fileName)
        {
            SteamLogger.Log("EventBus", $"Cloud 파일 저장: {fileName}");
            OnCloudFileWritten?.Invoke(fileName);
        }

        public static void RaiseCloudFileDeleted(string fileName)
        {
            SteamLogger.Log("EventBus", $"Cloud 파일 삭제: {fileName}");
            OnCloudFileDeleted?.Invoke(fileName);
        }

        public static void RaiseInventoryUpdated()
        {
            SteamLogger.Log("EventBus", "Inventory 업데이트 이벤트");
            OnInventoryUpdated?.Invoke();
        }

        /// <summary>
        /// 모든 이벤트 구독 해제
        /// 테스트나 씬 전환 시 사용
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnSteamInitialized = null;
            OnSteamShutdown = null;
            OnUserInfoChanged = null;
            OnStatsReceived = null;
            OnStatsStored = null;
            OnAchievementUnlocked = null;
            OnFriendStateChanged = null;
            OnFriendsListChanged = null;
            OnLobbyCreated = null;
            OnLobbyEntered = null;
            OnLobbyLeft = null;
            OnLobbyMemberJoined = null;
            OnLobbyMemberLeft = null;
            OnLobbyDataUpdated = null;
            OnP2PSessionRequested = null;
            OnP2PPacketReceived = null;
            OnP2PSessionClosed = null;
            OnCloudFileWritten = null;
            OnCloudFileDeleted = null;
            OnInventoryUpdated = null;

            SteamLogger.Log("EventBus", "모든 이벤트 구독 해제");
        }
    }
}
