using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;
using SteamworksDemo.Data;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam Stats 서비스
    /// 통계 읽기/쓰기 및 저장 관리
    /// SpaceWar에서 사용하는 Stats: NumGames, NumWins, NumLosses, FeetTraveled, MaxFeetTraveled
    /// </summary>
    public class SteamStatsService : MonoBehaviour, ISteamService
    {
        public static SteamStatsService Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        public bool StatsReceived { get; private set; }

        // SpaceWar에서 사용하는 Stats 목록
        private readonly Dictionary<string, StatData> _stats = new Dictionary<string, StatData>();

        // Stats 정의 (SpaceWar 기준)
        private static readonly (string id, string displayName, StatType type, float maxValue)[] StatDefinitions =
        {
            ("NumGames", "총 게임 수", StatType.Int, 100f),
            ("NumWins", "승리 횟수", StatType.Int, 50f),
            ("NumLosses", "패배 횟수", StatType.Int, 50f),
            ("FeetTraveled", "이동 거리", StatType.Float, 10000f),
            ("MaxFeetTraveled", "최대 이동 거리", StatType.Float, 1000f)
        };

        // Callbacks
        private Callback<UserStatsReceived_t> _userStatsReceivedCallback;
        private Callback<UserStatsStored_t> _userStatsStoredCallback;

        public IReadOnlyDictionary<string, StatData> Stats => _stats;

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
            if (!SteamManager.CheckInitialized("StatsService")) return;

            // Callback 등록
            _userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            _userStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnUserStatsStored);

            // Stats 초기화
            InitializeStatDefinitions();

            // Stats 요청
            RequestStats();

            IsInitialized = true;
            SteamLogger.Log("Stats", "StatsService 초기화 완료");
        }

        /// <summary>
        /// Stat 정의 초기화
        /// </summary>
        private void InitializeStatDefinitions()
        {
            _stats.Clear();

            foreach (var def in StatDefinitions)
            {
                _stats[def.id] = new StatData
                {
                    Id = def.id,
                    DisplayName = def.displayName,
                    Type = def.type,
                    MaxValue = def.maxValue
                };
            }
        }

        /// <summary>
        /// Steam에서 Stats 요청
        /// </summary>
        public void RequestStats()
        {
            if (!SteamManager.CheckInitialized("StatsService")) return;

            // 현재 사용자의 Stats 요청 (Callback으로 결과 수신)
            SteamAPICall_t apiCall = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            bool success = apiCall != SteamAPICall_t.Invalid;
            SteamLogger.LogResult("Stats", "Stats 요청", success);
        }

        /// <summary>
        /// 특정 Int Stat 값 가져오기
        /// </summary>
        public int GetStatInt(string statId)
        {
            if (!StatsReceived)
            {
                SteamLogger.LogWarning("Stats", "Stats가 아직 수신되지 않음");
                return 0;
            }

            if (SteamUserStats.GetStat(statId, out int value))
            {
                return value;
            }

            SteamLogger.LogWarning("Stats", $"Stat '{statId}' 읽기 실패");
            return 0;
        }

        /// <summary>
        /// 특정 Float Stat 값 가져오기
        /// </summary>
        public float GetStatFloat(string statId)
        {
            if (!StatsReceived)
            {
                SteamLogger.LogWarning("Stats", "Stats가 아직 수신되지 않음");
                return 0f;
            }

            if (SteamUserStats.GetStat(statId, out float value))
            {
                return value;
            }

            SteamLogger.LogWarning("Stats", $"Stat '{statId}' 읽기 실패");
            return 0f;
        }

        /// <summary>
        /// Int Stat 값 설정
        /// </summary>
        public bool SetStatInt(string statId, int value)
        {
            if (!SteamManager.CheckInitialized("StatsService")) return false;

            bool success = SteamUserStats.SetStat(statId, value);
            SteamLogger.LogResult("Stats", $"SetStat({statId}, {value})", success);

            if (success && _stats.ContainsKey(statId))
            {
                _stats[statId].IntValue = value;
            }

            return success;
        }

        /// <summary>
        /// Float Stat 값 설정
        /// </summary>
        public bool SetStatFloat(string statId, float value)
        {
            if (!SteamManager.CheckInitialized("StatsService")) return false;

            bool success = SteamUserStats.SetStat(statId, value);
            SteamLogger.LogResult("Stats", $"SetStat({statId}, {value:F2})", success);

            if (success && _stats.ContainsKey(statId))
            {
                _stats[statId].FloatValue = value;
            }

            return success;
        }

        /// <summary>
        /// Stat 값 증가 (Int)
        /// </summary>
        public bool IncrementStat(string statId, int amount = 1)
        {
            int current = GetStatInt(statId);
            return SetStatInt(statId, current + amount);
        }

        /// <summary>
        /// 변경된 Stats를 Steam에 저장
        /// </summary>
        public bool StoreStats()
        {
            if (!SteamManager.CheckInitialized("StatsService")) return false;

            bool success = SteamUserStats.StoreStats();
            SteamLogger.LogResult("Stats", "Stats 저장 요청", success);

            return success;
        }

        /// <summary>
        /// 모든 Stats 초기화 (개발/테스트용)
        /// </summary>
        public bool ResetAllStats(bool includeAchievements = false)
        {
            if (!SteamManager.CheckInitialized("StatsService")) return false;

            bool success = SteamUserStats.ResetAllStats(includeAchievements);
            SteamLogger.LogResult("Stats", $"Stats 초기화 (업적 포함: {includeAchievements})", success);

            if (success)
            {
                RequestStats(); // 초기화 후 다시 로드
            }

            return success;
        }

        #region Callbacks

        private void OnUserStatsReceived(UserStatsReceived_t result)
        {
            if (result.m_nGameID != SteamManager.Instance.AppId.m_AppId)
                return;

            if (result.m_eResult != EResult.k_EResultOK)
            {
                SteamLogger.LogError("Stats", $"Stats 수신 실패: {result.m_eResult}");
                return;
            }

            StatsReceived = true;

            // 모든 Stats 값 로드
            foreach (var kvp in _stats)
            {
                var stat = kvp.Value;
                if (stat.Type == StatType.Int)
                {
                    SteamUserStats.GetStat(stat.Id, out int intValue);
                    stat.IntValue = intValue;
                }
                else
                {
                    SteamUserStats.GetStat(stat.Id, out float floatValue);
                    stat.FloatValue = floatValue;
                }
            }

            SteamLogger.Log("Stats", "Stats 수신 완료");
            LogAllStats();

            SteamEventBus.RaiseStatsReceived();
        }

        private void OnUserStatsStored(UserStatsStored_t result)
        {
            if (result.m_nGameID != SteamManager.Instance.AppId.m_AppId)
                return;

            bool success = result.m_eResult == EResult.k_EResultOK;
            SteamLogger.LogResult("Stats", "Stats 저장", success, result.m_eResult.ToString());

            SteamEventBus.RaiseStatsStored(success);
        }

        #endregion

        /// <summary>
        /// 모든 Stats 로그 출력 (디버그용)
        /// </summary>
        private void LogAllStats()
        {
            SteamLogger.Log("Stats", "=== 현재 Stats ===");
            foreach (var kvp in _stats)
            {
                var stat = kvp.Value;
                SteamLogger.Log("Stats", $"  {stat.DisplayName}: {stat.GetValueText()}");
            }
        }

        public void Shutdown()
        {
            StatsReceived = false;
            IsInitialized = false;
            _stats.Clear();
            SteamLogger.Log("Stats", "StatsService 종료");
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
