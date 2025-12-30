using UnityEngine;

namespace SteamworksDemo.Core
{
    /// <summary>
    /// Steam 관련 로그를 구조화된 형태로 출력
    /// 카테고리별로 구분하여 디버깅 편의성 향상
    /// </summary>
    public static class SteamLogger
    {
        // 로그 활성화 여부 (에디터에서 조절 가능)
        public static bool IsEnabled { get; set; } = true;

        // 로그 레벨 필터
        public static LogLevel MinLogLevel { get; set; } = LogLevel.Info;

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        /// <summary>
        /// 일반 정보 로그
        /// </summary>
        public static void Log(string message)
        {
            if (!IsEnabled || MinLogLevel > LogLevel.Info) return;
            Debug.Log($"<color=#88C0D0>[Steam]</color> {message}");
        }

        /// <summary>
        /// 카테고리가 포함된 정보 로그
        /// </summary>
        public static void Log(string category, string message)
        {
            if (!IsEnabled || MinLogLevel > LogLevel.Info) return;
            Debug.Log($"<color=#88C0D0>[Steam:{category}]</color> {message}");
        }

        /// <summary>
        /// 경고 로그
        /// </summary>
        public static void LogWarning(string message)
        {
            if (!IsEnabled || MinLogLevel > LogLevel.Warning) return;
            Debug.LogWarning($"<color=#EBCB8B>[Steam]</color> {message}");
        }

        /// <summary>
        /// 카테고리가 포함된 경고 로그
        /// </summary>
        public static void LogWarning(string category, string message)
        {
            if (!IsEnabled || MinLogLevel > LogLevel.Warning) return;
            Debug.LogWarning($"<color=#EBCB8B>[Steam:{category}]</color> {message}");
        }

        /// <summary>
        /// 에러 로그
        /// </summary>
        public static void LogError(string message)
        {
            if (!IsEnabled) return;
            Debug.LogError($"<color=#BF616A>[Steam]</color> {message}");
        }

        /// <summary>
        /// 카테고리가 포함된 에러 로그
        /// </summary>
        public static void LogError(string category, string message)
        {
            if (!IsEnabled) return;
            Debug.LogError($"<color=#BF616A>[Steam:{category}]</color> {message}");
        }

        /// <summary>
        /// API 호출 결과 로그 (성공/실패 자동 색상 처리)
        /// </summary>
        public static void LogResult(string category, string operation, bool success, string details = null)
        {
            if (!IsEnabled) return;

            string statusColor = success ? "#A3BE8C" : "#BF616A";
            string status = success ? "성공" : "실패";
            string detailText = string.IsNullOrEmpty(details) ? "" : $" - {details}";

            Debug.Log($"<color=#88C0D0>[Steam:{category}]</color> {operation}: <color={statusColor}>{status}</color>{detailText}");
        }
    }
}
