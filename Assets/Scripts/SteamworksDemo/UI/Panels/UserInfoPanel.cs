using UnityEngine;
using UnityEngine.UI;
using SteamworksDemo.Core;
using SteamworksDemo.Services;

namespace SteamworksDemo.UI.Panels
{
    /// <summary>
    /// 사용자 정보 패널
    /// Steam ID, 닉네임, 레벨 등 표시
    /// </summary>
    public class UserInfoPanel : MonoBehaviour
    {
        [Header("사용자 정보 UI")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _personaNameText;
        [SerializeField] private Text _steamIdText;
        [SerializeField] private Text _steamLevelText;
        [SerializeField] private Text _personaStateText;
        [SerializeField] private Image _statusIndicator;

        [Header("앱 정보 UI")]
        [SerializeField] private Text _appIdText;
        [SerializeField] private Text _languageText;
        [SerializeField] private Text _ownershipText;

        [Header("상태 색상")]
        [SerializeField] private Color _onlineColor = new Color(0.64f, 0.74f, 0.55f);
        [SerializeField] private Color _awayColor = new Color(0.92f, 0.80f, 0.55f);
        [SerializeField] private Color _busyColor = new Color(0.75f, 0.38f, 0.42f);
        [SerializeField] private Color _offlineColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("버튼")]
        [SerializeField] private Button _refreshButton;

        private void Start()
        {
            // 이벤트 구독
            SteamEventBus.OnSteamInitialized += OnSteamInitialized;
            SteamEventBus.OnUserInfoChanged += OnUserInfoChanged;

            // 새로고침 버튼 연결
            if (_refreshButton != null)
            {
                _refreshButton.onClick.AddListener(RefreshUserInfo);
            }

            // 초기 데이터 로드
            if (SteamManager.Instance?.IsInitialized == true)
            {
                RefreshUserInfo();
            }
        }

        private void OnDestroy()
        {
            SteamEventBus.OnSteamInitialized -= OnSteamInitialized;
            SteamEventBus.OnUserInfoChanged -= OnUserInfoChanged;
        }

        private void OnEnable()
        {
            // 패널 활성화 시 정보 갱신
            if (SteamManager.Instance?.IsInitialized == true)
            {
                RefreshUserInfo();
            }
        }

        private void OnSteamInitialized()
        {
            // SteamUserService 초기화
            if (SteamUserService.Instance != null)
            {
                SteamUserService.Instance.Initialize();
            }
            RefreshUserInfo();
        }

        private void OnUserInfoChanged(Steamworks.CSteamID userId)
        {
            RefreshUserInfo();
        }

        /// <summary>
        /// 사용자 정보 새로고침
        /// </summary>
        public void RefreshUserInfo()
        {
            var userService = SteamUserService.Instance;
            if (userService == null || !userService.IsInitialized)
            {
                SetPlaceholderData();
                return;
            }

            var userData = userService.CurrentUser;
            if (userData == null)
            {
                SetPlaceholderData();
                return;
            }

            // 사용자 정보 표시
            if (_personaNameText != null)
                _personaNameText.text = userData.PersonaName;

            if (_steamIdText != null)
                _steamIdText.text = $"Steam ID: {userData.SteamId}";

            if (_steamLevelText != null)
                _steamLevelText.text = $"레벨: {userData.SteamLevel}";

            if (_personaStateText != null)
                _personaStateText.text = $"상태: {userData.GetPersonaStateText()}";

            // 상태 인디케이터 색상
            if (_statusIndicator != null)
            {
                _statusIndicator.color = GetStateColor(userData.PersonaState);
            }

            // 앱 정보 표시
            if (_appIdText != null)
                _appIdText.text = $"App ID: {SteamManager.Instance.AppId}";

            if (_languageText != null)
                _languageText.text = $"언어: {userService.GetGameLanguage()}";

            if (_ownershipText != null)
            {
                bool ownsGame = userService.IsRunningViaSteam();
                _ownershipText.text = ownsGame ? "게임 소유: 예" : "게임 소유: 아니오";
            }

            // 아바타 로드
            if (_avatarImage != null)
            {
                userService.GetMyAvatar(OnAvatarLoaded);
            }

            SteamLogger.Log("UI", "사용자 정보 UI 갱신 완료");
        }

        /// <summary>
        /// 아바타 로드 완료 콜백
        /// </summary>
        private void OnAvatarLoaded(Texture2D avatarTexture)
        {
            if (_avatarImage == null) return;

            if (avatarTexture != null)
            {
                Sprite avatarSprite = Sprite.Create(
                    avatarTexture,
                    new Rect(0, 0, avatarTexture.width, avatarTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
                _avatarImage.sprite = avatarSprite;
                _avatarImage.color = Color.white;
                SteamLogger.Log("UI", "아바타 이미지 적용 완료");
            }
            else
            {
                // 아바타 없음 - 기본 색상 표시
                _avatarImage.sprite = null;
                _avatarImage.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        /// <summary>
        /// 상태에 따른 색상 반환
        /// </summary>
        private Color GetStateColor(Steamworks.EPersonaState state)
        {
            return state switch
            {
                Steamworks.EPersonaState.k_EPersonaStateOnline => _onlineColor,
                Steamworks.EPersonaState.k_EPersonaStateAway => _awayColor,
                Steamworks.EPersonaState.k_EPersonaStateSnooze => _awayColor,
                Steamworks.EPersonaState.k_EPersonaStateBusy => _busyColor,
                Steamworks.EPersonaState.k_EPersonaStateLookingToTrade => _onlineColor,
                Steamworks.EPersonaState.k_EPersonaStateLookingToPlay => _onlineColor,
                _ => _offlineColor
            };
        }

        /// <summary>
        /// 연결되지 않은 경우 플레이스홀더 표시
        /// </summary>
        private void SetPlaceholderData()
        {
            if (_personaNameText != null)
                _personaNameText.text = "연결 안됨";

            if (_steamIdText != null)
                _steamIdText.text = "Steam ID: -";

            if (_steamLevelText != null)
                _steamLevelText.text = "레벨: -";

            if (_personaStateText != null)
                _personaStateText.text = "상태: 오프라인";

            if (_statusIndicator != null)
                _statusIndicator.color = _offlineColor;

            if (_appIdText != null)
                _appIdText.text = "App ID: -";

            if (_languageText != null)
                _languageText.text = "언어: -";

            if (_ownershipText != null)
                _ownershipText.text = "게임 소유: -";
        }
    }
}
