using System;
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

        [BsonIgnore]
        private readonly Dictionary<int, RogueOptionConfig> cardOptions = new();

        [BsonIgnore]
        private readonly Dictionary<int, RogueLevelConfig> tableLevels = new();

        [BsonIgnore]
        private readonly Dictionary<int, int> tableKillExpByUnitType = new();

        [BsonIgnore]
        private bool cardOptionsBuilt;

        [BsonIgnore]
        private bool tableLevelsBuilt;

        [BsonIgnore]
        private bool tableKillExpBuilt;

        public void Awake()
        {
        }

        public void ResolveRef()
        {
        }

        public bool TryGetLevel(int level, out RogueLevelConfig config)
        {
            return this.GetResolvedLevels().TryGetValue(level, out config);
        }

        public bool TryGetStartLevel(out int level, out RogueLevelConfig config)
        {
            level = 0;
            config = null;
            Dictionary<int, RogueLevelConfig> resolvedLevels = this.GetResolvedLevels();
            if (resolvedLevels.Count == 0)
            {
                return false;
            }

            foreach (int key in resolvedLevels.Keys)
            {
                if (level == 0 || key < level)
                {
                    level = key;
                }
            }

            return level > 0 && resolvedLevels.TryGetValue(level, out config);
        }

        public int GetKillExp(Unit unit)
        {
            if (unit == null)
            {
                return 0;
            }

            Dictionary<int, int> resolvedKillExp = this.GetResolvedKillExpByUnitType();
            if (resolvedKillExp.TryGetValue((int)unit.UnitType, out int exp))
            {
                return exp;
            }

            RogueGlobalConfigCategory globalCategory = RogueGlobalConfigCategory.Instance;
            if (globalCategory != null)
            {
                return globalCategory.DefaultKillExp;
            }

            return this.DefaultKillExp;
        }

        public int GetChoiceOptionCount()
        {
            RogueGlobalConfigCategory globalCategory = RogueGlobalConfigCategory.Instance;
            int count = globalCategory != null ? globalCategory.ChoiceOptionCount : this.ChoiceOptionCount;
            return count <= 0 ? 1 : count;
        }

        public Dictionary<int, RogueOptionConfig> GetOptions()
        {
            return this.GetResolvedOptions();
        }

        public bool TryGetOption(int optionId, out RogueOptionConfig optionConfig)
        {
            return this.GetResolvedOptions().TryGetValue(optionId, out optionConfig);
        }

        private Dictionary<int, RogueOptionConfig> GetResolvedOptions()
        {
            RogueCardConfigCategory cardCategory = RogueCardConfigCategory.Instance;
            if (cardCategory == null)
            {
                return this.options;
            }

            Dictionary<int, RogueCardConfig> configs = cardCategory.GetAll();
            if (configs == null || configs.Count == 0)
            {
                return this.options;
            }

            if (this.cardOptionsBuilt)
            {
                return this.cardOptions;
            }

            this.cardOptions.Clear();
            foreach (KeyValuePair<int, RogueCardConfig> kv in configs)
            {
                RogueCardConfig cardConfig = kv.Value;
                if (cardConfig == null)
                {
                    continue;
                }

                this.cardOptions[kv.Key] = RogueOptionConfig.CreateFromCardConfig(cardConfig);
            }

            this.cardOptionsBuilt = true;
            return this.cardOptions;
        }

        private Dictionary<int, RogueLevelConfig> GetResolvedLevels()
        {
            RogueLevelEntryConfigCategory levelCategory = RogueLevelEntryConfigCategory.Instance;
            if (levelCategory == null || levelCategory.DataList == null || levelCategory.DataList.Count == 0)
            {
                return this.levels;
            }

            if (this.tableLevelsBuilt)
            {
                return this.tableLevels;
            }

            Dictionary<int, List<RogueNumericConfig>> numericByLevel = new();
            RogueLevelNumericEntryConfigCategory numericCategory = RogueLevelNumericEntryConfigCategory.Instance;
            if (numericCategory != null && numericCategory.DataList != null)
            {
                foreach (RogueLevelNumericEntryConfig numericEntry in numericCategory.DataList)
                {
                    if (!numericByLevel.TryGetValue(numericEntry.Level, out List<RogueNumericConfig> list))
                    {
                        list = new List<RogueNumericConfig>();
                        numericByLevel.Add(numericEntry.Level, list);
                    }

                    list.Add(new RogueNumericConfig
                    {
                        NumericType = numericEntry.NumericType,
                        Value = numericEntry.Value,
                    });
                }
            }

            this.tableLevels.Clear();
            foreach (RogueLevelEntryConfig levelEntry in levelCategory.DataList)
            {
                RogueLevelConfig levelConfig = new RogueLevelConfig
                {
                    NeedExp = levelEntry.NeedExp,
                    TriggerChoice = levelEntry.TriggerChoice,
                };

                if (numericByLevel.TryGetValue(levelEntry.Level, out List<RogueNumericConfig> numericList))
                {
                    levelConfig.NumericDeltas.AddRange(numericList);
                }

                this.tableLevels[levelEntry.Level] = levelConfig;
            }

            this.tableLevelsBuilt = true;
            return this.tableLevels;
        }

        private Dictionary<int, int> GetResolvedKillExpByUnitType()
        {
            RogueKillExpEntryConfigCategory killExpCategory = RogueKillExpEntryConfigCategory.Instance;
            if (killExpCategory == null || killExpCategory.DataList == null || killExpCategory.DataList.Count == 0)
            {
                return this.killExpByUnitType;
            }

            if (this.tableKillExpBuilt)
            {
                return this.tableKillExpByUnitType;
            }

            this.tableKillExpByUnitType.Clear();
            foreach (RogueKillExpEntryConfig entry in killExpCategory.DataList)
            {
                this.tableKillExpByUnitType[entry.UnitType] = entry.Exp;
            }

            this.tableKillExpBuilt = true;
            return this.tableKillExpByUnitType;
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
        public string Name = string.Empty;

        [BsonElement]
        public string Desc = string.Empty;

        [BsonElement]
        public string ImagePath = string.Empty;

        [BsonElement]
        public string BTConfig = string.Empty;

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

        public static RogueOptionConfig CreateFromCardConfig(RogueCardConfig cardConfig)
        {
            return new RogueOptionConfig
            {
                Name = cardConfig.Name ?? string.Empty,
                Desc = cardConfig.Desc ?? string.Empty,
                ImagePath = cardConfig.ImagePath ?? string.Empty,
                BTConfig = cardConfig.BTConfig ?? string.Empty,
                Weight = cardConfig.Weight > 0 ? cardConfig.Weight : 1,
            };
        }

        public string GetDisplayName()
        {
            return !string.IsNullOrWhiteSpace(this.Name) ? this.Name : string.Empty;
        }

        public string GetDisplayDesc()
        {
            return !string.IsNullOrWhiteSpace(this.Desc) ? this.Desc : string.Empty;
        }

        public string GetImagePath()
        {
            if (!string.IsNullOrWhiteSpace(this.ImagePath))
            {
                return this.ImagePath;
            }

            return this.Icon ?? string.Empty;
        }

        public bool TryGetEffectBuffConfigId(out int buffConfigId)
        {
            buffConfigId = 0;

            if (!string.IsNullOrWhiteSpace(this.BTConfig))
            {
                string configText = this.BTConfig.Trim();
                const string prefix = "buff:";
                if (configText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    configText = configText.Substring(prefix.Length).Trim();
                }

                if (int.TryParse(configText, out buffConfigId) && buffConfigId > 0)
                {
                    return true;
                }
            }

            buffConfigId = this.BuffConfigId;
            return buffConfigId > 0;
        }
    }
}
