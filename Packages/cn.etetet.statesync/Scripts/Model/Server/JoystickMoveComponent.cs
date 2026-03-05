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

        /// <summary>服务端重复定时器ID（33ms = 30Hz）</summary>
        public long MoveTimerId;

        /// <summary>输入日志节流时间（毫秒）</summary>
        public long LastInputTraceLogTime;

        /// <summary>Tick日志节流时间（毫秒）</summary>
        public long LastTickTraceLogTime;
    }
}
