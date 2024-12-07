using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public abstract class BTNode : Object
    {
        #if UNITY
        [UnityEngine.SerializeReference]
        [Sirenix.OdinInspector.PropertyOrder(100)]
        #endif
        [LabelText("子节点")]
        public List<BTNode> Children = new();
    }
}