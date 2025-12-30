using UnityEngine;
using SteamworksDemo.Core;

namespace SteamworksDemo.UI
{
    /// <summary>
    /// UI 전체 관리자
    /// 패널 전환, 상태 표시 등 담당
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("패널 참조")]
        [SerializeField] private GameObject[] _panels;
        [SerializeField] private int _defaultPanelIndex = 0;

        [Header("상태 표시")]
        [SerializeField] private UnityEngine.UI.Image _connectionStatusImage;
        [SerializeField] private UnityEngine.UI.Text _connectionStatusText;

        [Header("색상 설정")]
        [SerializeField] private Color _connectedColor = new Color(0.64f, 0.74f, 0.55f); // 연두색
        [SerializeField] private Color _disconnectedColor = new Color(0.75f, 0.38f, 0.42f); // 빨간색

        private int _currentPanelIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Steam 이벤트 구독
            SteamEventBus.OnSteamInitialized += OnSteamInitialized;
            SteamEventBus.OnSteamShutdown += OnSteamShutdown;

            // 초기 상태 설정
            UpdateConnectionStatus(SteamManager.Instance?.IsInitialized ?? false);

            // 기본 패널 표시
            if (_panels != null && _panels.Length > 0)
            {
                ShowPanel(_defaultPanelIndex);
            }
        }

        private void OnDestroy()
        {
            SteamEventBus.OnSteamInitialized -= OnSteamInitialized;
            SteamEventBus.OnSteamShutdown -= OnSteamShutdown;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 특정 인덱스의 패널 표시
        /// </summary>
        public void ShowPanel(int index)
        {
            if (_panels == null || index < 0 || index >= _panels.Length)
            {
                SteamLogger.LogWarning("UI", $"잘못된 패널 인덱스: {index}");
                return;
            }

            // 현재 패널과 같으면 무시
            if (_currentPanelIndex == index) return;

            // 모든 패널 비활성화
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                {
                    _panels[i].SetActive(i == index);
                }
            }

            _currentPanelIndex = index;
            SteamLogger.Log("UI", $"패널 전환: {_panels[index].name}");
        }

        /// <summary>
        /// 연결 상태 UI 업데이트
        /// </summary>
        private void UpdateConnectionStatus(bool isConnected)
        {
            if (_connectionStatusImage != null)
            {
                _connectionStatusImage.color = isConnected ? _connectedColor : _disconnectedColor;
            }

            if (_connectionStatusText != null)
            {
                _connectionStatusText.text = isConnected ? "Steam 연결됨" : "Steam 연결 안됨";
            }
        }

        private void OnSteamInitialized()
        {
            UpdateConnectionStatus(true);
        }

        private void OnSteamShutdown()
        {
            UpdateConnectionStatus(false);
        }

        /// <summary>
        /// 팝업 메시지 표시 (간단한 피드백)
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            SteamLogger.Log("UI", $"메시지: {message}");
            // TODO: 실제 팝업 UI 구현
        }
    }
}
