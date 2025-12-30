using UnityEngine;
using UnityEngine.UI;

namespace SteamworksDemo.UI
{
    /// <summary>
    /// 탭 버튼 컨트롤러
    /// 탭 버튼 클릭 시 해당 패널로 전환
    /// </summary>
    public class TabController : MonoBehaviour
    {
        [Header("탭 버튼들")]
        [SerializeField] private Button[] _tabButtons;

        [Header("선택 상태 색상")]
        [SerializeField] private Color _selectedColor = new Color(0.53f, 0.75f, 0.82f);
        [SerializeField] private Color _normalColor = new Color(0.85f, 0.85f, 0.85f);

        private int _currentTabIndex = 0;

        private void Start()
        {
            // 각 탭 버튼에 클릭 이벤트 연결
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int index = i; // 클로저를 위한 로컬 변수
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].onClick.AddListener(() => OnTabClicked(index));
                }
            }

            // 첫 번째 탭 선택
            SelectTab(0);
        }

        /// <summary>
        /// 탭 클릭 처리
        /// </summary>
        private void OnTabClicked(int index)
        {
            SelectTab(index);

            // UIManager에 패널 전환 요청
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPanel(index);
            }
        }

        /// <summary>
        /// 탭 선택 상태 업데이트
        /// </summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= _tabButtons.Length) return;

            _currentTabIndex = index;

            // 모든 탭 색상 업데이트
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    var colors = _tabButtons[i].colors;
                    colors.normalColor = (i == index) ? _selectedColor : _normalColor;
                    colors.selectedColor = (i == index) ? _selectedColor : _normalColor;
                    _tabButtons[i].colors = colors;

                    // 텍스트 색상도 변경 (있는 경우)
                    var text = _tabButtons[i].GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        text.fontStyle = (i == index) ? FontStyle.Bold : FontStyle.Normal;
                    }
                }
            }
        }

        /// <summary>
        /// 현재 선택된 탭 인덱스
        /// </summary>
        public int CurrentTabIndex => _currentTabIndex;
    }
}
