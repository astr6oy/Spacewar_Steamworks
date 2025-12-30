using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;
using SteamworksDemo.Data;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam Achievement 서비스
    /// 도전과제 조회, 해제, 초기화 관리
    /// SpaceWar Achievement: ACH_WIN_ONE_GAME, ACH_WIN_100_GAMES, ACH_TRAVEL_FAR_ACCUM, ACH_TRAVEL_FAR_SINGLE
    /// </summary>
    public class SteamAchievementService : MonoBehaviour, ISteamService
    {
        public static SteamAchievementService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 도전과제 목록
        private readonly List<AchievementData> _achievements = new List<AchievementData>();
        public IReadOnlyList<AchievementData> Achievements => _achievements;

        // SpaceWar Achievement 정의
        private static readonly (string id, string displayName, string description)[] AchievementDefinitions =
        {
            ("ACH_WIN_ONE_GAME", "첫 승리", "첫 번째 게임에서 승리하세요"),
            ("ACH_WIN_100_GAMES", "백전백승", "100번의 게임에서 승리하세요"),
            ("ACH_TRAVEL_FAR_ACCUM", "마라토너", "누적 10,000 feet 이동하세요"),
            ("ACH_TRAVEL_FAR_SINGLE", "단거리 선수", "한 게임에서 500 feet 이동하세요")
        };

        // Callbacks
        private Callback<UserAchievementStored_t> _achievementStoredCallback;

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
            if (!SteamManager.CheckInitialized("AchievementService")) return;

            // Callback 등록
            _achievementStoredCallback = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

            // Stats 수신 이벤트 구독 (Stats 수신 후 Achievement 로드)
            SteamEventBus.OnStatsReceived += LoadAchievements;

            // 이미 Stats가 수신된 경우 바로 로드
            if (SteamStatsService.Instance?.StatsReceived == true)
            {
                LoadAchievements();
            }

            IsInitialized = true;
            SteamLogger.Log("Achievement", "AchievementService 초기화 완료");
        }

        /// <summary>
        /// Steam에서 도전과제 정보 로드
        /// </summary>
        private void LoadAchievements()
        {
            _achievements.Clear();

            int iconIndex = 0;
            foreach (var def in AchievementDefinitions)
            {
                var achievement = new AchievementData
                {
                    Id = def.id,
                    DisplayName = def.displayName,
                    Description = def.description,
                    IconIndex = iconIndex++
                };

                // 해제 상태 확인
                if (SteamUserStats.GetAchievement(def.id, out bool unlocked))
                {
                    achievement.IsUnlocked = unlocked;

                    // 해제 시간 가져오기
                    if (unlocked && SteamUserStats.GetAchievementAchievedPercent(def.id, out float percent))
                    {
                        // Note: 실제 해제 시간은 GetAchievementAndUnlockTime으로 가져와야 함
                        SteamUserStats.GetAchievementAndUnlockTime(def.id, out _, out uint unlockTime);
                        achievement.UnlockTime = unlockTime;
                    }
                }

                _achievements.Add(achievement);
            }

            SteamLogger.Log("Achievement", $"{_achievements.Count}개 도전과제 로드 완료");
            LogAllAchievements();
        }

        /// <summary>
        /// 도전과제 해제
        /// </summary>
        public bool UnlockAchievement(string achievementId)
        {
            if (!SteamManager.CheckInitialized("AchievementService")) return false;

            // 이미 해제된 경우 스킵
            var achievement = _achievements.Find(a => a.Id == achievementId);
            if (achievement != null && achievement.IsUnlocked)
            {
                SteamLogger.Log("Achievement", $"'{achievementId}'은(는) 이미 해제됨");
                return true;
            }

            bool success = SteamUserStats.SetAchievement(achievementId);
            SteamLogger.LogResult("Achievement", $"해제 시도: {achievementId}", success);

            if (success)
            {
                // 로컬 데이터 업데이트
                if (achievement != null)
                {
                    achievement.IsUnlocked = true;
                    achievement.UnlockTime = (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }

                // Steam에 저장
                SteamUserStats.StoreStats();
            }

            return success;
        }

        /// <summary>
        /// 도전과제 잠금 (개발/테스트용)
        /// </summary>
        public bool ClearAchievement(string achievementId)
        {
            if (!SteamManager.CheckInitialized("AchievementService")) return false;

            bool success = SteamUserStats.ClearAchievement(achievementId);
            SteamLogger.LogResult("Achievement", $"잠금 시도: {achievementId}", success);

            if (success)
            {
                // 로컬 데이터 업데이트
                var achievement = _achievements.Find(a => a.Id == achievementId);
                if (achievement != null)
                {
                    achievement.IsUnlocked = false;
                    achievement.UnlockTime = 0;
                }

                // Steam에 저장
                SteamUserStats.StoreStats();
            }

            return success;
        }

        /// <summary>
        /// 도전과제 진행률 설정 (진행형 도전과제)
        /// </summary>
        public bool SetAchievementProgress(string achievementId, uint currentProgress, uint maxProgress)
        {
            if (!SteamManager.CheckInitialized("AchievementService")) return false;

            bool success = SteamUserStats.IndicateAchievementProgress(achievementId, currentProgress, maxProgress);
            SteamLogger.LogResult("Achievement", $"진행률 설정: {achievementId} ({currentProgress}/{maxProgress})", success);

            return success;
        }

        /// <summary>
        /// 특정 도전과제 정보 가져오기
        /// </summary>
        public AchievementData GetAchievement(string achievementId)
        {
            return _achievements.Find(a => a.Id == achievementId);
        }

        /// <summary>
        /// 모든 도전과제 해제 (테스트용)
        /// </summary>
        public void UnlockAllAchievements()
        {
            foreach (var achievement in _achievements)
            {
                if (!achievement.IsUnlocked)
                {
                    UnlockAchievement(achievement.Id);
                }
            }
        }

        /// <summary>
        /// 모든 도전과제 잠금 (테스트용)
        /// </summary>
        public void ClearAllAchievements()
        {
            foreach (var achievement in _achievements)
            {
                if (achievement.IsUnlocked)
                {
                    ClearAchievement(achievement.Id);
                }
            }
        }

        /// <summary>
        /// 도전과제 다시 로드
        /// </summary>
        public void RefreshAchievements()
        {
            LoadAchievements();
        }

        #region Callbacks

        private void OnAchievementStored(UserAchievementStored_t result)
        {
            if (result.m_nGameID != SteamManager.Instance.AppId.m_AppId)
                return;

            SteamLogger.Log("Achievement", $"도전과제 저장됨: {result.m_rgchAchievementName}");
            SteamLogger.Log("Achievement", $"현재 진행: {result.m_nCurProgress}/{result.m_nMaxProgress}");

            // 해제 완료된 경우 이벤트 발생
            if (result.m_nCurProgress >= result.m_nMaxProgress)
            {
                SteamEventBus.RaiseAchievementUnlocked(result.m_rgchAchievementName);
            }
        }

        #endregion

        /// <summary>
        /// 모든 도전과제 로그 출력 (디버그용)
        /// </summary>
        private void LogAllAchievements()
        {
            SteamLogger.Log("Achievement", "=== 도전과제 목록 ===");
            foreach (var achievement in _achievements)
            {
                string status = achievement.IsUnlocked ? "[해제됨]" : "[잠김]";
                SteamLogger.Log("Achievement", $"  {status} {achievement.DisplayName}");
            }
        }

        public void Shutdown()
        {
            SteamEventBus.OnStatsReceived -= LoadAchievements;
            _achievements.Clear();
            IsInitialized = false;
            SteamLogger.Log("Achievement", "AchievementService 종료");
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
