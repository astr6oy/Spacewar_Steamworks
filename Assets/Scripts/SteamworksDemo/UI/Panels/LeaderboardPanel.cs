using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;
using SteamworksDemo.Data;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// 리더보드 패널
    /// 순위표 표시, 점수 업로드 기능
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        [Header("리더보드 설정")]
        [SerializeField] private InputField _leaderboardNameInput;
        [SerializeField] private Button _findButton;
        [SerializeField] private Text _currentLeaderboardText;

        [Header("점수 업로드")]
        [SerializeField] private InputField _scoreInput;
        [SerializeField] private Button _uploadButton;
        [SerializeField] private Text _uploadResultText;

        [Header("순위표")]
        [SerializeField] private Transform _entriesContainer;
        [SerializeField] private GameObject _entryPrefab;
        [SerializeField] private Button _refreshGlobalButton;
        [SerializeField] private Button _refreshAroundUserButton;
        [SerializeField] private Button _refreshFriendsButton;

        [Header("색상")]
        [SerializeField] private Color _goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color _silverColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color _bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color _normalColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color _currentUserColor = new Color(0.53f, 0.75f, 0.82f);

        private void Start()
        {
            // 버튼 연결
            if (_findButton != null)
                _findButton.onClick.AddListener(FindLeaderboard);

            if (_uploadButton != null)
                _uploadButton.onClick.AddListener(UploadScore);

            if (_refreshGlobalButton != null)
                _refreshGlobalButton.onClick.AddListener(() => DownloadScores("global"));

            if (_refreshAroundUserButton != null)
                _refreshAroundUserButton.onClick.AddListener(() => DownloadScores("around"));

            if (_refreshFriendsButton != null)
                _refreshFriendsButton.onClick.AddListener(() => DownloadScores("friends"));

            // 기본값 설정
            if (_leaderboardNameInput != null)
                _leaderboardNameInput.text = SteamLeaderboardService.DefaultLeaderboardName;

            UpdateCurrentLeaderboardText();
        }

        private void OnEnable()
        {
            UpdateCurrentLeaderboardText();

            // 리더보드가 이미 설정된 경우 자동 로드
            if (SteamLeaderboardService.Instance?.HasLeaderboard == true)
            {
                DownloadScores("global");
            }
        }

        /// <summary>
        /// 리더보드 검색/생성
        /// </summary>
        private void FindLeaderboard()
        {
            string name = _leaderboardNameInput?.text;
            if (string.IsNullOrEmpty(name))
            {
                SteamLogger.LogWarning("UI", "리더보드 이름을 입력하세요");
                return;
            }

            SteamLeaderboardService.Instance?.FindOrCreateLeaderboard(name, OnLeaderboardFound);
        }

        private void OnLeaderboardFound(bool success)
        {
            UpdateCurrentLeaderboardText();

            if (success)
            {
                // 리더보드 찾으면 자동으로 순위 로드
                DownloadScores("global");
            }
            else
            {
                SteamLogger.LogError("UI", "리더보드 검색 실패");
            }
        }

        /// <summary>
        /// 점수 업로드
        /// </summary>
        private void UploadScore()
        {
            if (!SteamLeaderboardService.Instance?.HasLeaderboard == true)
            {
                SteamLogger.LogWarning("UI", "먼저 리더보드를 검색하세요");
                return;
            }

            string scoreText = _scoreInput?.text;
            if (!int.TryParse(scoreText, out int score))
            {
                SteamLogger.LogWarning("UI", "올바른 점수를 입력하세요");
                return;
            }

            SteamLeaderboardService.Instance.UploadScore(score, OnScoreUploaded);
        }

        private void OnScoreUploaded(bool success, int newRank, int changed)
        {
            if (_uploadResultText != null)
            {
                if (success)
                {
                    string changeText = changed > 0 ? "(기록 갱신!)" : "(기존 기록 유지)";
                    _uploadResultText.text = $"업로드 성공! 순위: {newRank}위 {changeText}";
                    _uploadResultText.color = _currentUserColor;
                }
                else
                {
                    _uploadResultText.text = "업로드 실패";
                    _uploadResultText.color = Color.red;
                }
            }

            // 순위표 갱신
            DownloadScores("global");
        }

        /// <summary>
        /// 순위 다운로드
        /// </summary>
        private void DownloadScores(string type)
        {
            var service = SteamLeaderboardService.Instance;
            if (service == null || !service.HasLeaderboard) return;

            switch (type)
            {
                case "global":
                    service.DownloadScoresGlobal(1, 20, OnScoresDownloaded);
                    break;
                case "around":
                    service.DownloadScoresAroundUser(5, OnScoresDownloaded);
                    break;
                case "friends":
                    service.DownloadScoresFriends(OnScoresDownloaded);
                    break;
            }
        }

        private void OnScoresDownloaded(List<LeaderboardEntryData> entries)
        {
            RefreshEntriesUI(entries);
        }

        /// <summary>
        /// 순위표 UI 갱신
        /// </summary>
        private void RefreshEntriesUI(List<LeaderboardEntryData> entries)
        {
            if (_entriesContainer == null) return;

            // 기존 항목 제거
            foreach (Transform child in _entriesContainer)
            {
                Destroy(child.gameObject);
            }

            if (entries == null || entries.Count == 0)
            {
                SteamLogger.Log("UI", "표시할 순위가 없습니다");
                return;
            }

            // 항목 생성
            foreach (var entry in entries)
            {
                CreateEntryItem(entry);
            }
        }

        /// <summary>
        /// 순위 항목 UI 생성
        /// </summary>
        private void CreateEntryItem(LeaderboardEntryData entry)
        {
            if (_entryPrefab == null || _entriesContainer == null) return;

            GameObject item = Instantiate(_entryPrefab, _entriesContainer);

            // 배경색 (현재 사용자 또는 순위별)
            var background = item.GetComponent<Image>();
            if (background != null)
            {
                if (entry.IsCurrentUser)
                {
                    background.color = _currentUserColor;
                }
                else
                {
                    background.color = GetRankColor(entry.Rank);
                }
            }

            // 순위 텍스트
            var rankText = item.transform.Find("RankText")?.GetComponent<Text>();
            if (rankText != null)
            {
                rankText.text = $"#{entry.Rank}";
                rankText.color = GetRankTextColor(entry.Rank);
            }

            // 순위 아이콘 (1~3위)
            var rankIcon = item.transform.Find("RankIcon")?.GetComponent<Image>();
            if (rankIcon != null)
            {
                rankIcon.gameObject.SetActive(entry.Rank <= 3);
                rankIcon.color = GetRankIconColor(entry.Rank);
            }

            // 이름 텍스트
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                string marker = entry.IsCurrentUser ? " (나)" : "";
                nameText.text = entry.PlayerName + marker;
                nameText.fontStyle = entry.IsCurrentUser ? FontStyle.Bold : FontStyle.Normal;
            }

            // 점수 텍스트
            var scoreText = item.transform.Find("ScoreText")?.GetComponent<Text>();
            if (scoreText != null)
            {
                scoreText.text = entry.Score.ToString("N0");
            }
        }

        /// <summary>
        /// 순위별 배경 색상
        /// </summary>
        private Color GetRankColor(int rank)
        {
            return rank switch
            {
                1 => new Color(_goldColor.r, _goldColor.g, _goldColor.b, 0.3f),
                2 => new Color(_silverColor.r, _silverColor.g, _silverColor.b, 0.3f),
                3 => new Color(_bronzeColor.r, _bronzeColor.g, _bronzeColor.b, 0.3f),
                _ => _normalColor
            };
        }

        /// <summary>
        /// 순위 텍스트 색상
        /// </summary>
        private Color GetRankTextColor(int rank)
        {
            return rank switch
            {
                1 => _goldColor,
                2 => _silverColor,
                3 => _bronzeColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// 순위 아이콘 색상
        /// </summary>
        private Color GetRankIconColor(int rank)
        {
            return rank switch
            {
                1 => _goldColor,
                2 => _silverColor,
                3 => _bronzeColor,
                _ => Color.clear
            };
        }

        /// <summary>
        /// 현재 리더보드 텍스트 갱신
        /// </summary>
        private void UpdateCurrentLeaderboardText()
        {
            if (_currentLeaderboardText != null)
            {
                var service = SteamLeaderboardService.Instance;
                if (service?.HasLeaderboard == true)
                {
                    _currentLeaderboardText.text = $"현재 리더보드: {service.GetCurrentLeaderboardName()}";
                }
                else
                {
                    _currentLeaderboardText.text = "리더보드 없음";
                }
            }
        }
    }
}
