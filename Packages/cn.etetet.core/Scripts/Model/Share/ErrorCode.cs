namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_SessionSendOrRecvTimeout = ErrorCore.ERR_WithException + PackageType.Core * 1000 + 1;
    }
}