using System.Linq;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// Match人机补位测试：3v3模式下单人超时后补机器人开局
    /// </summary>
    public class Match_RobotFill_ThreeVsThree_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Match_RobotFill_ThreeVsThree_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();

            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();

            long playerId = 2001;
            long requestId = matchQueue.Enqueue(playerId, GameModeType.ThreeVsThree);
            if (requestId <= 0)
            {
                Log.Console("Match_RobotFill_ThreeVsThree_Test: Enqueue failed");
                return 1;
            }

            MatchRequest request = matchQueue.GetChild<MatchRequest>(requestId);
            if (request == null)
            {
                Log.Console("Match_RobotFill_ThreeVsThree_Test: Request should exist");
                return 2;
            }

            request.EnqueueTime = TimeInfo.Instance.ServerNow() - matchQueue.MatchTimeoutMs - 1;

            MatchResult? robotFillResult = matchQueue.TryMatchWithRobot(GameModeType.ThreeVsThree);
            if (robotFillResult == null)
            {
                Log.Console("Match_RobotFill_ThreeVsThree_Test: Robot fill match should succeed");
                return 3;
            }

            int requiredCount = MatchHelper.GetRequiredPlayerCount(matchQueue, GameModeType.ThreeVsThree);
            if (robotFillResult.Value.PlayerIds.Count != requiredCount)
            {
                Log.Console(
                    $"Match_RobotFill_ThreeVsThree_Test: Should have {requiredCount} ids, got {robotFillResult.Value.PlayerIds.Count}");
                return 4;
            }

            if (!robotFillResult.Value.PlayerIds.Contains(playerId))
            {
                Log.Console("Match_RobotFill_ThreeVsThree_Test: Should contain real player");
                return 5;
            }

            int robotCount = robotFillResult.Value.PlayerIds.Count(MatchHelper.IsRobotPlayerId);
            if (robotCount != requiredCount - 1)
            {
                Log.Console(
                    $"Match_RobotFill_ThreeVsThree_Test: Should contain {requiredCount - 1} robot ids, got {robotCount}");
                return 6;
            }

            if (matchQueue.GetQueueCount(GameModeType.ThreeVsThree) != 0)
            {
                Log.Console("Match_RobotFill_ThreeVsThree_Test: Queue should be empty after robot fill");
                return 7;
            }

            Log.Debug("Match_RobotFill_ThreeVsThree_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
