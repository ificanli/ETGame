namespace ET.Server
{
    /// <summary>
    /// 处理客户端摇杆输入消息，更新服务端Unit的移动方向。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_JoystickInputHandler : MessageLocationHandler<Unit, C2M_JoystickInput>
    {
        protected override async ETTask Run(Unit unit, C2M_JoystickInput message)
        {
            // 确保JoystickMoveComponent存在
            JoystickMoveComponent joystickMove = unit.GetComponent<JoystickMoveComponent>();
            if (joystickMove == null)
            {
                joystickMove = unit.AddComponent<JoystickMoveComponent>();
                Log.Info($"[JoystickTrace][ServerRecv] add JoystickMoveComponent unitId={unit.Id}");
            }

            long now = TimeInfo.Instance.ServerNow();
            bool isStop = message.DirX == 0f && message.DirZ == 0f;
            if (isStop || now - joystickMove.LastInputTraceLogTime >= 200)
            {
                joystickMove.LastInputTraceLogTime = now;
                Log.Info($"[JoystickTrace][ServerRecv] recv C2M_JoystickInput unitId={unit.Id}, dir=({message.DirX:F3},{message.DirZ:F3}), pos={unit.Position}");
            }

            joystickMove.SetDirection(message.DirX, message.DirZ);

            await ETTask.CompletedTask;
        }
    }
}
