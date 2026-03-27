using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 客户端记录最近一次权威摇杆移动同步，用于显示层区分“真实还在移动”与“停步后的残余纠偏”。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class JoystickMoveAuthorityStateComponent : Entity, IAwake
    {
        public float LastSpeed;
        public float3 LastDirection;
        public long LastSyncTime;
        public uint LastProcessedInputSequence;
    }
}
