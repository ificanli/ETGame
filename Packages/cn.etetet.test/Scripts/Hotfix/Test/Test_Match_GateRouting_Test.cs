using ET.Server;

namespace ET.Test
{
    public class Test_Match_GateRouting_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Test_Match_GateRouting_Test));

            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();

            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();

            const long gate1 = 1101;
            const long gate2 = 2202;
            const long player1 = 10001;
            const long player2 = 10002;

            long requestId1 = matchQueue.Enqueue(player1, GameModeType.OneVsOne, gate1);
            long requestId2 = matchQueue.Enqueue(player2, GameModeType.OneVsOne, gate2);
            if (requestId1 <= 0 || requestId2 <= 0)
            {
                Log.Console("enqueue failed");
                return 1;
            }

            MatchRequest request1 = matchQueue.GetChild<MatchRequest>(requestId1);
            MatchRequest request2 = matchQueue.GetChild<MatchRequest>(requestId2);
            if (request1 == null || request2 == null)
            {
                Log.Console("match request missing");
                return 2;
            }

            if (request1.GateActorId != gate1 || request2.GateActorId != gate2)
            {
                Log.Console($"gate actor id mismatch, request1={request1.GateActorId}, request2={request2.GateActorId}");
                return 3;
            }

            MatchResult? result = matchQueue.TryMatch(GameModeType.OneVsOne);
            if (result == null)
            {
                Log.Console("match should succeed");
                return 4;
            }

            if (result.Value.GatePlayerIds == null || result.Value.GatePlayerIds.Count != 2)
            {
                Log.Console($"gate grouping count mismatch: {result.Value.GatePlayerIds?.Count ?? 0}");
                return 5;
            }

            if (!result.Value.GatePlayerIds.TryGetValue(gate1, out var gate1Players) || gate1Players.Count != 1 || gate1Players[0] != player1)
            {
                Log.Console("gate1 routing mismatch");
                return 6;
            }

            if (!result.Value.GatePlayerIds.TryGetValue(gate2, out var gate2Players) || gate2Players.Count != 1 || gate2Players[0] != player2)
            {
                Log.Console("gate2 routing mismatch");
                return 7;
            }

            if (!result.Value.PlayerTeamIds.TryGetValue(player1, out int team1) || team1 != 1)
            {
                Log.Console($"player1 team mismatch: {team1}");
                return 8;
            }

            if (!result.Value.PlayerTeamIds.TryGetValue(player2, out int team2) || team2 != 2)
            {
                Log.Console($"player2 team mismatch: {team2}");
                return 9;
            }

            if (result.Value.GatePlayerIds.ContainsKey(3303))
            {
                Log.Console("unrelated gate should not be included");
                return 10;
            }

            if (result.Value.RobotPlayerIds == null || result.Value.RobotPlayerIds.Count != 0)
            {
                Log.Console("normal match should not contain robot ids");
                return 11;
            }

            if (result.Value.HumanPlayerIds == null || result.Value.HumanPlayerIds.Count != 2)
            {
                Log.Console($"human player count mismatch: {result.Value.HumanPlayerIds?.Count ?? 0}");
                return 12;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
