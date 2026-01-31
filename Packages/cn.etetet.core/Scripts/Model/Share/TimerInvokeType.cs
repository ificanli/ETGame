namespace ET
{
    [UniqueId]
    public static partial class TimerInvokeType
    {
        public const int SessionAcceptTimeout = PackageType.Core * 1000 + 1;
        public const int SessionIdleChecker = PackageType.Core * 1000 + 2;
        
        // Test
        public const int TestOnceTimer = PackageType.Core * 1000 + 100;
        public const int TestRepeatedTimer = PackageType.Core * 1000 + 101;
    }
}