using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 注册所有肉鸽卡牌的 EffectGroup 配置。
    /// 当前内置卡牌统一收敛为“EffectGroup -> 显式 BuffConfig”映射，避免旧占位语义残留。
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
            config.RegisterEffectGroup(BuffGroup(1001));
            config.RegisterEffectGroup(BuffGroup(1002));
            config.RegisterEffectGroup(BuffGroup(1003));
            config.RegisterEffectGroup(BuffGroup(1004));
            config.RegisterEffectGroup(BuffGroup(1005));
            config.RegisterEffectGroup(BuffGroup(1006));
            config.RegisterEffectGroup(BuffGroup(1007));
        }

        // ========== 全能搜查 ==========
        private static void RegisterAllSearch(RogueRuntimeConfigCategory config)
        {
            config.RegisterEffectGroup(BuffGroup(1011));
            config.RegisterEffectGroup(BuffGroup(1012));
            config.RegisterEffectGroup(BuffGroup(1013));
            config.RegisterEffectGroup(BuffGroup(1014));
            config.RegisterEffectGroup(BuffGroup(1015));
            config.RegisterEffectGroup(BuffGroup(1016));
        }

        // ========== 首领猎手 ==========
        private static void RegisterBossHunter(RogueRuntimeConfigCategory config)
        {
            config.RegisterEffectGroup(BuffGroup(1021));
            config.RegisterEffectGroup(BuffGroup(1022));
            config.RegisterEffectGroup(BuffGroup(1023));
            config.RegisterEffectGroup(BuffGroup(1024));
            config.RegisterEffectGroup(BuffGroup(1025));
            config.RegisterEffectGroup(BuffGroup(1026));
        }

        // ========== 火力进化 ==========
        private static void RegisterFirepower(RogueRuntimeConfigCategory config)
        {
            config.RegisterEffectGroup(BuffGroup(1031));
            config.RegisterEffectGroup(BuffGroup(1032));
            config.RegisterEffectGroup(BuffGroup(1033));
            config.RegisterEffectGroup(BuffGroup(1034));
            config.RegisterEffectGroup(BuffGroup(1035));
            config.RegisterEffectGroup(BuffGroup(1036));
        }

        // ========== 持久作战 ==========
        private static void RegisterEndurance(RogueRuntimeConfigCategory config)
        {
            config.RegisterEffectGroup(BuffGroup(1041));
            config.RegisterEffectGroup(BuffGroup(1042));
            config.RegisterEffectGroup(BuffGroup(1043));
            config.RegisterEffectGroup(BuffGroup(1044));
            config.RegisterEffectGroup(BuffGroup(1045));
        }

        // ========== 猎杀者 ==========
        private static void RegisterHunter(RogueRuntimeConfigCategory config)
        {
            config.RegisterEffectGroup(BuffGroup(1051));
            config.RegisterEffectGroup(BuffGroup(1052));
            config.RegisterEffectGroup(BuffGroup(1053));
            config.RegisterEffectGroup(BuffGroup(1054));
            config.RegisterEffectGroup(BuffGroup(1055));
            config.RegisterEffectGroup(BuffGroup(1056));
        }

        // ========== 掷骰狂人 ==========
        private static void RegisterDiceManiac(RogueRuntimeConfigCategory config)
        {
            config.RegisterEffectGroup(BuffGroup(1061));
            config.RegisterEffectGroup(BuffGroup(1062));
            config.RegisterEffectGroup(BuffGroup(1063));
            config.RegisterEffectGroup(BuffGroup(1064));
            config.RegisterEffectGroup(BuffGroup(1065));
        }

        // ========== 工具方法 ==========
        private static RogueEffectGroupConfig BuffGroup(int id, bool removeOnRunEnd = true)
        {
            return Group(id, removeOnRunEnd, Entry(RogueEffectExecuteType.AddBuff, refId: id));
        }

        private static RogueEffectGroupConfig Group(int id, params RogueEffectEntryConfig[] entries)
        {
            return Group(id, true, entries);
        }

        private static RogueEffectGroupConfig Group(int id, bool removeOnRunEnd, params RogueEffectEntryConfig[] entries)
        {
            RogueEffectGroupConfig group = new RogueEffectGroupConfig();
            group.Id = id;
            group.RemoveOnRunEnd = removeOnRunEnd;
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
