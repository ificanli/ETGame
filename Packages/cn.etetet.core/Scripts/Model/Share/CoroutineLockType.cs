namespace ET
{
    public static partial class CoroutineLockType
    {
        public const int Mailbox = PackageType.Core * 1000 + 1;                   // Mailbox中队列
        public const int TestLock = PackageType.Core * 1000 + 100;                // 测试用锁
    }
}