using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.ComponentModel;

namespace ET
{
    [Config]
    public partial class BuffConfigCategory : Singleton<BuffConfigCategory>, IMerge
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, BuffConfig> dict = new();
		
        public void Merge(object o)
        {
            BuffConfigCategory s = o as BuffConfigCategory;
            foreach (var kv in s.dict)
            {
                this.dict.Add(kv.Key, kv.Value);
            }
        }
		
        public BuffConfig Get(int id)
        {
            this.dict.TryGetValue(id, out BuffConfig item);

            if (item == null)
            {
                throw new Exception($"配置找不到，配置表名: {nameof (BuffConfig)}，配置id: {id}");
            }

            return item;
        }
		
        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }

        public Dictionary<int, BuffConfig> GetAll()
        {
            return this.dict;
        }

        public BuffConfig GetOne()
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

	public partial class BuffConfig: ProtoObject, IConfig
	{
		/// <summary>Id</summary>
		public int Id { get; set; }
		/// <summary>持续时间</summary>
		public int Duration { get; set; }
		/// <summary>服务器起始效果</summary>
		public int[] ServerEffects { get; set; }
		/// <summary>客户端起始效果</summary>
		public int[] ClientEffects { get; set; }
		/// <summary>Tick间隔时间</summary>
		public int TickTime { get; set; }
		/// <summary>其它效果</summary>
		public int[] OtherEffects { get; set; }
		/// <summary>最大层数</summary>
		public int MaxStack { get; set; }
		/// <summary>叠加规则类型</summary>
		public int OverLayRuleType { get; set; }
		/// <summary>移除条件</summary>
		public int[] RemoveConditions { get; set; }
		/// <summary>广播客户端类型</summary>
		public int NoticeClientType { get; set; }

	}
}
