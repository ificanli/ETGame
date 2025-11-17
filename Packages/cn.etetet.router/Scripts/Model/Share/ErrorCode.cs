namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_KcpRouterConnectFail = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 1; // 100015001
        public const int ERR_KcpRouterTimeout = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 2; // 100015002
        public const int ERR_KcpRouterSame = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 3; // 100015003
        public const int ERR_KcpRouterRouterSyncCountTooMuchTimes = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 4; // 100015004
        public const int ERR_KcpRouterTooManyPackets = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 5; // 100015005
        public const int ERR_KcpRouterSyncCountTooMuchTimes = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 6; // 100015006
    }
}