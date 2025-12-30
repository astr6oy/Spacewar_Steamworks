using System;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam P2P 네트워킹 서비스
    /// 피어 간 직접 통신 (ISteamNetworking)
    /// 로컬 테스트 위주로 구현
    /// </summary>
    public class SteamP2PService : MonoBehaviour, ISteamService
    {
        public static SteamP2PService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 연결된 피어 목록
        private readonly System.Collections.Generic.List<CSteamID> _connectedPeers = new System.Collections.Generic.List<CSteamID>();
        public System.Collections.Generic.IReadOnlyList<CSteamID> ConnectedPeers => _connectedPeers;

        // 패킷 통계
        public int PacketsSent { get; private set; }
        public int PacketsReceived { get; private set; }

        // Callbacks
        private Callback<P2PSessionRequest_t> _p2pSessionRequestCallback;
        private Callback<P2PSessionConnectFail_t> _p2pSessionConnectFailCallback;

        // 패킷 수신 이벤트
        public event Action<CSteamID, byte[]> OnPacketReceived;

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
            if (!SteamManager.CheckInitialized("P2PService")) return;

            // Callbacks 등록
            _p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            _p2pSessionConnectFailCallback = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);

            IsInitialized = true;
            SteamLogger.Log("P2P", "P2PService 초기화 완료");
        }

        private void Update()
        {
            if (!IsInitialized) return;

            // 패킷 수신 처리
            ProcessIncomingPackets();
        }

        /// <summary>
        /// 패킷 전송
        /// </summary>
        public bool SendPacket(CSteamID target, byte[] data, EP2PSend sendType = EP2PSend.k_EP2PSendReliable)
        {
            if (!SteamManager.CheckInitialized("P2PService")) return false;

            bool success = SteamNetworking.SendP2PPacket(target, data, (uint)data.Length, sendType);

            if (success)
            {
                PacketsSent++;
                SteamLogger.Log("P2P", $"패킷 전송 성공 -> {SteamFriends.GetFriendPersonaName(target)} ({data.Length} bytes)");
            }
            else
            {
                SteamLogger.LogError("P2P", $"패킷 전송 실패 -> {target}");
            }

            return success;
        }

        /// <summary>
        /// 문자열 메시지 전송 (편의 메서드)
        /// </summary>
        public bool SendMessage(CSteamID target, string message)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
            return SendPacket(target, data);
        }

        /// <summary>
        /// 로비의 모든 멤버에게 패킷 전송
        /// </summary>
        public void BroadcastToLobby(byte[] data, EP2PSend sendType = EP2PSend.k_EP2PSendReliable)
        {
            var lobbyService = SteamLobbyService.Instance;
            if (lobbyService == null || !lobbyService.IsInLobby) return;

            CSteamID myId = SteamUser.GetSteamID();

            foreach (var member in lobbyService.GetLobbyMembers())
            {
                // 자신에게는 보내지 않음
                if (member.SteamId != myId)
                {
                    SendPacket(member.SteamId, data, sendType);
                }
            }
        }

        /// <summary>
        /// 문자열 메시지를 로비에 브로드캐스트
        /// </summary>
        public void BroadcastMessage(string message)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
            BroadcastToLobby(data);
        }

        /// <summary>
        /// P2P 세션 요청 수락
        /// </summary>
        public bool AcceptSession(CSteamID remoteId)
        {
            bool success = SteamNetworking.AcceptP2PSessionWithUser(remoteId);

            if (success)
            {
                if (!_connectedPeers.Contains(remoteId))
                {
                    _connectedPeers.Add(remoteId);
                }
                SteamLogger.Log("P2P", $"세션 수락: {SteamFriends.GetFriendPersonaName(remoteId)}");
            }

            return success;
        }

        /// <summary>
        /// P2P 세션 종료
        /// </summary>
        public bool CloseSession(CSteamID remoteId)
        {
            bool success = SteamNetworking.CloseP2PSessionWithUser(remoteId);

            if (success)
            {
                _connectedPeers.Remove(remoteId);
                SteamLogger.Log("P2P", $"세션 종료: {SteamFriends.GetFriendPersonaName(remoteId)}");
                SteamEventBus.RaiseP2PSessionClosed(remoteId);
            }

            return success;
        }

        /// <summary>
        /// 모든 P2P 세션 종료
        /// </summary>
        public void CloseAllSessions()
        {
            foreach (var peer in _connectedPeers.ToArray())
            {
                CloseSession(peer);
            }
            _connectedPeers.Clear();
        }

        /// <summary>
        /// P2P 세션 상태 확인
        /// </summary>
        public P2PSessionState_t? GetSessionState(CSteamID remoteId)
        {
            P2PSessionState_t state;
            if (SteamNetworking.GetP2PSessionState(remoteId, out state))
            {
                return state;
            }
            return null;
        }

        /// <summary>
        /// 연결 상태 확인
        /// </summary>
        public bool IsConnectedTo(CSteamID remoteId)
        {
            var state = GetSessionState(remoteId);
            return state?.m_bConnectionActive == 1;
        }

        /// <summary>
        /// 수신 패킷 처리
        /// </summary>
        private void ProcessIncomingPackets()
        {
            uint packetSize;

            // 대기 중인 모든 패킷 처리
            while (SteamNetworking.IsP2PPacketAvailable(out packetSize))
            {
                byte[] buffer = new byte[packetSize];
                CSteamID senderId;
                uint bytesRead;

                if (SteamNetworking.ReadP2PPacket(buffer, packetSize, out bytesRead, out senderId))
                {
                    PacketsReceived++;

                    // 새 피어면 목록에 추가
                    if (!_connectedPeers.Contains(senderId))
                    {
                        _connectedPeers.Add(senderId);
                    }

                    SteamLogger.Log("P2P", $"패킷 수신 <- {SteamFriends.GetFriendPersonaName(senderId)} ({bytesRead} bytes)");

                    // 이벤트 발생
                    OnPacketReceived?.Invoke(senderId, buffer);
                    SteamEventBus.RaiseP2PPacketReceived(senderId, buffer);
                }
            }
        }

        #region Callbacks

        private void OnP2PSessionRequest(P2PSessionRequest_t result)
        {
            CSteamID remoteId = result.m_steamIDRemote;
            string remoteName = SteamFriends.GetFriendPersonaName(remoteId);

            SteamLogger.Log("P2P", $"세션 요청 수신: {remoteName}");
            SteamEventBus.RaiseP2PSessionRequested(remoteId);

            // 자동 수락 (실제 게임에서는 확인 절차 필요)
            AcceptSession(remoteId);
        }

        private void OnP2PSessionConnectFail(P2PSessionConnectFail_t result)
        {
            CSteamID remoteId = result.m_steamIDRemote;
            EP2PSessionError error = (EP2PSessionError)result.m_eP2PSessionError;

            string remoteName = SteamFriends.GetFriendPersonaName(remoteId);
            SteamLogger.LogError("P2P", $"세션 연결 실패: {remoteName} - {error}");

            _connectedPeers.Remove(remoteId);
            SteamEventBus.RaiseP2PSessionClosed(remoteId);
        }

        #endregion

        /// <summary>
        /// 통계 초기화
        /// </summary>
        public void ResetStats()
        {
            PacketsSent = 0;
            PacketsReceived = 0;
        }

        public void Shutdown()
        {
            CloseAllSessions();
            ResetStats();
            IsInitialized = false;
            SteamLogger.Log("P2P", "P2PService 종료");
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
