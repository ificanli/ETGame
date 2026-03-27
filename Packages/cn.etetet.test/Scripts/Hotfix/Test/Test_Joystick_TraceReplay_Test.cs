using System;
using System.Collections.Generic;
using System.Globalization;
using ET.Client;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_Joystick_TraceReplay_Smoke_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            const string traceText =
@"# deltaMs|seq|dirX|dirZ
0|1|1.000000|0.000000
32|2|0.707107|0.707107
32|3|0.000000|0.000000";

            return await JoystickTraceReplayScenarioRunner.Run(
                context,
                "smoke",
                traceText,
                stopTimeoutMs: 500,
                pollIntervalMs: 5,
                stableStoppedWaitMs: 80,
                maxAcceptedStopLatencyMs: 180,
                maxPostStopTravelDistance: 0.25f);
        }
    }

    public class Test_Joystick_TraceReplay_LogSeq29_StopTail_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            const string traceText =
@"[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512441378, seq=26, dirX=-0.682037, dirZ=-0.731318, source=keyboard, reason=force
[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512442129, seq=27, dirX=-0.999393, dirZ=-0.034847, source=keyboard, reason=force
[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512442329, seq=28, dirX=-0.731318, dirZ=0.682037, source=keyboard, reason=force
[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512445309, seq=29, dirX=0.000000, dirZ=0.000000, source=idle, reason=force";

            return await JoystickTraceReplayScenarioRunner.Run(
                context,
                "log-seq29-stop-tail",
                traceText,
                stopTimeoutMs: 1000,
                pollIntervalMs: 5,
                stableStoppedWaitMs: 80,
                maxAcceptedStopLatencyMs: 350,
                maxPostStopTravelDistance: 1.20f);
        }
    }

    public class Test_Joystick_TraceTimeline_LogSeq29_StopTail_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            const string traceText =
@"[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512441378, seq=26, dirX=-0.682037, dirZ=-0.731318, source=keyboard, reason=force
[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512442129, seq=27, dirX=-0.999393, dirZ=-0.034847, source=keyboard, reason=force
[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512442329, seq=28, dirX=-0.731318, dirZ=0.682037, source=keyboard, reason=force
[NavMove][TraceInputSend] unitId=322183624654868, clientNow=1774512445309, seq=29, dirX=0.000000, dirZ=0.000000, source=idle, reason=force
[NavMove][TraceServerInputRecv] unitId=322183624654868, serverNow=1774512441543, seq=26, dirX=-0.682037, dirZ=-0.731318, stop=False, lastSeqBefore=25, lastSeqAfter=26
[NavMove][TraceServerInputRecv] unitId=322183624654868, serverNow=1774512442290, seq=27, dirX=-0.999393, dirZ=-0.034847, stop=False, lastSeqBefore=26, lastSeqAfter=27
[NavMove][TraceServerInputRecv] unitId=322183624654868, serverNow=1774512442592, seq=28, dirX=-0.731318, dirZ=0.682037, stop=False, lastSeqBefore=27, lastSeqAfter=28
[NavMove][TraceServerInputRecv] unitId=322183624654868, serverNow=1774512445514, seq=29, dirX=0.000000, dirZ=0.000000, stop=True, lastSeqBefore=28, lastSeqAfter=29
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445311, moveSeq=590, ackInputSeq=28, posX=60.614550, posY=0.367757, posZ=2.833909, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445312, moveSeq=591, ackInputSeq=28, posX=60.579450, posY=0.367757, posZ=2.866646, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445313, moveSeq=592, ackInputSeq=28, posX=60.544350, posY=0.367757, posZ=2.899384, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445435, moveSeq=593, ackInputSeq=28, posX=60.509240, posY=0.367757, posZ=2.932122, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445436, moveSeq=594, ackInputSeq=28, posX=60.474140, posY=0.367757, posZ=2.964859, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445437, moveSeq=595, ackInputSeq=28, posX=60.439040, posY=0.367757, posZ=2.997597, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445547, moveSeq=596, ackInputSeq=28, posX=60.403930, posY=0.367757, posZ=3.030335, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445548, moveSeq=597, ackInputSeq=28, posX=60.368830, posY=0.367757, posZ=3.063073, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445549, moveSeq=598, ackInputSeq=28, posX=60.333730, posY=0.367757, posZ=3.095810, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445634, moveSeq=599, ackInputSeq=28, posX=60.298630, posY=0.367757, posZ=3.128548, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445635, moveSeq=600, ackInputSeq=28, posX=60.263520, posY=0.367757, posZ=3.161286, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445636, moveSeq=601, ackInputSeq=28, posX=60.228420, posY=0.367757, posZ=3.194024, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445729, moveSeq=602, ackInputSeq=28, posX=60.193320, posY=0.367757, posZ=3.226761, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445730, moveSeq=603, ackInputSeq=28, posX=60.158210, posY=0.367757, posZ=3.259499, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445731, moveSeq=604, ackInputSeq=28, posX=60.123110, posY=0.367757, posZ=3.292237, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445798, moveSeq=605, ackInputSeq=28, posX=60.088010, posY=0.367757, posZ=3.324975, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445799, moveSeq=606, ackInputSeq=28, posX=60.052910, posY=0.367757, posZ=3.357712, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445800, moveSeq=607, ackInputSeq=28, posX=60.017800, posY=0.367757, posZ=3.390450, dirX=-0.731318, dirZ=0.682037, speed=3.000
[NavMove][TraceAuthRecv] unitId=322183624654868, clientNow=1774512445801, moveSeq=608, ackInputSeq=29, posX=60.017800, posY=0.367757, posZ=3.390450, dirX=0.000000, dirZ=0.000000, speed=0.000";

            if (!JoystickTraceStopTailAnalyzer.TryAnalyze(
                    traceText,
                    out (uint StopSequence, int StopSendToServerLatencyMs, int StopSendToAuthorityLatencyMs, int StaleAuthorityMoveCount, uint FirstStaleMoveSequence, uint LastStaleMoveSequence, uint StopAuthorityMoveSequence) metrics,
                    out string error))
            {
                Log.Console($"[log-seq29-stop-tail] trace analysis failed: {error}");
                return 1;
            }

            if (metrics.StopSequence != 29)
            {
                Log.Console($"[log-seq29-stop-tail] stop sequence mismatch: expected=29, actual={metrics.StopSequence}");
                return 2;
            }

            if (metrics.StopSendToServerLatencyMs != 205)
            {
                Log.Console($"[log-seq29-stop-tail] stop send->server latency mismatch: expected=205, actual={metrics.StopSendToServerLatencyMs}");
                return 3;
            }

            if (metrics.StopSendToAuthorityLatencyMs != 492)
            {
                Log.Console($"[log-seq29-stop-tail] stop send->authority latency mismatch: expected=492, actual={metrics.StopSendToAuthorityLatencyMs}");
                return 4;
            }

            if (metrics.StaleAuthorityMoveCount != 18)
            {
                Log.Console($"[log-seq29-stop-tail] stale authority move count mismatch: expected=18, actual={metrics.StaleAuthorityMoveCount}");
                return 5;
            }

            if (metrics.FirstStaleMoveSequence != 590 || metrics.LastStaleMoveSequence != 607)
            {
                Log.Console(
                    $"[log-seq29-stop-tail] stale authority move sequence range mismatch: expected=590-607, actual={metrics.FirstStaleMoveSequence}-{metrics.LastStaleMoveSequence}");
                return 6;
            }

            if (metrics.StopAuthorityMoveSequence != 608)
            {
                Log.Console(
                    $"[log-seq29-stop-tail] stop authority move sequence mismatch: expected=608, actual={metrics.StopAuthorityMoveSequence}");
                return 7;
            }

            Log.Console(
                $"Joystick trace timeline [log-seq29-stop-tail] passed: stopSeq={metrics.StopSequence}, sendToServer={metrics.StopSendToServerLatencyMs}ms, sendToAuthority={metrics.StopSendToAuthorityLatencyMs}ms, staleMoves={metrics.StaleAuthorityMoveCount}, staleMoveSeq={metrics.FirstStaleMoveSequence}-{metrics.LastStaleMoveSequence}, stopMoveSeq={metrics.StopAuthorityMoveSequence}");
            return ErrorCode.ERR_Success;
        }
    }

    internal static class JoystickReplayTraceParser
    {
        private const string TraceInputSendTag = "[NavMove][TraceInputSend]";

        public static bool TryParse(string traceText, out List<(int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime)> steps, out string error)
        {
            steps = new List<(int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime)>();
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(traceText))
            {
                error = "trace text is empty";
                return false;
            }

            string[] lines = traceText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            long previousClientNow = 0;
            uint previousSequence = 0;
            bool usedRawTraceLines = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                bool matchedRawTraceLine = TryParseTraceInputSendLine(line, ref previousClientNow, out (int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) rawStep, out string rawError);
                if (!string.IsNullOrEmpty(rawError))
                {
                    error = rawError;
                    return false;
                }

                if (matchedRawTraceLine)
                {
                    usedRawTraceLines = true;
                    if (!ValidateAndAppend(rawStep, ref previousSequence, steps, out error))
                    {
                        return false;
                    }

                    continue;
                }

                if (usedRawTraceLines)
                {
                    error = $"mixed raw trace line with compact format: {line}";
                    return false;
                }

                string[] parts = line.Split('|');
                if (parts.Length != 4)
                {
                    error = $"invalid compact trace line: {line}";
                    return false;
                }

                if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int delayMs))
                {
                    error = $"invalid delay in compact trace line: {line}";
                    return false;
                }

                if (!uint.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint sequence))
                {
                    error = $"invalid sequence in compact trace line: {line}";
                    return false;
                }

                if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float dirX))
                {
                    error = $"invalid dirX in compact trace line: {line}";
                    return false;
                }

                if (!float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float dirZ))
                {
                    error = $"invalid dirZ in compact trace line: {line}";
                    return false;
                }

                (int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) compactStep =
                    (Math.Max(0, delayMs), sequence, dirX, dirZ, 0);

                if (!ValidateAndAppend(compactStep, ref previousSequence, steps, out error))
                {
                    return false;
                }
            }

            if (steps.Count == 0)
            {
                error = "no replay steps parsed from trace";
                return false;
            }

            return true;
        }

        private static bool TryParseTraceInputSendLine(
            string line,
            ref long previousClientNow,
            out (int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) step,
            out string error)
        {
            if (!line.Contains(TraceInputSendTag, StringComparison.Ordinal))
            {
                step = default;
                error = string.Empty;
                return false;
            }

            if (!TryReadLongField(line, "clientNow=", out long clientNow))
            {
                step = default;
                error = $"invalid clientNow in trace line: {line}";
                return false;
            }

            if (!TryReadUIntField(line, "seq=", out uint sequence))
            {
                step = default;
                error = $"invalid seq in trace line: {line}";
                return false;
            }

            if (!TryReadFloatField(line, "dirX=", out float dirX))
            {
                step = default;
                error = $"invalid dirX in trace line: {line}";
                return false;
            }

            if (!TryReadFloatField(line, "dirZ=", out float dirZ))
            {
                step = default;
                error = $"invalid dirZ in trace line: {line}";
                return false;
            }

            long deltaMsLong = previousClientNow <= 0 ? 0 : Math.Max(0, clientNow - previousClientNow);
            previousClientNow = clientNow;
            step = (deltaMsLong > int.MaxValue ? int.MaxValue : (int)deltaMsLong, sequence, dirX, dirZ, clientNow);
            error = string.Empty;
            return true;
        }

        private static bool TryReadLongField(string line, string key, out long value)
        {
            value = 0;
            if (!TryReadFieldToken(line, key, out string token))
            {
                return false;
            }

            return long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadUIntField(string line, string key, out uint value)
        {
            value = 0;
            if (!TryReadFieldToken(line, key, out string token))
            {
                return false;
            }

            return uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadFloatField(string line, string key, out float value)
        {
            value = 0f;
            if (!TryReadFieldToken(line, key, out string token))
            {
                return false;
            }

            return float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadFieldToken(string line, string key, out string token)
        {
            token = string.Empty;
            int keyIndex = line.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return false;
            }

            int valueStart = keyIndex + key.Length;
            int valueEnd = line.IndexOf(',', valueStart);
            if (valueEnd < 0)
            {
                valueEnd = line.Length;
            }

            token = line.Substring(valueStart, valueEnd - valueStart).Trim();
            return token.Length > 0;
        }

        private static bool ValidateAndAppend(
            (int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) step,
            ref uint previousSequence,
            List<(int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime)> steps,
            out string error)
        {
            if (step.Sequence == 0)
            {
                error = $"trace step sequence must be greater than zero: delay={step.DelayMs}, seq={step.Sequence}, dir=({step.DirX:F6},{step.DirZ:F6}), sourceTime={step.SourceTime}";
                return false;
            }

            if (previousSequence > 0 && step.Sequence <= previousSequence)
            {
                error = $"trace step sequence must be strictly increasing: prev={previousSequence}, current={step.Sequence}";
                return false;
            }

            previousSequence = step.Sequence;
            steps.Add(step);
            error = string.Empty;
            return true;
        }
    }

    internal static class JoystickTraceReplayScenarioRunner
    {
        private const float DirectionStoppedEpsilonSqr = 0.0001f;

        public static async ETTask<int> Run(
            TestContext context,
            string scenarioName,
            string traceText,
            int stopTimeoutMs,
            int pollIntervalMs,
            int stableStoppedWaitMs,
            int maxAcceptedStopLatencyMs,
            float maxPostStopTravelDistance)
        {
            if (!JoystickReplayTraceParser.TryParse(traceText, out List<(int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime)> steps, out string parseError))
            {
                Log.Console($"[{scenarioName}] trace parse failed: {parseError}");
                return 1;
            }

            (int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) stopStep = steps[steps.Count - 1];
            if (!IsStopStep(stopStep))
            {
                Log.Console(
                    $"[{scenarioName}] last trace step must be a stop step: delay={stopStep.DelayMs}, seq={stopStep.Sequence}, dir=({stopStep.DirX:F6},{stopStep.DirZ:F6}), sourceTime={stopStep.SourceTime}");
                return 2;
            }

            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Joystick_TraceReplay_Smoke_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Joystick_TraceReplay_Smoke_Test));
            Scene clientRoot = robot.Root;
            if (clientRoot == null || clientRoot.IsDisposed)
            {
                Log.Console($"[{scenarioName}] client root is null after robot create");
                return 3;
            }

            ClientSenderComponent sender = clientRoot.GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                Log.Console($"[{scenarioName}] client sender is null");
                return 4;
            }

            EntityRef<ClientSenderComponent> senderRef = sender;

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null || serverUnit.IsDisposed)
            {
                Log.Console($"[{scenarioName}] server unit is null after robot enter map");
                return 5;
            }

            Scene mapScene = serverUnit.Scene();
            if (mapScene == null || mapScene.IsDisposed || mapScene.TimerComponent == null)
            {
                Log.Console($"[{scenarioName}] map scene or timer component is null");
                return 6;
            }

            EntityRef<Unit> serverUnitRef = serverUnit;
            EntityRef<Scene> mapSceneRef = mapScene;

            long stopSentAt = 0;
            float3 stopSentPosition = serverUnit.Position;

            foreach ((int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) step in steps)
            {
                if (step.DelayMs > 0)
                {
                    mapScene = mapSceneRef;
                    if (mapScene == null || mapScene.IsDisposed || mapScene.TimerComponent == null)
                    {
                        Log.Console($"[{scenarioName}] map scene disposed before waiting trace step");
                        return 7;
                    }

                    await mapScene.TimerComponent.WaitAsync(step.DelayMs);
                }

                ClientSenderComponent liveSender = senderRef;
                if (liveSender == null || liveSender.IsDisposed)
                {
                    Log.Console($"[{scenarioName}] client sender disposed before sending trace step {step.Sequence}");
                    return 8;
                }

                if (IsStopStep(step))
                {
                    stopSentAt = TimeInfo.Instance.ServerNow();
                    serverUnit = serverUnitRef;
                    if (serverUnit == null || serverUnit.IsDisposed)
                    {
                        Log.Console($"[{scenarioName}] server unit disposed before stop trace step");
                        return 9;
                    }

                    stopSentPosition = serverUnit.Position;
                }

                SendJoystickInput(liveSender, step.DirX, step.DirZ, step.Sequence);
            }

            if (stopSentAt == 0)
            {
                Log.Console($"[{scenarioName}] stop step was not sent");
                return 10;
            }

            long stopObservedAt = await WaitForStop(mapSceneRef, serverUnitRef, stopStep.Sequence, stopTimeoutMs, pollIntervalMs);
            if (stopObservedAt == 0)
            {
                serverUnit = serverUnitRef;
                JoystickMoveComponent joystickMove = serverUnit?.GetComponent<JoystickMoveComponent>();
                Log.Console(
                    $"[{scenarioName}] stop was not observed in time, stopSeq={stopStep.Sequence}, lastSeq={joystickMove?.LastClientInputSequence ?? 0}, dir={joystickMove?.Direction.ToString() ?? "null"}");
                return 11;
            }

            long stopAcceptLatency = stopObservedAt - stopSentAt;
            if (stopAcceptLatency > maxAcceptedStopLatencyMs)
            {
                Log.Console(
                    $"[{scenarioName}] stop accept latency too high: latency={stopAcceptLatency}ms, limit={maxAcceptedStopLatencyMs}ms, stopSeq={stopStep.Sequence}");
                return 12;
            }

            mapScene = mapSceneRef;
            if (mapScene == null || mapScene.IsDisposed || mapScene.TimerComponent == null)
            {
                Log.Console($"[{scenarioName}] map scene disposed before stable stop wait");
                return 13;
            }

            await mapScene.TimerComponent.WaitAsync(stableStoppedWaitMs);

            serverUnit = serverUnitRef;
            if (serverUnit == null || serverUnit.IsDisposed)
            {
                Log.Console($"[{scenarioName}] server unit disposed during stable stop wait");
                return 14;
            }

            JoystickMoveComponent finalJoystickMove = serverUnit.GetComponent<JoystickMoveComponent>();
            if (!IsDirectionStopped(finalJoystickMove))
            {
                Log.Console(
                    $"[{scenarioName}] unit direction changed after stop, stopSeq={stopStep.Sequence}, lastSeq={finalJoystickMove?.LastClientInputSequence ?? 0}, dir={finalJoystickMove?.Direction.ToString() ?? "null"}");
                return 15;
            }

            if (finalJoystickMove == null || finalJoystickMove.LastClientInputSequence != stopStep.Sequence)
            {
                Log.Console(
                    $"[{scenarioName}] last accepted input seq mismatch: expected={stopStep.Sequence}, actual={finalJoystickMove?.LastClientInputSequence ?? 0}");
                return 16;
            }

            float postStopTravelDistance = math.distance(
                new float2(stopSentPosition.x, stopSentPosition.z),
                new float2(serverUnit.Position.x, serverUnit.Position.z));
            if (postStopTravelDistance > maxPostStopTravelDistance)
            {
                Log.Console(
                    $"[{scenarioName}] post-stop travel too high: distance={postStopTravelDistance:F4}, limit={maxPostStopTravelDistance:F4}, stopSeq={stopStep.Sequence}");
                return 17;
            }

            Log.Console(
                $"Joystick trace replay [{scenarioName}] passed: steps={steps.Count}, stopSeq={stopStep.Sequence}, latency={stopAcceptLatency}ms, postStopDistance={postStopTravelDistance:F4}");
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
            return joystickMove != null && math.lengthsq(joystickMove.Direction) <= DirectionStoppedEpsilonSqr;
        }

        private static bool IsStopStep((int DelayMs, uint Sequence, float DirX, float DirZ, long SourceTime) step)
        {
            return math.abs(step.DirX) < 0.000001f && math.abs(step.DirZ) < 0.000001f;
        }
    }

    internal static class JoystickTraceStopTailAnalyzer
    {
        private const string TraceInputSendTag = "[NavMove][TraceInputSend]";
        private const string TraceServerInputRecvTag = "[NavMove][TraceServerInputRecv]";
        private const string TraceAuthRecvTag = "[NavMove][TraceAuthRecv]";
        private const float StopDirectionEpsilon = 0.000001f;
        private const float StopSpeedEpsilon = 0.000001f;

        public static bool TryAnalyze(
            string traceText,
            out (uint StopSequence, int StopSendToServerLatencyMs, int StopSendToAuthorityLatencyMs, int StaleAuthorityMoveCount, uint FirstStaleMoveSequence, uint LastStaleMoveSequence, uint StopAuthorityMoveSequence) metrics,
            out string error)
        {
            metrics = default;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(traceText))
            {
                error = "trace text is empty";
                return false;
            }

            List<(long ClientNow, uint Sequence, float DirX, float DirZ)> inputSteps = new();
            List<(long ServerNow, uint Sequence, bool Stop)> serverSteps = new();
            List<(long ClientNow, uint MoveSequence, uint AckInputSequence, float DirX, float DirZ, float Speed)> authoritySteps = new();

            string[] lines = traceText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.Contains(TraceInputSendTag, StringComparison.Ordinal))
                {
                    if (!TryReadLongField(line, "clientNow=", out long clientNow) ||
                        !TryReadUIntField(line, "seq=", out uint sequence) ||
                        !TryReadFloatField(line, "dirX=", out float dirX) ||
                        !TryReadFloatField(line, "dirZ=", out float dirZ))
                    {
                        error = $"invalid trace input send line: {line}";
                        return false;
                    }

                    inputSteps.Add((clientNow, sequence, dirX, dirZ));
                    continue;
                }

                if (line.Contains(TraceServerInputRecvTag, StringComparison.Ordinal))
                {
                    if (!TryReadLongField(line, "serverNow=", out long serverNow) ||
                        !TryReadUIntField(line, "seq=", out uint sequence) ||
                        !TryReadBoolField(line, "stop=", out bool stop))
                    {
                        error = $"invalid trace server recv line: {line}";
                        return false;
                    }

                    serverSteps.Add((serverNow, sequence, stop));
                    continue;
                }

                if (line.Contains(TraceAuthRecvTag, StringComparison.Ordinal))
                {
                    if (!TryReadLongField(line, "clientNow=", out long clientNow) ||
                        !TryReadUIntField(line, "moveSeq=", out uint moveSequence) ||
                        !TryReadUIntField(line, "ackInputSeq=", out uint ackInputSequence) ||
                        !TryReadFloatField(line, "dirX=", out float dirX) ||
                        !TryReadFloatField(line, "dirZ=", out float dirZ) ||
                        !TryReadFloatField(line, "speed=", out float speed))
                    {
                        error = $"invalid trace authority recv line: {line}";
                        return false;
                    }

                    authoritySteps.Add((clientNow, moveSequence, ackInputSequence, dirX, dirZ, speed));
                    continue;
                }
            }

            if (inputSteps.Count == 0)
            {
                error = "trace does not contain input send lines";
                return false;
            }

            if (serverSteps.Count == 0)
            {
                error = "trace does not contain server recv lines";
                return false;
            }

            if (authoritySteps.Count == 0)
            {
                error = "trace does not contain authority recv lines";
                return false;
            }

            (long ClientNow, uint Sequence, float DirX, float DirZ) stopInput = default;
            foreach ((long ClientNow, uint Sequence, float DirX, float DirZ) step in inputSteps)
            {
                if (IsStopped(step.DirX, step.DirZ))
                {
                    stopInput = step;
                }
            }

            if (stopInput.Sequence == 0)
            {
                error = "trace does not contain a stop input";
                return false;
            }

            (long ServerNow, uint Sequence, bool Stop) stopServer = default;
            foreach ((long ServerNow, uint Sequence, bool Stop) step in serverSteps)
            {
                if (step.Sequence == stopInput.Sequence)
                {
                    stopServer = step;
                    break;
                }
            }

            if (stopServer.Sequence == 0 || !stopServer.Stop)
            {
                error = $"trace does not contain server stop for seq={stopInput.Sequence}";
                return false;
            }

            (long ClientNow, uint MoveSequence, uint AckInputSequence, float DirX, float DirZ, float Speed) stopAuthority = default;
            foreach ((long ClientNow, uint MoveSequence, uint AckInputSequence, float DirX, float DirZ, float Speed) step in authoritySteps)
            {
                if (step.AckInputSequence >= stopInput.Sequence &&
                    IsStopped(step.DirX, step.DirZ) &&
                    math.abs(step.Speed) <= StopSpeedEpsilon)
                {
                    stopAuthority = step;
                    break;
                }
            }

            if (stopAuthority.MoveSequence == 0)
            {
                error = $"trace does not contain authority stop for seq={stopInput.Sequence}";
                return false;
            }

            int staleAuthorityMoveCount = 0;
            uint firstStaleMoveSequence = 0;
            uint lastStaleMoveSequence = 0;
            foreach ((long ClientNow, uint MoveSequence, uint AckInputSequence, float DirX, float DirZ, float Speed) step in authoritySteps)
            {
                if (step.ClientNow < stopInput.ClientNow || step.ClientNow >= stopAuthority.ClientNow)
                {
                    continue;
                }

                if (step.AckInputSequence >= stopInput.Sequence || math.abs(step.Speed) <= StopSpeedEpsilon)
                {
                    continue;
                }

                if (staleAuthorityMoveCount == 0)
                {
                    firstStaleMoveSequence = step.MoveSequence;
                }

                staleAuthorityMoveCount++;
                lastStaleMoveSequence = step.MoveSequence;
            }

            if (staleAuthorityMoveCount == 0)
            {
                error = "trace does not contain stale authority tail after stop send";
                return false;
            }

            long stopSendToServerLatency = Math.Max(0, stopServer.ServerNow - stopInput.ClientNow);
            long stopSendToAuthorityLatency = Math.Max(0, stopAuthority.ClientNow - stopInput.ClientNow);
            metrics = (
                stopInput.Sequence,
                stopSendToServerLatency > int.MaxValue ? int.MaxValue : (int)stopSendToServerLatency,
                stopSendToAuthorityLatency > int.MaxValue ? int.MaxValue : (int)stopSendToAuthorityLatency,
                staleAuthorityMoveCount,
                firstStaleMoveSequence,
                lastStaleMoveSequence,
                stopAuthority.MoveSequence);
            return true;
        }

        private static bool IsStopped(float dirX, float dirZ)
        {
            return math.abs(dirX) <= StopDirectionEpsilon && math.abs(dirZ) <= StopDirectionEpsilon;
        }

        private static bool TryReadBoolField(string line, string key, out bool value)
        {
            value = false;
            return TryReadFieldToken(line, key, out string token) && bool.TryParse(token, out value);
        }

        private static bool TryReadLongField(string line, string key, out long value)
        {
            value = 0;
            return TryReadFieldToken(line, key, out string token) &&
                    long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadUIntField(string line, string key, out uint value)
        {
            value = 0;
            return TryReadFieldToken(line, key, out string token) &&
                    uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadFloatField(string line, string key, out float value)
        {
            value = 0f;
            return TryReadFieldToken(line, key, out string token) &&
                    float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadFieldToken(string line, string key, out string token)
        {
            token = string.Empty;
            int keyIndex = line.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return false;
            }

            int valueStart = keyIndex + key.Length;
            int valueEnd = line.IndexOf(',', valueStart);
            if (valueEnd < 0)
            {
                valueEnd = line.Length;
            }

            token = line.Substring(valueStart, valueEnd - valueStart).Trim();
            return token.Length > 0;
        }
    }
}
