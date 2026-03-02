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
            }

            joystickMove.SetDirection(message.DirX, message.DirZ);

            await ETTask.CompletedTask;
        }
    }
}
