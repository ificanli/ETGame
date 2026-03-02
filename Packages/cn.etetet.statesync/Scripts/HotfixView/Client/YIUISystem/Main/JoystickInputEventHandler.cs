namespace ET.Client
{
    /// <summary>
    /// 接收摇杆输入事件，调用 JoystickMoveHelper 发送网络消息
    /// </summary>
    [Event(SceneType.Client)]
    public class EventMain_JoystickInput_Handler : AEvent<Scene, EventMain_JoystickInput>
    {
        protected override async ETTask Run(Scene scene, EventMain_JoystickInput args)
        {
            JoystickMoveHelper.SendJoystickInput(scene, args.DirX, args.DirZ);
            await ETTask.CompletedTask;
        }
    }
}
