using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;
using SteamworksDemo.Data;
using Steamworks;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// 친구 목록 패널
    /// 친구 목록 표시, 상태 확인, 초대 기능
    /// </summary>
    public class FriendsPanel : MonoBehaviour
    {
        [Header("친구 목록")]
        [SerializeField] private Transform _friendsContainer;
        [SerializeField] private GameObject _friendItemPrefab;

        [Header("정보 표시")]
        [SerializeField] private Text _friendCountText;

        [Header("버튼")]
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _openFriendsOverlayButton;

        [Header("Rich Presence 테스트")]
        [SerializeField] private InputField _richPresenceKeyInput;
        [SerializeField] private InputField _richPresenceValueInput;
        [SerializeField] private Button _setRichPresenceButton;
        [SerializeField] private Button _clearRichPresenceButton;

        [Header("상태 색상")]
        [SerializeField] private Color _onlineColor = new Color(0.64f, 0.74f, 0.55f);
        [SerializeField] private Color _awayColor = new Color(0.92f, 0.80f, 0.55f);
        [SerializeField] private Color _busyColor = new Color(0.75f, 0.38f, 0.42f);
        [SerializeField] private Color _offlineColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _inGameColor = new Color(0.53f, 0.75f, 0.82f);

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnFriendStateChanged += OnFriendStateChanged;
            SteamEventBus.OnFriendsListChanged += OnFriendsListChanged;

            // 버튼 연결
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshFriends);

            if (_openFriendsOverlayButton != null)
                _openFriendsOverlayButton.onClick.AddListener(OpenFriendsOverlay);

            if (_setRichPresenceButton != null)
                _setRichPresenceButton.onClick.AddListener(SetRichPresence);

            if (_clearRichPresenceButton != null)
                _clearRichPresenceButton.onClick.AddListener(ClearRichPresence);

            // 초기 로드
            if (SteamFriendsService.Instance?.IsInitialized == true)
            {
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            SteamEventBus.OnFriendStateChanged -= OnFriendStateChanged;
            SteamEventBus.OnFriendsListChanged -= OnFriendsListChanged;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnFriendStateChanged(CSteamID friendId)
        {
            RefreshUI();
        }

        private void OnFriendsListChanged()
        {
            RefreshUI();
        }

        /// <summary>
        /// 친구 목록 새로고침
        /// </summary>
        private void RefreshFriends()
        {
            SteamFriendsService.Instance?.LoadFriendsList();
            RefreshUI();
        }

        /// <summary>
        /// Steam 친구 오버레이 열기
        /// </summary>
        private void OpenFriendsOverlay()
        {
            SteamFriendsService.Instance?.OpenAddFriendOverlay();
        }

        /// <summary>
        /// Rich Presence 설정
        /// </summary>
        private void SetRichPresence()
        {
            string key = _richPresenceKeyInput?.text ?? "status";
            string value = _richPresenceValueInput?.text ?? "";

            if (!string.IsNullOrEmpty(key))
            {
                SteamFriendsService.Instance?.SetRichPresence(key, value);
            }
        }

        /// <summary>
        /// Rich Presence 초기화
        /// </summary>
        private void ClearRichPresence()
        {
            SteamFriendsService.Instance?.ClearRichPresence();
        }

        /// <summary>
        /// UI 갱신
        /// </summary>
        private void RefreshUI()
        {
            var service = SteamFriendsService.Instance;
            if (service == null) return;

            // 친구 수 표시
            if (_friendCountText != null)
            {
                _friendCountText.text = $"친구: {service.OnlineFriendsCount}/{service.TotalFriendsCount}명 온라인";
            }

            // 친구 목록 UI 갱신
            RefreshFriendsListUI();
        }

        /// <summary>
        /// 친구 목록 UI 갱신
        /// </summary>
        private void RefreshFriendsListUI()
        {
            if (_friendsContainer == null) return;

            // 기존 아이템 제거
            foreach (Transform child in _friendsContainer)
            {
                Destroy(child.gameObject);
            }

            var service = SteamFriendsService.Instance;
            if (service == null) return;

            // 친구 아이템 생성
            foreach (var friend in service.Friends)
            {
                CreateFriendItem(friend);
            }
        }

        /// <summary>
        /// 친구 아이템 UI 생성
        /// </summary>
        private void CreateFriendItem(FriendData friend)
        {
            if (_friendItemPrefab == null || _friendsContainer == null) return;

            GameObject item = Instantiate(_friendItemPrefab, _friendsContainer);

            // 아바타 영역 (원형 배경)
            var avatarBg = item.transform.Find("AvatarBg")?.GetComponent<Image>();
            if (avatarBg != null)
            {
                // 게임 중이면 특별 색상
                if (friend.IsPlayingThisGame)
                {
                    avatarBg.color = _inGameColor;
                }
                else
                {
                    avatarBg.color = GetStateColor(friend.PersonaState);
                }
            }

            // 상태 표시 (작은 원)
            var statusDot = item.transform.Find("StatusDot")?.GetComponent<Image>();
            if (statusDot != null)
            {
                statusDot.color = GetStateColor(friend.PersonaState);
            }

            // 이름 텍스트
            var nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = friend.PersonaName;
            }

            // 상태 텍스트
            var stateText = item.transform.Find("StateText")?.GetComponent<Text>();
            if (stateText != null)
            {
                if (friend.IsPlayingThisGame)
                {
                    stateText.text = "같은 게임 중";
                    stateText.color = _inGameColor;
                }
                else if (!string.IsNullOrEmpty(friend.CurrentGameName))
                {
                    stateText.text = friend.CurrentGameName;
                    stateText.color = _onlineColor;
                }
                else
                {
                    stateText.text = friend.GetStateText();
                    stateText.color = GetStateColor(friend.PersonaState);
                }
            }

            // 초대 버튼
            var inviteButton = item.transform.Find("InviteButton")?.GetComponent<Button>();
            if (inviteButton != null)
            {
                CSteamID friendId = friend.SteamId;
                inviteButton.onClick.AddListener(() =>
                {
                    SteamFriendsService.Instance?.InviteFriend(friendId);
                });

                // 오프라인이면 비활성화
                inviteButton.interactable = friend.IsOnline();
            }

            // 프로필 버튼
            var profileButton = item.transform.Find("ProfileButton")?.GetComponent<Button>();
            if (profileButton != null)
            {
                CSteamID friendId = friend.SteamId;
                profileButton.onClick.AddListener(() =>
                {
                    SteamFriendsService.Instance?.OpenFriendProfile(friendId);
                });
            }

            // 채팅 버튼
            var chatButton = item.transform.Find("ChatButton")?.GetComponent<Button>();
            if (chatButton != null)
            {
                CSteamID friendId = friend.SteamId;
                chatButton.onClick.AddListener(() =>
                {
                    SteamFriendsService.Instance?.OpenFriendChat(friendId);
                });
            }
        }

        /// <summary>
        /// 상태에 따른 색상 반환
        /// </summary>
        private Color GetStateColor(EPersonaState state)
        {
            return state switch
            {
                EPersonaState.k_EPersonaStateOnline => _onlineColor,
                EPersonaState.k_EPersonaStateAway => _awayColor,
                EPersonaState.k_EPersonaStateSnooze => _awayColor,
                EPersonaState.k_EPersonaStateBusy => _busyColor,
                EPersonaState.k_EPersonaStateLookingToTrade => _onlineColor,
                EPersonaState.k_EPersonaStateLookingToPlay => _onlineColor,
                _ => _offlineColor
            };
        }
    }
}
