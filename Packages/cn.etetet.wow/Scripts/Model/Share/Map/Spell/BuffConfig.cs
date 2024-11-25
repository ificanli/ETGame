using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public struct BuffConfigLoader
    {
        public int Id;
    }
    
    public partial class BuffConfigCategory : Singleton<BuffConfigCategory>, ISingletonAwake
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, BuffConfig> dict = new();
        
        public void Awake()
        {
        }
		
        public BuffConfig Get(int id)
        {
            this.dict.TryGetValue(id, out BuffConfig item);

            if (item != null)
            {
                return item;
            }

            item = EventSystem.Instance.Invoke<BuffConfigLoader, BuffConfig>(new BuffConfigLoader() {Id = id});
            if (item == null)
            {
                throw new Exception($"not found spell config: {id}");
            }

            this.dict.Add(id, item);
            return item;
        }
		
        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }
    }
    
    
    [System.Serializable]
    public partial class BuffConfig: ProtoObject
    {
        public int Id;
        
        /// <summary>持续时间</summary>
        public int Duration;

        /// <summary>Tick间隔时间</summary>
        public int TickTime;

        /// <summary>最大层数</summary>
        public int MaxStack = 1;

        /// <summary>叠加规则类型</summary>
        public int OverLayRuleType;

        /// <summary>移除条件</summary>
        public List<BuffFlags> Flags;

        /// <summary>广播客户端类型</summary>
        public NoticeType NoticeType;
        
#if UNITY
        [UnityEngine.SerializeReference]
#endif
        public List<EffectNode> Effects = new();
    }
}