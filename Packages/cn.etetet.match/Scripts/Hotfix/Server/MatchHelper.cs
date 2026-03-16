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

        /// <summary>
        /// 指定模式是否启用超时机器人补位
        /// </summary>
        public static bool CanUseRobotFill(MatchQueueComponent queue, int gameMode)
        {
            return GetRequiredPlayerCount(queue, gameMode) > 1;
        }

        /// <summary>
        /// 机器人占位 Id 判定（约定为小于等于0）
        /// </summary>
        public static bool IsRobotPlayerId(long playerId)
        {
            return playerId <= 0;
        }

        /// <summary>
        /// 生成机器人占位 Id
        /// </summary>
        public static long GenerateRobotPlayerId()
        {
            return -IdGenerater.Instance.GenerateId();
        }

        /// <summary>
        /// 按匹配座位顺序为玩家分配出生队伍。
        /// </summary>
        public static int ResolveTeamId(int gameMode, int seatIndex, int totalPlayerCount)
        {
            if (seatIndex < 0)
            {
                return 1;
            }

            return gameMode switch
            {
                GameModeType.PVE => 1,
                GameModeType.OneVsOne => seatIndex + 1,
                GameModeType.ThreeVsThree => seatIndex < totalPlayerCount / 2 ? 1 : 2,
                GameModeType.Extraction => seatIndex + 1,
                _ => 1
            };
        }
    }
}
