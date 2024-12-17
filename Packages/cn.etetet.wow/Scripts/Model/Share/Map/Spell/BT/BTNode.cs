using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public abstract class BTNode : Object
    {
        public int Id;

        public string Desc;
        
        #if UNITY
        [UnityEngine.SerializeReference]
        [Sirenix.OdinInspector.PropertyOrder(100)]
        [UnityEngine.HideInInspector]
        #endif
        [LabelText("子节点")]
        
        public List<BTNode> Children = new();
    }
}