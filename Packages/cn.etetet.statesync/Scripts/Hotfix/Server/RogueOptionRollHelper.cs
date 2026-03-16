using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueOptionRollHelper
    {
        private const int HeroTagType = 2;
        private const int WeaponTagType = 3;

        public static List<int> RollOptions(Unit unit, RogueRuntimeConfigCategory configCategory, int requiredQuality)
        {
            int maxCount = configCategory != null ? configCategory.GetChoiceOptionCount() : 0;
            return RollOptions(unit, configCategory, requiredQuality, null, false, maxCount);
        }

        public static List<int> RollOptions(Unit unit, RogueRuntimeConfigCategory configCategory, int requiredQuality, bool ignoreTagFilter)
        {
            int maxCount = configCategory != null ? configCategory.GetChoiceOptionCount() : 0;
            return RollOptions(unit, configCategory, requiredQuality, null, ignoreTagFilter, maxCount);
        }

        public static List<int> RollOptions(
            Unit unit,
            RogueRuntimeConfigCategory configCategory,
            int requiredQuality,
            HashSet<int> excludedOptionIds,
            bool ignoreTagFilter,
            int maxCount)
        {
            List<int> result = new List<int>();
            List<int> candidates = CollectCandidates(unit, configCategory, requiredQuality, excludedOptionIds, ignoreTagFilter);
            if (candidates.Count == 0)
            {
                return result;
            }

            Dictionary<int, RogueOptionConfig> options = configCategory.GetOptions();
            int needCount = maxCount > 0 ? maxCount : 1;
            if (needCount > candidates.Count)
            {
                needCount = candidates.Count;
            }

            while (result.Count < needCount && candidates.Count > 0)
            {
                int selectedIndex = RollOptionIndexByWeight(candidates, options);
                if (selectedIndex < 0 || selectedIndex >= candidates.Count)
                {
                    break;
                }

                int optionId = candidates[selectedIndex];
                result.Add(optionId);
                candidates.RemoveAt(selectedIndex);
            }

            return result;
        }

        public static bool TryRollOneOption(Unit unit, RogueRuntimeConfigCategory configCategory, int requiredQuality, out int optionId)
        {
            return TryRollOneOption(unit, configCategory, requiredQuality, null, out optionId);
        }

        public static bool TryRollOneOption(Unit unit, RogueRuntimeConfigCategory configCategory, int requiredQuality, HashSet<int> excludedOptionIds, out int optionId)
        {
            optionId = 0;
            List<int> candidates = CollectCandidates(unit, configCategory, requiredQuality, excludedOptionIds, false);
            if (candidates.Count == 0)
            {
                return false;
            }

            int selectedIndex = RollOptionIndexByWeight(candidates, configCategory.GetOptions());
            if (selectedIndex < 0 || selectedIndex >= candidates.Count)
            {
                return false;
            }

            optionId = candidates[selectedIndex];
            return optionId > 0;
        }

        private static List<int> CollectCandidates(
            Unit unit,
            RogueRuntimeConfigCategory configCategory,
            int requiredQuality,
            HashSet<int> excludedOptionIds = null,
            bool ignoreTagFilter = false)
        {
            List<int> result = new List<int>();
            if (unit == null || unit.IsDisposed || configCategory == null)
            {
                return result;
            }

            RogueBuffConfigLoader.EnsureRegistered();
            Dictionary<int, RogueOptionConfig> options = configCategory.GetOptions();
            if (options == null || options.Count == 0)
            {
                return result;
            }

            foreach (KeyValuePair<int, RogueOptionConfig> kv in options)
            {
                RogueOptionConfig optionConfig = kv.Value;
                if (optionConfig == null || optionConfig.Quality != requiredQuality)
                {
                    continue;
                }

                if (excludedOptionIds != null && excludedOptionIds.Contains(kv.Key))
                {
                    continue;
                }

                if (!HasExecutableEffect(configCategory, optionConfig))
                {
                    continue;
                }

                if (!ignoreTagFilter && !MatchOptionByTags(unit, configCategory, optionConfig))
                {
                    continue;
                }

                result.Add(kv.Key);
            }

            return result;
        }

        private static bool HasExecutableEffect(RogueRuntimeConfigCategory configCategory, RogueOptionConfig optionConfig)
        {
            return RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, optionConfig, out _);
        }

        private static int RollOptionIndexByWeight(List<int> candidates, Dictionary<int, RogueOptionConfig> options)
        {
            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; ++i)
            {
                int optionId = candidates[i];
                if (!options.TryGetValue(optionId, out RogueOptionConfig optionConfig) || optionConfig == null)
                {
                    totalWeight += 1;
                    continue;
                }

                int weight = optionConfig.Weight > 0 ? optionConfig.Weight : 1;
                totalWeight += weight;
            }

            if (totalWeight <= 0)
            {
                return RandomGenerator.RandomNumber(0, candidates.Count);
            }

            int roll = RandomGenerator.RandomNumber(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < candidates.Count; ++i)
            {
                int optionId = candidates[i];
                int weight = 1;
                if (options.TryGetValue(optionId, out RogueOptionConfig optionConfig) && optionConfig != null && optionConfig.Weight > 0)
                {
                    weight = optionConfig.Weight;
                }

                cursor += weight;
                if (roll < cursor)
                {
                    return i;
                }
            }

            return candidates.Count - 1;
        }

        private static bool MatchOptionByTags(Unit unit, RogueRuntimeConfigCategory configCategory, RogueOptionConfig optionConfig)
        {
            if (unit == null || unit.IsDisposed || configCategory == null || optionConfig == null)
            {
                return false;
            }

            HashSet<int> heroTags = new();
            HashSet<int> weaponTags = new();
            CollectFilterTags(configCategory, optionConfig.ShowTags, heroTags, weaponTags);
            CollectFilterTags(configCategory, optionConfig.HideTags, heroTags, weaponTags);

            if (heroTags.Count > 0)
            {
                int heroConfigId = WeaponInitHelper.GetHeroConfigIdByUnitConfigId(unit.ConfigId);
                if (heroConfigId <= 0 || !MatchesAnyTag(configCategory, heroTags, heroConfigId))
                {
                    return false;
                }
            }

            if (weaponTags.Count > 0)
            {
                HashSet<int> weaponTypeIds = GetOwnedWeaponTypeIds(unit);
                if (weaponTypeIds.Count == 0 || !MatchesAnyTag(configCategory, weaponTags, weaponTypeIds))
                {
                    return false;
                }
            }

            return true;
        }

        private static void CollectFilterTags(RogueRuntimeConfigCategory configCategory, int[] tags, HashSet<int> heroTags, HashSet<int> weaponTags)
        {
            if (configCategory == null || tags == null || tags.Length == 0)
            {
                return;
            }

            foreach (int tagId in tags)
            {
                if (tagId <= 0 || !configCategory.TryGetTag(tagId, out RogueTagConfig tagConfig) || tagConfig == null)
                {
                    continue;
                }

                switch (tagConfig.TagType)
                {
                    case HeroTagType:
                        heroTags.Add(tagId);
                        break;
                    case WeaponTagType:
                        weaponTags.Add(tagId);
                        break;
                }
            }
        }

        private static bool MatchesAnyTag(RogueRuntimeConfigCategory configCategory, HashSet<int> tagIds, int matchValue)
        {
            foreach (int tagId in tagIds)
            {
                if (!configCategory.TryGetTag(tagId, out RogueTagConfig tagConfig) || tagConfig?.MatchValues == null)
                {
                    continue;
                }

                foreach (int value in tagConfig.MatchValues)
                {
                    if (value == matchValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MatchesAnyTag(RogueRuntimeConfigCategory configCategory, HashSet<int> tagIds, HashSet<int> matchValues)
        {
            foreach (int tagId in tagIds)
            {
                if (!configCategory.TryGetTag(tagId, out RogueTagConfig tagConfig) || tagConfig?.MatchValues == null)
                {
                    continue;
                }

                foreach (int value in tagConfig.MatchValues)
                {
                    if (matchValues.Contains(value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static HashSet<int> GetOwnedWeaponTypeIds(Unit unit)
        {
            HashSet<int> result = new();
            if (unit == null || unit.IsDisposed)
            {
                return result;
            }

            EquipmentComponent equipmentComponent = unit.GetComponent<EquipmentComponent>();
            if (equipmentComponent != null)
            {
                TryAddWeaponTypeId(result, equipmentComponent.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId ?? 0);
                TryAddWeaponTypeId(result, equipmentComponent.GetEquippedItem(EquipmentSlotType.OffHand)?.ConfigId ?? 0);
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent != null)
            {
                TryAddWeaponTypeId(result, weaponComponent.Slot1WeaponId);
                TryAddWeaponTypeId(result, weaponComponent.Slot2WeaponId);
            }

            return result;
        }

        private static void TryAddWeaponTypeId(HashSet<int> weaponTypeIds, int weaponConfigId)
        {
            if (weaponConfigId <= 0)
            {
                return;
            }

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(weaponConfigId);
            if (weaponConfig == null || weaponConfig.WeaponTypeId <= 0)
            {
                return;
            }

            weaponTypeIds.Add(weaponConfig.WeaponTypeId);
        }
    }
}
