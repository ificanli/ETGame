namespace ET
{
    /// <summary>
    /// 匹配状态
    /// </summary>
    public static class MatchState
    {
        public const int Waiting = 0;    // 等待中
        public const int Matched = 1;    // 匹配成功
        public const int Timeout = 2;    // 超时
        public const int Cancelled = 3;  // 已取消
    }
}
