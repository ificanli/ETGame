using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.ComponentModel;

namespace ET
{
    [Config]
    public partial class NumericConfigCategory : Singleton<NumericConfigCategory>, IMerge
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, NumericConfig> dict = new();
		
        public void Merge(object o)
        {
            NumericConfigCategory s = o as NumericConfigCategory;
            foreach (var kv in s.dict)
            {
                this.dict.Add(kv.Key, kv.Value);
            }
        }
		
        public NumericConfig Get(int id)
        {
            this.dict.TryGetValue(id, out NumericConfig item);

            if (item == null)
            {
                throw new Exception($"配置找不到，配置表名: {nameof (NumericConfig)}，配置id: {id}");
            }

            return item;
        }
		
        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }

        public Dictionary<int, NumericConfig> GetAll()
        {
            return this.dict;
        }

        public NumericConfig GetOne()
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

	public partial class NumericConfig: ProtoObject, IConfig
	{
		/// <summary>Id</summary>
		public int Id { get; set; }
		/// <summary>名字</summary>
		public string Name { get; set; }
		/// <summary>是否存数据库</summary>
		public int NeedSaveDB { get; set; }
		/// <summary>广播给客户端类型</summary>
		public int NoticeClientType { get; set; }
		/// <summary>对应的最大值属性</summary>
		public int MaxNumericType { get; set; }
		/// <summary>影响的属性id</summary>
		public int[] AffectNumericType { get; set; }
		/// <summary>影响的属性数值</summary>
		public int[] AffectValue { get; set; }

	}
}
