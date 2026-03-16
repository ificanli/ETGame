using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueTagEffectHelper
    {
        private const int CommonShowTagType = 1;

        public static void RefreshAppliedTagBuffs(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return;
            }

            Dictionary<int, int> tagCounts = BuildCommonShowTagCounts(progress);
            Dictionary<int, int> desiredBuffConfigs = BuildDesiredBuffConfigs(tagCounts);

            progress.CommonShowTagCounts.Clear();
            foreach (KeyValuePair<int, int> kv in tagCounts)
            {
                progress.CommonShowTagCounts[kv.Key] = kv.Value;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            List<KeyValuePair<int, long>> appliedBuffs = new(progress.AppliedShowTagBuffIds);
            foreach (KeyValuePair<int, long> kv in appliedBuffs)
            {
                int desiredBuffConfigId = desiredBuffConfigs.GetValueOrDefault(kv.Key);
                Buff currentBuff = buffComponent?.GetChild<Buff>(kv.Value);
                int currentBuffConfigId = currentBuff?.ConfigId ?? 0;
                if (desiredBuffConfigId > 0 && currentBuff != null && currentBuffConfigId == desiredBuffConfigId)
                {
                    continue;
                }

                if (currentBuff != null)
                {
                    BuffHelper.RemoveBuff(unit, kv.Value, BuffFlags.SameConfigIdReplaceRemove);
                }

                progress.AppliedShowTagBuffIds.Remove(kv.Key);
            }

            if (buffComponent == null)
            {
                return;
            }

            foreach (KeyValuePair<int, int> kv in desiredBuffConfigs)
            {
                if (progress.AppliedShowTagBuffIds.ContainsKey(kv.Key))
                {
                    continue;
                }

                Buff buff = BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), kv.Value, null);
                if (buff != null)
                {
                    progress.AppliedShowTagBuffIds[kv.Key] = buff.Id;
                }
            }
        }

        public static void ClearAppliedTagBuffs(Unit unit, RogueProgressComponent progress)
        {
            if (progress == null)
            {
                return;
            }

            if (unit != null && !unit.IsDisposed)
            {
                List<KeyValuePair<int, long>> appliedBuffs = new(progress.AppliedShowTagBuffIds);
                foreach (KeyValuePair<int, long> kv in appliedBuffs)
                {
                    BuffHelper.RemoveBuff(unit, kv.Value, BuffFlags.SameConfigIdReplaceRemove);
                }
            }

            progress.CommonShowTagCounts.Clear();
            progress.AppliedShowTagBuffIds.Clear();
        }

        private static Dictionary<int, int> BuildCommonShowTagCounts(RogueProgressComponent progress)
        {
            Dictionary<int, int> result = new();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (progress == null || configCategory == null)
            {
                return result;
            }

            foreach (int optionId in progress.SelectedOptionIds)
            {
                if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) || optionConfig == null)
                {
                    continue;
                }

                HashSet<int> uniqueTags = new();
                CountCommonShowTags(configCategory, optionConfig.ShowTags, uniqueTags, result);
                CountCommonShowTags(configCategory, optionConfig.HideTags, uniqueTags, result);
            }

            return result;
        }

        private static void CountCommonShowTags(RogueRuntimeConfigCategory configCategory, int[] tagIds, HashSet<int> uniqueTags, Dictionary<int, int> result)
        {
            if (configCategory == null || tagIds == null || tagIds.Length == 0)
            {
                return;
            }

            foreach (int tagId in tagIds)
            {
                if (tagId <= 0 || !uniqueTags.Add(tagId))
                {
                    continue;
                }

                if (!configCategory.TryGetTag(tagId, out RogueTagConfig tagConfig) || tagConfig == null || tagConfig.TagType != CommonShowTagType)
                {
                    continue;
                }

                if (result.TryGetValue(tagId, out int count))
                {
                    result[tagId] = count + 1;
                }
                else
                {
                    result[tagId] = 1;
                }
            }
        }

        private static Dictionary<int, int> BuildDesiredBuffConfigs(Dictionary<int, int> tagCounts)
        {
            Dictionary<int, int> result = new();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null || tagCounts == null || tagCounts.Count == 0)
            {
                return result;
            }

            foreach (KeyValuePair<int, int> kv in tagCounts)
            {
                if (kv.Value < 2)
                {
                    continue;
                }

                if (!configCategory.TryGetTag(kv.Key, out RogueTagConfig tagConfig) || tagConfig == null)
                {
                    continue;
                }

                int buffConfigId = GetBuffConfigIdByCount(tagConfig, kv.Value);
                if (buffConfigId <= 0)
                {
                    continue;
                }

                if (!BuffConfigCategory.Instance.Contain(buffConfigId))
                {
                    Log.Warning($"[Rogue] common show tag buff missing: tagId={kv.Key}, buffConfigId={buffConfigId}");
                    continue;
                }

                result[kv.Key] = buffConfigId;
            }

            return result;
        }

        private static int GetBuffConfigIdByCount(RogueTagConfig tagConfig, int count)
        {
            if (tagConfig?.ShowTagsBuffId == null || tagConfig.ShowTagsBuffId.Length == 0 || count < 2)
            {
                return 0;
            }

            int index = count - 1;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= tagConfig.ShowTagsBuffId.Length)
            {
                index = tagConfig.ShowTagsBuffId.Length - 1;
            }

            return tagConfig.ShowTagsBuffId[index];
        }
    }
}
