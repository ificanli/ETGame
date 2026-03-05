using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    [ConfigProcess(ConfigType.Bson)]
    public class RogueRuntimeConfigCategory : Singleton<RogueRuntimeConfigCategory>, ISingletonAwake, IConfig
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, RogueLevelConfig> levels = new();

        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, RogueOptionConfig> options = new();

        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, int> killExpByUnitType = new();

        [BsonElement]
        public int DefaultKillExp = 0;

        [BsonElement]
        public int ChoiceOptionCount = 3;

        public void Awake()
        {
        }

        public void ResolveRef()
        {
        }

        public bool TryGetLevel(int level, out RogueLevelConfig config)
        {
            return this.levels.TryGetValue(level, out config);
        }

        public bool TryGetStartLevel(out int level, out RogueLevelConfig config)
        {
            level = 0;
            config = null;
            if (this.levels.Count == 0)
            {
                return false;
            }

            foreach (int key in this.levels.Keys)
            {
                if (level == 0 || key < level)
                {
                    level = key;
                }
            }

            return level > 0 && this.levels.TryGetValue(level, out config);
        }

        public int GetKillExp(Unit unit)
        {
            if (unit == null)
            {
                return 0;
            }

            if (this.killExpByUnitType.TryGetValue((int)unit.UnitType, out int exp))
            {
                return exp;
            }

            return this.DefaultKillExp;
        }

        public int GetChoiceOptionCount()
        {
            return this.ChoiceOptionCount <= 0 ? 1 : this.ChoiceOptionCount;
        }

        public Dictionary<int, RogueOptionConfig> GetOptions()
        {
            return this.options;
        }

        public bool TryGetOption(int optionId, out RogueOptionConfig optionConfig)
        {
            return this.options.TryGetValue(optionId, out optionConfig);
        }
    }

    [EnableClass]
    public class RogueLevelConfig
    {
        [BsonElement]
        public int NeedExp = 0;

        [BsonElement]
        public bool TriggerChoice = false;

        [BsonElement]
        public List<RogueNumericConfig> NumericDeltas = new();
    }

    [EnableClass]
    public class RogueNumericConfig
    {
        [BsonElement]
        public int NumericType = 0;

        [BsonElement]
        public long Value = 0;
    }

    [EnableClass]
    public class RogueOptionConfig
    {
        [BsonElement]
        public int BuffConfigId = 0;

        [BsonElement]
        public int NameTextId = 0;

        [BsonElement]
        public int DescTextId = 0;

        [BsonElement]
        public string Icon = string.Empty;

        [BsonElement]
        public int Weight = 1;
    }
}
