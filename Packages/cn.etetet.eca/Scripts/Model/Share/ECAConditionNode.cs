using System;

namespace ET
{
    /// <summary>
    /// ECA 条件节点
    /// 表示状态转换的条件判断
    /// </summary>
    [Serializable]
    [EnableClass]
    public class ECAConditionNode : ECANode
    {
        /// <summary>
        /// 条件类型
        /// 0=None, 1=HasItem, 2=LevelCheck, 3=StateCheck, 4=TimeRange, 5=TeamSize
        /// </summary>
        public int ConditionType;

        /// <summary>
        /// 条件参数（JSON格式）
        /// </summary>
        public string ConditionParams;
    }
}
