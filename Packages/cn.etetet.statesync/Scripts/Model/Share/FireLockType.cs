namespace ET
{
    /// <summary>
    /// 射击锁定类型：决定子弹如何飞向目标
    /// </summary>
    public enum FireLockType
    {
        /// <summary>强制目标锁定：子弹持续追踪目标（适用于步枪1号）</summary>
        ForcedTarget = 1,

        /// <summary>方向锁定：发射时锁定方向，之后直线飞行（适用于步枪2号）</summary>
        Direction = 2,

        /// <summary>位置锁定：发射到目标当前位置，之后直线飞行（适用于火箭炮）</summary>
        Position = 3,
    }
}
