namespace ET.Client
{
    /// <summary>
    /// 客户端摇杆移动同步状态。
    /// 挂在 Unit 上，用于记录最后一次已应用的权威移动包序号。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class JoystickMoveSyncStateComponent : Entity, IAwake
    {
        public uint LastAppliedMoveSequence;
    }
}
