using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public abstract class BTNode: Object
    {
#if UNITY
        [UnityEngine.SerializeReference]
#endif
        [PropertyOrder(100)]
        public List<BTNode> Children = new();
    }
}