using System;

namespace ET
{
    /// <summary>
    /// ECA 动作节点
    /// 表示状态转换后执行的动作
    /// </summary>
    [Serializable]
    [EnableClass]
    public class ECAActionNode : ECANode
    {
        /// <summary>
        /// 动作类型
        /// 1=StartEvacuation, 2=SpawnMonster, 3=OpenContainer, 4=ShowUI, 5=PlayEffect
        /// </summary>
        public int ActionType;

        /// <summary>
        /// 动作参数（JSON格式）
        /// </summary>
        public string ActionParams;
    }
}
