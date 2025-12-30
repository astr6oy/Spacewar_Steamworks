using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;
using SteamworksDemo.Data;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// Stats 패널
    /// 통계 표시 및 수정 기능 제공
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [Header("Stats 목록")]
        [SerializeField] private Transform _statsContainer;
        [SerializeField] private GameObject _statItemPrefab;

        [Header("버튼")]
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _storeButton;
        [SerializeField] private Button _resetButton;

        [Header("테스트용 입력")]
        [SerializeField] private InputField _statIdInput;
        [SerializeField] private InputField _statValueInput;
        [SerializeField] private Button _setStatButton;

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnStatsReceived += OnStatsReceived;
            SteamEventBus.OnStatsStored += OnStatsStored;

            // 버튼 연결
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshStats);

            if (_storeButton != null)
                _storeButton.onClick.AddListener(StoreStats);

            if (_resetButton != null)
                _resetButton.onClick.AddListener(ResetStats);

            if (_setStatButton != null)
                _setStatButton.onClick.AddListener(SetStatFromInput);

            // 초기 로드
            if (SteamStatsService.Instance?.StatsReceived == true)
            {
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            SteamEventBus.OnStatsReceived -= OnStatsReceived;
            SteamEventBus.OnStatsStored -= OnStatsStored;
        }

        private void OnEnable()
        {
            if (SteamStatsService.Instance?.StatsReceived == true)
            {
                RefreshUI();
            }
        }

        private void OnStatsReceived()
        {
            RefreshUI();
        }

        private void OnStatsStored(bool success)
        {
            if (success)
            {
                SteamLogger.Log("UI", "Stats 저장 완료");
            }
        }

        /// <summary>
        /// Stats 요청
        /// </summary>
        private void RefreshStats()
        {
            SteamStatsService.Instance?.RequestStats();
        }

        /// <summary>
        /// Stats 저장
        /// </summary>
        private void StoreStats()
        {
            SteamStatsService.Instance?.StoreStats();
        }

        /// <summary>
        /// Stats 초기화
        /// </summary>
        private void ResetStats()
        {
            SteamStatsService.Instance?.ResetAllStats(false);
        }

        /// <summary>
        /// 입력 필드에서 Stat 설정
        /// </summary>
        private void SetStatFromInput()
        {
            if (_statIdInput == null || _statValueInput == null) return;

            string statId = _statIdInput.text;
            string valueText = _statValueInput.text;

            if (string.IsNullOrEmpty(statId) || string.IsNullOrEmpty(valueText))
            {
                SteamLogger.LogWarning("UI", "Stat ID와 값을 입력하세요");
                return;
            }

            // Int로 파싱 시도
            if (int.TryParse(valueText, out int intValue))
            {
                SteamStatsService.Instance?.SetStatInt(statId, intValue);
            }
            // Float로 파싱 시도
            else if (float.TryParse(valueText, out float floatValue))
            {
                SteamStatsService.Instance?.SetStatFloat(statId, floatValue);
            }
            else
            {
                SteamLogger.LogWarning("UI", "올바른 숫자를 입력하세요");
            }
        }

        /// <summary>
        /// UI 갱신
        /// </summary>
        private void RefreshUI()
        {
            if (_statsContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _statsContainer)
            {
                Destroy(child.gameObject);
            }

            var statsService = SteamStatsService.Instance;
            if (statsService == null || !statsService.StatsReceived) return;

            // Stats 아이템 생성
            foreach (var kvp in statsService.Stats)
            {
                CreateStatItem(kvp.Value);
            }
        }

        /// <summary>
        /// Stat 아이템 UI 생성
        /// </summary>
        private void CreateStatItem(StatData stat)
        {
            if (_statItemPrefab == null || _statsContainer == null) return;

            GameObject item = Instantiate(_statItemPrefab, _statsContainer);

            // 이름 텍스트
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
                nameText.text = stat.DisplayName;

            // 값 텍스트
            var valueText = item.transform.Find("ValueText")?.GetComponent<Text>();
            if (valueText != null)
                valueText.text = stat.GetValueText();

            // 게이지바
            var gauge = item.transform.Find("Gauge")?.GetComponent<Image>();
            if (gauge != null)
                gauge.fillAmount = stat.GetProgress();

            // ID 텍스트 (디버그용)
            var idText = item.transform.Find("IdText")?.GetComponent<Text>();
            if (idText != null)
                idText.text = $"({stat.Id})";

            // +1 버튼 (Int 타입인 경우)
            var incrementButton = item.transform.Find("IncrementButton")?.GetComponent<Button>();
            if (incrementButton != null && stat.Type == StatType.Int)
            {
                string statId = stat.Id;
                incrementButton.onClick.AddListener(() =>
                {
                    SteamStatsService.Instance?.IncrementStat(statId);
                    RefreshUI();
                });
            }
            else if (incrementButton != null)
            {
                incrementButton.gameObject.SetActive(false);
            }
        }
    }
}
