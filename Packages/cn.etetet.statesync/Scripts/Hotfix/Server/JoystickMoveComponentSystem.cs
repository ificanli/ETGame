using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(JoystickMoveComponent))]
    public static partial class JoystickMoveComponentSystem
    {
        private const float MinTickDeltaTime = 0.001f;
        private const float StopDirectionEpsilonSqr = 0.0001f;

        [Invoke(HighFrequencyInvokeType.Move16msTick)]
        public class JoystickMoveTickInvoker : AInvokeHandler<HighFrequencyTickCallback>
        {
            public override void Handle(HighFrequencyTickCallback args)
            {
                Entity entity = args.Entity;
                JoystickMoveComponent self = entity as JoystickMoveComponent;
                if (self == null || self.IsDisposed)
                {
                    return;
                }

                self.TickFixedStep(args.DeltaTimeMs, args.NowMs);
            }
        }

        [Invoke(HighFrequencyInvokeType.Move16msRemoved)]
        public class JoystickMoveRemovedInvoker : AInvokeHandler<HighFrequencyEntityRemovedCallback>
        {
            public override void Handle(HighFrequencyEntityRemovedCallback args)
            {
                Entity entity = args.Entity;
                JoystickMoveComponent self = entity as JoystickMoveComponent;
                if (self == null || self.IsDisposed)
                {
                    return;
                }

                self.OnMoveChannelRemoved(args);
            }
        }

        [EntitySystem]
        private static void Awake(this JoystickMoveComponent self)
        {
            self.Direction = float3.zero;
            self.IsMoveChannelRegistered = false;
            self.PendingStopBroadcast = false;
            self.LastTickTraceLogTime = 0;
            self.LastNavResolveLogTime = 0;
            self.LastInputProcessTime = 0;
            self.MoveSequence = 0;
            self.LastClientInputSequence = 0;
            self.LastStaleInputLogTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this JoystickMoveComponent self)
        {
            self.LeaveMoveChannel();
        }

        /// <summary>
        /// 更新摇杆方向并接入统一高频调度器
        /// </summary>
        public static void SetDirection(this JoystickMoveComponent self, float dirX, float dirZ)
        {
            float3 newDir = new float3(dirX, 0, dirZ);

            // 超过1时归一化（摇杆输入已在客户端归一化，这里做安全检查）
            float len = math.length(newDir);
            if (len > 1f)
            {
                newDir /= len;
            }

            self.Direction = newDir;

            if (len < 0.01f)
            {
                self.RequestStopMove();
            }
            else
            {
                self.PendingStopBroadcast = false;
                self.EnterMoveChannel();
            }
        }

        private static void EnterMoveChannel(this JoystickMoveComponent self)
        {
            Scene scene = self.Scene();
            HighFrequencySchedulerComponent scheduler = scene?.GetComponent<HighFrequencySchedulerComponent>();
            if (scheduler == null)
            {
                self.IsMoveChannelRegistered = false;
                Log.Warning($"[HighFreq][Move] scheduler missing when register move. unitId={self.GetParent<Unit>()?.Id ?? 0}");
                return;
            }

            if (scheduler.AddEntity(HighFrequencyChannelId.Move16ms, self))
            {
                self.IsMoveChannelRegistered = true;
                return;
            }

            self.IsMoveChannelRegistered = false;
        }

        private static void LeaveMoveChannel(this JoystickMoveComponent self)
        {
            Scene scene = self.Scene();
            HighFrequencySchedulerComponent scheduler = scene?.GetComponent<HighFrequencySchedulerComponent>();
            if (scheduler == null)
            {
                self.IsMoveChannelRegistered = false;
                self.PendingStopBroadcast = false;
                return;
            }

            scheduler.RequestRemoveEntity(HighFrequencyChannelId.Move16ms, self, out _);
            self.IsMoveChannelRegistered = false;
            self.PendingStopBroadcast = false;
        }

        private static void RequestStopMove(this JoystickMoveComponent self)
        {
            Scene scene = self.Scene();
            HighFrequencySchedulerComponent scheduler = scene?.GetComponent<HighFrequencySchedulerComponent>();
            if (scheduler == null)
            {
                self.IsMoveChannelRegistered = false;
                self.PendingStopBroadcast = false;
                self.BroadcastStop();
                return;
            }

            if (!scheduler.RequestRemoveEntity(HighFrequencyChannelId.Move16ms, self, out bool deferred))
            {
                self.IsMoveChannelRegistered = false;
                self.PendingStopBroadcast = false;
                self.BroadcastStop();
                return;
            }

            if (deferred)
            {
                self.PendingStopBroadcast = true;
                return;
            }

            self.IsMoveChannelRegistered = false;
            self.PendingStopBroadcast = false;
            self.BroadcastStop();
        }

        public static void OnMoveChannelRemoved(this JoystickMoveComponent self, HighFrequencyEntityRemovedCallback args)
        {
            self.IsMoveChannelRegistered = false;
            if (!self.PendingStopBroadcast)
            {
                return;
            }

            self.PendingStopBroadcast = false;
            if (math.lengthsq(self.Direction) <= StopDirectionEpsilonSqr)
            {
                self.BroadcastStop();
            }
        }

        public static void TickFixedStep(this JoystickMoveComponent self, long deltaTimeMs, long nowMs)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (math.lengthsq(self.Direction) <= StopDirectionEpsilonSqr)
            {
                return;
            }

            float fixedDeltaTime = math.max(deltaTimeMs / 1000f, MinTickDeltaTime);

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                if (nowMs - self.LastTickTraceLogTime >= 500)
                {
                    self.LastTickTraceLogTime = nowMs;
                    Log.Debug($"[JoystickTrace][ServerMove] tick skipped by speed unitId={unit.Id}, speed={speed:F3}, dir=({self.Direction.x:F3},{self.Direction.z:F3})");
                }
                return;
            }

            float3 oldPos = unit.Position;
            float3 delta = self.Direction * speed * fixedDeltaTime;
            float3 expectedNextPos = oldPos + delta;
            expectedNextPos.y = oldPos.y;
            float3 nextPos = expectedNextPos;
            bool reachedExpectedPos = true;

            // NavMesh 防穿墙：沿 NavMesh 表面移动，碰墙自动贴墙滑动
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            if (self.EnableNavRaycast && pathfinding != null)
            {
                try
                {
                    reachedExpectedPos = pathfinding.TryMoveAlongSurface(oldPos, expectedNextPos, out nextPos);
                }
                catch (System.Exception e)
                {
                    // 异常时放行，避免卡死
                    nextPos = expectedNextPos;
                    reachedExpectedPos = true;
                    if (nowMs - self.LastTickTraceLogTime >= 500)
                    {
                        self.LastTickTraceLogTime = nowMs;
                        Log.Warning($"[NavMove] exception, allow move. unitId={unit.Id}, error={e.Message}");
                    }
                }
            }

            float expectedMoveDist = math.distance(new float2(oldPos.x, oldPos.z), new float2(expectedNextPos.x, expectedNextPos.z));
            float actualMoveDist = math.distance(new float2(oldPos.x, oldPos.z), new float2(nextPos.x, nextPos.z));
            if (!reachedExpectedPos && nowMs - self.LastNavResolveLogTime >= 250)
            {
                self.LastNavResolveLogTime = nowMs;
                string resolveState = actualMoveDist > 0.001f ? "slide" : "blocked";
                float moveRatio = expectedMoveDist > 0.0001f ? actualMoveDist / expectedMoveDist : 0f;
                Log.Info(
                    $"[NavMove][ServerResolve] state={resolveState}, unitId={unit.Id}, oldPos={oldPos}, expectedNextPos={expectedNextPos}, nextPos={nextPos}, expectedMove={expectedMoveDist:F4}, actualMove={actualMoveDist:F4}, moveRatio={moveRatio:F3}, dir=({self.Direction.x:F3},{self.Direction.z:F3}), speed={speed:F3}, dt={fixedDeltaTime:F4}");
            }

            unit.Position = nextPos;

            // 旋转朝向移动方向
            TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
            if (math.lengthsq(self.Direction) > 0.0001f && (turnComponent == null || !turnComponent.IsTurning()))
            {
                unit.Rotation = quaternion.LookRotation(self.Direction, math.up());
            }

            // 先发给自己，保证玩家在不被任何人看见时也能收到自身位移同步
            uint moveSequence = self.NextMoveSequence();

            M2C_JoystickMove selfMsg = CreateMoveMessage(unit, self.Direction, speed, moveSequence, self.LastClientInputSequence);
            MapMessageHelper.NoticeClient(unit, selfMsg, NoticeType.Self);

            M2C_JoystickMove broadcastMsg = CreateMoveMessage(unit, self.Direction, speed, moveSequence, self.LastClientInputSequence);
            MapMessageHelper.NoticeClient(unit, broadcastMsg, NoticeType.BroadcastWithoutSelf);
        }

        private static void BroadcastStop(this JoystickMoveComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            // 先发给自己，保证玩家松手时自身状态立即同步
            uint moveSequence = self.NextMoveSequence();

            M2C_JoystickMove selfMsg = CreateMoveMessage(unit, float3.zero, 0f, moveSequence, self.LastClientInputSequence);
            MapMessageHelper.NoticeClient(unit, selfMsg, NoticeType.Self);

            M2C_JoystickMove broadcastMsg = CreateMoveMessage(unit, float3.zero, 0f, moveSequence, self.LastClientInputSequence);
            MapMessageHelper.NoticeClient(unit, broadcastMsg, NoticeType.BroadcastWithoutSelf);
        }

        private static uint NextMoveSequence(this JoystickMoveComponent self)
        {
            ++self.MoveSequence;
            return self.MoveSequence;
        }

        private static M2C_JoystickMove CreateMoveMessage(Unit unit, float3 direction, float speed, uint moveSequence, uint lastProcessedInputSequence)
        {
            M2C_JoystickMove msg = M2C_JoystickMove.Create();
            msg.UnitId = unit.Id;
            msg.PosX = unit.Position.x;
            msg.PosY = unit.Position.y;
            msg.PosZ = unit.Position.z;
            msg.RotX = unit.Rotation.value.x;
            msg.RotY = unit.Rotation.value.y;
            msg.RotZ = unit.Rotation.value.z;
            msg.RotW = unit.Rotation.value.w;
            msg.DirX = direction.x;
            msg.DirZ = direction.z;
            msg.Speed = speed;
            msg.MoveSequence = moveSequence;
            msg.LastProcessedInputSequence = lastProcessedInputSequence;
            return msg;
        }
    }
}
