using System;

namespace ET
{
    /// <summary>
    /// ECA 连接数据
    /// 表示节点之间的连接关系
    /// </summary>
    [Serializable]
    [EnableClass]
    public class ECAConnection
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public int FromNodeId;

        /// <summary>
        /// 源端口名称
        /// </summary>
        public string FromPortName;

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public int ToNodeId;

        /// <summary>
        /// 目标端口名称
        /// </summary>
        public string ToPortName;
    }
}
