using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;
using SteamworksDemo.Data;
using System;
using System.Collections.Generic;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam 사용자 정보 서비스
    /// 현재 로그인한 사용자의 정보 제공
    /// </summary>
    public class SteamUserService : MonoBehaviour, ISteamService
    {
        public static SteamUserService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 현재 사용자 정보
        public UserData CurrentUser { get; private set; }

        // 아바타 캐시 (SteamID → Texture2D)
        private Dictionary<CSteamID, Texture2D> _avatarCache = new Dictionary<CSteamID, Texture2D>();

        // 아바타 로드 대기 중인 콜백
        private Dictionary<CSteamID, Action<Texture2D>> _pendingAvatarCallbacks = new Dictionary<CSteamID, Action<Texture2D>>();

        // Steam 콜백
        private Callback<AvatarImageLoaded_t> _avatarImageLoadedCallback;

        // 편의 프로퍼티
        public CSteamID SteamId => CurrentUser?.SteamId ?? CSteamID.Nil;
        public string PersonaName => CurrentUser?.PersonaName ?? "Unknown";
        public int SteamLevel => CurrentUser?.SteamLevel ?? 0;
        public bool IsLoggedOn => CurrentUser?.IsLoggedOn ?? false;

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
            if (!SteamManager.CheckInitialized("UserService")) return;

            // 아바타 로드 완료 콜백 등록
            _avatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);

            LoadCurrentUserData();
            IsInitialized = true;

            SteamLogger.Log("User", "UserService 초기화 완료");
        }

        /// <summary>
        /// 현재 사용자 정보 로드
        /// </summary>
        private void LoadCurrentUserData()
        {
            CurrentUser = new UserData
            {
                SteamId = SteamUser.GetSteamID(),
                PersonaName = SteamFriends.GetPersonaName(),
                SteamLevel = SteamUser.GetPlayerSteamLevel(),
                PersonaState = SteamFriends.GetPersonaState(),
                IsLoggedOn = SteamUser.BLoggedOn()
            };

            SteamLogger.Log("User", $"사용자 정보 로드: {CurrentUser.PersonaName}");
            SteamLogger.Log("User", $"Steam ID: {CurrentUser.SteamId}");
            SteamLogger.Log("User", $"레벨: {CurrentUser.SteamLevel}");
            SteamLogger.Log("User", $"상태: {CurrentUser.GetPersonaStateText()}");
        }

        /// <summary>
        /// 사용자 정보 새로고침
        /// </summary>
        public void RefreshUserData()
        {
            if (!SteamManager.CheckInitialized("UserService")) return;

            LoadCurrentUserData();
            SteamEventBus.RaiseUserInfoChanged(CurrentUser.SteamId);
        }

        /// <summary>
        /// 사용자가 게임을 소유하고 있는지 확인
        /// </summary>
        public bool OwnsApp(AppId_t appId)
        {
            if (!SteamManager.CheckInitialized("UserService")) return false;
            return SteamApps.BIsSubscribedApp(appId);
        }

        /// <summary>
        /// 현재 게임 언어 반환
        /// </summary>
        public string GetGameLanguage()
        {
            if (!SteamManager.CheckInitialized("UserService")) return "unknown";
            return SteamApps.GetCurrentGameLanguage();
        }

        /// <summary>
        /// 게임이 Steam을 통해 실행되었는지 확인
        /// </summary>
        public bool IsRunningViaSteam()
        {
            if (!SteamManager.CheckInitialized("UserService")) return false;
            return SteamApps.BIsSubscribed();
        }

        /// <summary>
        /// VAC 밴 여부 확인
        /// </summary>
        public bool IsVACBanned()
        {
            if (!SteamManager.CheckInitialized("UserService")) return false;
            return SteamApps.BIsVACBanned();
        }

        /// <summary>
        /// 사용자 아바타 로드 (비동기)
        /// 캐시된 경우 즉시 반환, 아니면 Steam에서 로드 후 콜백
        /// </summary>
        /// <param name="steamId">대상 Steam ID (null이면 현재 사용자)</param>
        /// <param name="onLoaded">로드 완료 콜백 (null이면 로드 실패)</param>
        public void GetAvatar(CSteamID? steamId, Action<Texture2D> onLoaded)
        {
            if (!SteamManager.CheckInitialized("UserService"))
            {
                onLoaded?.Invoke(null);
                return;
            }

            CSteamID targetId = steamId ?? SteamUser.GetSteamID();

            // 캐시 확인
            if (_avatarCache.TryGetValue(targetId, out Texture2D cachedAvatar))
            {
                onLoaded?.Invoke(cachedAvatar);
                return;
            }

            // Steam에서 아바타 핸들 가져오기
            int avatarHandle = SteamFriends.GetLargeFriendAvatar(targetId);

            if (avatarHandle == -1)
            {
                // 아바타 없음
                SteamLogger.LogWarning("User", $"아바타 없음: {targetId}");
                onLoaded?.Invoke(null);
                return;
            }

            if (avatarHandle == 0)
            {
                // 아직 로드 중 - 콜백 등록
                _pendingAvatarCallbacks[targetId] = onLoaded;
                SteamLogger.Log("User", $"아바타 로드 대기: {targetId}");
                return;
            }

            // 이미 로드됨 - 텍스처 생성
            Texture2D avatar = CreateTextureFromHandle(avatarHandle);
            if (avatar != null)
            {
                _avatarCache[targetId] = avatar;
            }
            onLoaded?.Invoke(avatar);
        }

        /// <summary>
        /// 현재 사용자 아바타 로드 (편의 메서드)
        /// </summary>
        public void GetMyAvatar(Action<Texture2D> onLoaded)
        {
            GetAvatar(null, onLoaded);
        }

        /// <summary>
        /// Steam 이미지 핸들로부터 Texture2D 생성
        /// </summary>
        private Texture2D CreateTextureFromHandle(int imageHandle)
        {
            if (!SteamUtils.GetImageSize(imageHandle, out uint width, out uint height))
            {
                SteamLogger.LogWarning("User", "아바타 이미지 크기 가져오기 실패");
                return null;
            }

            if (width == 0 || height == 0)
            {
                return null;
            }

            uint bufferSize = width * height * 4; // RGBA
            byte[] buffer = new byte[bufferSize];

            if (!SteamUtils.GetImageRGBA(imageHandle, buffer, (int)bufferSize))
            {
                SteamLogger.LogWarning("User", "아바타 이미지 데이터 가져오기 실패");
                return null;
            }

            // Steam 이미지는 상하 반전되어 있음
            byte[] flippedBuffer = FlipImageVertically(buffer, (int)width, (int)height);

            Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(flippedBuffer);
            texture.Apply();

            SteamLogger.Log("User", $"아바타 텍스처 생성: {width}x{height}");
            return texture;
        }

        /// <summary>
        /// 이미지 상하 반전 (Steam 이미지 보정용)
        /// </summary>
        private byte[] FlipImageVertically(byte[] source, int width, int height)
        {
            byte[] flipped = new byte[source.Length];
            int stride = width * 4;

            for (int y = 0; y < height; y++)
            {
                int srcOffset = y * stride;
                int dstOffset = (height - 1 - y) * stride;
                Array.Copy(source, srcOffset, flipped, dstOffset, stride);
            }

            return flipped;
        }

        /// <summary>
        /// 아바타 이미지 로드 완료 콜백 (Steam에서 호출)
        /// </summary>
        private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
        {
            CSteamID steamId = callback.m_steamID;

            SteamLogger.Log("User", $"아바타 로드 완료: {steamId}");

            // 텍스처 생성
            Texture2D avatar = CreateTextureFromHandle(callback.m_iImage);
            if (avatar != null)
            {
                _avatarCache[steamId] = avatar;
            }

            // 대기 중인 콜백 호출
            if (_pendingAvatarCallbacks.TryGetValue(steamId, out Action<Texture2D> pendingCallback))
            {
                _pendingAvatarCallbacks.Remove(steamId);
                pendingCallback?.Invoke(avatar);
            }

            // 이벤트 발생 (UI 갱신용)
            SteamEventBus.RaiseUserInfoChanged(steamId);
        }

        public void Shutdown()
        {
            IsInitialized = false;
            CurrentUser = null;
            _avatarCache.Clear();
            _pendingAvatarCallbacks.Clear();
            _avatarImageLoadedCallback?.Dispose();
            _avatarImageLoadedCallback = null;
            SteamLogger.Log("User", "UserService 종료");
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
