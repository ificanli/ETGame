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
#if UNITY
            ,UnityEngine.ISerializationCallbackReceiver
#endif
    {
        public int Id;
        
        public string Desc;
        
        /// <summary>持续时间</summary>
        public int Duration = 1000000000;

        /// <summary>Tick间隔时间</summary>
        public int TickTime;

        /// <summary>最大层数</summary>
        public int MaxStack = 1;

        public int Stack = 1;

        /// <summary>叠加规则类型</summary>
        public OverLayRuleType OverLayRuleType;

        /// <summary>移除条件</summary>
        public HashSet<BuffFlags> Flags = new();

        /// <summary>广播客户端类型</summary>
        public NoticeType NoticeType;
        
#if UNITY
        [UnityEngine.SerializeReference]
#endif
        public List<EffectNode> Effects = new();
        
        
#if UNITY
        [NonSerialized]
        [UnityEngine.HideInInspector]
#endif
        public Dictionary<Type, EffectNode> effectDict;

        public void OnBeforeSerialize()
        {
            this.effectDict ??= new Dictionary<Type, EffectNode>();
            this.effectDict.Clear();
            foreach (EffectNode effectNode in this.Effects)
            {
                this.effectDict.Add(effectNode.GetType(), effectNode);
            }
        }
        
        public void OnAfterDeserialize()
        {
            this.effectDict ??= new Dictionary<Type, EffectNode>();
            this.effectDict.Clear();
            foreach (EffectNode effectNode in this.Effects)
            {
                this.effectDict.Add(effectNode.GetType(), effectNode);
            }
        }
        
        public T GetEffect<T>() where T : EffectNode
        {
            this.effectDict.TryGetValue(typeof(T), out EffectNode effectNode);
            return effectNode as T;
        }
    }
    
    public enum OverLayRuleType
    {
        None,
        AddStack,
        AddTime,
        Replace,
    }
}