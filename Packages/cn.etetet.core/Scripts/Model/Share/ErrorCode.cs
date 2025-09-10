namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_NotFoundActor = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 1;
        public const int ERR_RpcFail = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 2;
        public const int ERR_MessageTimeout = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 3;
        public const int ERR_SessionSendOrRecvTimeout = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 4;
        
        public const int ERR_WithException = 100000000;
    }
}