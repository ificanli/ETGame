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
            return Math.Max(0, category?.SafeSlotStart ?? 0);
        }

        public static int GetSafeSlotCount()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            return Math.Max(0, category?.SafeSlotCount ?? 0);
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
            return Math.Max(0f, category?.CorpseInteractRange ?? 0f);
        }

        public static int GetCorpseButtonTextId()
        {
            RogueGlobalConfigCategory category = RogueGlobalConfigCategory.Instance;
            return Math.Max(0, category?.CorpseButtonTextId ?? 0);
        }
    }
}
