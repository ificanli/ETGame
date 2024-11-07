using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.ComponentModel;

namespace ET
{
    [Config]
    public partial class TextConfigCategory : Singleton<TextConfigCategory>, IMerge
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, TextConfig> dict = new();
		
        public void Merge(object o)
        {
            TextConfigCategory s = o as TextConfigCategory;
            foreach (var kv in s.dict)
            {
                this.dict.Add(kv.Key, kv.Value);
            }
        }
		
        public TextConfig Get(int id)
        {
            this.dict.TryGetValue(id, out TextConfig item);

            if (item == null)
            {
                throw new Exception($"配置找不到，配置表名: {nameof (TextConfig)}，配置id: {id}");
            }

            return item;
        }
		
        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }

        public Dictionary<int, TextConfig> GetAll()
        {
            return this.dict;
        }

        public TextConfig GetOne()
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

	public partial class TextConfig: ProtoObject, IConfig
	{
		/// <summary>Id</summary>
		public int Id { get; set; }
		/// <summary>名字</summary>
		public string Name { get; set; }
		/// <summary>中文</summary>
		public string CN { get; set; }
		/// <summary>英文</summary>
		public string EN { get; set; }

	}
}
