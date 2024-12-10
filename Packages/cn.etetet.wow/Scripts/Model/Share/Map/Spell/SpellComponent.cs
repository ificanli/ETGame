using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public enum SpellModType
    {
        SPELLMOD_DAMAGE                    = 110,  // 伤害
        SPELLMOD_DURATION                  = 111,  // 持续时间
        SPELLMOD_THREAT                    = 112,  // 仇恨
        SPELLMOD_EFFECT1                   = 113,  // 
        SPELLMOD_CHARGES                   = 114,
        SPELLMOD_RANGE                     = 115,  // 范围
        SPELLMOD_RADIUS                    = 116,  // 半径
        SPELLMOD_CRITICAL_CHANCE           = 117,  
        SPELLMOD_ALL_EFFECTS               = 118,
        SPELLMOD_NOT_LOSE_CASTING_TIME     = 119,  // 不减少施放时间
        SPELLMOD_CASTING_TIME              = 120,  // 施放时间
        SPELLMOD_COOLDOWN                  = 121,  // 冷却
        SPELLMOD_IGNORE_ARMOR              = 123,  // 忽略护甲
        SPELLMOD_COST                      = 124,  // 消耗
        SPELLMOD_CRIT_DAMAGE_BONUS         = 125,  // 暴击伤害增加
        SPELLMOD_RESIST_MISS_CHANCE        = 126,  // 抗性
        SPELLMOD_JUMP_TARGETS              = 127,  //
        SPELLMOD_CHANCE_OF_SUCCESS         = 128,  //
        SPELLMOD_ACTIVATION_TIME           = 129,  
        SPELLMOD_DAMAGE_MULTIPLIER         = 130,
        SPELLMOD_GLOBAL_COOLDOWN           = 131,  // 全局CD
        SPELLMOD_DOT                       = 132,
        SPELLMOD_EFFECT3                   = 133,
        SPELLMOD_BONUS_MULTIPLIER          = 134,
        SPELLMOD_PROC_PER_MINUTE           = 136,
        SPELLMOD_VALUE_MULTIPLIER          = 137,
        SPELLMOD_RESIST_DISPEL_CHANCE      = 138,
        SPELLMOD_CRIT_DAMAGE_BONUS_2       = 139, // 暴击伤害增加2
        SPELLMOD_SPELL_COST_REFUND_ON_FAIL = 140
    };
    
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