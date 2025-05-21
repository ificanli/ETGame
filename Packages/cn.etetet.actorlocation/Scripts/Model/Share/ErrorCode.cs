namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_NotFoundActor = ErrorCore.ERR_WithException + PackageType.ActorLocation * 1000 + 1;
        public const int ERR_RpcFail = ErrorCore.ERR_WithException + PackageType.ActorLocation * 1000 + 2;
        public const int ERR_MessageTimeout = ErrorCore.ERR_WithException + PackageType.ActorLocation * 1000 + 3;
        public const int ERR_ActorLocationSenderTimeout2 = ErrorCore.ERR_WithException + PackageType.ActorLocation * 1000 + 4;
        public const int ERR_ActorLocationSenderTimeout3 = ErrorCore.ERR_WithException + PackageType.ActorLocation * 1000 + 5;
        public const int ERR_ActorLocationSenderTimeout4 = ErrorCore.ERR_WithException + PackageType.ActorLocation * 1000 + 6;
    }
}