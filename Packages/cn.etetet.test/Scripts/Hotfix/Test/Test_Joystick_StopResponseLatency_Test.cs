using ET.Client;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_Joystick_StopResponseLatency_Normal_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            return await JoystickStopLatencyScenarioRunner.Run(
                context,
                "normal",
                initialMoveBurstCount: 1,
                staleReplayCount: 0,
                preStopMoveDurationMs: 120,
                stopTimeoutMs: 400,
                pollIntervalMs: 5,
                stableStoppedWaitMs: 80,
                maxAcceptedStopLatencyMs: 120,
                requireStartObserved: true);
        }
    }

    public class Test_Joystick_StopResponseLatency_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            return await JoystickStopLatencyScenarioRunner.Run(
                context,
                "pressure",
                initialMoveBurstCount: 32,
                staleReplayCount: 12,
                preStopMoveDurationMs: 0,
                stopTimeoutMs: 800,
                pollIntervalMs: 10,
                stableStoppedWaitMs: 120,
                maxAcceptedStopLatencyMs: 300,
                requireStartObserved: false);
        }
    }

    internal static class JoystickStopLatencyScenarioRunner
    {
        private const float DirectionStoppedEpsilonSqr = 0.0001f;

        public static async ETTask<int> Run(
            TestContext context,
            string scenarioName,
            int initialMoveBurstCount,
            int staleReplayCount,
            int preStopMoveDurationMs,
            int stopTimeoutMs,
            int pollIntervalMs,
            int stableStoppedWaitMs,
            int maxAcceptedStopLatencyMs,
            bool requireStartObserved)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Joystick_StopResponseLatency_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Joystick_StopResponseLatency_Test));
            Scene clientRoot = robot.Root;
            if (clientRoot == null || clientRoot.IsDisposed)
            {
                Log.Console("client root is null after robot create");
                return 1;
            }

            ClientSenderComponent sender = clientRoot.GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                Log.Console("client sender is null");
                return 2;
            }
            EntityRef<ClientSenderComponent> senderRef = sender;

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null || serverUnit.IsDisposed)
            {
                Log.Console("server unit is null after robot enter map");
                return 3;
            }

            Scene mapScene = serverUnit.Scene();
            if (mapScene == null || mapScene.IsDisposed || mapScene.TimerComponent == null)
            {
                Log.Console("map scene or timer component is null");
                return 4;
            }

            EntityRef<Unit> serverUnitRef = serverUnit;
            EntityRef<Scene> mapSceneRef = mapScene;
            JoystickMoveComponent joystickMove = serverUnit.GetComponent<JoystickMoveComponent>() ?? serverUnit.AddComponent<JoystickMoveComponent>();
            joystickMove.LastInputProcessTime = TimeInfo.Instance.ServerNow();

            uint nextSequence = 1;
            ClientSenderComponent liveSender = senderRef;
            if (liveSender == null || liveSender.IsDisposed)
            {
                Log.Console($"[{scenarioName}] client sender disposed before input send");
                return 5;
            }

            for (int i = 0; i < initialMoveBurstCount; ++i)
            {
                GetDeterministicDirection(i, out float dirX, out float dirZ);
                SendJoystickInput(liveSender, dirX, dirZ, nextSequence);
                nextSequence++;
            }

            if (requireStartObserved && initialMoveBurstCount > 0)
            {
                uint startSequence = nextSequence - 1;
                long startObservedAt = await WaitForMoveStart(mapSceneRef, serverUnitRef, startSequence, stopTimeoutMs, pollIntervalMs);
                if (startObservedAt == 0)
                {
                    serverUnit = serverUnitRef;
                    joystickMove = serverUnit?.GetComponent<JoystickMoveComponent>();
                    Log.Console(
                        $"[{scenarioName}] move start was not observed in time, startSeq={startSequence}, lastSeq={joystickMove?.LastClientInputSequence ?? 0}, dir={joystickMove?.Direction.ToString() ?? "null"}");
                    return 6;
                }
            }

            mapScene = mapSceneRef;
            if (preStopMoveDurationMs > 0)
            {
                await mapScene.TimerComponent.WaitAsync(preStopMoveDurationMs);
            }

            liveSender = senderRef;
            if (liveSender == null || liveSender.IsDisposed)
            {
                Log.Console($"[{scenarioName}] client sender disposed before stop send");
                return 7;
            }

            long stopSentAt = TimeInfo.Instance.ServerNow();
            uint stopSequence = nextSequence;
            SendJoystickInput(liveSender, 0f, 0f, stopSequence);
            nextSequence++;

            for (int i = 0; i < staleReplayCount; ++i)
            {
                uint staleSequence = stopSequence - (uint)staleReplayCount + (uint)i;
                GetDeterministicDirection(i + initialMoveBurstCount * 2, out float staleDirX, out float staleDirZ);
                SendJoystickInput(liveSender, staleDirX, staleDirZ, staleSequence);
            }

            long stopObservedAt = await WaitForStop(mapSceneRef, serverUnitRef, stopSequence, stopTimeoutMs, pollIntervalMs);
            if (stopObservedAt == 0)
            {
                serverUnit = serverUnitRef;
                joystickMove = serverUnit?.GetComponent<JoystickMoveComponent>();
                Log.Console(
                    $"[{scenarioName}] stop was not observed in time, stopSeq={stopSequence}, lastSeq={joystickMove?.LastClientInputSequence ?? 0}, dir={joystickMove?.Direction.ToString() ?? "null"}");
                return 8;
            }

            long stopAcceptLatency = stopObservedAt - stopSentAt;
            if (stopAcceptLatency > maxAcceptedStopLatencyMs)
            {
                Log.Console(
                    $"[{scenarioName}] stop accept latency too high: latency={stopAcceptLatency}ms, limit={maxAcceptedStopLatencyMs}ms, stopSeq={stopSequence}");
                return 9;
            }

            mapScene = mapSceneRef;
            await mapScene.TimerComponent.WaitAsync(stableStoppedWaitMs);

            serverUnit = serverUnitRef;
            if (serverUnit == null || serverUnit.IsDisposed)
            {
                Log.Console($"[{scenarioName}] server unit disposed during stable stop wait");
                return 10;
            }

            joystickMove = serverUnit.GetComponent<JoystickMoveComponent>();
            if (!IsDirectionStopped(joystickMove))
            {
                Log.Console(
                    $"[{scenarioName}] unit direction changed after stop, stopSeq={stopSequence}, lastSeq={joystickMove?.LastClientInputSequence ?? 0}, dir={joystickMove?.Direction.ToString() ?? "null"}");
                return 11;
            }

            if (joystickMove.LastClientInputSequence != stopSequence)
            {
                Log.Console(
                    $"[{scenarioName}] last accepted input seq mismatch: expected={stopSequence}, actual={joystickMove.LastClientInputSequence}");
                return 12;
            }

            Log.Console(
                $"Joystick stop accept latency [{scenarioName}] passed: stopSeq={stopSequence}, burst={initialMoveBurstCount}, staleReplay={staleReplayCount}, latency={stopAcceptLatency}ms, lastSeq={joystickMove.LastClientInputSequence}");
            return ErrorCode.ERR_Success;
        }

        private static void SendJoystickInput(ClientSenderComponent sender, float dirX, float dirZ, uint sequence)
        {
            C2M_JoystickInput message = C2M_JoystickInput.Create();
            message.DirX = dirX;
            message.DirZ = dirZ;
            message.InputSequence = sequence;
            sender.Send(message);
        }

        private static async ETTask<long> WaitForMoveStart(
            EntityRef<Scene> mapSceneRef,
            EntityRef<Unit> serverUnitRef,
            uint startSequence,
            int timeoutMs,
            int pollIntervalMs)
        {
            long deadline = TimeInfo.Instance.ServerNow() + timeoutMs;
            while (TimeInfo.Instance.ServerNow() <= deadline)
            {
                Unit serverUnit = serverUnitRef;
                if (serverUnit != null && !serverUnit.IsDisposed)
                {
                    JoystickMoveComponent joystickMove = serverUnit.GetComponent<JoystickMoveComponent>();
                    if (joystickMove != null &&
                        joystickMove.LastClientInputSequence >= startSequence &&
                        !IsDirectionStopped(joystickMove))
                    {
                        return TimeInfo.Instance.ServerNow();
                    }
                }

                Scene mapScene = mapSceneRef;
                if (mapScene == null || mapScene.IsDisposed || mapScene.TimerComponent == null)
                {
                    return 0;
                }

                await mapScene.TimerComponent.WaitAsync(pollIntervalMs);
            }

            return 0;
        }

        private static async ETTask<long> WaitForStop(
            EntityRef<Scene> mapSceneRef,
            EntityRef<Unit> serverUnitRef,
            uint stopSequence,
            int timeoutMs,
            int pollIntervalMs)
        {
            long deadline = TimeInfo.Instance.ServerNow() + timeoutMs;
            while (TimeInfo.Instance.ServerNow() <= deadline)
            {
                Unit serverUnit = serverUnitRef;
                if (serverUnit != null && !serverUnit.IsDisposed)
                {
                    JoystickMoveComponent joystickMove = serverUnit.GetComponent<JoystickMoveComponent>();
                    if (joystickMove != null &&
                        joystickMove.LastClientInputSequence >= stopSequence &&
                        IsDirectionStopped(joystickMove))
                    {
                        return TimeInfo.Instance.ServerNow();
                    }
                }

                Scene mapScene = mapSceneRef;
                if (mapScene == null || mapScene.IsDisposed || mapScene.TimerComponent == null)
                {
                    return 0;
                }

                await mapScene.TimerComponent.WaitAsync(pollIntervalMs);
            }

            return 0;
        }

        private static bool IsDirectionStopped(JoystickMoveComponent joystickMove)
        {
            return joystickMove != null &&
                math.lengthsq(joystickMove.Direction) <= DirectionStoppedEpsilonSqr;
        }

        private static void GetDeterministicDirection(int index, out float dirX, out float dirZ)
        {
            float angle = math.radians((index * 137) % 360);
            dirX = math.cos(angle);
            dirZ = math.sin(angle);
        }
    }
}
