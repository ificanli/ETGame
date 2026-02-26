using System;

namespace ET
{
    /// <summary>
    /// ECA 状态节点
    /// 表示交互物的一个状态
    /// </summary>
    [Serializable]
    [EnableClass]
    public class ECAStateNode : ECANode
    {
        /// <summary>
        /// 状态名称
        /// </summary>
        public string StateName;

        /// <summary>
        /// 是否为初始状态
        /// </summary>
        public bool IsInitialState;

        /// <summary>
        /// 是否可重置
        /// </summary>
        public bool IsResetable;

        /// <summary>
        /// 重置冷却时间（毫秒）
        /// </summary>
        public long ResetCooldown;
    }
}
