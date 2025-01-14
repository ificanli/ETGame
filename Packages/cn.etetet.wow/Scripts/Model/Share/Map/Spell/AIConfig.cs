using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public struct AIConfigLoader
    {
        public int Id;
    }

    [CodeProcess]
    public partial class AIConfigCategory : Singleton<AIConfigCategory>, ISingletonAwake, IConfig
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, AIConfig> dict = new();

        public void Awake()
        {
        }
        
        public void Add(AIConfig aiConfig)
        {
            this.dict.Add(aiConfig.Id, aiConfig);
        }

        public AIConfig Get(int id)
        {
            this.dict.TryGetValue(id, out AIConfig item);

            if (item != null)
            {
                return item;
            }

            item = EventSystem.Instance.Invoke<AIConfigLoader, AIConfig>(new AIConfigLoader() { Id = id });
            if (item == null)
            {
                throw new Exception($"not found ai config: {id}");
            }

            this.dict.Add(id, item);
            return item;
        }

        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }

        public void ResolveRef()
        {
        }
    }
    
    [EnableClass]
    public class AIConfig
    {
        public int Id;

        public string Desc;

        public AIRoot Root;
    }
}