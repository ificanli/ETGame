using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 摇杆移动组件（服务端），挂载在 Unit 上。
    /// 根据客户端发来的摇杆方向，在服务端持续移动Unit并广播位置。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class JoystickMoveComponent : Entity, IAwake, IDestroy
    {
        /// <summary>摇杆输入方向（归一化后的XZ分量）</summary>
        public float3 Direction;

        /// <summary>是否已注册到 Move16ms 高频通道。</summary>
        public bool IsMoveChannelRegistered;

        /// <summary>是否等待在当前通道 Tick 提交后广播停步。</summary>
        public bool PendingStopBroadcast;

        /// <summary>Tick日志节流时间（毫秒）</summary>
        public long LastTickTraceLogTime;

        /// <summary>NavMesh 解析日志节流时间（毫秒）</summary>
        public long LastNavResolveLogTime;

        /// <summary>服务端实际处理输入的时间（毫秒）</summary>
        public long LastInputProcessTime;

        /// <summary>服务端权威移动消息序号，客户端据此丢弃过期包。</summary>
        public uint MoveSequence;

        /// <summary>最近一次已接受的客户端输入序号，用于丢弃乱序旧输入。</summary>
        public uint LastClientInputSequence;

        /// <summary>乱序旧输入丢弃日志节流时间（毫秒）。</summary>
        public long LastStaleInputLogTime;

        /// <summary>是否启用 NavMesh Raycast 防穿墙。设为 false 可关闭该功能。</summary>
        public bool EnableNavRaycast = true;

    }
}
