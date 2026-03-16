namespace ET.Client
{
    /// <summary>
    /// 接收摇杆输入事件，写入本地输入组件，由输入组件统一汇总方向输入后发送移动消息。
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
