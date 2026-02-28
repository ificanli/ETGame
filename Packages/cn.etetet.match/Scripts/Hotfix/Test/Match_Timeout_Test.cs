using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// Match超时测试：超时清理
    /// </summary>
    public class Match_Timeout_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Match_Timeout_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            EntityRef<Scene> rootRef = root;
            
            // 添加MatchQueueComponent
            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();
            matchQueue.MatchTimeoutMs = 1000; // 设置1秒超时用于测试
            
            // 加入1个请求（1v1模式，凑不齐）
            long player1 = 1001;

            long requestId = matchQueue.Enqueue(player1, GameModeType.OneVsOne);
            if (requestId <= 0)
            {
                Log.Console("Match_Timeout_Test: Enqueue failed");
                return 1;
            }
            
            // 获取请求并手动设置入队时间为过去
            MatchRequest request = matchQueue.GetChild<MatchRequest>(requestId);
            if (request == null)
            {
                Log.Console("Match_Timeout_Test: Request should exist");
                return 2;
            }
            request.EnqueueTime = TimeInfo.Instance.ServerNow() - 2000; // 2秒前
            
            // 调用CleanTimeoutRequests
            matchQueue.CleanTimeoutRequests();
            
            // 验证请求被清理
            root = rootRef;
            if (matchQueue.GetQueueCount(GameModeType.OneVsOne) != 0)
            {
                Log.Console("Match_Timeout_Test: Queue should be empty after timeout");
                return 3;
            }
            
            // 验证请求已被Dispose
            MatchRequest checkRequest = matchQueue.GetChild<MatchRequest>(requestId);
            if (checkRequest != null)
            {
                Log.Console("Match_Timeout_Test: Request should be disposed");
                return 4;
            }
            
            Log.Debug("Match_Timeout_Test passed");
            await ETTask.CompletedTask;
            return ErrorCode.ERR_Success;
        }
    }
}
