using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;
using SteamworksDemo.Data;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam Leaderboard 서비스
    /// 리더보드 검색, 점수 업로드, 순위 조회
    /// CallResult 패턴 사용 (비동기 작업)
    /// </summary>
    public class SteamLeaderboardService : MonoBehaviour, ISteamService
    {
        public static SteamLeaderboardService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 현재 활성 리더보드
        private SteamLeaderboard_t _currentLeaderboard;
        private string _currentLeaderboardName;
        public bool HasLeaderboard => _currentLeaderboard.m_SteamLeaderboard != 0;

        // 마지막으로 조회한 entries
        private readonly List<LeaderboardEntryData> _entries = new List<LeaderboardEntryData>();
        public IReadOnlyList<LeaderboardEntryData> Entries => _entries;

        // CallResults (비동기 결과 수신)
        private CallResult<LeaderboardFindResult_t> _findLeaderboardCallResult;
        private CallResult<LeaderboardScoreUploaded_t> _uploadScoreCallResult;
        private CallResult<LeaderboardScoresDownloaded_t> _downloadScoresCallResult;

        // 콜백
        private Action<bool> _onLeaderboardFound;
        private Action<bool, int, int> _onScoreUploaded; // success, newRank, changed
        private Action<List<LeaderboardEntryData>> _onScoresDownloaded;

        // SpaceWar 기본 리더보드 이름
        public const string DefaultLeaderboardName = "Spacewar";

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
            if (!SteamManager.CheckInitialized("LeaderboardService")) return;

            // CallResult 초기화
            _findLeaderboardCallResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
            _uploadScoreCallResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
            _downloadScoresCallResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnScoresDownloaded);

            IsInitialized = true;
            SteamLogger.Log("Leaderboard", "LeaderboardService 초기화 완료");
        }

        /// <summary>
        /// 리더보드 찾기 (없으면 생성)
        /// </summary>
        public void FindOrCreateLeaderboard(string leaderboardName, Action<bool> callback = null)
        {
            if (!SteamManager.CheckInitialized("LeaderboardService"))
            {
                callback?.Invoke(false);
                return;
            }

            _onLeaderboardFound = callback;
            _currentLeaderboardName = leaderboardName;

            SteamAPICall_t apiCall = SteamUserStats.FindOrCreateLeaderboard(
                leaderboardName,
                ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
                ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric
            );

            _findLeaderboardCallResult.Set(apiCall);
            SteamLogger.Log("Leaderboard", $"리더보드 검색/생성 요청: {leaderboardName}");
        }

        /// <summary>
        /// 기존 리더보드 찾기
        /// </summary>
        public void FindLeaderboard(string leaderboardName, Action<bool> callback = null)
        {
            if (!SteamManager.CheckInitialized("LeaderboardService"))
            {
                callback?.Invoke(false);
                return;
            }

            _onLeaderboardFound = callback;
            _currentLeaderboardName = leaderboardName;

            SteamAPICall_t apiCall = SteamUserStats.FindLeaderboard(leaderboardName);
            _findLeaderboardCallResult.Set(apiCall);

            SteamLogger.Log("Leaderboard", $"리더보드 검색 요청: {leaderboardName}");
        }

        /// <summary>
        /// 점수 업로드
        /// </summary>
        public void UploadScore(int score, Action<bool, int, int> callback = null)
        {
            if (!HasLeaderboard)
            {
                SteamLogger.LogWarning("Leaderboard", "리더보드가 설정되지 않음");
                callback?.Invoke(false, 0, 0);
                return;
            }

            _onScoreUploaded = callback;

            SteamAPICall_t apiCall = SteamUserStats.UploadLeaderboardScore(
                _currentLeaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                null,
                0
            );

            _uploadScoreCallResult.Set(apiCall);
            SteamLogger.Log("Leaderboard", $"점수 업로드 요청: {score}");
        }

        /// <summary>
        /// 전체 순위 다운로드
        /// </summary>
        public void DownloadScoresGlobal(int start, int end, Action<List<LeaderboardEntryData>> callback = null)
        {
            if (!HasLeaderboard)
            {
                SteamLogger.LogWarning("Leaderboard", "리더보드가 설정되지 않음");
                callback?.Invoke(new List<LeaderboardEntryData>());
                return;
            }

            _onScoresDownloaded = callback;

            SteamAPICall_t apiCall = SteamUserStats.DownloadLeaderboardEntries(
                _currentLeaderboard,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
                start,
                end
            );

            _downloadScoresCallResult.Set(apiCall);
            SteamLogger.Log("Leaderboard", $"전체 순위 다운로드 요청 ({start}~{end})");
        }

        /// <summary>
        /// 현재 사용자 주변 순위 다운로드
        /// </summary>
        public void DownloadScoresAroundUser(int range, Action<List<LeaderboardEntryData>> callback = null)
        {
            if (!HasLeaderboard)
            {
                SteamLogger.LogWarning("Leaderboard", "리더보드가 설정되지 않음");
                callback?.Invoke(new List<LeaderboardEntryData>());
                return;
            }

            _onScoresDownloaded = callback;

            SteamAPICall_t apiCall = SteamUserStats.DownloadLeaderboardEntries(
                _currentLeaderboard,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
                -range,
                range
            );

            _downloadScoresCallResult.Set(apiCall);
            SteamLogger.Log("Leaderboard", $"내 주변 순위 다운로드 요청 (범위: {range})");
        }

        /// <summary>
        /// 친구 순위만 다운로드
        /// </summary>
        public void DownloadScoresFriends(Action<List<LeaderboardEntryData>> callback = null)
        {
            if (!HasLeaderboard)
            {
                SteamLogger.LogWarning("Leaderboard", "리더보드가 설정되지 않음");
                callback?.Invoke(new List<LeaderboardEntryData>());
                return;
            }

            _onScoresDownloaded = callback;

            SteamAPICall_t apiCall = SteamUserStats.DownloadLeaderboardEntries(
                _currentLeaderboard,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends,
                1,
                100
            );

            _downloadScoresCallResult.Set(apiCall);
            SteamLogger.Log("Leaderboard", "친구 순위 다운로드 요청");
        }

        #region CallResult Handlers

        private void OnLeaderboardFound(LeaderboardFindResult_t result, bool bIOFailure)
        {
            if (bIOFailure || result.m_bLeaderboardFound == 0)
            {
                SteamLogger.LogError("Leaderboard", $"리더보드 검색 실패: {_currentLeaderboardName}");
                _onLeaderboardFound?.Invoke(false);
                return;
            }

            _currentLeaderboard = result.m_hSteamLeaderboard;

            int entryCount = SteamUserStats.GetLeaderboardEntryCount(_currentLeaderboard);
            SteamLogger.Log("Leaderboard", $"리더보드 찾음: {_currentLeaderboardName} (총 {entryCount}개 항목)");

            _onLeaderboardFound?.Invoke(true);
        }

        private void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool bIOFailure)
        {
            if (bIOFailure || result.m_bSuccess == 0)
            {
                SteamLogger.LogError("Leaderboard", "점수 업로드 실패");
                _onScoreUploaded?.Invoke(false, 0, 0);
                return;
            }

            bool scoreChanged = result.m_bScoreChanged != 0;
            int newRank = result.m_nGlobalRankNew;
            int previousRank = result.m_nGlobalRankPrevious;

            SteamLogger.Log("Leaderboard", $"점수 업로드 성공 - 점수: {result.m_nScore}");
            SteamLogger.Log("Leaderboard", $"순위: {previousRank} -> {newRank} (변경: {scoreChanged})");

            _onScoreUploaded?.Invoke(true, newRank, scoreChanged ? 1 : 0);
        }

        private void OnScoresDownloaded(LeaderboardScoresDownloaded_t result, bool bIOFailure)
        {
            _entries.Clear();

            if (bIOFailure)
            {
                SteamLogger.LogError("Leaderboard", "순위 다운로드 실패");
                _onScoresDownloaded?.Invoke(_entries);
                return;
            }

            int entryCount = result.m_cEntryCount;
            CSteamID currentUserId = SteamUser.GetSteamID();

            SteamLogger.Log("Leaderboard", $"순위 다운로드 완료: {entryCount}개");

            for (int i = 0; i < entryCount; i++)
            {
                LeaderboardEntry_t entry;
                int[] details = new int[3];

                if (SteamUserStats.GetDownloadedLeaderboardEntry(
                    result.m_hSteamLeaderboardEntries,
                    i,
                    out entry,
                    details,
                    details.Length))
                {
                    var entryData = new LeaderboardEntryData
                    {
                        Rank = entry.m_nGlobalRank,
                        SteamId = entry.m_steamIDUser,
                        PlayerName = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser),
                        Score = entry.m_nScore,
                        IsCurrentUser = entry.m_steamIDUser == currentUserId,
                        Details = details
                    };

                    _entries.Add(entryData);
                }
            }

            _onScoresDownloaded?.Invoke(_entries);
        }

        #endregion

        /// <summary>
        /// 현재 리더보드 이름 반환
        /// </summary>
        public string GetCurrentLeaderboardName()
        {
            return _currentLeaderboardName ?? "없음";
        }

        public void Shutdown()
        {
            _currentLeaderboard = default;
            _currentLeaderboardName = null;
            _entries.Clear();
            IsInitialized = false;
            SteamLogger.Log("Leaderboard", "LeaderboardService 종료");
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
