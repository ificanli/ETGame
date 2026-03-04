using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(JoystickMoveComponent))]
    public static partial class JoystickMoveComponentSystem
    {
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
            }
            else
            {
                self.StartTimer();
            }
        }

        private static void StartTimer(this JoystickMoveComponent self)
        {
            if (self.MoveTimerId != 0)
            {
                return;
            }

            // 33ms ≈ 30Hz
            self.MoveTimerId = self.Root().TimerComponent.NewRepeatedTimer(33, TimerInvokeType.JoystickMoveTimer, self);
        }

        private static void StopTimer(this JoystickMoveComponent self)
        {
            if (self.MoveTimerId == 0)
            {
                return;
            }

            self.Root()?.TimerComponent?.Remove(ref self.MoveTimerId);
            self.MoveTimerId = 0;
        }

        private static void Tick(this JoystickMoveComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            if (speed < 0.01f)
            {
                return;
            }

            // 每帧移动距离 = 速度 * 时间（33ms）
            float deltaTime = 0.033f;
            float3 delta = self.Direction * speed * deltaTime;
            unit.Position += delta;

            // 旋转朝向移动方向
            if (math.lengthsq(self.Direction) > 0.0001f)
            {
                unit.Rotation = quaternion.LookRotation(self.Direction, math.up());
            }

            // 先发给自己，保证玩家在不被任何人看见时也能收到自身位移同步
            M2C_JoystickMove selfMsg = CreateMoveMessage(unit);
            MapMessageHelper.NoticeClient(unit, selfMsg, NoticeType.Self);

            M2C_JoystickMove broadcastMsg = CreateMoveMessage(unit);
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
            M2C_JoystickMove selfMsg = CreateMoveMessage(unit);
            MapMessageHelper.NoticeClient(unit, selfMsg, NoticeType.Self);

            M2C_JoystickMove broadcastMsg = CreateMoveMessage(unit);
            MapMessageHelper.NoticeClient(unit, broadcastMsg, NoticeType.BroadcastWithoutSelf);
        }

        private static M2C_JoystickMove CreateMoveMessage(Unit unit)
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
            return msg;
        }
    }
}
