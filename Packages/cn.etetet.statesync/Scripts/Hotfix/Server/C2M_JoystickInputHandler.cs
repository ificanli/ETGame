using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 处理客户端摇杆输入消息，更新服务端Unit的移动方向。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_JoystickInputHandler : MessageLocationHandler<Unit, C2M_JoystickInput>
    {
        private const int InputProcessThrottleMs = 16;

        protected override async ETTask Run(Unit unit, C2M_JoystickInput message)
        {
            // 确保JoystickMoveComponent存在
            JoystickMoveComponent joystickMove = unit.GetComponent<JoystickMoveComponent>();
            if (joystickMove == null)
            {
                joystickMove = unit.AddComponent<JoystickMoveComponent>();
            }

            long now = TimeInfo.Instance.ServerNow();
            bool isStop = message.DirX == 0f && message.DirZ == 0f;

            // 节流窗口内：仅静默覆盖方向，下次 Tick 使用最新值；不走 SetDirection 避免多余日志
            if (!isStop && now - joystickMove.LastInputProcessTime < InputProcessThrottleMs)
            {
                float3 newDir = new float3(message.DirX, 0, message.DirZ);
                float len = math.length(newDir);
                if (len > 1f) newDir /= len;
                joystickMove.Direction = newDir;
                await ETTask.CompletedTask;
                return;
            }

            joystickMove.LastInputProcessTime = now;
            joystickMove.SetDirection(message.DirX, message.DirZ);

            await ETTask.CompletedTask;
        }
    }
}
