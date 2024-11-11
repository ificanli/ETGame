using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public partial class SpellConfigCategory : Singleton<SpellConfigCategory>
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, SpellConfig> dict = new();
		
        public void Merge(object o)
        {
            SpellConfigCategory s = o as SpellConfigCategory;
            foreach (var kv in s.dict)
            {
                this.dict.Add(kv.Key, kv.Value);
            }
        }
		
        public SpellConfig Get(int id)
        {
            this.dict.TryGetValue(id, out SpellConfig item);

            if (item == null)
            {
                throw new Exception($"配置找不到，配置表名: {nameof (SpellConfig)}，配置id: {id}");
            }

            return item;
        }
		
        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }

        public Dictionary<int, SpellConfig> GetAll()
        {
            return this.dict;
        }

        public SpellConfig GetOne()
        {
            if (this.dict == null || this.dict.Count <= 0)
            {
                return null;
            }
            
            var enumerator = this.dict.Values.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current; 
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

        /// <summary>效果</summary>
#if UNITY
        [UnityEngine.SerializeReference]
#endif
        public List<EffectConfig> Effects = new();
    }
}