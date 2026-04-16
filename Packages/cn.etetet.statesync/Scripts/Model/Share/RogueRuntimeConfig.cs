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
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, RogueEffectGroupConfig> effectGroups = new();

        [BsonElement]
        public int DefaultKillExp = 0;

        [BsonElement]
        public int ChoiceOptionCount = 3;

        [BsonElement]
        public int ChoiceOptionRerollCount = 1;

        [BsonIgnore]
        private readonly Dictionary<int, RogueOptionConfig> cardOptions = new();

        [BsonIgnore]
        private readonly Dictionary<int, RogueLevelConfig> tableLevels = new();

        [BsonIgnore]
        private readonly Dictionary<int, int> tableKillExpByUnitType = new();

        [BsonIgnore]
        private readonly Dictionary<int, RogueTagConfig> tableTags = new();

        [BsonIgnore]
        private bool cardOptionsBuilt;

        [BsonIgnore]
        private bool tableLevelsBuilt;

        [BsonIgnore]
        private bool tableKillExpBuilt;

        [BsonIgnore]
        private bool tableTagsBuilt;

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

            return this.GetKillExp((int)unit.UnitType);
        }

        public int GetKillExp(int unitType)
        {
            if (unitType <= 0)
            {
                return 0;
            }

            Dictionary<int, int> resolvedKillExp = this.GetResolvedKillExpByUnitType();
            if (resolvedKillExp.TryGetValue(unitType, out int exp))
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

        public int GetChoiceOptionRerollCount()
        {
            return this.ChoiceOptionRerollCount <= 0 ? 1 : this.ChoiceOptionRerollCount;
        }

        public Dictionary<int, RogueOptionConfig> GetOptions()
        {
            return this.GetResolvedOptions();
        }

        public bool TryGetOption(int optionId, out RogueOptionConfig optionConfig)
        {
            return this.GetResolvedOptions().TryGetValue(optionId, out optionConfig);
        }

        public Dictionary<int, RogueTagConfig> GetTags()
        {
            return this.GetResolvedTags();
        }

        public bool TryGetTag(int tagId, out RogueTagConfig tagConfig)
        {
            return this.GetResolvedTags().TryGetValue(tagId, out tagConfig);
        }

        public bool TryGetEffectGroup(int effectGroupId, out RogueEffectGroupConfig groupConfig)
        {
            return this.effectGroups.TryGetValue(effectGroupId, out groupConfig);
        }

        public void RegisterEffectGroup(RogueEffectGroupConfig groupConfig)
        {
            if (groupConfig == null || groupConfig.Id <= 0)
            {
                return;
            }

            this.effectGroups[groupConfig.Id] = groupConfig;
        }

        public void RemoveEffectGroup(int effectGroupId)
        {
            if (effectGroupId <= 0)
            {
                return;
            }

            this.effectGroups.Remove(effectGroupId);
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
                        UnitType = numericEntry.UnitType,
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

        private Dictionary<int, RogueTagConfig> GetResolvedTags()
        {
            TagsConfigCategory tagsCategory = TagsConfigCategory.Instance;
            if (tagsCategory == null || tagsCategory.DataList == null || tagsCategory.DataList.Count == 0)
            {
                return this.tableTags;
            }

            if (this.tableTagsBuilt)
            {
                return this.tableTags;
            }

            this.tableTags.Clear();
            foreach (TagsConfig tagConfig in tagsCategory.DataList)
            {
                if (tagConfig == null)
                {
                    continue;
                }

                this.tableTags[tagConfig.Id] = RogueTagConfig.CreateFromConfig(tagConfig);
            }

            this.tableTagsBuilt = true;
            return this.tableTags;
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

        [BsonElement]
        public int UnitType = 0; // 0=全体, 1=玩家(Player), 2=怪物(Monster)
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
        public int EffectGroupId = 0;

        [BsonElement]
        public int NameTextId = 0;

        [BsonElement]
        public int DescTextId = 0;

        [BsonElement]
        public string Icon = string.Empty;

        [BsonElement]
        public int Weight = 1;

        [BsonElement]
        public int Quality = 0;

        [BsonElement]
        public int[] ShowTags = Array.Empty<int>();

        [BsonElement]
        public int[] HideTags = Array.Empty<int>();

        public static RogueOptionConfig CreateFromCardConfig(RogueCardConfig cardConfig)
        {
            return new RogueOptionConfig
            {
                Name = cardConfig.Name ?? string.Empty,
                Desc = cardConfig.Desc ?? string.Empty,
                ImagePath = cardConfig.ImagePath ?? string.Empty,
                BTConfig = cardConfig.BTConfig ?? string.Empty,
                EffectGroupId = 0,
                Weight = cardConfig.Weight > 0 ? cardConfig.Weight : 1,
                Quality = cardConfig.Quality,
                ShowTags = cardConfig.ShowTags ?? Array.Empty<int>(),
                HideTags = cardConfig.HideTags ?? Array.Empty<int>(),
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

        public bool TryGetEffectGroupId(out int effectGroupId)
        {
            effectGroupId = this.EffectGroupId;
            if (effectGroupId > 0)
            {
                return true;
            }

            return TryParseConfigId(this.BTConfig, "group:", out effectGroupId) || TryParseConfigId(this.BTConfig, "effect:", out effectGroupId);
        }

        public bool TryGetLegacyBuffConfigId(out int buffConfigId)
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

            if (this.BuffConfigId > 0)
            {
                buffConfigId = this.BuffConfigId;
                return true;
            }

            if (this.EffectGroupId > 0)
            {
                buffConfigId = this.EffectGroupId;
                return true;
            }

            return false;
        }

        private static bool TryParseConfigId(string configText, string prefix, out int configId)
        {
            configId = 0;
            if (string.IsNullOrWhiteSpace(configText))
            {
                return false;
            }

            string trimmedText = configText.Trim();
            if (!trimmedText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string idText = trimmedText.Substring(prefix.Length).Trim();
            return int.TryParse(idText, out configId) && configId > 0;
        }
    }

    public static class RogueEffectExecuteType
    {
        public const int AddBuff = 1;
        public const int RegisterObjective = 2;
        public const int GrantRandomCard = 3;
        public const int ReplaceAllCards = 4;
        public const int AddGold = 5;
        public const int AddKillGold = 6;

        // Phase B/C/D 新增
        public const int OnKillHeal = 7;            // 击杀回血。Value1 = 回复已损生命千分比
        public const int OnKillStackAttack = 8;     // 击杀叠加攻击。Value1 = 每层攻击力加成, Value2 = 最大层数
        public const int WeaponModifier = 9;        // 枪械改造。RefId = 改造类型(WeaponModType), Value1 = 数值千分比
        public const int AreaDiscoveryGold = 10;    // 进入新区域获得金币。Value1 = 金币数量
        public const int ProbabilityMultiplier = 11; // 概率提升。Value1 = 倍率千分比(1500=1.5倍)
        public const int GoldDamageBonus = 12;      // 金币转伤害。Value1 = 每多少金币提升1%伤害
        public const int SkillDisable = 13;         // 禁用技能
        public const int ScaleModifier = 14;        // 体型变化。Value1 = 缩放千分比(正=变大,负=变小)
        public const int LowHpDamageBonus = 15;     // 低血量额外伤害。Value1 = 血量阈值千分比, Value2 = 伤害加成千分比
        public const int DamageReduction = 16;      // 固定减伤。Value1 = 减伤值, Value2 = 来源过滤(0=全部)
        public const int LifeSteal = 17;            // 吸血。Value1 = 吸血千分比
        public const int InstantKillChance = 18;    // 秒杀概率。Value1 = 概率千分比, Value2 = 掉落金币
        public const int ExtendGameTime = 19;       // 延长游戏时长。Value1 = 延长毫秒数
        public const int PeriodicHpScale = 20;      // 周期性生命上限增长。Value1 = 间隔毫秒, Value2 = 每次增长千分比
    }

    [EnableClass]
    public class RogueEffectGroupConfig
    {
        [BsonElement]
        public int Id = 0;

        [BsonElement]
        public bool RemoveOnRunEnd = true;

        [BsonElement]
        public List<RogueEffectEntryConfig> Entries = new();

        public bool TryGetPreviewBuffConfigId(out int buffConfigId)
        {
            buffConfigId = 0;
            if (this.Entries == null)
            {
                return false;
            }

            foreach (RogueEffectEntryConfig entry in this.Entries)
            {
                if (entry == null || entry.ExecuteType != RogueEffectExecuteType.AddBuff || entry.RefId <= 0)
                {
                    continue;
                }

                buffConfigId = entry.RefId;
                return true;
            }

            return false;
        }
    }

    [EnableClass]
    public class RogueEffectEntryConfig
    {
        [BsonElement]
        public int ExecuteType = 0;

        [BsonElement]
        public int RefId = 0;

        [BsonElement]
        public int Value1 = 0;

        [BsonElement]
        public int Value2 = 0;
    }

    [EnableClass]
    public class RogueTagConfig
    {
        [BsonElement]
        public int Id = 0;

        [BsonElement]
        public int TagType = 0;

        [BsonElement]
        public string TagDes = string.Empty;

        [BsonElement]
        public string ShowTagsName = string.Empty;

        [BsonElement]
        public string ShowTagsDesc = string.Empty;

        [BsonElement]
        public int[] ShowTagsBuffId = Array.Empty<int>();

        [BsonElement]
        public int[] MatchValues = Array.Empty<int>();

        public static RogueTagConfig CreateFromConfig(TagsConfig tagConfig)
        {
            return new RogueTagConfig
            {
                Id = tagConfig.Id,
                TagType = tagConfig.TagType,
                TagDes = tagConfig.TagDes ?? string.Empty,
                ShowTagsName = tagConfig.ShowTagsName ?? string.Empty,
                ShowTagsDesc = tagConfig.ShowTagsDesc ?? string.Empty,
                ShowTagsBuffId = tagConfig.ShowTagsBuffId ?? Array.Empty<int>(),
                MatchValues = tagConfig.MatchValues ?? Array.Empty<int>(),
            };
        }
    }
}
