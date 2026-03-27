namespace ET
{
    /// <summary>
    /// ECA 撤离状态常量，供服务端与客户端共享。
    /// </summary>
    public static class ECAEvacuationState
    {
        public const int None = 0;
        public const int Running = 1;
        public const int Completed = 2;
        public const int Cancelled = 3;
    }
}
