using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// Match搜打撤模式测试：4人匹配
    /// </summary>
    public class Match_Extraction_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Match_Extraction_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            
            // 添加MatchQueueComponent
            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();
            
            // 设置搜打撤模式人数为4（用于测试）
            matchQueue.ModePlayerCountDict[GameModeType.Extraction] = 4;
            
            // 加入4个搜打撤请求
            long[] playerIds = new long[] { 1001, 1002, 1003, 1004 };

            foreach (long playerId in playerIds)
            {
                long requestId = matchQueue.Enqueue(playerId, GameModeType.Extraction);
                if (requestId <= 0)
                {
                    Log.Console($"Match_Extraction_Test: Player {playerId} enqueue failed");
                    return 1;
                }
            }
            
            if (matchQueue.GetQueueCount(GameModeType.Extraction) != 4)
            {
                Log.Console("Match_Extraction_Test: Queue should have 4 players");
                return 2;
            }
            
            // 调用TryMatch
            MatchResult? result = matchQueue.TryMatch(GameModeType.Extraction);
            
            // 验证匹配成功
            if (result == null)
            {
                Log.Console("Match_Extraction_Test: Match should succeed with 4 players");
                return 3;
            }
            
            if (result.Value.PlayerIds.Count != 4)
            {
                Log.Console($"Match_Extraction_Test: Should have 4 players, got {result.Value.PlayerIds.Count}");
                return 4;
            }
            
            if (result.Value.GameMode != GameModeType.Extraction)
            {
                Log.Console("Match_Extraction_Test: GameMode should be Extraction");
                return 5;
            }
            
            // 验证所有玩家都在结果中
            foreach (long playerId in playerIds)
            {
                if (!result.Value.PlayerIds.Contains(playerId))
                {
                    Log.Console($"Match_Extraction_Test: Should contain player {playerId}");
                    return 6;
                }
            }
            
            // 验证队列已清空
            if (matchQueue.GetQueueCount(GameModeType.Extraction) != 0)
            {
                Log.Console("Match_Extraction_Test: Queue should be empty after match");
                return 7;
            }
            
            Log.Debug("Match_Extraction_Test passed");
            await ETTask.CompletedTask;
            return ErrorCode.ERR_Success;
        }
    }
}
