using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    public abstract class BTNodeHasChildren: BTNode
    {
        [LabelText("子节点")]
        [PropertyOrder(100)]
#if UNITY
        [UnityEngine.SerializeReference]
        [UnityEngine.HideInInspector]
#endif
        public List<BTNode> Children = new();
    }
}