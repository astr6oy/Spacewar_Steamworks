using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam Input 서비스
    /// Steam 컨트롤러 및 게임패드 입력 처리
    /// Xbox, PlayStation, Switch, Steam 컨트롤러 지원
    /// </summary>
    public class SteamInputService : MonoBehaviour, ISteamService
    {
        public static SteamInputService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // 연결된 컨트롤러
        private readonly InputHandle_t[] _controllers = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
        private int _connectedControllerCount;
        public int ConnectedControllerCount => _connectedControllerCount;

        // 액션 세트 핸들
        private InputActionSetHandle_t _mainActionSet;
        private InputActionSetHandle_t _menuActionSet;

        // 디지털 액션 핸들
        private InputDigitalActionHandle_t _fireAction;
        private InputDigitalActionHandle_t _jumpAction;
        private InputDigitalActionHandle_t _pauseAction;

        // 아날로그 액션 핸들
        private InputAnalogActionHandle_t _moveAction;
        private InputAnalogActionHandle_t _lookAction;

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
            if (!SteamManager.CheckInitialized("InputService")) return;

            // Steam Input 초기화
            bool success = SteamInput.Init(false);
            if (!success)
            {
                SteamLogger.LogError("Input", "Steam Input 초기화 실패");
                return;
            }

            // 액션 핸들 로드
            LoadActionHandles();

            // 컨트롤러 목록 갱신
            RefreshControllers();

            IsInitialized = true;
            SteamLogger.Log("Input", $"InputService 초기화 완료 (컨트롤러: {_connectedControllerCount}개)");
        }

        private void Update()
        {
            if (!IsInitialized) return;

            // Steam Input 상태 갱신
            SteamInput.RunFrame();
        }

        /// <summary>
        /// 액션 핸들 로드
        /// </summary>
        private void LoadActionHandles()
        {
            // 액션 세트 (IGA 파일에 정의되어 있어야 함)
            _mainActionSet = SteamInput.GetActionSetHandle("InGameControls");
            _menuActionSet = SteamInput.GetActionSetHandle("MenuControls");

            // 디지털 액션
            _fireAction = SteamInput.GetDigitalActionHandle("fire");
            _jumpAction = SteamInput.GetDigitalActionHandle("jump");
            _pauseAction = SteamInput.GetDigitalActionHandle("pause");

            // 아날로그 액션
            _moveAction = SteamInput.GetAnalogActionHandle("move");
            _lookAction = SteamInput.GetAnalogActionHandle("look");

            SteamLogger.Log("Input", "액션 핸들 로드 완료");
        }

        /// <summary>
        /// 연결된 컨트롤러 목록 갱신
        /// </summary>
        public void RefreshControllers()
        {
            if (!IsInitialized && !SteamManager.CheckInitialized("InputService")) return;

            _connectedControllerCount = SteamInput.GetConnectedControllers(_controllers);
            SteamLogger.Log("Input", $"연결된 컨트롤러: {_connectedControllerCount}개");

            // 각 컨트롤러에 기본 액션 세트 활성화
            for (int i = 0; i < _connectedControllerCount; i++)
            {
                ActivateActionSet(_controllers[i], _mainActionSet);
            }
        }

        /// <summary>
        /// 액션 세트 활성화
        /// </summary>
        public void ActivateActionSet(InputHandle_t controller, InputActionSetHandle_t actionSet)
        {
            SteamInput.ActivateActionSet(controller, actionSet);
        }

        /// <summary>
        /// 모든 컨트롤러에 액션 세트 활성화
        /// </summary>
        public void ActivateActionSetForAll(InputActionSetHandle_t actionSet)
        {
            for (int i = 0; i < _connectedControllerCount; i++)
            {
                ActivateActionSet(_controllers[i], actionSet);
            }
        }

        /// <summary>
        /// 게임 내 컨트롤로 전환
        /// </summary>
        public void SwitchToGameControls()
        {
            ActivateActionSetForAll(_mainActionSet);
            SteamLogger.Log("Input", "게임 컨트롤로 전환");
        }

        /// <summary>
        /// 메뉴 컨트롤로 전환
        /// </summary>
        public void SwitchToMenuControls()
        {
            ActivateActionSetForAll(_menuActionSet);
            SteamLogger.Log("Input", "메뉴 컨트롤로 전환");
        }

        /// <summary>
        /// 디지털 액션 상태 가져오기
        /// </summary>
        public bool GetDigitalAction(InputHandle_t controller, InputDigitalActionHandle_t action)
        {
            var data = SteamInput.GetDigitalActionData(controller, action);
            return data.bState != 0;
        }

        /// <summary>
        /// 디지털 액션 상태 (첫 번째 컨트롤러)
        /// </summary>
        public bool GetDigitalAction(InputDigitalActionHandle_t action)
        {
            if (_connectedControllerCount == 0) return false;
            return GetDigitalAction(_controllers[0], action);
        }

        /// <summary>
        /// 아날로그 액션 상태 가져오기
        /// </summary>
        public Vector2 GetAnalogAction(InputHandle_t controller, InputAnalogActionHandle_t action)
        {
            var data = SteamInput.GetAnalogActionData(controller, action);
            return new Vector2(data.x, data.y);
        }

        /// <summary>
        /// 아날로그 액션 상태 (첫 번째 컨트롤러)
        /// </summary>
        public Vector2 GetAnalogAction(InputAnalogActionHandle_t action)
        {
            if (_connectedControllerCount == 0) return Vector2.zero;
            return GetAnalogAction(_controllers[0], action);
        }

        /// <summary>
        /// Fire 액션 상태
        /// </summary>
        public bool IsFiring()
        {
            return GetDigitalAction(_fireAction);
        }

        /// <summary>
        /// Jump 액션 상태
        /// </summary>
        public bool IsJumping()
        {
            return GetDigitalAction(_jumpAction);
        }

        /// <summary>
        /// Pause 액션 상태
        /// </summary>
        public bool IsPausing()
        {
            return GetDigitalAction(_pauseAction);
        }

        /// <summary>
        /// Move 액션 상태
        /// </summary>
        public Vector2 GetMoveInput()
        {
            return GetAnalogAction(_moveAction);
        }

        /// <summary>
        /// Look 액션 상태
        /// </summary>
        public Vector2 GetLookInput()
        {
            return GetAnalogAction(_lookAction);
        }

        /// <summary>
        /// 컨트롤러 타입 가져오기
        /// </summary>
        public ESteamInputType GetControllerType(int index)
        {
            if (index < 0 || index >= _connectedControllerCount)
                return ESteamInputType.k_ESteamInputType_Unknown;

            return SteamInput.GetInputTypeForHandle(_controllers[index]);
        }

        /// <summary>
        /// 컨트롤러 타입 이름
        /// </summary>
        public string GetControllerTypeName(int index)
        {
            var type = GetControllerType(index);
            return type switch
            {
                ESteamInputType.k_ESteamInputType_SteamController => "Steam Controller",
                ESteamInputType.k_ESteamInputType_XBox360Controller => "Xbox 360",
                ESteamInputType.k_ESteamInputType_XBoxOneController => "Xbox One",
                ESteamInputType.k_ESteamInputType_PS3Controller => "PlayStation 3",
                ESteamInputType.k_ESteamInputType_PS4Controller => "PlayStation 4",
                ESteamInputType.k_ESteamInputType_PS5Controller => "PlayStation 5",
                ESteamInputType.k_ESteamInputType_SwitchProController => "Switch Pro",
                ESteamInputType.k_ESteamInputType_SwitchJoyConSingle => "Joy-Con",
                ESteamInputType.k_ESteamInputType_SwitchJoyConPair => "Joy-Con Pair",
                ESteamInputType.k_ESteamInputType_MobileTouch => "Mobile Touch",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 컨트롤러 핸들 가져오기
        /// </summary>
        public InputHandle_t GetController(int index)
        {
            if (index < 0 || index >= _connectedControllerCount)
                return new InputHandle_t(0);

            return _controllers[index];
        }

        /// <summary>
        /// 진동 피드백
        /// </summary>
        public void TriggerVibration(int controllerIndex, ushort leftSpeed, ushort rightSpeed)
        {
            if (controllerIndex < 0 || controllerIndex >= _connectedControllerCount) return;

            SteamInput.TriggerVibration(_controllers[controllerIndex], leftSpeed, rightSpeed);
        }

        /// <summary>
        /// 모든 컨트롤러 진동
        /// </summary>
        public void TriggerVibrationAll(ushort leftSpeed, ushort rightSpeed)
        {
            for (int i = 0; i < _connectedControllerCount; i++)
            {
                SteamInput.TriggerVibration(_controllers[i], leftSpeed, rightSpeed);
            }
        }

        /// <summary>
        /// 바인딩 설정 UI 열기
        /// </summary>
        public void ShowBindingPanel(int controllerIndex = 0)
        {
            if (controllerIndex < 0 || controllerIndex >= _connectedControllerCount) return;

            bool success = SteamInput.ShowBindingPanel(_controllers[controllerIndex]);
            SteamLogger.LogResult("Input", "바인딩 패널 열기", success);
        }

        public void Shutdown()
        {
            if (IsInitialized)
            {
                SteamInput.Shutdown();
            }

            _connectedControllerCount = 0;
            IsInitialized = false;
            SteamLogger.Log("Input", "InputService 종료");
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
