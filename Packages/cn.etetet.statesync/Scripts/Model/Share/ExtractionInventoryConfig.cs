using System;

namespace ET
{
    /// <summary>
    /// 搜打撤背包与尸体盒运行时配置读取。
    /// </summary>
    public static class ExtractionInventoryConfig
    {
        public static int GetSafeSlotStart()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            RogueGlobalConfig data = category?.Data;
            return Math.Max(0, data?.SafeSlotStart ?? 0);
        }

        public static int GetSafeSlotCount()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            RogueGlobalConfig data = category?.Data;
            return Math.Max(0, data?.SafeSlotCount ?? 0);
        }

        public static bool IsSafeSlot(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            int safeSlotStart = GetSafeSlotStart();
            int safeSlotCount = GetSafeSlotCount();
            return safeSlotCount > 0 && slotIndex >= safeSlotStart && slotIndex < safeSlotStart + safeSlotCount;
        }

        public static float GetCorpseInteractRange()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            RogueGlobalConfig data = category?.Data;
            return Math.Max(0f, data?.CorpseInteractRange ?? 0f);
        }

        public static int GetCorpseButtonTextId()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            RogueGlobalConfig data = category?.Data;
            return Math.Max(0, data?.CorpseButtonTextId ?? 0);
        }

        public static float GetGroundDropInteractRange()
        {
            return GetCorpseInteractRange();
        }

        public static int GetGroundDropButtonTextId()
        {
            return GetCorpseButtonTextId();
        }

        public static int GetDefaultMonsterCorpseLootBoxUnitConfigId()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            RogueGlobalConfig data = category?.Data;
            return Math.Max(0, data?.DefaultMonsterCorpseLootBoxUnitConfigId ?? 0);
        }

        public static float GetGroundDropForwardDistance()
        {
            return 1f;
        }

        /// <summary>
        /// 根据物品品质获取搜索动效持续时间（毫秒）
        /// 品质越高，搜索时间越长
        /// </summary>
        /// <param name="quality">物品品质（1-5）</param>
        /// <returns>搜索持续时间（毫秒）</returns>
        public static long GetItemSearchDurationMsByQuality(int quality)
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            RogueGlobalConfig data = category?.Data;
            
            // 尝试从配置读取，如果配置不存在则使用默认值
            // 配置字段命名规则：SearchDurationQ1, SearchDurationQ2, ...
            // 目前配置表中没有这些字段，使用默认值
            // TODO: 后续在RogueGlobalConfig配置表中添加 SearchDurationQ1~Q5 字段
            
            // 默认搜索时间配置（毫秒）：
            // 品质1（白色）: 500ms
            // 品质2（绿色）: 1000ms
            // 品质3（蓝色）: 1500ms
            // 品质4（紫色）: 2000ms
            // 品质5（橙色）: 2500ms
            return quality switch
            {
                1 => 500,
                2 => 1000,
                3 => 1500,
                4 => 2000,
                5 => 2500,
                _ => 1000 // 默认值
            };
        }

        /// <summary>
        /// 获取默认物品搜索动效持续时间（毫秒）
        /// </summary>
        public static long GetDefaultItemSearchDurationMs()
        {
            return 1000;
        }
    }
}
