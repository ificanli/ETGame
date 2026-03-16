using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(JoystickMoveComponent))]
    public static partial class JoystickMoveComponentSystem
    {
        private const int MoveTickIntervalMs = 50;
        private const float MaxTickDeltaTime = 0.1f;
        private const float MinTickDeltaTime = 0.001f;
        private const float MaxTrustedGroundDeltaY = 1.5f;

        [Invoke(TimerInvokeType.JoystickMoveTimer)]
        public class JoystickMoveTimer : ATimer<JoystickMoveComponent>
        {
            protected override void Run(JoystickMoveComponent self)
            {
                self.Tick();
            }
        }

        [EntitySystem]
        private static void Awake(this JoystickMoveComponent self)
        {
            self.Direction = float3.zero;
            self.MoveTimerId = 0;
            self.LastInputTraceLogTime = 0;
            self.LastTickTraceLogTime = 0;
            self.LastInputProcessTime = 0;
            self.LastTickTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this JoystickMoveComponent self)
        {
            self.StopTimer();
        }

        /// <summary>
        /// 更新摇杆方向并确保定时器在运行
        /// </summary>
        public static void SetDirection(this JoystickMoveComponent self, float dirX, float dirZ)
        {
            Unit unit = self.GetParent<Unit>();
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
                // 方向接近零，停止移动
                self.StopTimer();
                self.BroadcastStop();
                Log.Info($"[JoystickTrace][ServerMove] stop by zero dir unitId={unit?.Id ?? 0}, dir=({dirX:F3},{dirZ:F3})");
            }
            else
            {
                self.StartTimer();
                if (unit != null && !unit.IsDisposed)
                {
                    Log.Info($"[JoystickTrace][ServerMove] set dir unitId={unit.Id}, dir=({self.Direction.x:F3},{self.Direction.z:F3}), timer={self.MoveTimerId}");
                }
            }
        }

        private static void StartTimer(this JoystickMoveComponent self)
        {
            if (self.MoveTimerId != 0)
            {
                return;
            }

            self.LastTickTime = TimeInfo.Instance.ServerNow();
            self.MoveTimerId = self.Root().TimerComponent.NewRepeatedTimer(MoveTickIntervalMs, TimerInvokeType.JoystickMoveTimer, self);
            Unit unit = self.GetParent<Unit>();
            if (unit != null && !unit.IsDisposed)
            {
                Log.Info($"[JoystickTrace][ServerMove] timer started unitId={unit.Id}, timerId={self.MoveTimerId}");
            }
        }

        private static void StopTimer(this JoystickMoveComponent self)
        {
            if (self.MoveTimerId == 0)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            long timerId = self.MoveTimerId;
            self.Root()?.TimerComponent?.Remove(ref self.MoveTimerId);
            self.MoveTimerId = 0;
            if (unit != null && !unit.IsDisposed)
            {
                Log.Info($"[JoystickTrace][ServerMove] timer stopped unitId={unit.Id}, oldTimerId={timerId}");
            }
        }

        private static void Tick(this JoystickMoveComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            long now = TimeInfo.Instance.ServerNow();
            float realDeltaTime = (now - self.LastTickTime) / 1000f;
            realDeltaTime = math.clamp(realDeltaTime, MinTickDeltaTime, MaxTickDeltaTime);
            self.LastTickTime = now;

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                if (now - self.LastTickTraceLogTime >= 500)
                {
                    self.LastTickTraceLogTime = now;
                    Log.Warning($"[JoystickTrace][ServerMove] tick skipped by speed unitId={unit.Id}, speed={speed:F3}, dir=({self.Direction.x:F3},{self.Direction.z:F3})");
                }
                return;
            }

            float3 oldPos = unit.Position;
            float3 delta = self.Direction * speed * realDeltaTime;
            float3 expectedNextPos = oldPos + delta;
            expectedNextPos.y = oldPos.y;
            float3 nextPos = expectedNextPos;

            // 摇杆移动优先信任导航修正后的平面位置；
            // Y 轴仅在高度差处于可信范围内时跟随导航，避免被吸到错误楼层。
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            bool shouldApplyNavGuard = pathfinding != null && unit.GetComponent<UnitGateInfoComponent>() == null;
            if (shouldApplyNavGuard)
            {
                float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                try
                {
                    if (!pathfinding.TryRecastFindNearestPointForMovement(expectedNextPos, unitRadius, out float3 projectedPos, out float projectedDeltaXZ))
                    {
                        self.Direction = float3.zero;
                        self.StopTimer();
                        self.BroadcastStop();
                        Log.Warning($"[NavGuard] stop joystick move: no nearest poly. unitId={unit.Id}, pos={expectedNextPos}");
                        return;
                    }

                    float maxProjectedDeltaXZ = pathfinding.GetMovementRejectDistance(unitRadius);
                    if (projectedDeltaXZ > maxProjectedDeltaXZ)
                    {
                        self.Direction = float3.zero;
                        self.StopTimer();
                        self.BroadcastStop();
                        Log.Warning($"[NavGuard] stop joystick move: projection delta too large. unitId={unit.Id}, expected={expectedNextPos}, projected={projectedPos}, deltaXZ={projectedDeltaXZ:F3}, threshold={maxProjectedDeltaXZ:F3}");
                        return;
                    }

                    nextPos.x = projectedPos.x;
                    nextPos.z = projectedPos.z;
                    float projectedDeltaY = math.abs(projectedPos.y - oldPos.y);
                    if (projectedDeltaY <= MaxTrustedGroundDeltaY)
                    {
                        nextPos.y = projectedPos.y;
                    }
                    else
                    {
                        long nowForGround = TimeInfo.Instance.ServerNow();
                        if (nowForGround - self.LastTickTraceLogTime >= 500)
                        {
                            self.LastTickTraceLogTime = nowForGround;
                            Log.Warning($"[JoystickTrace][ServerMove] ignore abnormal ground snap unitId={unit.Id}, oldY={oldPos.y:F3}, projectedY={projectedPos.y:F3}, deltaY={projectedDeltaY:F3}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    self.Direction = float3.zero;
                    self.StopTimer();
                    self.BroadcastStop();
                    Log.Warning($"[NavGuard] stop joystick move: navmesh project failed. unitId={unit.Id}, pos={expectedNextPos}, error={e.Message}");
                    return;
                }
            }

            unit.Position = nextPos;

            // 旋转朝向移动方向
            if (math.lengthsq(self.Direction) > 0.0001f)
            {
                unit.Rotation = quaternion.LookRotation(self.Direction, math.up());
            }

            // 先发给自己，保证玩家在不被任何人看见时也能收到自身位移同步
            M2C_JoystickMove selfMsg = CreateMoveMessage(unit, self.Direction, speed);
            MapMessageHelper.NoticeClient(unit, selfMsg, NoticeType.Self);

            M2C_JoystickMove broadcastMsg = CreateMoveMessage(unit, self.Direction, speed);
            MapMessageHelper.NoticeClient(unit, broadcastMsg, NoticeType.BroadcastWithoutSelf);

            if (now - self.LastTickTraceLogTime >= 500)
            {
                self.LastTickTraceLogTime = now;
                Log.Info($"[JoystickTrace][ServerMove] tick unitId={unit.Id}, speed={speed:F3}, dir=({self.Direction.x:F3},{self.Direction.z:F3}), oldPos={oldPos}, newPos={unit.Position}");
            }
        }

        private static void BroadcastStop(this JoystickMoveComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            // 先发给自己，保证玩家松手时自身状态立即同步
            M2C_JoystickMove selfMsg = CreateMoveMessage(unit, float3.zero, 0f);
            MapMessageHelper.NoticeClient(unit, selfMsg, NoticeType.Self);

            M2C_JoystickMove broadcastMsg = CreateMoveMessage(unit, float3.zero, 0f);
            MapMessageHelper.NoticeClient(unit, broadcastMsg, NoticeType.BroadcastWithoutSelf);
        }

        private static M2C_JoystickMove CreateMoveMessage(Unit unit, float3 direction, float speed)
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
            return msg;
        }
    }
}
