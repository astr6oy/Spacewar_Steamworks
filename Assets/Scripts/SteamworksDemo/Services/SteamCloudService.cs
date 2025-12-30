using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using SteamworksDemo.Core;

namespace SteamworksDemo.Services
{
    /// <summary>
    /// Steam Cloud 서비스
    /// 클라우드 파일 저장/로드/삭제
    /// </summary>
    public class SteamCloudService : MonoBehaviour, ISteamService
    {
        public static SteamCloudService Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Steam Cloud 활성화 여부
        /// </summary>
        public bool IsCloudEnabled
        {
            get
            {
                if (!SteamManager.CheckInitialized("CloudService")) return false;
                return SteamRemoteStorage.IsCloudEnabledForAccount() &&
                       SteamRemoteStorage.IsCloudEnabledForApp();
            }
        }

        /// <summary>
        /// 저장된 파일 수
        /// </summary>
        public int FileCount
        {
            get
            {
                if (!SteamManager.CheckInitialized("CloudService")) return 0;
                return SteamRemoteStorage.GetFileCount();
            }
        }

        /// <summary>
        /// 사용 중인 용량 (bytes)
        /// </summary>
        public ulong UsedQuota { get; private set; }

        /// <summary>
        /// 전체 용량 (bytes)
        /// </summary>
        public ulong TotalQuota { get; private set; }

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
            if (!SteamManager.CheckInitialized("CloudService")) return;

            RefreshQuotaInfo();

            IsInitialized = true;
            SteamLogger.Log("Cloud", $"CloudService 초기화 완료 (활성: {IsCloudEnabled})");
            SteamLogger.Log("Cloud", $"용량: {FormatBytes(UsedQuota)} / {FormatBytes(TotalQuota)}");
        }

        /// <summary>
        /// 파일 쓰기
        /// </summary>
        public bool WriteFile(string fileName, byte[] data)
        {
            if (!SteamManager.CheckInitialized("CloudService")) return false;

            bool success = SteamRemoteStorage.FileWrite(fileName, data, data.Length);
            SteamLogger.LogResult("Cloud", $"파일 쓰기: {fileName} ({data.Length} bytes)", success);

            if (success)
            {
                RefreshQuotaInfo();
                SteamEventBus.RaiseCloudFileWritten(fileName);
            }

            return success;
        }

        /// <summary>
        /// 문자열로 파일 쓰기 (편의 메서드)
        /// </summary>
        public bool WriteFile(string fileName, string content)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
            return WriteFile(fileName, data);
        }

        /// <summary>
        /// 파일 읽기
        /// </summary>
        public byte[] ReadFile(string fileName)
        {
            if (!SteamManager.CheckInitialized("CloudService")) return null;

            if (!FileExists(fileName))
            {
                SteamLogger.LogWarning("Cloud", $"파일 없음: {fileName}");
                return null;
            }

            int fileSize = SteamRemoteStorage.GetFileSize(fileName);
            byte[] data = new byte[fileSize];

            int bytesRead = SteamRemoteStorage.FileRead(fileName, data, fileSize);

            if (bytesRead > 0)
            {
                SteamLogger.Log("Cloud", $"파일 읽기 성공: {fileName} ({bytesRead} bytes)");
                return data;
            }

            SteamLogger.LogError("Cloud", $"파일 읽기 실패: {fileName}");
            return null;
        }

        /// <summary>
        /// 문자열로 파일 읽기 (편의 메서드)
        /// </summary>
        public string ReadFileAsString(string fileName)
        {
            byte[] data = ReadFile(fileName);
            if (data == null) return null;
            return System.Text.Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// 파일 삭제
        /// </summary>
        public bool DeleteFile(string fileName)
        {
            if (!SteamManager.CheckInitialized("CloudService")) return false;

            bool success = SteamRemoteStorage.FileDelete(fileName);
            SteamLogger.LogResult("Cloud", $"파일 삭제: {fileName}", success);

            if (success)
            {
                RefreshQuotaInfo();
                SteamEventBus.RaiseCloudFileDeleted(fileName);
            }

            return success;
        }

        /// <summary>
        /// 파일 존재 여부 확인
        /// </summary>
        public bool FileExists(string fileName)
        {
            if (!SteamManager.CheckInitialized("CloudService")) return false;
            return SteamRemoteStorage.FileExists(fileName);
        }

        /// <summary>
        /// 파일 크기 가져오기
        /// </summary>
        public int GetFileSize(string fileName)
        {
            if (!SteamManager.CheckInitialized("CloudService")) return 0;
            return SteamRemoteStorage.GetFileSize(fileName);
        }

        /// <summary>
        /// 모든 파일 목록 가져오기
        /// </summary>
        public List<CloudFileInfo> GetAllFiles()
        {
            var files = new List<CloudFileInfo>();
            if (!SteamManager.CheckInitialized("CloudService")) return files;

            int count = SteamRemoteStorage.GetFileCount();

            for (int i = 0; i < count; i++)
            {
                int fileSize;
                string fileName = SteamRemoteStorage.GetFileNameAndSize(i, out fileSize);

                files.Add(new CloudFileInfo
                {
                    FileName = fileName,
                    Size = fileSize,
                    Timestamp = SteamRemoteStorage.GetFileTimestamp(fileName)
                });
            }

            return files;
        }

        /// <summary>
        /// 용량 정보 갱신
        /// </summary>
        public void RefreshQuotaInfo()
        {
            if (!SteamManager.CheckInitialized("CloudService")) return;

            ulong totalBytes, usedBytes;
            SteamRemoteStorage.GetQuota(out totalBytes, out usedBytes);
            TotalQuota = totalBytes;
            UsedQuota = usedBytes;
        }

        /// <summary>
        /// 바이트를 읽기 쉬운 형태로 변환
        /// </summary>
        public static string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public void Shutdown()
        {
            IsInitialized = false;
            SteamLogger.Log("Cloud", "CloudService 종료");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>
    /// Cloud 파일 정보
    /// </summary>
    public class CloudFileInfo
    {
        public string FileName { get; set; }
        public int Size { get; set; }
        public long Timestamp { get; set; }

        public string GetFormattedSize()
        {
            return SteamCloudService.FormatBytes((ulong)Size);
        }

        public System.DateTime GetDateTime()
        {
            return new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
                .AddSeconds(Timestamp)
                .ToLocalTime();
        }
    }
}
