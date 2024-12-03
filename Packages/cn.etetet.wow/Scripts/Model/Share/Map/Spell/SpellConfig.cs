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
    
    public enum CastTimeType
    {
        Attack, // 普通攻击
        Cast,   // 读条
        Channeling, // 吟唱
    }
    
    
    [Serializable]
    public partial class SpellConfig: ProtoObject
#if UNITY
            ,UnityEngine.ISerializationCallbackReceiver
#endif
    {
        /// <summary>Id</summary>
        public int Id;

        public string Desc;
        
//#if UNITY
//        [UnityEngine.SerializeReference]
//#endif
        public List<CostNode> Cost = new();

#if UNITY
        [UnityEngine.SerializeReference]
#endif
        public BTNode PreCheck;
        
        /// <summary>目标选择</summary>
#if UNITY
        [UnityEngine.SerializeReference]
#endif
        public TargetSelector TargetSelector;

        /// <summary>命中时间</summary>
        public int HitTime;

        /// <summary>持续时间</summary>
        public int Duration;

        /// <summary>CD（毫秒）</summary>
        public int CD;

        public HashSet<SpellFlags> Flags = new();

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
}