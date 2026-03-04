using System.Linq;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// Match人机补位测试：1v1模式下单人超时后补机器人开局
    /// </summary>
    public class Match_RobotFill_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Match_RobotFill_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();

            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();

            long playerId = 1001;
            long requestId = matchQueue.Enqueue(playerId, GameModeType.OneVsOne);
            if (requestId <= 0)
            {
                Log.Console("Match_RobotFill_Test: Enqueue failed");
                return 1;
            }

            MatchRequest request = matchQueue.GetChild<MatchRequest>(requestId);
            if (request == null)
            {
                Log.Console("Match_RobotFill_Test: Request should exist");
                return 2;
            }

            request.EnqueueTime = TimeInfo.Instance.ServerNow() - matchQueue.MatchTimeoutMs - 1;

            // 普通匹配不足真人人数，应失败
            MatchResult? normalResult = matchQueue.TryMatch(GameModeType.OneVsOne);
            if (normalResult != null)
            {
                Log.Console("Match_RobotFill_Test: Normal match should fail with single player");
                return 3;
            }

            // 人机补位匹配应成功
            MatchResult? robotFillResult = matchQueue.TryMatchWithRobot(GameModeType.OneVsOne);
            if (robotFillResult == null)
            {
                Log.Console("Match_RobotFill_Test: Robot fill match should succeed");
                return 4;
            }

            if (robotFillResult.Value.PlayerIds.Count != 2)
            {
                Log.Console($"Match_RobotFill_Test: Should have 2 ids, got {robotFillResult.Value.PlayerIds.Count}");
                return 5;
            }

            if (!robotFillResult.Value.PlayerIds.Contains(playerId))
            {
                Log.Console("Match_RobotFill_Test: Should contain real player");
                return 6;
            }

            int robotCount = robotFillResult.Value.PlayerIds.Count(MatchHelper.IsRobotPlayerId);
            if (robotCount != 1)
            {
                Log.Console($"Match_RobotFill_Test: Should contain exactly 1 robot id, got {robotCount}");
                return 7;
            }

            if (matchQueue.GetQueueCount(GameModeType.OneVsOne) != 0)
            {
                Log.Console("Match_RobotFill_Test: Queue should be empty after robot fill");
                return 8;
            }

            if (matchQueue.GetChild<MatchRequest>(requestId) != null)
            {
                Log.Console("Match_RobotFill_Test: Request should be disposed");
                return 9;
            }

            Log.Debug("Match_RobotFill_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
