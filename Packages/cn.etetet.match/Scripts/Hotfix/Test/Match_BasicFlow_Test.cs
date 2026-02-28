using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// Match基础流程测试：2个玩家1v1匹配
    /// </summary>
    public class Match_BasicFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Match_BasicFlow_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            
            // 添加MatchQueueComponent
            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();
            
            // 加入2个1v1请求
            long player1 = 1001;
            long player2 = 1002;

            long requestId1 = matchQueue.Enqueue(player1, GameModeType.OneVsOne);
            long requestId2 = matchQueue.Enqueue(player2, GameModeType.OneVsOne);
            
            if (requestId1 <= 0 || requestId2 <= 0)
            {
                Log.Console("Match_BasicFlow_Test: Enqueue failed");
                return 1;
            }
            
            // 调用TryMatch
            MatchResult? result = matchQueue.TryMatch(GameModeType.OneVsOne);
            
            // 验证匹配成功
            if (result == null)
            {
                Log.Console("Match_BasicFlow_Test: Match should succeed with 2 players");
                return 2;
            }
            
            if (result.Value.PlayerIds.Count != 2)
            {
                Log.Console($"Match_BasicFlow_Test: Should have 2 players, got {result.Value.PlayerIds.Count}");
                return 3;
            }
            
            if (!result.Value.PlayerIds.Contains(player1) || !result.Value.PlayerIds.Contains(player2))
            {
                Log.Console("Match_BasicFlow_Test: Should contain both players");
                return 4;
            }
            
            if (result.Value.GameMode != GameModeType.OneVsOne)
            {
                Log.Console("Match_BasicFlow_Test: GameMode should be 1v1");
                return 5;
            }
            
            // 验证请求已被清理
            if (matchQueue.GetQueueCount(GameModeType.OneVsOne) != 0)
            {
                Log.Console("Match_BasicFlow_Test: Queue should be empty after match");
                return 6;
            }
            
            Log.Debug("Match_BasicFlow_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
