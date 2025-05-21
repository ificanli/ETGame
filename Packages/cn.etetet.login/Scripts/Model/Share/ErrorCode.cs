namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ConnectGateKeyError = ErrorCore.ERR_WithException + PackageType.Login * 1000 + 1;
    }
}