using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(InputSystemComponent))]
    public static partial class InputSystemComponentSystem
    {
        private const int ContinuousMoveSyncIntervalMs = 33;
        private const int MaxPendingMoveHistoryCount = 64;
        private const int ReplayFixedStepMs = 16;
        private const float ImmediateDirectionChangeDot = 0.9238795f;
        private const float DefaultPredictionDeltaTimeSeconds = 0.016f;
        private const float MinPredictionDeltaTimeSeconds = 0.001f;

        [EntitySystem]
        private static void Destroy(this InputSystemComponent self)
        {
            self.InputSystem?.Dispose();
        }
        
        [EntitySystem]
        private static void Awake(this InputSystemComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();

            self.InputSystem = new InputSystem();
            self.InputSystem.Player.Enable();
            self.KeyboardMoveInput = Vector2.zero;
            self.JoystickMoveInput = Vector2.zero;
            self.LastSentMoveInput = Vector2.zero;
            self.LastMoveSendTime = 0;
            self.MoveInputSequence = 0;
            self.LastAcknowledgedMoveInputSequence = 0;
            self.LastInputTraceLogTime = 0;
            self.PendingStopActive = false;
            self.PendingStopSequence = 0;
            self.PendingStopAnchorPosition = Vector3.zero;
            self.PendingStopInitialAnchorDelta = Vector3.zero;
            self.PendingMoveInputs ??= new List<PendingMoveInputSample>();
            self.PendingMoveInputs.Clear();

            self.InputSystem.Player.Jump.started += self.Jump;
            self.InputSystem.Player.SelectTarget.canceled += self.SelectTarget;
            self.InputSystem.Player.ChangeTarget.canceled += self.ChangeTarget;
            self.InputSystem.Player.Spell.started += self.CastSpell;
            self.InputSystem.Player.PetAttack.started += self.PetAttack;
        }

        [EntitySystem]
        private static void Update(this InputSystemComponent self)
        {
            Vector2 keyboardInput = self.InputSystem.Player.Move.ReadValue<Vector2>();
            if (keyboardInput.sqrMagnitude < 0.000001f)
            {
                keyboardInput = Vector2.zero;
            }

            if ((keyboardInput - self.KeyboardMoveInput).sqrMagnitude > 0.000001f)
            {
                self.KeyboardMoveInput = keyboardInput;
                self.PressTime = TimeInfo.Instance.ClientNow();
                if (self.JoystickMoveInput.sqrMagnitude < 0.000001f)
                {
                    self.SyncDirectionalMove(true);
                }
            }

            if (self.JoystickMoveInput.sqrMagnitude < 0.000001f &&
                keyboardInput != Vector2.zero &&
                TimeInfo.Instance.ClientNow() - self.PressTime > 16)
            {
                self.PressTime = TimeInfo.Instance.ClientNow();
                self.SyncDirectionalMove(false);
            }

            // 摇杆长按时 UI 不一定持续触发事件，这里统一走同一套发送节奏，
            // 把“最后一个未确认移动片段”限制在可接受范围内，避免松手后尾走过长。
            if (self.JoystickMoveInput.sqrMagnitude > 0.000001f)
            {
                self.SyncDirectionalMove(false);
            }

            self.SyncLocalPredictionState();
        }

        public static void SetJoystickMoveInput(this InputSystemComponent self, Vector2 joystickInput, bool forceSync)
        {
            Vector2 previousInput = self.JoystickMoveInput;
            if (joystickInput.sqrMagnitude < 0.000001f)
            {
                joystickInput = Vector2.zero;
            }

            self.JoystickMoveInput = joystickInput;
            self.TraceJoystickInputEvent(previousInput, joystickInput, forceSync);
            self.SyncDirectionalMove(forceSync);
        }

        private static void SyncDirectionalMove(this InputSystemComponent self, bool forceSync)
        {
            Vector2 worldDirection = self.GetSelectedWorldMoveDirection();
            if (!self.ShouldSendDirectionalMove(worldDirection, forceSync, out string sendReason))
            {
                return;
            }

            self.UpdatePendingStopStateOnDirectionalSend(worldDirection, sendReason, self.MoveInputSequence + 1);
            self.LastSentMoveInput = worldDirection;
            self.LastMoveSendTime = TimeInfo.Instance.ClientNow();
            self.SendDirectionalMove(worldDirection, sendReason);
        }

        private static Vector2 GetSelectedMoveInput(this InputSystemComponent self)
        {
            Vector2 selectedInput = self.JoystickMoveInput.sqrMagnitude > 0.000001f
                ? self.JoystickMoveInput
                : self.KeyboardMoveInput;

            if (self.IsJumping)
            {
                return Vector2.zero;
            }

            return selectedInput;
        }

        private static Vector2 GetSelectedWorldMoveDirection(this InputSystemComponent self)
        {
            return self.ToWorldMoveDirection(self.GetSelectedMoveInput());
        }

        private static string GetSelectedMoveSource(this InputSystemComponent self)
        {
            if (self.IsJumping)
            {
                return "jump-stop";
            }

            if (self.JoystickMoveInput.sqrMagnitude > 0.000001f)
            {
                return "joystick";
            }

            if (self.KeyboardMoveInput.sqrMagnitude > 0.000001f)
            {
                return "keyboard";
            }

            return "idle";
        }

        private static Vector2 ToWorldMoveDirection(this InputSystemComponent self, Vector2 input)
        {
            if (input.sqrMagnitude < 0.000001f)
            {
                return Vector2.zero;
            }

            input = input.normalized;
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            if (cinemachineComponent == null || cinemachineComponent.IsDisposed || cinemachineComponent.Follow == null)
            {
                return input;
            }

            Vector3 eulerAngles = cinemachineComponent.Follow.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;

            Vector3 rotV = Quaternion.Euler(eulerAngles) * new float3(input.x, 0, input.y);
            Vector2 worldDirection = new Vector2(rotV.x, rotV.z);
            if (worldDirection.sqrMagnitude < 0.000001f)
            {
                return Vector2.zero;
            }

            return worldDirection.normalized;
        }

        private static bool ShouldSendDirectionalMove(this InputSystemComponent self, Vector2 worldDirection, bool forceSync, out string sendReason)
        {
            sendReason = forceSync ? "force" : string.Empty;
            if (forceSync)
            {
                return true;
            }

            bool sameDirection = (worldDirection - self.LastSentMoveInput).sqrMagnitude < 0.000001f;
            bool currentStopped = worldDirection.sqrMagnitude < 0.000001f;
            bool previousStopped = self.LastSentMoveInput.sqrMagnitude < 0.000001f;
            if (currentStopped)
            {
                sendReason = "stop";
                return !previousStopped;
            }

            if (previousStopped)
            {
                sendReason = "start";
                return true;
            }

            if (!sameDirection)
            {
                float directionDot = Vector2.Dot(worldDirection.normalized, self.LastSentMoveInput.normalized);
                if (directionDot <= ImmediateDirectionChangeDot)
                {
                    sendReason = "sharp-turn";
                    return true;
                }
            }

            if (TimeInfo.Instance.ClientNow() - self.LastMoveSendTime >= ContinuousMoveSyncIntervalMs)
            {
                sendReason = sameDirection ? "hold" : "coalesced-turn";
                return true;
            }

            return false;
        }

        private static void UpdatePendingStopStateOnDirectionalSend(this InputSystemComponent self, Vector2 worldDirection, string sendReason,
            uint pendingStopSequence)
        {
            if (worldDirection.sqrMagnitude > 0.000001f)
            {
                self.ClearPendingStopReplayState();
                return;
            }

            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed || !unit.IsMyUnit())
            {
                return;
            }

            UnitViewInterpolationComponent interpolationComponent = unit.GetComponent<UnitViewInterpolationComponent>();
            if (interpolationComponent == null || interpolationComponent.IsDisposed || !interpolationComponent.PredictionEnabled)
            {
                return;
            }

            bool hasLocalMotion = interpolationComponent.PredictedSpeed > 0.01f ||
                interpolationComponent.PositionPredictionSpeed > 0.01f ||
                interpolationComponent.PredictedDelta.sqrMagnitude > 0.000001f ||
                interpolationComponent.VisualCorrection.sqrMagnitude > 0.000001f;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            Vector3 currentViewPosition = gameObjectComponent?.Transform != null
                ? gameObjectComponent.Transform.position
                : interpolationComponent.TargetPosition;
            self.PendingStopActive = true;
            self.PendingStopSequence = pendingStopSequence;
            self.PendingStopAnchorPosition = currentViewPosition;
            self.PendingStopInitialAnchorDelta = currentViewPosition - interpolationComponent.AuthoritativePosition;
            self.PendingStopInitialAnchorDelta.y = 0f;

            if (!hasLocalMotion)
            {
                return;
            }

            interpolationComponent.FreezePredictionOnLocalStop(currentViewPosition);

            Log.Info(
                $"[NavMove][StopFreeze] unitId={unit.Id}, reason={sendReason}, seq={pendingStopSequence}, viewPos={currentViewPosition}, " +
                $"authPos={interpolationComponent.AuthoritativePosition}, targetPos={interpolationComponent.TargetPosition}, predictedDelta={interpolationComponent.PredictedDelta}, " +
                $"visualCorrection={interpolationComponent.VisualCorrection}, predictedSpeed={interpolationComponent.PredictedSpeed:F3}, constrainedSpeed={interpolationComponent.PositionPredictionSpeed:F3}");
        }

        private static void ClearPendingStopReplayState(this InputSystemComponent self)
        {
            self.PendingStopActive = false;
            self.PendingStopSequence = 0;
            self.PendingStopAnchorPosition = Vector3.zero;
            self.PendingStopInitialAnchorDelta = Vector3.zero;
        }

        private static void SendDirectionalMove(this InputSystemComponent self, Vector2 worldDirection, string sendReason)
        {
            ClientSenderComponent sender = self.Root().GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                return;
            }

            Vector2 selectedInput = self.GetSelectedMoveInput();
            string inputSource = self.GetSelectedMoveSource();
            Unit unit = self.GetParent<Unit>();
            long unitId = unit != null ? unit.Id : 0;
            float inputSpeed = unit?.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            uint inputSequence = ++self.MoveInputSequence;
            long clientNow = TimeInfo.Instance.ClientNow();
            Log.Info(
                $"[NavMove][InputSend] unitId={unitId}, source={inputSource}, reason={sendReason}, seq={inputSequence}, joystick=({self.JoystickMoveInput.x:F2}, {self.JoystickMoveInput.y:F2}), " +
                $"keyboard=({self.KeyboardMoveInput.x:F2}, {self.KeyboardMoveInput.y:F2}), selected=({selectedInput.x:F2}, {selectedInput.y:F2}), " +
                $"world=({worldDirection.x:F2}, {worldDirection.y:F2}), jumping={self.IsJumping}");
            Log.Info(
                $"[NavMove][TraceInputSend] unitId={unitId}, clientNow={clientNow}, seq={inputSequence}, dirX={worldDirection.x:F6}, dirZ={worldDirection.y:F6}, source={inputSource}, reason={sendReason}");

            C2M_JoystickInput msg = C2M_JoystickInput.Create();
            msg.DirX = worldDirection.x;
            msg.DirZ = worldDirection.y;
            msg.InputSequence = inputSequence;
            sender.Send(msg);
            self.RecordPendingMoveInput(inputSequence, worldDirection, inputSpeed, clientNow);
        }

        private static void RecordPendingMoveInput(this InputSystemComponent self, uint inputSequence, Vector2 worldDirection, float speed, long clientTime)
        {
            self.PendingMoveInputs ??= new List<PendingMoveInputSample>();

            self.PendingMoveInputs.Add(new PendingMoveInputSample
            {
                Sequence = inputSequence,
                WorldDirection = worldDirection,
                Speed = speed,
                ClientTime = clientTime,
            });

            if (self.PendingMoveInputs.Count > MaxPendingMoveHistoryCount)
            {
                self.PendingMoveInputs.RemoveRange(0, self.PendingMoveInputs.Count - MaxPendingMoveHistoryCount);
            }
        }

        private static void TrimAcknowledgedPendingMoveInputs(this InputSystemComponent self, uint lastProcessedInputSequence)
        {
            if (lastProcessedInputSequence <= self.LastAcknowledgedMoveInputSequence)
            {
                return;
            }

            self.LastAcknowledgedMoveInputSequence = lastProcessedInputSequence;
            if (self.PendingMoveInputs == null || self.PendingMoveInputs.Count == 0)
            {
                if (self.PendingStopActive && lastProcessedInputSequence >= self.PendingStopSequence)
                {
                    self.ClearPendingStopReplayState();
                }

                return;
            }

            int writeIndex = 0;
            for (int i = 0; i < self.PendingMoveInputs.Count; ++i)
            {
                PendingMoveInputSample sample = self.PendingMoveInputs[i];
                if (sample.Sequence <= lastProcessedInputSequence)
                {
                    continue;
                }

                self.PendingMoveInputs[writeIndex++] = sample;
            }

            if (writeIndex < self.PendingMoveInputs.Count)
            {
                self.PendingMoveInputs.RemoveRange(writeIndex, self.PendingMoveInputs.Count - writeIndex);
            }

            if (self.PendingStopActive && lastProcessedInputSequence >= self.PendingStopSequence)
            {
                self.ClearPendingStopReplayState();
            }
        }

        public static bool TryReconcileAuthoritativeMove(this InputSystemComponent self, Unit unit,
            UnitViewInterpolationComponent interpolationComponent, float3 authoritativePosition, uint lastProcessedInputSequence)
        {
            if (unit == null || unit.IsDisposed || interpolationComponent == null || interpolationComponent.IsDisposed)
            {
                return false;
            }

            self.TrimAcknowledgedPendingMoveInputs(lastProcessedInputSequence);

            Vector2 currentWorldDirection2D = self.GetSelectedWorldMoveDirection();
            bool hasCurrentInput = currentWorldDirection2D.sqrMagnitude > 0.000001f;
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            Vector3 replayPosition = authoritativePosition;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            Vector3 currentViewPosition = gameObjectComponent?.Transform != null
                ? gameObjectComponent.Transform.position
                : interpolationComponent.TargetPosition;
            self.ReplayPendingMoveInputs(unit, pathfinding, ref replayPosition);

            float configuredMoveSpeed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            float replayLeadSpeed = hasCurrentInput ? configuredMoveSpeed : 0f;
            if (!hasCurrentInput)
            {
                self.TryCapReplayPositionByPendingStopAnchor(authoritativePosition, currentViewPosition, ref replayPosition);
            }

            replayPosition = interpolationComponent.ClampReplayPredictionPosition(unit, authoritativePosition, replayPosition, replayLeadSpeed);

            float3 currentInputDirection = hasCurrentInput
                ? new float3(currentWorldDirection2D.x, 0f, currentWorldDirection2D.y)
                : float3.zero;

            float3 constrainedDirection = float3.zero;
            float constrainedSpeed = 0f;
            bool blocked = false;
            if (replayLeadSpeed > 0.01f && math.lengthsq(currentInputDirection) > 0.000001f)
            {
                if (pathfinding != null)
                {
                    try
                    {
                        self.ResolveLocalConstrainedPrediction(unit, pathfinding, replayPosition, currentInputDirection, replayLeadSpeed,
                            self.GetPredictionDeltaTimeSeconds(), out constrainedDirection, out constrainedSpeed, out blocked);
                    }
                    catch (System.Exception e)
                    {
                        Log.Warning($"[NavMove][ClientReplay] fallback move. unitId={unit.Id}, error={e.Message}");
                        constrainedDirection = currentInputDirection;
                        constrainedSpeed = replayLeadSpeed;
                        blocked = false;
                    }
                }
                else
                {
                    constrainedDirection = currentInputDirection;
                    constrainedSpeed = replayLeadSpeed;
                }
            }

            interpolationComponent.ApplyAuthoritativeReplay(currentViewPosition, authoritativePosition, replayPosition - (Vector3)authoritativePosition,
                currentInputDirection, replayLeadSpeed, constrainedDirection, constrainedSpeed, blocked);
            return true;
        }

        private static void TryCapReplayPositionByPendingStopAnchor(this InputSystemComponent self,
            float3 authoritativePosition, Vector3 currentViewPosition, ref Vector3 replayPosition)
        {
            if (!self.PendingStopActive)
            {
                return;
            }

            // 本地已经执行停步冻结后，等待 stop ack 期间不能再让旧移动尾包把目标重新往前推。
            // 这里直接以当前可见位置作为回放上限，保持“手已松开就先停住”的本地体感。
            replayPosition = currentViewPosition;
        }

        private static void ReplayPendingMoveInputs(this InputSystemComponent self, Unit unit, PathfindingComponent pathfinding, ref Vector3 replayPosition)
        {
            if (self.PendingMoveInputs == null || self.PendingMoveInputs.Count == 0)
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            for (int i = 0; i < self.PendingMoveInputs.Count; ++i)
            {
                PendingMoveInputSample sample = self.PendingMoveInputs[i];
                long segmentEndTime = i + 1 < self.PendingMoveInputs.Count
                    ? System.Math.Min(self.PendingMoveInputs[i + 1].ClientTime, now)
                    : now;
                long segmentDurationMs = System.Math.Max(0, segmentEndTime - sample.ClientTime);
                if (segmentDurationMs <= 0)
                {
                    continue;
                }

                self.ReplayPendingMoveSegment(unit, pathfinding, sample, segmentDurationMs, ref replayPosition);
            }
        }

        private static void ReplayPendingMoveSegment(this InputSystemComponent self, Unit unit, PathfindingComponent pathfinding,
            PendingMoveInputSample sample, long segmentDurationMs, ref Vector3 replayPosition)
        {
            if (sample.Speed < 0.01f || sample.WorldDirection.sqrMagnitude < 0.000001f)
            {
                return;
            }

            float3 inputDirection = new float3(sample.WorldDirection.x, 0f, sample.WorldDirection.y);
            long remainingDurationMs = segmentDurationMs;
            while (remainingDurationMs > 0)
            {
                long stepDurationMs = System.Math.Min(remainingDurationMs, ReplayFixedStepMs);
                float stepDeltaTimeSeconds = math.max(stepDurationMs / 1000f, MinPredictionDeltaTimeSeconds);
                if (pathfinding != null)
                {
                    if (self.TryResolveConstrainedStep(unit, pathfinding, replayPosition, inputDirection, sample.Speed, stepDeltaTimeSeconds,
                            out float3 nextPosition, out _, out _))
                    {
                        replayPosition = nextPosition;
                    }
                }
                else
                {
                    replayPosition += (Vector3)(inputDirection * sample.Speed * stepDeltaTimeSeconds);
                }

                remainingDurationMs -= stepDurationMs;
            }
        }

        private static float GetPredictionDeltaTimeSeconds(this InputSystemComponent self)
        {
            return Time.deltaTime > 0f ? Time.deltaTime : DefaultPredictionDeltaTimeSeconds;
        }

        private static void SyncLocalPredictionState(this InputSystemComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            Vector2 worldDirection = self.GetSelectedWorldMoveDirection();
            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            UnitViewInterpolationComponent interpolationComponent = unit.GetComponent<UnitViewInterpolationComponent>();
            self.TracePredictionFlow(unit, worldDirection, speed, interpolationComponent);
            if (unit.IsMyUnit())
            {
                if (worldDirection.sqrMagnitude < 0.000001f || speed < 0.01f)
                {
                    if (interpolationComponent != null)
                    {
                        GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
                        Vector3 currentViewPosition = gameObjectComponent?.Transform != null
                            ? gameObjectComponent.Transform.position
                            : interpolationComponent.TargetPosition;
                        interpolationComponent.FreezePredictionOnLocalStop(currentViewPosition);
                    }
                    return;
                }

                if (interpolationComponent != null && interpolationComponent.PredictionEnabled && Time.deltaTime > 0f)
                {
                    float3 inputDirection = new float3(worldDirection.x, 0f, worldDirection.y);
                    // 本地约束要基于“权威位置 + 纯预测位移”，不能把 VisualCorrection 算进去。
                    // 否则纠偏期间显示层可能暂时偏出 NavMesh，导致客户端本地约束与服务端结果失真。
                    Vector3 predictionStartVector = interpolationComponent.AuthoritativePosition + interpolationComponent.PredictedDelta;
                    float3 predictionStartPos = predictionStartVector;
                    PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
                    float3 constrainedDirection = inputDirection;
                    float constrainedSpeed = speed;
                    bool blocked = false;
                    if (pathfinding != null)
                    {
                        try
                        {
                            self.ResolveLocalConstrainedPrediction(unit, pathfinding, predictionStartPos, inputDirection, speed,
                                out constrainedDirection, out constrainedSpeed, out blocked);
                        }
                        catch (System.Exception e)
                        {
                            Log.Warning($"[NavMove][ClientLocal] fallback move. unitId={unit.Id}, error={e.Message}");
                        }
                    }

                    interpolationComponent.SetPredictionMotion(inputDirection, speed);
                    interpolationComponent.SetConstrainedPredictionMotion(constrainedDirection, constrainedSpeed, blocked);
                    return;
                }
            }

            if (interpolationComponent != null)
            {
                if (worldDirection.sqrMagnitude < 0.000001f || speed < 0.01f)
                {
                    interpolationComponent.SetPredictionMotion(float3.zero, 0f);
                    return;
                }

                float3 predictedDirection = new float3(worldDirection.x, 0f, worldDirection.y);
                interpolationComponent.SetPredictionMotion(predictedDirection, speed);
                return;
            }

            if (worldDirection.sqrMagnitude < 0.000001f || speed < 0.01f || Time.deltaTime <= 0f)
            {
                return;
            }

            float3 direction = new float3(worldDirection.x, 0f, worldDirection.y);
            float3 predictedDelta = direction * speed * Time.deltaTime;
            unit.Position += predictedDelta;

            TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
            if (turnComponent == null || !turnComponent.IsTurning())
            {
                unit.Rotation = quaternion.LookRotation(direction, math.up());
            }
        }

        private static void ResolveLocalConstrainedPrediction(this InputSystemComponent self, Unit unit, PathfindingComponent pathfinding,
            float3 predictionStartPos, float3 inputDirection, float speed, out float3 constrainedDirection, out float constrainedSpeed, out bool blocked)
        {
            self.ResolveLocalConstrainedPrediction(unit, pathfinding, predictionStartPos, inputDirection, speed,
                self.GetPredictionDeltaTimeSeconds(), out constrainedDirection, out constrainedSpeed, out blocked);
        }

        private static void ResolveLocalConstrainedPrediction(this InputSystemComponent self, Unit unit, PathfindingComponent pathfinding,
            float3 predictionStartPos, float3 inputDirection, float speed, float deltaTimeSeconds,
            out float3 constrainedDirection, out float constrainedSpeed, out bool blocked)
        {
            if (self.TryResolveConstrainedStep(unit, pathfinding, predictionStartPos, inputDirection, speed, deltaTimeSeconds,
                    out _, out constrainedDirection, out constrainedSpeed))
            {
                blocked = false;
                return;
            }

            constrainedDirection = float3.zero;
            constrainedSpeed = 0f;
            blocked = true;
        }

        private static bool TryResolveConstrainedStep(this InputSystemComponent self, Unit unit, PathfindingComponent pathfinding,
            float3 startPos, float3 inputDirection, float speed, float deltaTimeSeconds, out float3 nextPosition,
            out float3 constrainedDirection, out float constrainedSpeed)
        {
            if (self.TryGetAlignedConstrainedMotion(pathfinding, startPos, inputDirection, speed, deltaTimeSeconds, out nextPosition,
                    out constrainedDirection, out constrainedSpeed))
            {
                return true;
            }

            if (pathfinding != null)
            {
                float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                if (pathfinding.TryRecastFindNearestPointForMovement(startPos, unitRadius, out float3 projectedStartPos, out float projectedDistance) &&
                    projectedDistance > 0.0005f &&
                    self.TryGetAlignedConstrainedMotion(pathfinding, projectedStartPos, inputDirection, speed, deltaTimeSeconds, out nextPosition,
                        out constrainedDirection, out constrainedSpeed))
                {
                    return true;
                }
            }

            nextPosition = startPos;
            constrainedDirection = float3.zero;
            constrainedSpeed = 0f;
            return false;
        }

        private static bool TryGetAlignedConstrainedMotion(this InputSystemComponent self, PathfindingComponent pathfinding, float3 startPos,
            float3 inputDirection, float speed, float deltaTimeSeconds, out float3 nextPosition, out float3 constrainedDirection,
            out float constrainedSpeed)
        {
            if (deltaTimeSeconds < MinPredictionDeltaTimeSeconds ||
                speed < 0.01f ||
                math.lengthsq(inputDirection) <= 0.000001f)
            {
                nextPosition = startPos;
                constrainedDirection = float3.zero;
                constrainedSpeed = 0f;
                return false;
            }

            float3 expectedNextPos = startPos + inputDirection * speed * deltaTimeSeconds;
            expectedNextPos.y = startPos.y;

            if (pathfinding != null)
            {
                pathfinding.TryMoveAlongSurface(startPos, expectedNextPos, out nextPosition);
            }
            else
            {
                nextPosition = expectedNextPos;
            }

            float3 constrainedDelta = nextPosition - startPos;
            constrainedDelta.y = 0f;
            float constrainedDistance = math.length(new float2(constrainedDelta.x, constrainedDelta.z));
            if (constrainedDistance <= 0.0005f)
            {
                nextPosition = startPos;
                constrainedDirection = float3.zero;
                constrainedSpeed = 0f;
                return false;
            }

            constrainedDirection = math.normalize(new float3(constrainedDelta.x, 0f, constrainedDelta.z));
            float directionDot = math.dot(constrainedDirection, inputDirection);
            if (directionDot <= -0.1f)
            {
                nextPosition = startPos;
                constrainedDirection = float3.zero;
                constrainedSpeed = 0f;
                return false;
            }

            constrainedSpeed = math.min(speed, constrainedDistance / math.max(deltaTimeSeconds, MinPredictionDeltaTimeSeconds));
            return constrainedSpeed > 0.01f;
        }

        private static void TraceJoystickInputEvent(this InputSystemComponent self, Vector2 previousInput, Vector2 currentInput, bool forceSync)
        {
            long now = TimeInfo.Instance.ClientNow();
            bool changed = (currentInput - previousInput).sqrMagnitude > 0.0001f;
            if (!forceSync && !changed && now - self.LastInputTraceLogTime < 80)
            {
                return;
            }

            self.LastInputTraceLogTime = now;
            Unit unit = self.GetParent<Unit>();
            Log.Info(
                $"[NavMove][InputEvent] unitId={unit?.Id ?? 0}, force={forceSync}, previousJoystick=({previousInput.x:F2},{previousInput.y:F2}), currentJoystick=({currentInput.x:F2},{currentInput.y:F2}), keyboard=({self.KeyboardMoveInput.x:F2},{self.KeyboardMoveInput.y:F2}), lastSent=({self.LastSentMoveInput.x:F2},{self.LastSentMoveInput.y:F2}), seq={self.MoveInputSequence}");
        }

        private static void TracePredictionFlow(this InputSystemComponent self, Unit unit, Vector2 worldDirection, float speed,
            UnitViewInterpolationComponent interpolationComponent)
        {
            if (unit == null || !unit.IsMyUnit())
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            bool moving = worldDirection.sqrMagnitude > 0.000001f || (interpolationComponent != null &&
                (interpolationComponent.PredictedDelta.sqrMagnitude > 0.000001f || interpolationComponent.VisualCorrection.sqrMagnitude > 0.000001f));
            if (!moving || now - self.LastInputTraceLogTime < 80)
            {
                return;
            }

            self.LastInputTraceLogTime = now;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            Vector3 viewPosition = gameObjectComponent?.Transform != null ? gameObjectComponent.Transform.position : unit.Position;
            if (interpolationComponent == null)
            {
                Log.Info(
                    $"[NavMove][InputFlow] unitId={unit.Id}, world=({worldDirection.x:F2},{worldDirection.y:F2}), speed={speed:F3}, noInterpolation=True, unitPos={unit.Position}, viewPos={viewPosition}");
                return;
            }

            Log.Info(
                $"[NavMove][InputFlow] unitId={unit.Id}, world=({worldDirection.x:F2},{worldDirection.y:F2}), speed={speed:F3}, unitPos={unit.Position}, viewPos={viewPosition}, authPos={interpolationComponent.AuthoritativePosition}, targetPos={interpolationComponent.TargetPosition}, predictedDelta={interpolationComponent.PredictedDelta}, visualCorrection={interpolationComponent.VisualCorrection}, predictedSpeed={interpolationComponent.PredictedSpeed:F3}, constrainedSpeed={interpolationComponent.PositionPredictionSpeed:F3}, blocked={interpolationComponent.PositionPredictionBlocked}, hold={interpolationComponent.HoldLocalPredictionOnStationarySync}");
        }

        private static void SelectTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            const float maxDistance = 1000.0f;
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask("Unit")))
            {
                return;
            }
            GameObject clickedObject = hit.collider.gameObject;

            GameObjectEntityRef gameObjectEntityRef = clickedObject.GetComponent<GameObjectEntityRef>();
            if (gameObjectEntityRef is null)
            {
                return;
            }

            if (gameObjectEntityRef.Entity is not Unit targetUnit)
            {
                return;
            }
            
            UnitClickHelper.Click(self.Root(), targetUnit.Id).Coroutine();
        }

        private static void ChangeTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {

        }

        private static void Jump(this InputSystemComponent self, InputAction.CallbackContext context)
        {

        }

        private static void PetAttack(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            C2M_PetAttack c2MPetAttack = C2M_PetAttack.Create();
            c2MPetAttack.UnitId = self.GetParent<Unit>().GetComponent<TargetComponent>().Unit.Id;
            self.Root().GetComponent<ClientSenderComponent>().Send(c2MPetAttack);
        }

        private static void CastSpell(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (context.control is not KeyControl keyControl)
            {
                return;
            }

            if (keyControl.keyCode != Key.Digit1)
            {
                return;
            }

            MainPanelComponent mainPanel = self.Root().YIUIMgr().GetPanel<MainPanelComponent>();
            ActionBarComponent actionBar = mainPanel?.UIActionBar;
           // int spellConfigId = actionBar?.UISlot12?.u_DataId?.GetValue() ?? 0;
           int spellConfigId = 0;
           //todo 更新actionBar的数据，读表，用自己的slot
           if (spellConfigId <= 0)
            {
                Log.Warning("[Input] cast spell skipped: action bar skill not bound");
                return;
            }

            EventSystem.Instance.Publish(self.Scene(), new OnSpellTrigger
            {
                Unit = self.GetParent<Unit>(),
                SpellConfigId = spellConfigId,
            });
        }
    }
}
