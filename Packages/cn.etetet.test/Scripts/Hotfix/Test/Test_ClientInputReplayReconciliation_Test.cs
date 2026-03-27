using System;
using System.Collections;
using System.IO;
using System.Reflection;
using ET.Client;
using ET.Server;
using Unity.Mathematics;
using ClientUnitHelper = ET.Client.UnitHelper;
using ServerUnitFactory = ET.Server.UnitFactory;

namespace ET.Test
{
    public class Test_ClientInputReplayReconciliation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayReconciliation_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplayReconciliation_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            IList pendingMoveInputs = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            long now = TimeInfo.Instance.ClientNow();
            Type sampleType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.PendingMoveInputSample");
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 11, 1f, 0f, 3f, now - 64));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 12, 0f, 1f, 3f, now - 32));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 13, 0f, 0f, 0f, now - 16));

            bool reconciled = ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, unit.Position, 11u);
            if (!reconciled)
            {
                Log.Console("reconcile returned false");
                return 2;
            }

            uint acknowledgedSequence = ClientInputReplayTestHelper.GetField<uint>(inputSystem, "LastAcknowledgedMoveInputSequence");
            if (acknowledgedSequence != 11)
            {
                Log.Console($"ack sequence mismatch: expected=11, actual={acknowledgedSequence}");
                return 3;
            }

            if (pendingMoveInputs.Count != 2 ||
                ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[0], "Sequence") != 12 ||
                ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[1], "Sequence") != 13)
            {
                uint seq0 = pendingMoveInputs.Count > 0 ? ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[0], "Sequence") : 0;
                uint seq1 = pendingMoveInputs.Count > 1 ? ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[1], "Sequence") : 0;
                Log.Console($"pending input trim mismatch: count={pendingMoveInputs.Count}, seq0={seq0}, seq1={seq1}");
                return 4;
            }

            float3 expectedPredictedDelta = ClientInputReplayTestHelper.SimulateReplay(unit, unit.Position, pendingMoveInputs, now);
            float3 actualPredictedDelta = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.distance(expectedPredictedDelta, actualPredictedDelta) > 0.02f)
            {
                Log.Console($"predicted delta mismatch: expected={expectedPredictedDelta}, actual={actualPredictedDelta}");
                return 5;
            }

            float predictedSpeed = ClientInputReplayTestHelper.GetField<float>(interpolation, "PredictedSpeed");
            float constrainedSpeed = ClientInputReplayTestHelper.GetField<float>(interpolation, "PositionPredictionSpeed");
            if (predictedSpeed > 0.01f || constrainedSpeed > 0.01f)
            {
                Log.Console($"stop replay should not keep motion: predicted={predictedSpeed}, constrained={constrainedSpeed}");
                return 6;
            }

            float3 visualCorrection = ClientInputReplayTestHelper.ReadVector3(interpolation, "VisualCorrection");
            if (math.lengthsq(visualCorrection) > 0.000001f)
            {
                Log.Console($"visual correction should be cleared: {visualCorrection}");
                return 7;
            }

            float3 targetPosition = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            float3 expectedTargetPosition = unit.Position + expectedPredictedDelta;
            if (math.distance(targetPosition, expectedTargetPosition) > 0.001f)
            {
                Log.Console($"target position mismatch: expected={expectedTargetPosition}, actual={targetPosition}");
                return 8;
            }

            Log.Console("client input replay reconciliation passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplayAckAdvance_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayAckAdvance_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplayAckAdvance_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            IList pendingMoveInputs = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            long now = TimeInfo.Instance.ClientNow();
            Type sampleType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.PendingMoveInputSample");
            object sample11 = ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 11, 1f, 0f, 3f, now - 128);
            object sample12 = ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 12, 0f, 1f, 3f, now - 96);
            object sample13 = ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 13, 0f, 1f, 3f, now - 64);
            object sample14 = ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 14, 0f, 0f, 0f, now - 32);

            pendingMoveInputs.Add(sample11);
            pendingMoveInputs.Add(sample12);
            pendingMoveInputs.Add(sample13);
            pendingMoveInputs.Add(sample14);

            ArrayList ack11Prefix = new ArrayList { sample11, sample12 };
            float3 authoritativeAfter11 = unit.Position + ClientInputReplayTestHelper.SimulateReplay(
                unit,
                unit.Position,
                ack11Prefix,
                ClientInputReplayTestHelper.GetField<long>(sample12, "ClientTime"));

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativeAfter11, 11u))
            {
                Log.Console("first reconcile returned false");
                return 2;
            }

            if (pendingMoveInputs.Count != 3 ||
                ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[0], "Sequence") != 12 ||
                ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[2], "Sequence") != 14)
            {
                Log.Console($"ack11 trim mismatch: count={pendingMoveInputs.Count}");
                return 3;
            }

            float3 expectedAfterAck11 = ClientInputReplayTestHelper.SimulateReplay(unit, authoritativeAfter11, pendingMoveInputs, now);
            float3 actualAfterAck11 = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.distance(expectedAfterAck11, actualAfterAck11) > 0.02f)
            {
                Log.Console($"ack11 predicted delta mismatch: expected={expectedAfterAck11}, actual={actualAfterAck11}");
                return 4;
            }

            ArrayList ack12Prefix = new ArrayList { sample11, sample12, sample13 };
            float3 authoritativeAfter12 = unit.Position + ClientInputReplayTestHelper.SimulateReplay(
                unit,
                unit.Position,
                ack12Prefix,
                ClientInputReplayTestHelper.GetField<long>(sample13, "ClientTime"));

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativeAfter12, 12u))
            {
                Log.Console("second reconcile returned false");
                return 5;
            }

            if (pendingMoveInputs.Count != 2 ||
                ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[0], "Sequence") != 13 ||
                ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[1], "Sequence") != 14)
            {
                uint seq0 = pendingMoveInputs.Count > 0 ? ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[0], "Sequence") : 0;
                uint seq1 = pendingMoveInputs.Count > 1 ? ClientInputReplayTestHelper.GetField<uint>(pendingMoveInputs[1], "Sequence") : 0;
                Log.Console($"ack12 trim mismatch: count={pendingMoveInputs.Count}, seq0={seq0}, seq1={seq1}");
                return 6;
            }

            uint acknowledgedSequence = ClientInputReplayTestHelper.GetField<uint>(inputSystem, "LastAcknowledgedMoveInputSequence");
            if (acknowledgedSequence != 12)
            {
                Log.Console($"ack12 sequence mismatch: expected=12, actual={acknowledgedSequence}");
                return 7;
            }

            float3 expectedAfterAck12 = ClientInputReplayTestHelper.SimulateReplay(unit, authoritativeAfter12, pendingMoveInputs, now);
            float3 actualAfterAck12 = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.distance(expectedAfterAck12, actualAfterAck12) > 0.02f)
            {
                Log.Console($"ack12 predicted delta mismatch: expected={expectedAfterAck12}, actual={actualAfterAck12}");
                return 8;
            }

            if (math.lengthsq(new float2(actualAfterAck12.x, actualAfterAck12.z)) >=
                math.lengthsq(new float2(actualAfterAck11.x, actualAfterAck11.z)))
            {
                Log.Console($"ack advance did not shrink replay tail: ack11={actualAfterAck11}, ack12={actualAfterAck12}");
                return 9;
            }

            float3 targetPosition = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            float3 expectedTargetPosition = authoritativeAfter12 + expectedAfterAck12;
            if (math.distance(targetPosition, expectedTargetPosition) > 0.001f)
            {
                Log.Console($"ack12 target position mismatch: expected={expectedTargetPosition}, actual={targetPosition}");
                return 10;
            }

            Log.Console("client input replay ack advance passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplayPreserveActiveLead_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayPreserveActiveLead_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplayPreserveActiveLead_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                Log.Console($"invalid move speed: {speed}");
                return 2;
            }

            Type interpolationType = interpolation.GetType();
            Type inputSystemType = inputSystem.GetType();
            float3 initialAuthoritativePosition = unit.Position;
            float3 currentTargetPosition = initialAuthoritativePosition + new float3(0.18f, 0f, 0f);
            float3 nextAuthoritativePosition = initialAuthoritativePosition + new float3(0.05f, 0f, 0f);

            ClientInputReplayTestHelper.SetField(inputSystem, "JoystickMoveInput", ClientInputReplayTestHelper.CreateVector2(inputSystemType, 1f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PredictionEnabled", true);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "AuthoritativePosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, initialAuthoritativePosition.x, initialAuthoritativePosition.y, initialAuthoritativePosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "TargetPosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, currentTargetPosition.x, currentTargetPosition.y, currentTargetPosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDelta",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0.18f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "VisualCorrection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PredictedSpeed", speed);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PositionPredictionDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionSpeed", speed);
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionBlocked", false);
            ClientInputReplayTestHelper.SetField(interpolation, "HoldLocalPredictionOnStationarySync", false);
            ClientInputReplayTestHelper.SetField(interpolation, "WallBlockedCount", 0);

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, nextAuthoritativePosition, 0u))
            {
                Log.Console("preserve active lead reconcile returned false");
                return 3;
            }

            float3 expectedPredictedDelta = currentTargetPosition - nextAuthoritativePosition;
            float3 actualPredictedDelta = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.distance(expectedPredictedDelta, actualPredictedDelta) > 0.02f)
            {
                Log.Console($"preserve active lead predicted delta mismatch: expected={expectedPredictedDelta}, actual={actualPredictedDelta}");
                return 4;
            }

            float3 targetPosition = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetPosition, currentTargetPosition) > 0.02f)
            {
                Log.Console($"preserve active lead target mismatch: expected={currentTargetPosition}, actual={targetPosition}");
                return 5;
            }

            float3 visualCorrection = ClientInputReplayTestHelper.ReadVector3(interpolation, "VisualCorrection");
            if (math.lengthsq(visualCorrection) > 0.000001f)
            {
                Log.Console($"preserve active lead should not introduce visual correction: {visualCorrection}");
                return 6;
            }

            Log.Console("client input replay preserve active lead passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplaySmoothRetreatingTarget_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplaySmoothRetreatingTarget_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplaySmoothRetreatingTarget_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                Log.Console($"invalid move speed: {speed}");
                return 2;
            }

            Type interpolationType = interpolation.GetType();
            Type interpolationSystemType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.UnitViewInterpolationComponentSystem");
            float3 initialAuthoritativePosition = unit.Position;
            float3 nextAuthoritativePosition = initialAuthoritativePosition + new float3(0.05f, 0f, 0f);
            float3 currentViewPosition = initialAuthoritativePosition + new float3(0.40f, 0f, 0f);
            float3 replayedPredictedDelta = new float3(0.08f, 0f, 0f);
            float3 replayTargetPosition = nextAuthoritativePosition + replayedPredictedDelta;
            float smoothingStep = speed * 0.05f;
            float3 expectedTargetPosition = currentViewPosition + math.normalize(replayTargetPosition - currentViewPosition) * smoothingStep;
            float3 expectedVisualCorrection = expectedTargetPosition - replayTargetPosition;

            ClientInputReplayTestHelper.SetField(interpolation, "PredictionEnabled", true);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "AuthoritativePosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, initialAuthoritativePosition.x, initialAuthoritativePosition.y, initialAuthoritativePosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "TargetPosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, currentViewPosition.x, currentViewPosition.y, currentViewPosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDelta",
                ClientInputReplayTestHelper.CreateVector3(interpolationType,
                    currentViewPosition.x - initialAuthoritativePosition.x,
                    currentViewPosition.y - initialAuthoritativePosition.y,
                    currentViewPosition.z - initialAuthoritativePosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "VisualCorrection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PredictedSpeed", speed);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PositionPredictionDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionSpeed", speed);
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionBlocked", false);
            ClientInputReplayTestHelper.SetField(interpolation, "HoldLocalPredictionOnStationarySync", false);
            ClientInputReplayTestHelper.SetField(interpolation, "WallBlockedCount", 0);

            ClientInputReplayTestHelper.InvokeStatic(
                interpolationSystemType,
                "ApplyAuthoritativeReplay",
                interpolation,
                currentViewPosition,
                nextAuthoritativePosition,
                replayedPredictedDelta,
                new float3(1f, 0f, 0f),
                speed,
                new float3(1f, 0f, 0f),
                speed,
                false);

            float3 actualPredictedDelta = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.distance(actualPredictedDelta, replayedPredictedDelta) > 0.001f)
            {
                Log.Console($"retreat smoothing should not alter replay delta: expected={replayedPredictedDelta}, actual={actualPredictedDelta}");
                return 3;
            }

            float3 actualTargetPosition = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(actualTargetPosition, expectedTargetPosition) > 0.02f)
            {
                Log.Console($"retreat smoothing target mismatch: expected={expectedTargetPosition}, actual={actualTargetPosition}");
                return 4;
            }

            if (actualTargetPosition.x >= currentViewPosition.x || actualTargetPosition.x <= replayTargetPosition.x)
            {
                Log.Console(
                    $"retreat smoothing should keep target between current view and replay target: currentView={currentViewPosition}, replayTarget={replayTargetPosition}, actual={actualTargetPosition}");
                return 5;
            }

            float3 actualVisualCorrection = ClientInputReplayTestHelper.ReadVector3(interpolation, "VisualCorrection");
            if (math.distance(actualVisualCorrection, expectedVisualCorrection) > 0.02f)
            {
                Log.Console($"retreat smoothing visual correction mismatch: expected={expectedVisualCorrection}, actual={actualVisualCorrection}");
                return 6;
            }

            Log.Console("client input replay smooth retreating target passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplaySmoothSharpRetarget_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplaySmoothSharpRetarget_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplaySmoothSharpRetarget_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                Log.Console($"invalid move speed: {speed}");
                return 2;
            }

            Type interpolationType = interpolation.GetType();
            Type interpolationSystemType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.UnitViewInterpolationComponentSystem");
            float3 initialAuthoritativePosition = unit.Position;
            float3 currentViewPosition = initialAuthoritativePosition + new float3(0.20f, 0f, 0.60f);
            float3 previousTargetPosition = initialAuthoritativePosition + new float3(0.25f, 0f, 0.55f);
            float3 nextAuthoritativePosition = initialAuthoritativePosition + new float3(0.05f, 0f, 0f);
            float3 replayedPredictedDelta = new float3(0.27f, 0f, 0.01f);
            float3 replayTargetPosition = nextAuthoritativePosition + replayedPredictedDelta;
            float smoothingStep = speed * 0.05f;
            float3 expectedTargetPosition = currentViewPosition + math.normalize(replayTargetPosition - currentViewPosition) * smoothingStep;
            float3 expectedVisualCorrection = expectedTargetPosition - replayTargetPosition;

            ClientInputReplayTestHelper.SetField(interpolation, "PredictionEnabled", true);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "AuthoritativePosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, initialAuthoritativePosition.x, initialAuthoritativePosition.y, initialAuthoritativePosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "TargetPosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, previousTargetPosition.x, previousTargetPosition.y, previousTargetPosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDelta",
                ClientInputReplayTestHelper.CreateVector3(interpolationType,
                    previousTargetPosition.x - initialAuthoritativePosition.x,
                    previousTargetPosition.y - initialAuthoritativePosition.y,
                    previousTargetPosition.z - initialAuthoritativePosition.z));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "VisualCorrection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PredictedSpeed", speed);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PositionPredictionDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionSpeed", speed);
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionBlocked", false);
            ClientInputReplayTestHelper.SetField(interpolation, "HoldLocalPredictionOnStationarySync", false);
            ClientInputReplayTestHelper.SetField(interpolation, "WallBlockedCount", 0);

            ClientInputReplayTestHelper.InvokeStatic(
                interpolationSystemType,
                "ApplyAuthoritativeReplay",
                interpolation,
                currentViewPosition,
                nextAuthoritativePosition,
                replayedPredictedDelta,
                new float3(1f, 0f, 0f),
                speed,
                new float3(1f, 0f, 0f),
                speed,
                false);

            float3 actualTargetPosition = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(actualTargetPosition, expectedTargetPosition) > 0.02f)
            {
                Log.Console($"sharp retarget smoothing target mismatch: expected={expectedTargetPosition}, actual={actualTargetPosition}");
                return 3;
            }

            float3 actualVisualCorrection = ClientInputReplayTestHelper.ReadVector3(interpolation, "VisualCorrection");
            if (math.distance(actualVisualCorrection, expectedVisualCorrection) > 0.02f)
            {
                Log.Console($"sharp retarget smoothing visual correction mismatch: expected={expectedVisualCorrection}, actual={actualVisualCorrection}");
                return 4;
            }

            if (actualTargetPosition.z >= currentViewPosition.z)
            {
                Log.Console($"sharp retarget smoothing should rotate target toward replay target: currentView={currentViewPosition}, actual={actualTargetPosition}");
                return 5;
            }

            Log.Console("client input replay smooth sharp retarget passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplayWallConstraint_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayWallConstraint_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber mapManagerFiber = testFiber.GetFiber("MapManager");
            if (mapManagerFiber == null)
            {
                Log.Console("map manager fiber is null");
                return 1;
            }

            MapManagerComponent mapManagerComponent = mapManagerFiber.Root.GetComponent<MapManagerComponent>();
            if (mapManagerComponent == null)
            {
                Log.Console("map manager component is null");
                return 2;
            }

            long mapId = IdGenerater.Instance.GenerateId();
            EntityRef<MapManagerComponent> mapManagerComponentRef = mapManagerComponent;
            MapCopy mapCopy = await mapManagerComponent.GetMapAsync("SDCMap", mapId);
            mapManagerComponent = mapManagerComponentRef;
            if (mapCopy == null)
            {
                Log.Console("failed to create SDCMap map copy");
                return 3;
            }

            Fiber mapFiber = mapManagerFiber.GetFiber(mapCopy.FiberId);
            if (mapFiber == null)
            {
                Log.Console("SDCMap fiber is null");
                return 4;
            }

            Scene mapScene = mapFiber.Root;
            Unit unit = ServerUnitFactory.Create(mapScene, IdGenerater.Instance.GenerateId(), 1003);
            if (unit == null)
            {
                Log.Console("failed to create probe monster unit");
                return 5;
            }

            if (!ClientInputReplayTestHelper.TryPrepareMoveReplayContext(unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 6;
            }

            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                Log.Console("pathfinding component is null");
                return 7;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                Log.Console($"invalid move speed: {speed}");
                return 8;
            }

            float3 authoritativePosition = new float3(58.09703f, 0.3677568f, 25.00241f);
            float3 expectedBlockedTarget = new float3(58.09703f, 0.3677568f, 25.17641f);
            float3 expectedSafePosition = new float3(58.09703f, 0.3677568f, 25.03333f);
            float2 blockedDirection = math.normalizesafe(new float2(
                expectedBlockedTarget.x - authoritativePosition.x,
                expectedBlockedTarget.z - authoritativePosition.z));
            float requestedDistance = math.distance(
                new float2(authoritativePosition.x, authoritativePosition.z),
                new float2(expectedBlockedTarget.x, expectedBlockedTarget.z));

            long durationMs = Math.Max(64, (long)Math.Ceiling(requestedDistance / speed * 1000f));
            long now = TimeInfo.Instance.ClientNow();
            Type sampleType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.PendingMoveInputSample");
            IList pendingMoveInputs = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(
                sampleType, 21, blockedDirection.x, blockedDirection.y, speed, now - durationMs));

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativePosition, 0u))
            {
                Log.Console("wall reconcile returned false");
                return 9;
            }

            float3 expectedPredictedDelta = ClientInputReplayTestHelper.SimulateReplay(unit, authoritativePosition, pendingMoveInputs, now);
            float3 actualPredictedDelta = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.distance(expectedPredictedDelta, actualPredictedDelta) > 0.03f)
            {
                Log.Console($"wall replay delta mismatch: expected={expectedPredictedDelta}, actual={actualPredictedDelta}");
                return 10;
            }

            float straightDistance = speed * durationMs / 1000f;
            float constrainedDistance = math.length(new float2(actualPredictedDelta.x, actualPredictedDelta.z));
            if (constrainedDistance >= straightDistance - 0.05f)
            {
                Log.Console(
                    $"wall replay was not constrained: straight={straightDistance:F3}, constrained={constrainedDistance:F3}");
                return 11;
            }

            float3 actualSafePosition = authoritativePosition + actualPredictedDelta;
            if (math.distance(actualSafePosition, expectedSafePosition) > 0.03f)
            {
                Log.Console($"wall safe position mismatch: expected={expectedSafePosition}, actual={actualSafePosition}");
                return 12;
            }

            float3 targetPosition = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            float3 expectedTargetPosition = authoritativePosition + expectedPredictedDelta;
            if (math.distance(targetPosition, expectedTargetPosition) > 0.001f)
            {
                Log.Console($"wall target position mismatch: expected={expectedTargetPosition}, actual={targetPosition}");
                return 13;
            }

            Log.Console("client input replay wall constraint passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplayStopAnchor_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayStopAnchor_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplayStopAnchor_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, unit.Position);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            float3 authoritativePosition1 = unit.Position;
            float3 anchoredTargetPosition = authoritativePosition1 + new float3(0.20f, 0f, 0.08f);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "TargetPosition",
                ClientInputReplayTestHelper.CreateVector3(interpolation.GetType(), anchoredTargetPosition.x, anchoredTargetPosition.y, anchoredTargetPosition.z));
            ClientInputReplayTestHelper.SetPendingStopState(inputSystem, 13u, anchoredTargetPosition, authoritativePosition1);

            IList pendingMoveInputs = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            long now = TimeInfo.Instance.ClientNow();
            Type sampleType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.PendingMoveInputSample");
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 11, 1f, 0f, 3f, now - 220));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 12, 0f, 1f, 3f, now - 120));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 13, 0f, 0f, 3f, now - 40));

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativePosition1, 10u))
            {
                Log.Console("first stop-anchor reconcile returned false");
                return 2;
            }

            float3 targetAfterFirst = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetAfterFirst, anchoredTargetPosition) > 0.001f)
            {
                Log.Console($"first stop-anchor target mismatch: expected={anchoredTargetPosition}, actual={targetAfterFirst}");
                return 3;
            }

            float3 predictedAfterFirst = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            float3 expectedPredictedAfterFirst = anchoredTargetPosition - authoritativePosition1;
            if (math.distance(predictedAfterFirst, expectedPredictedAfterFirst) > 0.001f)
            {
                Log.Console($"first stop-anchor predicted mismatch: expected={expectedPredictedAfterFirst}, actual={predictedAfterFirst}");
                return 4;
            }

            float3 authoritativePosition2 = authoritativePosition1 + new float3(0.08f, 0f, 0.02f);
            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativePosition2, 10u))
            {
                Log.Console("second stop-anchor reconcile returned false");
                return 5;
            }

            float3 targetAfterSecond = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetAfterSecond, anchoredTargetPosition) > 0.001f)
            {
                Log.Console($"second stop-anchor target mismatch: expected={anchoredTargetPosition}, actual={targetAfterSecond}");
                return 6;
            }

            float3 predictedAfterSecond = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            float3 expectedPredictedAfterSecond = anchoredTargetPosition - authoritativePosition2;
            if (math.distance(predictedAfterSecond, expectedPredictedAfterSecond) > 0.001f)
            {
                Log.Console($"second stop-anchor predicted mismatch: expected={expectedPredictedAfterSecond}, actual={predictedAfterSecond}");
                return 7;
            }

            if (math.lengthsq(new float2(predictedAfterSecond.x, predictedAfterSecond.z)) >=
                math.lengthsq(new float2(predictedAfterFirst.x, predictedAfterFirst.z)))
            {
                Log.Console($"stop-anchor predicted delta did not shrink: first={predictedAfterFirst}, second={predictedAfterSecond}");
                return 8;
            }

            Log.Console("client input replay stop anchor passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Test_ClientInputReplayImmediateStopFreezeAnchor_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayImmediateStopFreezeAnchor_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplayImmediateStopFreezeAnchor_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            Type interpolationType = interpolation.GetType();
            Type interpolationSystemType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.UnitViewInterpolationComponentSystem");
            Type sampleType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.PendingMoveInputSample");
            float3 authoritativePosition = new float3(0.05f, 0f, 0f);
            float3 currentViewPosition = new float3(0.18f, 0f, 0f);
            long now = TimeInfo.Instance.ClientNow();

            ConfigureMovingPrediction(interpolation, interpolationType);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);
            IList pendingWithoutFreeze = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            pendingWithoutFreeze.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 11, 1f, 0f, 3f, now - 140));
            pendingWithoutFreeze.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 12, 0f, 0f, 3f, now - 20));

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativePosition, 10u))
            {
                Log.Console("stop-freeze baseline reconcile returned false");
                return 2;
            }

            float3 targetWithoutFreeze = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");

            ConfigureMovingPrediction(interpolation, interpolationType);
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);
            IList pendingWithFreeze = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            pendingWithFreeze.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 11, 1f, 0f, 3f, now - 140));
            pendingWithFreeze.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 12, 0f, 0f, 3f, now - 20));

            ClientInputReplayTestHelper.InvokeStatic(interpolationSystemType, "FreezePredictionOnLocalStop", interpolation, currentViewPosition);
            ClientInputReplayTestHelper.SetPendingStopState(inputSystem, 12u, currentViewPosition, authoritativePosition);
            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, authoritativePosition, 10u))
            {
                Log.Console("stop-freeze anchored reconcile returned false");
                return 3;
            }

            float3 targetWithFreeze = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetWithFreeze, currentViewPosition) > 0.001f)
            {
                Log.Console($"stop freeze anchor target mismatch: expected={currentViewPosition}, actual={targetWithFreeze}");
                return 4;
            }

            float extraTravelWithoutFreeze = targetWithoutFreeze.x - targetWithFreeze.x;
            if (extraTravelWithoutFreeze < 0.05f)
            {
                Log.Console(
                    $"stop freeze did not materially shrink stale tail anchor: withoutFreeze={targetWithoutFreeze}, withFreeze={targetWithFreeze}, delta={extraTravelWithoutFreeze:F4}");
                return 5;
            }

            Log.Console(
                $"client immediate stop freeze anchor passed: withoutFreeze={targetWithoutFreeze}, withFreeze={targetWithFreeze}, delta={extraTravelWithoutFreeze:F4}");
            return ErrorCode.ERR_Success;
        }

        private static void ConfigureMovingPrediction(Entity interpolation, Type interpolationType)
        {
            ClientInputReplayTestHelper.SetField(interpolation, "PredictionEnabled", true);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "AuthoritativePosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "TargetPosition",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0.30f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDelta",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0.30f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "VisualCorrection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 0f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PredictedDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PredictedSpeed", 3f);
            ClientInputReplayTestHelper.SetField(
                interpolation,
                "PositionPredictionDirection",
                ClientInputReplayTestHelper.CreateVector3(interpolationType, 1f, 0f, 0f));
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionSpeed", 3f);
            ClientInputReplayTestHelper.SetField(interpolation, "PositionPredictionBlocked", false);
            ClientInputReplayTestHelper.SetField(interpolation, "HoldLocalPredictionOnStationarySync", false);
            ClientInputReplayTestHelper.SetField(interpolation, "WallBlockedCount", 0);
        }
    }

    public class Test_ClientInputReplayPendingStopSuppressesStaleAuthorityTail_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientInputReplayPendingStopSuppressesStaleAuthorityTail_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientInputReplayPendingStopSuppressesStaleAuthorityTail_Test));
            if (!ClientInputReplayTestHelper.TryPrepareClientMoveContext(robot.Root, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error))
            {
                Log.Console(error);
                return 1;
            }

            ClientInputReplayTestHelper.ResetInterpolation(interpolation, new float3(0.05f, 0f, 0f));
            ClientInputReplayTestHelper.ResetInputSystem(inputSystem);

            Type interpolationSystemType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.UnitViewInterpolationComponentSystem");
            Type sampleType = ClientInputReplayTestHelper.GetRequiredType("ET.Client.PendingMoveInputSample");
            IList pendingMoveInputs = ClientInputReplayTestHelper.GetPendingMoveInputs(inputSystem);
            long now = TimeInfo.Instance.ClientNow();
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 57, 1f, 0f, 3f, now - 96));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 58, 1f, 0f, 3f, now - 72));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 59, 1f, 0f, 3f, now - 48));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 60, 1f, 0f, 3f, now - 24));
            pendingMoveInputs.Add(ClientInputReplayTestHelper.CreatePendingMoveInputSample(sampleType, 61, 0f, 0f, 3f, now - 8));

            float3 stopAnchor = new float3(0.18f, 0f, 0f);
            ClientInputReplayTestHelper.InvokeStatic(interpolationSystemType, "FreezePredictionOnLocalStop", interpolation, stopAnchor);
            float3 initialAnchorDelta = stopAnchor - new float3(0.05f, 0f, 0f);
            ClientInputReplayTestHelper.SetField(inputSystem, "PendingStopActive", true);
            ClientInputReplayTestHelper.SetField(inputSystem, "PendingStopSequence", 61u);
            ClientInputReplayTestHelper.SetField(
                inputSystem,
                "PendingStopAnchorPosition",
                ClientInputReplayTestHelper.CreateFieldValue(inputSystem, "PendingStopAnchorPosition", stopAnchor.x, stopAnchor.y, stopAnchor.z));
            ClientInputReplayTestHelper.SetField(
                inputSystem,
                "PendingStopInitialAnchorDelta",
                ClientInputReplayTestHelper.CreateFieldValue(inputSystem, "PendingStopInitialAnchorDelta", initialAnchorDelta.x, initialAnchorDelta.y, initialAnchorDelta.z));

            float3 staleAuthority1 = new float3(0.10f, 0f, 0f);
            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, staleAuthority1, 58u))
            {
                Log.Console("first stale authority reconcile returned false");
                return 2;
            }

            float3 targetAfterFirstStale = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetAfterFirstStale, stopAnchor) > 0.001f)
            {
                Log.Console($"first stale authority should hold stop anchor: expected={stopAnchor}, actual={targetAfterFirstStale}");
                return 3;
            }

            float3 predictedAfterFirstStale = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");

            float3 staleAuthority2 = new float3(0.15f, 0f, 0f);
            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, staleAuthority2, 60u))
            {
                Log.Console("second stale authority reconcile returned false");
                return 4;
            }

            float3 targetAfterSecondStale = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetAfterSecondStale, stopAnchor) > 0.001f)
            {
                Log.Console($"second stale authority should keep stop anchor: expected={stopAnchor}, actual={targetAfterSecondStale}");
                return 5;
            }

            float3 predictedAfterSecondStale = ClientInputReplayTestHelper.ReadVector3(interpolation, "PredictedDelta");
            if (math.lengthsq(new float2(predictedAfterSecondStale.x, predictedAfterSecondStale.z)) >=
                math.lengthsq(new float2(predictedAfterFirstStale.x, predictedAfterFirstStale.z)))
            {
                Log.Console(
                    $"stale authority should only consume pending stop tail: first={predictedAfterFirstStale}, second={predictedAfterSecondStale}");
                return 6;
            }

            float3 overshootAuthority = new float3(0.21f, 0f, 0f);
            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, overshootAuthority, 60u))
            {
                Log.Console("overshoot authority reconcile returned false");
                return 7;
            }

            float3 targetAfterOvershoot = ClientInputReplayTestHelper.ReadVector3(interpolation, "TargetPosition");
            if (math.distance(targetAfterOvershoot, stopAnchor) > 0.001f)
            {
                Log.Console($"authority overshoot before stop ack should still keep local stop freeze: expected={stopAnchor}, actual={targetAfterOvershoot}");
                return 8;
            }

            if (!ClientInputReplayTestHelper.TryReconcileAuthoritativeMove(inputSystem, unit, interpolation, stopAnchor, 61u))
            {
                Log.Console("final stop ack reconcile returned false");
                return 9;
            }

            bool pendingStopActive = ClientInputReplayTestHelper.GetField<bool>(inputSystem, "PendingStopActive");
            if (pendingStopActive)
            {
                Log.Console("pending stop state should be cleared after stop ack");
                return 10;
            }

            Log.Console("client pending stop suppress stale authority tail passed");
            return ErrorCode.ERR_Success;
        }
    }

    internal static class ClientInputReplayTestHelper
    {
        private const int ReplayFixedStepMs = 16;
        private const float MinReplayStepSeconds = 0.001f;

        public static bool TryPrepareClientMoveContext(Scene clientRoot, out Unit unit, out Entity inputSystem, out Entity interpolation, out string error)
        {
            unit = null;
            inputSystem = null;
            interpolation = null;
            error = string.Empty;

            if (clientRoot == null || clientRoot.IsDisposed)
            {
                error = "client root is null";
                return false;
            }

            Scene currentScene = clientRoot.CurrentScene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                error = "current scene is null";
                return false;
            }

            unit = ClientUnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (unit == null || unit.IsDisposed)
            {
                error = "my unit is null";
                return false;
            }

            return TryPrepareMoveReplayContext(unit, out inputSystem, out interpolation, out error);
        }

        public static bool TryPrepareMoveReplayContext(Unit unit, out Entity inputSystem, out Entity interpolation, out string error)
        {
            inputSystem = null;
            interpolation = null;
            error = string.Empty;

            if (unit == null || unit.IsDisposed)
            {
                error = "unit is null";
                return false;
            }

            Type inputSystemType = GetRequiredType("ET.Client.InputSystemComponent");
            inputSystem = unit.GetComponent(inputSystemType) ?? unit.AddComponent(inputSystemType);
            if (inputSystem == null || inputSystem.IsDisposed)
            {
                error = "input system is null";
                return false;
            }

            Type interpolationType = GetRequiredType("ET.Client.UnitViewInterpolationComponent");
            interpolation = unit.GetComponent(interpolationType) ?? unit.AddComponent(interpolationType);
            if (interpolation == null || interpolation.IsDisposed)
            {
                error = "interpolation component is null";
                return false;
            }

            return true;
        }

        public static void ResetInterpolation(Entity interpolation, float3 authoritativePosition)
        {
            Type interpolationType = interpolation.GetType();
            SetField(interpolation, "PredictionEnabled", true);
            SetField(interpolation, "AuthoritativePosition", CreateVector3(interpolationType, authoritativePosition.x, authoritativePosition.y, authoritativePosition.z));
            SetField(interpolation, "TargetPosition", CreateVector3(interpolationType, authoritativePosition.x, authoritativePosition.y, authoritativePosition.z));
            SetField(interpolation, "PredictedDelta", CreateVector3(interpolationType, 0f, 0f, 0f));
            SetField(interpolation, "VisualCorrection", CreateVector3(interpolationType, 0f, 0f, 0f));
            SetField(interpolation, "PredictedDirection", CreateVector3(interpolationType, 0f, 0f, 0f));
            SetField(interpolation, "PredictedSpeed", 0f);
            SetField(interpolation, "PositionPredictionDirection", CreateVector3(interpolationType, 0f, 0f, 0f));
            SetField(interpolation, "PositionPredictionSpeed", 0f);
            SetField(interpolation, "PositionPredictionBlocked", false);
            SetField(interpolation, "HoldLocalPredictionOnStationarySync", false);
            SetField(interpolation, "WallBlockedCount", 0);
        }

        public static void ResetInputSystem(Entity inputSystem)
        {
            Type inputSystemType = inputSystem.GetType();
            SetField(inputSystem, "KeyboardMoveInput", CreateVector2(inputSystemType, 0f, 0f));
            SetField(inputSystem, "JoystickMoveInput", CreateVector2(inputSystemType, 0f, 0f));
            SetField(inputSystem, "LastSentMoveInput", CreateVector2(inputSystemType, 0f, 0f));
            SetField(inputSystem, "LastAcknowledgedMoveInputSequence", 0u);
            SetField(inputSystem, "PendingStopActive", false);
            SetField(inputSystem, "PendingStopSequence", 0u);
            SetField(inputSystem, "PendingStopAnchorPosition", CreateFieldValue(inputSystem, "PendingStopAnchorPosition", 0f, 0f, 0f));
            SetField(inputSystem, "PendingStopInitialAnchorDelta", CreateFieldValue(inputSystem, "PendingStopInitialAnchorDelta", 0f, 0f, 0f));
            IList pendingMoveInputs = GetPendingMoveInputs(inputSystem);
            pendingMoveInputs.Clear();
        }

        public static void SetPendingStopState(Entity inputSystem, uint sequence, float3 anchorPosition, float3 authoritativePosition)
        {
            float3 initialAnchorDelta = anchorPosition - authoritativePosition;
            SetField(inputSystem, "PendingStopActive", true);
            SetField(inputSystem, "PendingStopSequence", sequence);
            SetField(inputSystem, "PendingStopAnchorPosition", CreateFieldValue(inputSystem, "PendingStopAnchorPosition", anchorPosition.x, anchorPosition.y, anchorPosition.z));
            SetField(inputSystem, "PendingStopInitialAnchorDelta", CreateFieldValue(inputSystem, "PendingStopInitialAnchorDelta", initialAnchorDelta.x, initialAnchorDelta.y, initialAnchorDelta.z));
        }

        public static bool TryReconcileAuthoritativeMove(Entity inputSystem, Unit unit, Entity interpolation, float3 authoritativePosition,
            uint lastProcessedInputSequence)
        {
            Type inputSystemSystemType = GetRequiredType("ET.Client.InputSystemComponentSystem");
            return (bool)InvokeStatic(inputSystemSystemType, "TryReconcileAuthoritativeMove",
                inputSystem, unit, interpolation, authoritativePosition, lastProcessedInputSequence);
        }

        public static float3 SimulateReplay(Unit unit, float3 startPosition, IList samples, long now)
        {
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            float3 position = startPosition;
            for (int i = 0; i < samples.Count; ++i)
            {
                object sample = samples[i];
                long sampleTime = GetField<long>(sample, "ClientTime");
                long segmentEndTime = i + 1 < samples.Count ? Math.Min(GetField<long>(samples[i + 1], "ClientTime"), now) : now;
                long segmentDurationMs = Math.Max(0, segmentEndTime - sampleTime);
                float sampleSpeed = GetField<float>(sample, "Speed");
                float2 sampleDirection2 = ReadFloat2(sample, "WorldDirection");
                if (segmentDurationMs <= 0 || sampleSpeed < 0.01f || math.lengthsq(sampleDirection2) < 0.000001f)
                {
                    continue;
                }

                float3 direction = new float3(sampleDirection2.x, 0f, sampleDirection2.y);
                long remainingMs = segmentDurationMs;
                while (remainingMs > 0)
                {
                    long stepMs = Math.Min(remainingMs, ReplayFixedStepMs);
                    float stepSeconds = math.max(stepMs / 1000f, MinReplayStepSeconds);
                    float3 expectedNextPosition = position + direction * sampleSpeed * stepSeconds;
                    expectedNextPosition.y = position.y;
                    if (pathfinding != null)
                    {
                        pathfinding.TryMoveAlongSurface(position, expectedNextPosition, out position);
                    }
                    else
                    {
                        position = expectedNextPosition;
                    }

                    remainingMs -= stepMs;
                }
            }

            return position - startPosition;
        }

        public static IList GetPendingMoveInputs(object inputSystem)
        {
            FieldInfo fieldInfo = inputSystem.GetType().GetField("PendingMoveInputs");
            if (fieldInfo == null)
            {
                throw new Exception("PendingMoveInputs field not found");
            }

            return fieldInfo.GetValue(inputSystem) as IList ?? throw new Exception("PendingMoveInputs is not IList");
        }

        public static object CreatePendingMoveInputSample(Type sampleType, uint sequence, float dirX, float dirY, float speed, long clientTime)
        {
            object sample = Activator.CreateInstance(sampleType);
            SetField(sample, "Sequence", sequence);
            SetField(sample, "WorldDirection", CreateVector2(sampleType, dirX, dirY));
            SetField(sample, "Speed", speed);
            SetField(sample, "ClientTime", clientTime);
            return sample;
        }

        public static Type GetRequiredType(string fullTypeName)
        {
            Type type = Type.GetType(fullTypeName);
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            string[] candidateAssemblies =
            {
                "ET.ModelView",
                "ET.HotfixView",
                "ET.Model",
                "ET.Hotfix",
            };
            foreach (string assemblyName in candidateAssemblies)
            {
                try
                {
                    Assembly assembly = Assembly.Load(assemblyName);
                    type = assembly?.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            string[] candidatePaths =
            {
                Path.Combine(Environment.CurrentDirectory, "Temp", "Bin", "Debug", "ET.ModelView", "ET.ModelView.dll"),
                Path.Combine(Environment.CurrentDirectory, "Temp", "Bin", "Debug", "ET.HotfixView", "ET.HotfixView.dll"),
            };
            foreach (string assemblyPath in candidatePaths)
            {
                try
                {
                    if (!File.Exists(assemblyPath))
                    {
                        continue;
                    }

                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    type = assembly?.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            throw new Exception($"type not found: {fullTypeName}");
        }

        public static object CreateVector3(Type ownerType, float x, float y, float z)
        {
            FieldInfo fieldInfo = ownerType.GetField("TargetPosition");
            Type vectorType = fieldInfo?.FieldType;
            if (vectorType == null)
            {
                throw new Exception("TargetPosition field not found");
            }

            return Activator.CreateInstance(vectorType, x, y, z);
        }

        public static object CreateVector2(Type ownerType, float x, float y)
        {
            FieldInfo fieldInfo = ownerType.GetField("KeyboardMoveInput") ?? ownerType.GetField("WorldDirection");
            Type vectorType = fieldInfo?.FieldType;
            if (vectorType == null)
            {
                throw new Exception("Vector2 field not found");
            }

            return Activator.CreateInstance(vectorType, x, y);
        }

        public static object CreateFieldValue(object instance, string fieldName, params object[] args)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            return Activator.CreateInstance(fieldInfo.FieldType, args);
        }

        public static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            fieldInfo.SetValue(instance, value);
        }

        public static T GetField<T>(object instance, string fieldName)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            return (T)fieldInfo.GetValue(instance);
        }

        public static float3 ReadVector3(object instance, string fieldName)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            object value = fieldInfo.GetValue(instance);
            if (value == null)
            {
                return float3.zero;
            }

            Type vectorType = value.GetType();
            float x = Convert.ToSingle(vectorType.GetField("x")?.GetValue(value) ?? 0f);
            float y = Convert.ToSingle(vectorType.GetField("y")?.GetValue(value) ?? 0f);
            float z = Convert.ToSingle(vectorType.GetField("z")?.GetValue(value) ?? 0f);
            return new float3(x, y, z);
        }

        public static float2 ReadFloat2(object instance, string fieldName)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            object value = fieldInfo.GetValue(instance);
            if (value == null)
            {
                return float2.zero;
            }

            Type vectorType = value.GetType();
            float x = Convert.ToSingle(vectorType.GetField("x")?.GetValue(value) ?? 0f);
            float y = Convert.ToSingle(vectorType.GetField("y")?.GetValue(value) ?? 0f);
            return new float2(x, y);
        }

        public static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (methodInfo.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                object[] invokeArgs = new object[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    invokeArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
                }

                return methodInfo.Invoke(null, invokeArgs);
            }

            throw new Exception($"method not found: {type.FullName}.{methodName}/{args.Length}");
        }

        private static object ConvertArgument(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            if (targetType.FullName == "Unity.Mathematics.float3")
            {
                float3 source = (float3)value;
                return Activator.CreateInstance(targetType, source.x, source.y, source.z);
            }

            if (targetType.FullName == "Unity.Mathematics.float2")
            {
                float2 source = (float2)value;
                return Activator.CreateInstance(targetType, source.x, source.y);
            }

            return value;
        }
    }
}
