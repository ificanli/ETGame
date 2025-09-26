using System.Collections;
using System.Collections.Generic;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    [EnableClass]
    [MemoryPackable]
    public partial class StringKV : IEnumerable<KeyValuePair<string, string>>
    {
        [MemoryPackOrder(0)]
        [MemoryPackInclude]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<string, string> data = new();

        [MemoryPackConstructor]
        public StringKV() { }

        public StringKV(StringKV other)
        {
            this.data = new Dictionary<string, string>(other.data);
        }

        public override string ToString()
        {
            return MongoHelper.ToJson(this.data);
        }

        // ✅ 这个方法让集合初始化器语法可用
        public void Add(string key, string value) => this.data.Add(key, value);

        // ✅ 实现 IEnumerable，让集合初始化器合法
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this.data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ✅ 一些常用封装（可选）
        public string this[string key]
        {
            get => this.data[key];
            set => this.data[key] = value;
        }

        public bool TryGetValue(string key, out string value)
        {
            return this.data.TryGetValue(key, out value);
        }

        public bool ContainsKey(string key) => this.data.ContainsKey(key);
        public bool Remove(string key) => this.data.Remove(key);
        public void Clear() => this.data.Clear();
        public int Count => this.data.Count;
    }
}