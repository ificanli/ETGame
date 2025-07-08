namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_NotFoundActor = ErrorCore.ERR_WithException + PackageType.Core * 1000 + 1;
        public const int ERR_RpcFail = ErrorCore.ERR_WithException + PackageType.Core * 1000 + 2;
        public const int ERR_MessageTimeout = ErrorCore.ERR_WithException + PackageType.Core * 1000 + 3;
        public const int ERR_SessionSendOrRecvTimeout = ErrorCore.ERR_WithException + PackageType.Core * 1000 + 4;
        
        public const int ERR_WithException = 100000000;
    }
}