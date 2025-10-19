namespace ET
{
    [UniqueId]
    public static partial class TimerInvokeType
    {
        public const int SessionAcceptTimeout = PackageType.Core * 1000 + 1;
        public const int SessionIdleChecker = PackageType.Core * 1000 + 2;
    }
}