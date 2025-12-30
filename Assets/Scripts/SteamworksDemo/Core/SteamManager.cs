using UnityEngine;
using Steamworks;
using System;
using System.IO;

namespace SteamworksDemo.Core
{
    /// <summary>
    /// Steam API 초기화 및 생명주기 관리
    /// 앱 시작 시 가장 먼저 초기화되어야 함
    /// 싱글톤으로 구현, 씬 전환에도 유지
    /// </summary>
    public class SteamManager : MonoBehaviour
    {
        /// <summary>
        /// Steam App ID (Inspector에서 설정 가능)
        /// SpaceWar 테스트 앱: 480
        /// </summary>
        [SerializeField]
        private uint _appId = 480;

        public static SteamManager Instance { get; private set; }

        /// <summary>
        /// Steam API 초기화 성공 여부
        /// 다른 Steam 서비스 사용 전 반드시 확인 필요
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 현재 사용 중인 App ID
        /// </summary>
        public AppId_t AppId { get; private set; }

        // 경고 메시지 출력 여부 (중복 방지)
        private bool _hasLoggedWarning;

        private void Awake()
        {
            // 싱글톤 패턴 - 중복 인스턴스 방지
            if (Instance != null && Instance != this)
            {
                SteamLogger.LogWarning("SteamManager", "중복 인스턴스 감지, 제거됨");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSteam();
        }

        /// <summary>
        /// Steam API 초기화
        /// </summary>
        private void InitializeSteam()
        {
            // 이미 초기화된 경우 스킵
            if (IsInitialized)
            {
                SteamLogger.LogWarning("SteamManager", "이미 초기화됨");
                return;
            }

            // Packsize 테스트 - 빌드 설정 검증
            if (!Packsize.Test())
            {
                SteamLogger.LogError("SteamManager", "Packsize 테스트 실패 - 빌드 설정 확인 필요");
                return;
            }

            // DllCheck - 네이티브 라이브러리 검증
            if (!DllCheck.Test())
            {
                SteamLogger.LogError("SteamManager", "DllCheck 실패 - steam_api 라이브러리 확인 필요");
                return;
            }

            try
            {
                InteropHelp.TestIfPlatformSupported();

                // Editor에서 steam_appid.txt 자동 생성
                EnsureSteamAppIdFile();

                SteamLogger.Log("SteamManager", $"플랫폼: {Application.platform}");
                SteamLogger.Log("SteamManager", $"App ID 설정값: {_appId}");
                SteamLogger.Log("SteamManager", "SteamAPI.Init() 호출 시도...");

                string OutSteamErrMsg;
                ESteamAPIInitResult initResult = SteamAPI.InitEx(out OutSteamErrMsg);
                bool result = initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;

                // 디버깅: 초기화 결과 상세 출력
                SteamLogger.Log("SteamManager", $"InitEx 결과: {initResult} ({(int)initResult})");
                SteamLogger.Log("SteamManager", $"에러 메시지 길이: {OutSteamErrMsg?.Length ?? -1}");
                SteamLogger.Log("SteamManager", $"에러 메시지: [{OutSteamErrMsg}]");

                // Steam API 초기화 시도
                // steam_appid.txt 파일이 필요 (에디터에서 실행 시)
                if (!result)
                {
                    SteamLogger.LogError("SteamManager", $"SteamAPI.Init() 실패");
                    SteamLogger.LogError("SteamManager", $"에러 코드: {initResult}");

                    // 에러 유형별 안내
                    switch (initResult)
                    {
                        case ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient:
                            SteamLogger.LogError("SteamManager", "Steam 클라이언트가 실행되지 않음");
                            break;
                        case ESteamAPIInitResult.k_ESteamAPIInitResult_VersionMismatch:
                            SteamLogger.LogError("SteamManager", "Steam 클라이언트 버전이 오래됨 - 업데이트 필요");
                            break;
                        case ESteamAPIInitResult.k_ESteamAPIInitResult_FailedGeneric:
                            if (OutSteamErrMsg?.Contains("CSteamAPIContext") == true)
                            {
                                SteamLogger.LogError("SteamManager", "CSteamAPIContext 초기화 실패 - 인터페이스 버전 불일치 가능성");
                            }
                            else
                            {
                                SteamLogger.LogError("SteamManager", $"일반 실패: {OutSteamErrMsg}");
                            }
                            break;
                    }
                    return;
                }

                // 초기화 성공
                IsInitialized = true;
                AppId = SteamUtils.GetAppID();

                // 현재 사용자 정보 로그
                string userName = SteamFriends.GetPersonaName();
                CSteamID steamId = SteamUser.GetSteamID();

                SteamLogger.Log("SteamManager", $"Steam 초기화 성공");
                SteamLogger.Log("SteamManager", $"App ID: {AppId}");
                SteamLogger.Log("SteamManager", $"사용자: {userName} ({steamId})");

                // 초기화 완료 이벤트 발생
                SteamEventBus.RaiseSteamInitialized();
            }
            catch (Exception e)
            {
                SteamLogger.LogError("SteamManager", $"초기화 중 예외 발생: {e.Message}");
                IsInitialized = false;
            }
        }

        private void Update()
        {
            // Steam API가 초기화되지 않은 경우
            if (!IsInitialized)
            {
                // 경고는 한 번만 출력
                if (!_hasLoggedWarning)
                {
                    SteamLogger.LogWarning("SteamManager", "Steam이 초기화되지 않아 RunCallbacks를 건너뜀");
                    _hasLoggedWarning = true;
                }
                return;
            }

            // 매 프레임 Steam 콜백 처리
            // 이 호출이 없으면 Steam 이벤트를 받을 수 없음
            SteamAPI.RunCallbacks();
        }

        private void OnApplicationQuit()
        {
            ShutdownSteam();
        }

        private void OnDestroy()
        {
            // 인스턴스가 파괴될 때 정리
            if (Instance == this)
            {
                ShutdownSteam();
                Instance = null;
            }
        }

        /// <summary>
        /// Steam API 종료
        /// </summary>
        private void ShutdownSteam()
        {
            if (!IsInitialized) return;

            SteamLogger.Log("SteamManager", "Steam API 종료 중...");

            // 종료 이벤트 발생 (서비스들이 정리할 시간 제공)
            SteamEventBus.RaiseSteamShutdown();

            // Steam API 종료
            SteamAPI.Shutdown();
            IsInitialized = false;

            SteamLogger.Log("SteamManager", "Steam API 종료 완료");
        }

        /// <summary>
        /// steam_appid.txt 파일 자동 생성/업데이트
        /// Editor에서만 동작, 빌드에서는 Steam이 App ID 제공
        /// </summary>
        private void EnsureSteamAppIdFile()
        {
#if UNITY_EDITOR
            // Editor: 프로젝트 루트에 생성
            string path = Path.Combine(Application.dataPath, "..", "steam_appid.txt");
            string expectedContent = _appId.ToString();

            // 파일이 없거나 내용이 다르면 생성/업데이트
            if (!File.Exists(path) || File.ReadAllText(path).Trim() != expectedContent)
            {
                File.WriteAllText(path, expectedContent);
                SteamLogger.Log("SteamManager", $"steam_appid.txt 생성됨: {_appId}");
            }
#endif
        }

        /// <summary>
        /// Steam 초기화 상태 확인 유틸리티
        /// 다른 서비스에서 사용
        /// </summary>
        public static bool CheckInitialized(string callerName = null)
        {
            if (Instance == null || !Instance.IsInitialized)
            {
                string caller = string.IsNullOrEmpty(callerName) ? "Unknown" : callerName;
                SteamLogger.LogWarning(caller, "Steam이 초기화되지 않음");
                return false;
            }
            return true;
        }
    }
}
