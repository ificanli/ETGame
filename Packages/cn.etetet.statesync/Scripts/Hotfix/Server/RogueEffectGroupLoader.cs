using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 注册所有肉鸽卡牌的 EffectGroup 配置。在地图初始化时调用。
    /// 卡牌 ID 规则：
    ///   打金专家 1001-1007, 全能搜查 1011-1016, 首领猎手 1021-1026
    ///   火力进化 1031-1036, 持久作战 1041-1045, 猎杀者 1051-1056, 掷骰狂人 1061-1065
    /// EffectGroup ID = 卡牌 ID（一一对应）
    /// ShowTag ID: 打金=101, 搜查=102, 猎手=103, 火力=104, 持久=105, 猎杀=106, 掷骰=107
    /// </summary>
    public static class RogueEffectGroupLoader
    {
        public static void RegisterAll()
        {
            RogueRuntimeConfigCategory config = RogueRuntimeConfigCategory.Instance;
            if (config == null) return;

            RegisterGoldExpert(config);
            RegisterAllSearch(config);
            RegisterBossHunter(config);
            RegisterFirepower(config);
            RegisterEndurance(config);
            RegisterHunter(config);
            RegisterDiceManiac(config);

            Log.Info("[RogueLoader] all effect groups registered");
        }

        // ========== 打金专家 ==========
        private static void RegisterGoldExpert(RogueRuntimeConfigCategory config)
        {
            // 1001: 商人给予30000金币（紫色）→ AddGold
            config.RegisterEffectGroup(Group(1001, Entry(RogueEffectExecuteType.AddGold, value1: 30000)));

            // 1002: 直接给予5000金币（蓝色）→ AddGold
            config.RegisterEffectGroup(Group(1002, Entry(RogueEffectExecuteType.AddGold, value1: 5000)));

            // 1003: 每击杀怪物额外获得300金币（紫色）→ AddKillGold
            config.RegisterEffectGroup(Group(1003, Entry(RogueEffectExecuteType.AddKillGold, value1: 300)));

            // 1004: 致命伤害免死，消耗50%金币（橙色）→ AddBuff + Value2=1 标记  
            config.RegisterEffectGroup(Group(1004, Entry(RogueEffectExecuteType.AddBuff, refId: 0, value2: 1)));

            // 1005: 废品回收：搜索未获高品质物品额外300金币（紫色）→ 存储为 Runtime，由搜索事件触发
            config.RegisterEffectGroup(Group(1005, Entry(RogueEffectExecuteType.AddKillGold, value1: 300))); // 暂用 AddKillGold 占位

            // 1006: 每500金币提升1%全伤害（紫色）→ GoldDamageBonus
            config.RegisterEffectGroup(Group(1006, Entry(RogueEffectExecuteType.GoldDamageBonus, value1: 500)));

            // 1007: 进入新区域获得800金币（紫色）→ AreaDiscoveryGold
            config.RegisterEffectGroup(Group(1007, Entry(RogueEffectExecuteType.AreaDiscoveryGold, value1: 800)));
        }

        // ========== 全能搜查 ==========
        private static void RegisterAllSearch(RogueRuntimeConfigCategory config)
        {
            // 1011: 简易雷达（蓝色）→ 存储为 Runtime，由定时器触发（暂占位）
            config.RegisterEffectGroup(Group(1011, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1012: 刮刮乐区域宝箱概率增加（紫色）→ 存储为 Runtime
            config.RegisterEffectGroup(Group(1012, Entry(RogueEffectExecuteType.ProbabilityMultiplier, value1: 2000)));

            // 1013: 获得紫色钥匙（紫色）→ AddGold 占位（实际需要物品系统）
            config.RegisterEffectGroup(Group(1013, Entry(RogueEffectExecuteType.AddGold, value1: 0)));

            // 1014: 远程搜索3米（橙色）→ 存储为 Runtime
            config.RegisterEffectGroup(Group(1014, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1015: 搜索不发出声音（紫色）→ 存储为 Runtime
            config.RegisterEffectGroup(Group(1015, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1016: 背包越满临时生命越高（橙色）→ 存储为 Runtime
            config.RegisterEffectGroup(Group(1016, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));
        }

        // ========== 首领猎手 ==========
        private static void RegisterBossHunter(RogueRuntimeConfigCategory config)
        {
            // 1021: 任务击杀15怪 → 对怪物伤害+50%（橙色）
            config.RegisterEffectGroup(Group(1021,
                Entry(RogueEffectExecuteType.RegisterObjective, refId: 0, value1: 15),
                Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1022: Boss伤害降低30% + 对Boss吸血30%（紫色）
            config.RegisterEffectGroup(Group(1022,
                Entry(RogueEffectExecuteType.DamageReduction, value1: 0, value2: 1),
                Entry(RogueEffectExecuteType.LifeSteal, value1: 300)));

            // 1023: 击杀怪物回复2%已损生命（蓝色）
            config.RegisterEffectGroup(Group(1023, Entry(RogueEffectExecuteType.OnKillHeal, value1: 20)));

            // 1024: 刮刮乐区域怪物概率增加（紫色）
            config.RegisterEffectGroup(Group(1024, Entry(RogueEffectExecuteType.ProbabilityMultiplier, value1: 2000)));

            // 1025: 击杀精英/Boss后武器获得永久附魔（橙色）
            config.RegisterEffectGroup(Group(1025,
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.Penetration, value1: 100),
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.BulletDamage, value1: 150)));

            // 1026: 向30m内怪物移动速度+30%（蓝色）
            config.RegisterEffectGroup(Group(1026, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));
        }

        // ========== 火力进化 ==========
        private static void RegisterFirepower(RogueRuntimeConfigCategory config)
        {
            // 1031: 攻击距离+20%（紫色）
            config.RegisterEffectGroup(Group(1031, Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.AttackRange, value1: 200)));

            // 1032: 技能禁用 + 攻击速度+50%（橙色）
            config.RegisterEffectGroup(Group(1032,
                Entry(RogueEffectExecuteType.SkillDisable),
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.AttackSpeed, value1: 500)));

            // 1033: 命中敌方英雄临时+1%暴击率（橙色）
            config.RegisterEffectGroup(Group(1033, Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.CritRate, value1: 10)));

            // 1034: 弹匣+50% + 换弹速度+30%（蓝色）
            config.RegisterEffectGroup(Group(1034,
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.MagazineCapacity, value1: 500),
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.ReloadSpeed, value1: 300)));

            // 1035: 静止5秒后射击抛射陷阱（紫色）→ 存储为 Runtime
            config.RegisterEffectGroup(Group(1035, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1036: 换弹后前3发伤害+200%+穿透（紫色）
            config.RegisterEffectGroup(Group(1036,
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.BulletDamage, value1: 2000),
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.Penetration, value1: 100)));
        }

        // ========== 持久作战 ==========
        private static void RegisterEndurance(RogueRuntimeConfigCategory config)
        {
            // 1041: 体型变大 + 生命+30%（紫色）
            config.RegisterEffectGroup(Group(1041, Entry(RogueEffectExecuteType.ScaleModifier, value1: 200, value2: 300)));

            // 1042: 1%/秒生命恢复（紫色）→ AddBuff
            config.RegisterEffectGroup(Group(1042, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1043: 不倒翁：低于20%HP获得50%MaxHP护盾（橙色）→ AddBuff
            config.RegisterEffectGroup(Group(1043, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1044: 游戏延长5分钟 + 每30秒生命上限+3%（橙色）
            config.RegisterEffectGroup(Group(1044,
                Entry(RogueEffectExecuteType.ExtendGameTime, value1: 300000),
                Entry(RogueEffectExecuteType.PeriodicHpScale, value1: 30000, value2: 30)));

            // 1045: 每30秒召唤绿树精灵（橙色）→ AddBuff
            config.RegisterEffectGroup(Group(1045, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));
        }

        // ========== 猎杀者 ==========
        private static void RegisterHunter(RogueRuntimeConfigCategory config)
        {
            // 1051: 对低于30%HP目标额外30%伤害（紫色）
            config.RegisterEffectGroup(Group(1051, Entry(RogueEffectExecuteType.LowHpDamageBonus, value1: 300, value2: 300)));

            // 1052: 击杀叠加30%攻击力，最多3层（橙色）
            config.RegisterEffectGroup(Group(1052, Entry(RogueEffectExecuteType.OnKillStackAttack, value1: 300, value2: 3)));

            // 1053: 脱战隐身（橙色）→ AddBuff
            config.RegisterEffectGroup(Group(1053, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));

            // 1054: 体型变小 + 移速+30% + 体型差额外伤害（紫色）
            config.RegisterEffectGroup(Group(1054,
                Entry(RogueEffectExecuteType.ScaleModifier, value1: -200, value2: -200)));

            // 1055: 攻击距离-80% + 伤害+400%（橙色）
            config.RegisterEffectGroup(Group(1055,
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.AttackRange, value1: -800),
                Entry(RogueEffectExecuteType.WeaponModifier, refId: WeaponModType.BulletDamage, value1: 4000)));

            // 1056: 释放技能后+10%移速（蓝色）→ AddBuff
            config.RegisterEffectGroup(Group(1056, Entry(RogueEffectExecuteType.AddBuff, refId: 0)));
        }

        // ========== 掷骰狂人 ==========
        private static void RegisterDiceManiac(RogueRuntimeConfigCategory config)
        {
            // 1061: 获得1个随机紫色卡牌（蓝色）
            config.RegisterEffectGroup(Group(1061, Entry(RogueEffectExecuteType.GrantRandomCard, value1: 2, value2: 1)));

            // 1062: 获得2个随机橙色卡牌（橙色）
            config.RegisterEffectGroup(Group(1062, Entry(RogueEffectExecuteType.GrantRandomCard, value1: 3, value2: 2)));

            // 1063: 1%秒杀非Boss + 爆8888金币（橙色）
            config.RegisterEffectGroup(Group(1063, Entry(RogueEffectExecuteType.InstantKillChance, value1: 10, value2: 8888)));

            // 1064: 幸运女神：概率触发效果×1.5（紫色）
            config.RegisterEffectGroup(Group(1064, Entry(RogueEffectExecuteType.ProbabilityMultiplier, value1: 1500)));

            // 1065: 命运重置：移除所有卡牌，替换为随机紫/橙卡（橙色）
            config.RegisterEffectGroup(Group(1065,
                Entry(RogueEffectExecuteType.ReplaceAllCards),
                Entry(RogueEffectExecuteType.GrantRandomCard, value1: 2, value2: 2),
                Entry(RogueEffectExecuteType.GrantRandomCard, value1: 3, value2: 1)));
        }

        // ========== 工具方法 ==========
        private static RogueEffectGroupConfig Group(int id, params RogueEffectEntryConfig[] entries)
        {
            RogueEffectGroupConfig group = new RogueEffectGroupConfig();
            group.Id = id;
            group.RemoveOnRunEnd = true;
            group.Entries = new List<RogueEffectEntryConfig>(entries);
            return group;
        }

        private static RogueEffectEntryConfig Entry(int executeType, int refId = 0, int value1 = 0, int value2 = 0)
        {
            RogueEffectEntryConfig entry = new RogueEffectEntryConfig();
            entry.ExecuteType = executeType;
            entry.RefId = refId;
            entry.Value1 = value1;
            entry.Value2 = value2;
            return entry;
        }
    }
}
