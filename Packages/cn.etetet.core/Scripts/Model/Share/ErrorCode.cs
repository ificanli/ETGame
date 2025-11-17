namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_NotFoundActor = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 1; // 100001001
        public const int ERR_RpcFail = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 2; // 100001002
        public const int ERR_MessageTimeout = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 3; // 100001003
        public const int ERR_SessionSendOrRecvTimeout = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 4; // 100001004
        public const int ERR_MessageCountTooMany = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 5; // 100001005
        
        public const int ERR_WithException = 100000000;
    }
}