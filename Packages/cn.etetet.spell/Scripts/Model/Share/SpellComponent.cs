using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public enum SpellModType
    {
        SPELLMOD_DAMAGE                    = 10,  // 伤害
        SPELLMOD_DURATION                  = 11,  // 持续时间
        SPELLMOD_THREAT                    = 12,  // 仇恨
        SPELLMOD_EFFECT1                   = 13,  // 
        SPELLMOD_CHARGES                   = 14,
        SPELLMOD_RANGE                     = 15,  // 范围
        SPELLMOD_RADIUS                    = 16,  // 半径
        SPELLMOD_CRITICAL_CHANCE           = 17,  
        SPELLMOD_ALL_EFFECTS               = 18,
        SPELLMOD_NOT_LOSE_CASTING_TIME     = 19,  // 不减少施放时间
        SPELLMOD_CASTING_TIME              = 20,  // 施放时间
        SPELLMOD_COOLDOWN                  = 21,  // 冷却
        SPELLMOD_IGNORE_ARMOR              = 23,  // 忽略护甲
        SPELLMOD_COST                      = 24,  // 消耗
        SPELLMOD_CRIT_DAMAGE_BONUS         = 25,  // 暴击伤害增加
        SPELLMOD_RESIST_MISS_CHANCE        = 26,  // 抗性
        SPELLMOD_JUMP_TARGETS              = 27,  //
        SPELLMOD_CHANCE_OF_SUCCESS         = 28,  //
        SPELLMOD_ACTIVATION_TIME           = 29,  
        SPELLMOD_DAMAGE_MULTIPLIER         = 30,
        SPELLMOD_GLOBAL_COOLDOWN           = 31,  // 全局CD
        SPELLMOD_DOT                       = 32,
        SPELLMOD_EFFECT3                   = 33,
        SPELLMOD_BONUS_MULTIPLIER          = 34,
        SPELLMOD_PROC_PER_MINUTE           = 36,
        SPELLMOD_VALUE_MULTIPLIER          = 37,
        SPELLMOD_RESIST_DISPEL_CHANCE      = 38,
        SPELLMOD_CRIT_DAMAGE_BONUS_2       = 39, // 暴击伤害增加2
        SPELLMOD_SPELL_COST_REFUND_ON_FAIL = 40
    };
    
    [Module(ModuleName.Spell)]
    [ComponentOf(typeof(Unit))]
    public class SpellComponent: Entity, IAwake, ITransfer
    {
        [BsonIgnore]
        public EntityRef<Buff> Current { get; set; }
        
        public long CDTime { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<int, long> SpellCD = new();

        // 技能修改点
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<int, Dictionary<int, int>> SpellMods = new();
    }
}