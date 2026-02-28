namespace ET.Server
{
    public static class MatchHelper
    {
        /// <summary>
        /// 获取指定模式需要的玩家数量
        /// </summary>
        public static int GetRequiredPlayerCount(MatchQueueComponent queue, int gameMode)
        {
            if (queue.ModePlayerCountDict.TryGetValue(gameMode, out int count))
            {
                return count;
            }
            return 1; // 默认单人
        }
    }
}
