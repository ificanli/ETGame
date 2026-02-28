using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// Match取消测试：取消匹配请求
    /// </summary>
    public class Match_Cancel_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Match_Cancel_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            
            // 添加MatchQueueComponent
            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();
            
            // 加入1个请求
            long player1 = 1001;

            long requestId = matchQueue.Enqueue(player1, GameModeType.OneVsOne);
            if (requestId <= 0)
            {
                Log.Console("Match_Cancel_Test: Enqueue failed");
                return 1;
            }
            
            if (matchQueue.GetQueueCount(GameModeType.OneVsOne) != 1)
            {
                Log.Console("Match_Cancel_Test: Queue should have 1 player");
                return 2;
            }
            
            // 调用Cancel
            bool cancelResult = matchQueue.Cancel(requestId);
            if (!cancelResult)
            {
                Log.Console("Match_Cancel_Test: Cancel should succeed");
                return 3;
            }
            
            // 验证请求被移除
            if (matchQueue.GetQueueCount(GameModeType.OneVsOne) != 0)
            {
                Log.Console("Match_Cancel_Test: Queue should be empty after cancel");
                return 4;
            }
            
            // 再次TryMatch不会匹配到已取消的请求
            MatchResult? matchResult = matchQueue.TryMatch(GameModeType.OneVsOne);
            if (matchResult != null)
            {
                Log.Console("Match_Cancel_Test: Should not match cancelled request");
                return 5;
            }
            
            Log.Debug("Match_Cancel_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
