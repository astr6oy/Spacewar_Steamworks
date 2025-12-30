using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;
using SteamworksDemo.Data;
using Steamworks;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// 로비 패널
    /// 로비 생성, 검색, 참가, 멤버 관리 UI
    /// </summary>
    public class LobbyPanel : MonoBehaviour
    {
        [Header("로비 생성")]
        [SerializeField] private InputField _lobbyNameInput;
        [SerializeField] private Dropdown _lobbyTypeDropdown;
        [SerializeField] private Slider _maxMembersSlider;
        [SerializeField] private Text _maxMembersText;
        [SerializeField] private Button _createButton;

        [Header("로비 검색")]
        [SerializeField] private Button _searchButton;
        [SerializeField] private Transform _lobbyListContainer;
        [SerializeField] private GameObject _lobbyItemPrefab;

        [Header("현재 로비 정보")]
        [SerializeField] private GameObject _currentLobbyPanel;
        [SerializeField] private Text _lobbyIdText;
        [SerializeField] private Text _lobbyNameText;
        [SerializeField] private Text _memberCountText;
        [SerializeField] private Button _leaveLobbyButton;

        [Header("멤버 슬롯")]
        [SerializeField] private Transform _memberSlotsContainer;
        [SerializeField] private GameObject _memberSlotPrefab;

        [Header("색상")]
        [SerializeField] private Color _emptySlotColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color _filledSlotColor = new Color(0.53f, 0.75f, 0.82f);
        [SerializeField] private Color _ownerSlotColor = new Color(0.92f, 0.80f, 0.55f);

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnLobbyCreated += OnLobbyCreated;
            SteamEventBus.OnLobbyEntered += OnLobbyEntered;
            SteamEventBus.OnLobbyLeft += OnLobbyLeft;
            SteamEventBus.OnLobbyMemberJoined += OnLobbyMemberChanged;
            SteamEventBus.OnLobbyMemberLeft += OnLobbyMemberChanged;
            SteamEventBus.OnLobbyDataUpdated += OnLobbyDataUpdated;

            // 버튼 연결
            if (_createButton != null)
                _createButton.onClick.AddListener(CreateLobby);

            if (_searchButton != null)
                _searchButton.onClick.AddListener(SearchLobbies);

            if (_leaveLobbyButton != null)
                _leaveLobbyButton.onClick.AddListener(LeaveLobby);

            // 슬라이더 연결
            if (_maxMembersSlider != null)
            {
                _maxMembersSlider.onValueChanged.AddListener(OnMaxMembersChanged);
                OnMaxMembersChanged(_maxMembersSlider.value);
            }

            // 초기 상태
            UpdateCurrentLobbyUI();
        }

        private void OnDestroy()
        {
            SteamEventBus.OnLobbyCreated -= OnLobbyCreated;
            SteamEventBus.OnLobbyEntered -= OnLobbyEntered;
            SteamEventBus.OnLobbyLeft -= OnLobbyLeft;
            SteamEventBus.OnLobbyMemberJoined -= OnLobbyMemberChanged;
            SteamEventBus.OnLobbyMemberLeft -= OnLobbyMemberChanged;
            SteamEventBus.OnLobbyDataUpdated -= OnLobbyDataUpdated;
        }

        private void OnEnable()
        {
            UpdateCurrentLobbyUI();
        }

        #region 로비 생성

        private void CreateLobby()
        {
            int maxMembers = (int)(_maxMembersSlider?.value ?? 4);
            ELobbyType lobbyType = GetSelectedLobbyType();

            SteamLobbyService.Instance?.CreateLobby(lobbyType, maxMembers, OnLobbyCreateResult);
        }

        private void OnLobbyCreateResult(bool success, CSteamID lobbyId)
        {
            if (success)
            {
                // 로비 이름 설정
                string name = _lobbyNameInput?.text;
                if (!string.IsNullOrEmpty(name))
                {
                    SteamLobbyService.Instance?.SetLobbyName(name);
                }
            }
        }

        private ELobbyType GetSelectedLobbyType()
        {
            if (_lobbyTypeDropdown == null) return ELobbyType.k_ELobbyTypePublic;

            return _lobbyTypeDropdown.value switch
            {
                0 => ELobbyType.k_ELobbyTypePublic,
                1 => ELobbyType.k_ELobbyTypeFriendsOnly,
                2 => ELobbyType.k_ELobbyTypePrivate,
                _ => ELobbyType.k_ELobbyTypePublic
            };
        }

        private void OnMaxMembersChanged(float value)
        {
            if (_maxMembersText != null)
            {
                _maxMembersText.text = $"최대 인원: {(int)value}명";
            }
        }

        #endregion

        #region 로비 검색

        private void SearchLobbies()
        {
            SteamLobbyService.Instance?.SearchLobbies(OnLobbiesFound);
        }

        private void OnLobbiesFound(List<LobbyData> lobbies)
        {
            RefreshLobbyListUI(lobbies);
        }

        private void RefreshLobbyListUI(List<LobbyData> lobbies)
        {
            if (_lobbyListContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _lobbyListContainer)
            {
                Destroy(child.gameObject);
            }

            if (lobbies == null || lobbies.Count == 0)
            {
                SteamLogger.Log("UI", "사용 가능한 로비가 없습니다");
                return;
            }

            // 로비 아이템 생성
            foreach (var lobby in lobbies)
            {
                CreateLobbyListItem(lobby);
            }
        }

        private void CreateLobbyListItem(LobbyData lobby)
        {
            if (_lobbyItemPrefab == null) return;

            GameObject item = Instantiate(_lobbyItemPrefab, _lobbyListContainer);

            // 로비 이름
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(lobby.Name) ? $"Lobby {lobby.LobbyId}" : lobby.Name;
            }

            // 인원 수
            var countText = item.transform.Find("CountText")?.GetComponent<Text>();
            if (countText != null)
            {
                countText.text = $"{lobby.CurrentMembers}/{lobby.MaxMembers}";
            }

            // 참가 버튼
            var joinButton = item.transform.Find("JoinButton")?.GetComponent<Button>();
            if (joinButton != null)
            {
                CSteamID lobbyId = lobby.LobbyId;
                joinButton.onClick.AddListener(() => JoinLobby(lobbyId));
                joinButton.interactable = !lobby.IsFull;
            }
        }

        private void JoinLobby(CSteamID lobbyId)
        {
            SteamLobbyService.Instance?.JoinLobby(lobbyId);
        }

        private void LeaveLobby()
        {
            SteamLobbyService.Instance?.LeaveLobby();
        }

        #endregion

        #region 현재 로비 UI

        private void UpdateCurrentLobbyUI()
        {
            var service = SteamLobbyService.Instance;
            bool isInLobby = service?.IsInLobby == true;

            // 현재 로비 패널 활성화/비활성화
            if (_currentLobbyPanel != null)
            {
                _currentLobbyPanel.SetActive(isInLobby);
            }

            if (!isInLobby) return;

            var lobby = service.CurrentLobby;
            if (lobby == null) return;

            // 로비 정보 표시
            if (_lobbyIdText != null)
                _lobbyIdText.text = $"ID: {lobby.LobbyId}";

            if (_lobbyNameText != null)
                _lobbyNameText.text = lobby.Name;

            if (_memberCountText != null)
                _memberCountText.text = $"{lobby.CurrentMembers}/{lobby.MaxMembers}명";

            // 멤버 슬롯 갱신
            RefreshMemberSlots();
        }

        private void RefreshMemberSlots()
        {
            if (_memberSlotsContainer == null) return;

            // 기존 슬롯 제거
            foreach (Transform child in _memberSlotsContainer)
            {
                Destroy(child.gameObject);
            }

            var service = SteamLobbyService.Instance;
            if (service == null || !service.IsInLobby) return;

            var members = service.GetLobbyMembers();
            int maxMembers = service.CurrentLobby?.MaxMembers ?? 4;

            // 멤버 슬롯 생성
            for (int i = 0; i < maxMembers; i++)
            {
                CreateMemberSlot(i < members.Count ? members[i] : null);
            }
        }

        private void CreateMemberSlot(LobbyMemberData member)
        {
            if (_memberSlotPrefab == null) return;

            GameObject slot = Instantiate(_memberSlotPrefab, _memberSlotsContainer);

            // 배경색
            var background = slot.GetComponent<Image>();
            if (background != null)
            {
                if (member == null)
                {
                    background.color = _emptySlotColor;
                }
                else if (member.IsOwner)
                {
                    background.color = _ownerSlotColor;
                }
                else
                {
                    background.color = _filledSlotColor;
                }
            }

            // 이름
            var nameText = slot.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                if (member != null)
                {
                    string ownerMark = member.IsOwner ? " [호스트]" : "";
                    nameText.text = member.PersonaName + ownerMark;
                }
                else
                {
                    nameText.text = "빈 슬롯";
                }
            }

            // 상태 표시
            var statusImage = slot.transform.Find("StatusImage")?.GetComponent<Image>();
            if (statusImage != null)
            {
                statusImage.gameObject.SetActive(member != null);
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void OnLobbyCreated(CSteamID lobbyId)
        {
            UpdateCurrentLobbyUI();
        }

        private void OnLobbyEntered(CSteamID lobbyId)
        {
            UpdateCurrentLobbyUI();
        }

        private void OnLobbyLeft()
        {
            UpdateCurrentLobbyUI();
        }

        private void OnLobbyMemberChanged(CSteamID memberId)
        {
            UpdateCurrentLobbyUI();
        }

        private void OnLobbyDataUpdated()
        {
            UpdateCurrentLobbyUI();
        }

        #endregion
    }
}
