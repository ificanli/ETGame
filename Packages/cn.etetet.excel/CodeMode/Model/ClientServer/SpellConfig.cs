using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.ComponentModel;

namespace ET
{
    [Config]
    public partial class SpellConfigCategory : Singleton<SpellConfigCategory>, IMerge
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

	public partial class SpellConfig: ProtoObject, IConfig
	{
		/// <summary>Id</summary>
		public int Id { get; set; }
		/// <summary>目标选择</summary>
		public int[] TargetSelector { get; set; }
		/// <summary>吟唱时间</summary>
		public int Chanting { get; set; }
		/// <summary>持续时间</summary>
		public int LastTime { get; set; }
		/// <summary>命中时间</summary>
		public int HitTime { get; set; }
		/// <summary>效果</summary>
		public int[] Effects { get; set; }
		/// <summary>Buffs</summary>
		public int[] Buffs { get; set; }
		/// <summary>CD（毫秒）</summary>
		public int CD { get; set; }
		/// <summary>打断情况</summary>
		public int[] Interrupt { get; set; }

	}
}
