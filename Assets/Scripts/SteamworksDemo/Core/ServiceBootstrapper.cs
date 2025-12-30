using UnityEngine;
using SteamworksDemo.Services;

namespace SteamworksDemo.Core
{
    /// <summary>
    /// 모든 Steam 서비스 초기화 관리
    /// SteamManager 초기화 후 순차적으로 서비스 시작
    /// </summary>
    public class ServiceBootstrapper : MonoBehaviour
    {
        [Header("서비스 컴포넌트 (자동 할당됨)")]
        [SerializeField] private SteamUserService _userService;
        [SerializeField] private SteamStatsService _statsService;
        [SerializeField] private SteamAchievementService _achievementService;
        [SerializeField] private SteamLeaderboardService _leaderboardService;
        [SerializeField] private SteamFriendsService _friendsService;
        [SerializeField] private SteamLobbyService _lobbyService;
        [SerializeField] private SteamP2PService _p2pService;
        [SerializeField] private SteamInventoryService _inventoryService;

        private void Start()
        {
            // Steam 초기화 이벤트 구독
            SteamEventBus.OnSteamInitialized += InitializeAllServices;
            SteamEventBus.OnSteamShutdown += ShutdownAllServices;

            // 이미 초기화된 경우 바로 서비스 시작
            if (SteamManager.Instance?.IsInitialized == true)
            {
                InitializeAllServices();
            }
        }

        private void OnDestroy()
        {
            SteamEventBus.OnSteamInitialized -= InitializeAllServices;
            SteamEventBus.OnSteamShutdown -= ShutdownAllServices;
        }

        /// <summary>
        /// 모든 서비스 초기화
        /// </summary>
        private void InitializeAllServices()
        {
            SteamLogger.Log("Bootstrap", "=== 서비스 초기화 시작 ===");

            // 컴포넌트 자동 할당 (없는 경우)
            EnsureServiceComponents();

            // 순차적 초기화 (의존성 순서)
            _userService?.Initialize();
            _statsService?.Initialize();
            _achievementService?.Initialize();
            _leaderboardService?.Initialize();
            _friendsService?.Initialize();
            _lobbyService?.Initialize();
            _p2pService?.Initialize();
            _inventoryService?.Initialize();

            SteamLogger.Log("Bootstrap", "=== 서비스 초기화 완료 ===");
        }

        /// <summary>
        /// 모든 서비스 종료
        /// </summary>
        private void ShutdownAllServices()
        {
            SteamLogger.Log("Bootstrap", "=== 서비스 종료 시작 ===");

            // 역순으로 종료
            _inventoryService?.Shutdown();
            _p2pService?.Shutdown();
            _lobbyService?.Shutdown();
            _friendsService?.Shutdown();
            _leaderboardService?.Shutdown();
            _achievementService?.Shutdown();
            _statsService?.Shutdown();
            _userService?.Shutdown();

            SteamLogger.Log("Bootstrap", "=== 서비스 종료 완료 ===");
        }

        /// <summary>
        /// 서비스 컴포넌트 자동 할당
        /// </summary>
        private void EnsureServiceComponents()
        {
            if (_userService == null)
                _userService = GetOrAddComponent<SteamUserService>();

            if (_statsService == null)
                _statsService = GetOrAddComponent<SteamStatsService>();

            if (_achievementService == null)
                _achievementService = GetOrAddComponent<SteamAchievementService>();

            if (_leaderboardService == null)
                _leaderboardService = GetOrAddComponent<SteamLeaderboardService>();

            if (_friendsService == null)
                _friendsService = GetOrAddComponent<SteamFriendsService>();

            if (_lobbyService == null)
                _lobbyService = GetOrAddComponent<SteamLobbyService>();

            if (_p2pService == null)
                _p2pService = GetOrAddComponent<SteamP2PService>();

            if (_inventoryService == null)
                _inventoryService = GetOrAddComponent<SteamInventoryService>();
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}
