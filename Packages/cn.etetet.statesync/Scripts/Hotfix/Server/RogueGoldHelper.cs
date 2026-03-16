namespace ET.Server
{
    public static class RogueGoldHelper
    {
        private const int CommonShowTagType = 1;
        private const string GoldArchetypeShowTagName = "打金专家";
        private const string GoldArchetypeShowTagAlias = "1";
        private const int RogueCardGoldBonusAtTwoPermille = 1150;
        private const int RogueCardGoldBonusAtThreePermille = 1300;

        public static int GetGoldDeltaBySource(RogueProgressComponent progress, int delta, int sourceType)
        {
            if (delta <= 0)
            {
                return delta;
            }

            int bonusPermille = sourceType switch
            {
                RogueGoldSourceType.RogueCard => GetRogueCardGoldBonusPermille(progress),
                _ => 1000,
            };

            if (bonusPermille <= 1000)
            {
                return delta;
            }

            long finalDelta = (long)delta * bonusPermille / 1000;
            if (finalDelta > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)finalDelta;
        }

        public static int AddGold(RogueProgressComponent progress, int delta)
        {
            if (progress == null || progress.IsDisposed || delta == 0)
            {
                return progress?.CurrentGold ?? 0;
            }

            long nextGold = (long)progress.CurrentGold + delta;
            if (nextGold < 0)
            {
                nextGold = 0;
            }
            else if (nextGold > int.MaxValue)
            {
                nextGold = int.MaxValue;
            }

            progress.CurrentGold = (int)nextGold;
            return progress.CurrentGold;
        }

        public static bool CanAfford(RogueProgressComponent progress, int cost)
        {
            if (progress == null || progress.IsDisposed)
            {
                return false;
            }

            if (cost <= 0)
            {
                return true;
            }

            return progress.CurrentGold >= cost;
        }

        public static bool TryCostGold(RogueProgressComponent progress, int cost)
        {
            if (!CanAfford(progress, cost))
            {
                return false;
            }

            if (cost <= 0)
            {
                return true;
            }

            progress.CurrentGold -= cost;
            return true;
        }

        public static int GetKillGoldBonus(Unit unit, int targetUnitType)
        {
            return RogueEffectQueryHelper.GetKillGoldBonus(unit, targetUnitType);
        }

        // 兼容旧测试入口：仍允许从 runtime 查询击杀金币加成。
        public static int GetKillGoldBonus(RogueEffectRuntimeComponent runtimeComponent, int targetUnitType)
        {
            if (runtimeComponent == null || targetUnitType != (int)UnitType.Monster)
            {
                return 0;
            }

            return runtimeComponent.GetTotalValue1ByExecuteType(RogueEffectExecuteType.AddKillGold);
        }

        private static int GetRogueCardGoldBonusPermille(RogueProgressComponent progress)
        {
            if (progress == null || progress.IsDisposed || progress.CommonShowTagCounts == null || progress.CommonShowTagCounts.Count == 0)
            {
                return 1000;
            }

            int tagCount = GetShowTagCount(progress.CommonShowTagCounts, GoldArchetypeShowTagName);
            if (tagCount <= 0)
            {
                tagCount = GetShowTagCount(progress.CommonShowTagCounts, GoldArchetypeShowTagAlias);
            }

            if (tagCount <= 0)
            {
                return 1000;
            }

            if (tagCount >= 3)
            {
                return RogueCardGoldBonusAtThreePermille;
            }

            if (tagCount >= 2)
            {
                return RogueCardGoldBonusAtTwoPermille;
            }

            return 1000;
        }

        private static int GetShowTagCount(System.Collections.Generic.Dictionary<int, int> tagCounts, string showTagName)
        {
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null || tagCounts == null || string.IsNullOrEmpty(showTagName))
            {
                return 0;
            }

            int totalCount = 0;
            foreach ((int tagId, int tagCount) in tagCounts)
            {
                if (!configCategory.TryGetTag(tagId, out RogueTagConfig tagConfig) ||
                    tagConfig == null ||
                    tagConfig.TagType != CommonShowTagType ||
                    !string.Equals(tagConfig.ShowTagsName, showTagName))
                {
                    continue;
                }

                totalCount += tagCount;
            }

            return totalCount;
        }
    }
}
