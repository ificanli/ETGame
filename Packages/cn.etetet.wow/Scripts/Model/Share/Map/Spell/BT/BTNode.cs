using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public abstract class BTNode : Object
    {
        public int Id;
        
        [PropertyOrder(100)]
#if UNITY
        [UnityEngine.SerializeReference]
        [UnityEngine.HideInInspector]
#endif
        [LabelText("子节点")]
        public List<BTNode> Children = new();
        

        // 主要用于行为树编辑器保存一些显示层的数据
#if UNITY_EDITOR

        [BsonIgnore]
        public string Desc;
        
        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public bool IsCollapsed;
        
        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public bool ChildrenCollapsed;

        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public UnityEngine.Vector2 Position;
#endif
    }
}