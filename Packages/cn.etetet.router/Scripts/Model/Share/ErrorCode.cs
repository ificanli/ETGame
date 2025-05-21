namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_KcpRouterConnectFail = ErrorCore.ERR_WithException + PackageType.Router * 1000 + 1;
        public const int ERR_KcpRouterTimeout = ErrorCore.ERR_WithException + PackageType.Router * 1000 + 2;
        public const int ERR_KcpRouterSame = ErrorCore.ERR_WithException + PackageType.Router * 1000 + 3;
        public const int ERR_KcpRouterRouterSyncCountTooMuchTimes = ErrorCore.ERR_WithException + PackageType.Router * 1000 + 4;
        public const int ERR_KcpRouterTooManyPackets = ErrorCore.ERR_WithException + PackageType.Router * 1000 + 5;
        public const int ERR_KcpRouterSyncCountTooMuchTimes = ErrorCore.ERR_WithException + PackageType.Router * 1000 + 6;
    }
}