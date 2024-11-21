using System.Collections.Generic;

namespace ET
{
    [System.Serializable]
    public abstract class BTNode: Object
    {
#if UNITY
        [UnityEngine.SerializeReference]
        [Sirenix.OdinInspector.PropertyOrder(100)]
#endif
        public List<BTNode> Children = new();
    }
    
    [System.Serializable]
    public abstract class EffectNode: BTNode
    {
        
    }
}