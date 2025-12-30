using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;
using SteamworksDemo.Data;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// 도전과제 패널
    /// Achievement 목록 표시 및 해제/잠금 테스트
    /// </summary>
    public class AchievementsPanel : MonoBehaviour
    {
        [Header("Achievement 그리드")]
        [SerializeField] private Transform _achievementContainer;
        [SerializeField] private GameObject _achievementItemPrefab;

        [Header("버튼")]
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _unlockAllButton;
        [SerializeField] private Button _clearAllButton;

        [Header("색상")]
        [SerializeField] private Color _unlockedColor = new Color(0.64f, 0.74f, 0.55f);
        [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _lockedOverlayColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnStatsReceived += OnStatsReceived;
            SteamEventBus.OnAchievementUnlocked += OnAchievementUnlocked;

            // 버튼 연결
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshAchievements);

            if (_unlockAllButton != null)
                _unlockAllButton.onClick.AddListener(UnlockAllAchievements);

            if (_clearAllButton != null)
                _clearAllButton.onClick.AddListener(ClearAllAchievements);

            // 초기 로드
            if (SteamAchievementService.Instance?.IsInitialized == true)
            {
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            SteamEventBus.OnStatsReceived -= OnStatsReceived;
            SteamEventBus.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnStatsReceived()
        {
            RefreshUI();
        }

        private void OnAchievementUnlocked(string achievementId)
        {
            SteamLogger.Log("UI", $"도전과제 해제 알림: {achievementId}");
            RefreshUI();

            // TODO: 해제 애니메이션 재생
        }

        /// <summary>
        /// Achievement 목록 새로고침
        /// </summary>
        private void RefreshAchievements()
        {
            SteamAchievementService.Instance?.RefreshAchievements();
            RefreshUI();
        }

        /// <summary>
        /// 모든 Achievement 해제 (테스트용)
        /// </summary>
        private void UnlockAllAchievements()
        {
            SteamAchievementService.Instance?.UnlockAllAchievements();
            RefreshUI();
        }

        /// <summary>
        /// 모든 Achievement 잠금 (테스트용)
        /// </summary>
        private void ClearAllAchievements()
        {
            SteamAchievementService.Instance?.ClearAllAchievements();
            RefreshUI();
        }

        /// <summary>
        /// UI 갱신
        /// </summary>
        private void RefreshUI()
        {
            if (_achievementContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _achievementContainer)
            {
                Destroy(child.gameObject);
            }

            var achievementService = SteamAchievementService.Instance;
            if (achievementService == null) return;

            // Achievement 아이템 생성
            foreach (var achievement in achievementService.Achievements)
            {
                CreateAchievementItem(achievement);
            }
        }

        /// <summary>
        /// Achievement 아이템 UI 생성
        /// </summary>
        private void CreateAchievementItem(AchievementData achievement)
        {
            if (_achievementItemPrefab == null || _achievementContainer == null) return;

            GameObject item = Instantiate(_achievementItemPrefab, _achievementContainer);

            // 아이콘 배경 (기본 사각형)
            var iconBg = item.transform.Find("IconBg")?.GetComponent<Image>();
            if (iconBg != null)
            {
                iconBg.color = achievement.IsUnlocked ? _unlockedColor : _lockedColor;
            }

            // 잠금 오버레이
            var lockOverlay = item.transform.Find("LockOverlay")?.GetComponent<Image>();
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(!achievement.IsUnlocked);
                lockOverlay.color = _lockedOverlayColor;
            }

            // 이름 텍스트
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
                nameText.text = achievement.DisplayName;

            // 설명 텍스트
            var descText = item.transform.Find("DescText")?.GetComponent<Text>();
            if (descText != null)
                descText.text = achievement.Description;

            // 해제 시간
            var timeText = item.transform.Find("TimeText")?.GetComponent<Text>();
            if (timeText != null)
            {
                timeText.text = achievement.IsUnlocked
                    ? achievement.GetUnlockTimeText()
                    : "";
            }

            // 상태 텍스트
            var statusText = item.transform.Find("StatusText")?.GetComponent<Text>();
            if (statusText != null)
            {
                statusText.text = achievement.IsUnlocked ? "해제됨" : "잠김";
                statusText.color = achievement.IsUnlocked ? _unlockedColor : _lockedColor;
            }
        }
    }
}
