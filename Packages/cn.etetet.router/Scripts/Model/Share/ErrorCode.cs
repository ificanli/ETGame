namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_KcpRouterConnectFail = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 1;
        public const int ERR_KcpRouterTimeout = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 2;
        public const int ERR_KcpRouterSame = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 3;
        public const int ERR_KcpRouterRouterSyncCountTooMuchTimes = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 4;
        public const int ERR_KcpRouterTooManyPackets = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 5;
        public const int ERR_KcpRouterSyncCountTooMuchTimes = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 6;
    }
}