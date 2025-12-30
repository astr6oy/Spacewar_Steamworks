namespace SteamworksDemo.Core
{
    /// <summary>
    /// 모든 Steam 서비스가 구현해야 하는 기본 인터페이스
    /// 초기화와 정리 로직의 일관성을 보장
    /// </summary>
    public interface ISteamService
    {
        /// <summary>
        /// 서비스 초기화 여부
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 서비스 초기화
        /// SteamManager 초기화 이후에 호출됨
        /// </summary>
        void Initialize();

        /// <summary>
        /// 서비스 정리
        /// 앱 종료 시 호출됨
        /// </summary>
        void Shutdown();
    }
}
