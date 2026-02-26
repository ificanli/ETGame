using System;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// ECA 节点基类
    /// 所有 ECA 节点的基类
    /// </summary>
    [Serializable]
    [EnableClass]
    public abstract class ECANode : Object
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public int Id;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName;

#if UNITY_EDITOR
        /// <summary>
        /// 节点在编辑器中的位置
        /// </summary>
        [HideInInspector]
        public Vector2 Position;

        /// <summary>
        /// 节点描述
        /// </summary>
        [TextArea(2, 5)]
        public string Description;
#endif
    }
}
