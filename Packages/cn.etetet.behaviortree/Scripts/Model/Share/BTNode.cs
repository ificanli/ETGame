using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public abstract class BTNode : Object
    {
#if UNITY
        [DisplayAsString]
#endif
        public int Id;
        
        [LabelText("子节点")]
        [PropertyOrder(100)]
#if UNITY
        [UnityEngine.SerializeReference]
        [UnityEngine.HideInInspector]
#endif
        public List<BTNode> Children = new();

        // 主要用于行为树编辑器保存一些显示层的数据
#if UNITY_EDITOR

        [BsonIgnore]
        //[HideInInspector]
        [PropertyOrder(99)]
        [UnityEngine.TextArea]
        [ShowIf("@!DescCollapsed")]
        public string Desc;
        
        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public bool IsCollapsed;
        
        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public bool ChildrenCollapsed;

        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public bool DescCollapsed = true; // 默认折叠说明

        [BsonIgnore]
        [UnityEngine.HideInInspector]
        public UnityEngine.Vector2 Position;
#endif
    }
}