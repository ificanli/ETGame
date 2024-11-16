using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public struct SpellConfigLoader
    {
        public int Id;
    }
    
    public partial class SpellConfigCategory : Singleton<SpellConfigCategory>, ISingletonAwake
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, SpellConfig> dict = new();
        
        public void Awake()
        {
        }
		
        public SpellConfig Get(int id)
        {
            this.dict.TryGetValue(id, out SpellConfig item);

            if (item != null)
            {
                return item;
            }

            item = EventSystem.Instance.Invoke<SpellConfigLoader, SpellConfig>(new SpellConfigLoader() {Id = id});
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
    
    
    [Serializable]
    public partial class SpellConfig: ProtoObject
    {
        /// <summary>Id</summary>
        public int Id;

        /// <summary>目标选择</summary>
#if UNITY
        [UnityEngine.Header("目标选择")]
        [UnityEngine.Tooltip("目标选择")]
        [UnityEngine.SerializeReference]
#endif
        public TargetSelector TargetSelector;

        /// <summary>吟唱时间</summary>
        public int Chanting;
        
        /// <summary>命中时间</summary>
        public int HitTime;

        /// <summary>持续时间</summary>
        public int Duration;

        /// <summary>CD（毫秒）</summary>
        public int CD;

        /// <summary>打断情况</summary>
        public List<SpellFlags> Flags;

        /// <summary>广播客户端类型</summary>

        public NoticeType NoticeType;

#if UNITY
        [UnityEngine.Header("Effect")]
        [UnityEngine.SerializeReference]
#endif
        public List<BTNode> Effects = new();
    }
}