using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueEffectHelper
    {
        public static int ApplySelectedOption(Unit unit, RogueProgressComponent progress, RogueOptionConfig optionConfig, int optionId, Action<int, long> onBuffApplied)
        {
            if (unit == null || unit.IsDisposed || progress == null || optionConfig == null)
            {
                return ErrorCode.ERR_Cancel;
            }

            RogueBuffConfigLoader.EnsureRegistered();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            bool hasEffectGroup = RogueOptionConfigHelper.TryGetExecutableEffectGroup(configCategory, optionConfig, out int effectGroupId, out RogueEffectGroupConfig groupConfig);
            if (!RogueOptionConfigHelper.TryGetBuffConfigId(optionConfig, out int buffConfigId))
            {
                return ErrorCode.ERR_RogueBuffConfigNotFound;
            }

            BuffConfig buffConfig = BuffConfigCategory.Instance.Get(buffConfigId);
            if (buffConfig == null)
            {
                return ErrorCode.ERR_RogueBuffConfigNotFound;
            }

            Buff buff = BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), buffConfigId, null);
            long appliedBuffId = 0;
            if (buff != null && ShouldKeepBuff(buffConfig))
            {
                appliedBuffId = buff.Id;
                progress.AppliedBuffIds.Add(buff.Id);
            }
            else if (buff != null)
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
            }

            if (hasEffectGroup && groupConfig?.Entries != null)
            {
                bool hasAddBuffEntry = TryGetFirstAddBuffConfigId(groupConfig, out int addBuffConfigId);
                if (hasAddBuffEntry)
                {
                    RegisterAddBuffGroupRuntimes(unit, progress, optionId, effectGroupId, groupConfig, buffConfigId, appliedBuffId);

                    // 兼容旧测试：当 groupId 与主 BuffId 不一致时，允许 group 追加执行额外条目（如 RegisterObjective）。
                    if (effectGroupId != addBuffConfigId)
                    {
                        ExecuteCompatGroupExtraEntries(unit, progress, optionId, effectGroupId, groupConfig);
                    }
                }
                else
                {
                    RegisterGroupRuntimesWithoutAddBuff(unit, optionId, effectGroupId, groupConfig, buffConfigId, appliedBuffId);
                }
            }
            else if (appliedBuffId > 0)
            {
                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>() ?? unit.AddComponent<RogueEffectRuntimeComponent>();
                runtimeComponent.AddLegacyBuffEffect(optionId, buffConfigId, buffConfigId, appliedBuffId);
            }

            bool skipRecord = buffConfig.GetEffect<EffectRogueReplaceAllCards>() != null;
            if (!skipRecord)
            {
                progress.SelectedOptionIds.Add(optionId);
                progress.AppliedOptionBuffIds.Add(appliedBuffId);
                RogueTagEffectHelper.RefreshAppliedTagBuffs(unit, progress);
            }

            int callbackBuffConfigId = buffConfigId;
            if (hasEffectGroup && TryGetFirstAddBuffConfigId(groupConfig, out int groupAddBuffConfigId))
            {
                callbackBuffConfigId = groupAddBuffConfigId;
            }

            onBuffApplied?.Invoke(callbackBuffConfigId, appliedBuffId);
            return ErrorCode.ERR_Success;
        }

        public static void ClearAppliedEffects(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent != null && progress.AppliedBuffIds != null)
            {
                foreach (long buffId in progress.AppliedBuffIds.ToArray())
                {
                    if (buffId <= 0)
                    {
                        continue;
                    }

                    Buff buff = buffComponent.GetChild<Buff>(buffId);
                    if (buff != null)
                    {
                        BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
                    }
                }
            }

            RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
            if (runtimeComponent != null)
            {
                runtimeComponent.CleanupAllRuntimeSideEffects();
                unit.RemoveComponent<RogueEffectRuntimeComponent>();
            }

            RogueObjectiveComponent objectiveComponent = unit.GetComponent<RogueObjectiveComponent>();
            if (objectiveComponent != null)
            {
                objectiveComponent.ClearObjectives();
                unit.RemoveComponent<RogueObjectiveComponent>();
            }

            progress.AppliedBuffIds.Clear();
            progress.AppliedOptionBuffIds.Clear();
        }

        public static int RemoveSelectedOption(Unit unit, RogueProgressComponent progress, int optionId)
        {
            if (unit == null || unit.IsDisposed || progress == null || optionId <= 0)
            {
                return 0;
            }

            int selectedIndex = progress.SelectedOptionIds.LastIndexOf(optionId);
            if (selectedIndex < 0)
            {
                return 0;
            }

            RemoveSelectedOptionAt(unit, progress, selectedIndex);
            return 1;
        }

        public static int RemoveEffectGroup(Unit unit, RogueProgressComponent progress, int effectGroupId)
        {
            if (unit == null || unit.IsDisposed || progress == null || effectGroupId <= 0)
            {
                return 0;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            int removedCount = 0;
            for (int i = progress.SelectedOptionIds.Count - 1; i >= 0; --i)
            {
                int optionId = progress.SelectedOptionIds[i];
                if (configCategory == null ||
                    !configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) ||
                    optionConfig == null)
                {
                    continue;
                }

                if (RogueOptionConfigHelper.TryGetExecutableEffectGroup(configCategory, optionConfig, out int optionEffectGroupId, out _) &&
                    optionEffectGroupId != effectGroupId)
                {
                    continue;
                }

                if (!optionConfig.TryGetEffectGroupId(out _) &&
                    (!RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, optionConfig, out int buffConfigId) ||
                        buffConfigId != effectGroupId))
                {
                    continue;
                }

                RemoveSelectedOptionAt(unit, progress, i);
                ++removedCount;
            }

            return removedCount;
        }

        public static int ClearSelectedOptions(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return 0;
            }

            int removedCount = 0;
            while (progress.SelectedOptionIds.Count > 0)
            {
                RemoveSelectedOptionAt(unit, progress, progress.SelectedOptionIds.Count - 1);
                ++removedCount;
            }

            return removedCount;
        }

        public static int RemoveRunEndEffectGroups(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return 0;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            int removedCount = 0;
            for (int i = progress.SelectedOptionIds.Count - 1; i >= 0; --i)
            {
                int optionId = progress.SelectedOptionIds[i];
                bool shouldRemove = true;
                if (configCategory != null &&
                    configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) &&
                    optionConfig != null &&
                    RogueOptionConfigHelper.TryGetExecutableEffectGroup(configCategory, optionConfig, out _, out RogueEffectGroupConfig groupConfig) &&
                    groupConfig != null)
                {
                    shouldRemove = groupConfig.RemoveOnRunEnd;
                }

                if (!shouldRemove)
                {
                    continue;
                }

                RemoveSelectedOptionAt(unit, progress, i);
                ++removedCount;
            }

            return removedCount;
        }

        private static void RemoveSelectedOptionAt(Unit unit, RogueProgressComponent progress, int index)
        {
            if (index < 0 || index >= progress.SelectedOptionIds.Count)
            {
                return;
            }

            int removedOptionId = progress.SelectedOptionIds[index];
            long appliedBuffId = 0;
            if (progress.AppliedOptionBuffIds != null && index >= 0 && index < progress.AppliedOptionBuffIds.Count)
            {
                appliedBuffId = progress.AppliedOptionBuffIds[index];
                progress.AppliedOptionBuffIds.RemoveAt(index);
            }

            progress.SelectedOptionIds.RemoveAt(index);

            HashSet<long> removedRuntimeBuffIds = new();
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
            runtimeComponent?.RemoveOneRuntimeByOptionId(removedOptionId, buffComponent, removedRuntimeBuffIds);
            if (runtimeComponent != null && runtimeComponent.ChildrenCount() == 0)
            {
                unit.RemoveComponent<RogueEffectRuntimeComponent>();
            }

            foreach (long removedBuffId in removedRuntimeBuffIds)
            {
                progress.AppliedBuffIds.Remove(removedBuffId);
            }

            if (appliedBuffId > 0)
            {
                if (!removedRuntimeBuffIds.Contains(appliedBuffId))
                {
                    Buff buff = buffComponent?.GetChild<Buff>(appliedBuffId);
                    if (buff != null)
                    {
                        BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
                    }
                }

                progress.AppliedBuffIds.Remove(appliedBuffId);
            }

            RogueObjectiveComponent objectiveComponent = unit.GetComponent<RogueObjectiveComponent>();
            if (objectiveComponent != null)
            {
                objectiveComponent.RemoveOneObjectiveByOptionId(removedOptionId);
                if (objectiveComponent.ChildrenCount() == 0)
                {
                    unit.RemoveComponent<RogueObjectiveComponent>();
                }
            }

            RogueTagEffectHelper.RefreshAppliedTagBuffs(unit, progress);
        }

        private static void RegisterAddBuffGroupRuntimes(
            Unit unit,
            RogueProgressComponent progress,
            int optionId,
            int effectGroupId,
            RogueEffectGroupConfig groupConfig,
            int mainBuffConfigId,
            long mainAppliedBuffId)
        {
            if (unit == null || unit.IsDisposed || groupConfig?.Entries == null)
            {
                return;
            }

            RogueEffectRuntimeComponent runtimeComponent = null;
            foreach (RogueEffectEntryConfig entry in groupConfig.Entries)
            {
                if (entry == null || entry.ExecuteType != RogueEffectExecuteType.AddBuff || entry.RefId <= 0)
                {
                    continue;
                }

                long appliedBuffId = 0;
                int buffConfigId = entry.RefId;
                if (buffConfigId == mainBuffConfigId)
                {
                    appliedBuffId = mainAppliedBuffId;
                }
                else
                {
                    BuffConfig subBuffConfig = BuffConfigCategory.Instance.Get(buffConfigId);
                    if (subBuffConfig == null)
                    {
                        continue;
                    }

                    Buff subBuff = BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), buffConfigId, null);
                    if (subBuff != null && ShouldKeepBuff(subBuffConfig))
                    {
                        appliedBuffId = subBuff.Id;
                        progress.AppliedBuffIds.Add(subBuff.Id);
                    }
                    else if (subBuff != null)
                    {
                        BuffHelper.RemoveBuff(subBuff, BuffFlags.NoDurationRemove);
                    }
                }

                runtimeComponent ??= unit.GetComponent<RogueEffectRuntimeComponent>() ?? unit.AddComponent<RogueEffectRuntimeComponent>();
                RogueEffectRuntime runtime = runtimeComponent.AddEffectRuntime(optionId, effectGroupId, entry, appliedBuffId);
                runtime.EffectBuffConfigId = buffConfigId;
            }
        }

        private static void RegisterGroupRuntimesWithoutAddBuff(
            Unit unit,
            int optionId,
            int effectGroupId,
            RogueEffectGroupConfig groupConfig,
            int effectBuffConfigId,
            long appliedBuffId)
        {
            if (unit == null || unit.IsDisposed || groupConfig?.Entries == null)
            {
                return;
            }

            RogueEffectRuntimeComponent runtimeComponent = null;
            foreach (RogueEffectEntryConfig entry in groupConfig.Entries)
            {
                if (entry == null || !ShouldRecordRuntime(entry.ExecuteType))
                {
                    continue;
                }

                runtimeComponent ??= unit.GetComponent<RogueEffectRuntimeComponent>() ?? unit.AddComponent<RogueEffectRuntimeComponent>();
                RogueEffectRuntime runtime = runtimeComponent.AddEffectRuntime(optionId, effectGroupId, entry, appliedBuffId);
                runtime.EffectBuffConfigId = effectBuffConfigId;
            }
        }

        private static void ExecuteCompatGroupExtraEntries(
            Unit unit,
            RogueProgressComponent progress,
            int optionId,
            int effectGroupId,
            RogueEffectGroupConfig groupConfig)
        {
            if (unit == null || unit.IsDisposed || progress == null || groupConfig?.Entries == null)
            {
                return;
            }

            foreach (RogueEffectEntryConfig entry in groupConfig.Entries)
            {
                if (entry == null || entry.ExecuteType == RogueEffectExecuteType.AddBuff)
                {
                    continue;
                }

                switch (entry.ExecuteType)
                {
                    case RogueEffectExecuteType.RegisterObjective:
                    {
                        RogueObjectiveComponent objectiveComponent = unit.GetComponent<RogueObjectiveComponent>() ?? unit.AddComponent<RogueObjectiveComponent>();
                        objectiveComponent.AddObjective(optionId, effectGroupId, entry.RefId, entry.Value1 > 0 ? entry.Value1 : 1);
                        break;
                    }
                    case RogueEffectExecuteType.AddGold:
                    {
                        int finalDelta = RogueGoldHelper.GetGoldDeltaBySource(progress, entry.Value1, RogueGoldSourceType.RogueCard);
                        if (finalDelta != 0)
                        {
                            RogueGoldHelper.AddGold(progress, finalDelta);
                        }

                        break;
                    }
                    case RogueEffectExecuteType.ReplaceAllCards:
                    {
                        ClearSelectedOptions(unit, progress);
                        break;
                    }
                    case RogueEffectExecuteType.GrantRandomCard:
                    {
                        GrantRandomCardsByEntry(unit, progress, entry);
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
            }
        }

        private static void GrantRandomCardsByEntry(Unit unit, RogueProgressComponent progress, RogueEffectEntryConfig entry)
        {
            if (unit == null || unit.IsDisposed || progress == null || entry == null)
            {
                return;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                return;
            }

            int grantCount = entry.Value2 > 0 ? entry.Value2 : 1;
            HashSet<int> excludedOptionIds = new();
            for (int i = 0; i < grantCount; ++i)
            {
                int grantOptionId = entry.RefId;
                if (grantOptionId <= 0 &&
                    !RogueOptionRollHelper.TryRollOneOption(unit, configCategory, entry.Value1, excludedOptionIds, out grantOptionId))
                {
                    continue;
                }

                if (grantOptionId <= 0 ||
                    !configCategory.TryGetOption(grantOptionId, out RogueOptionConfig grantOptionConfig) ||
                    grantOptionConfig == null)
                {
                    continue;
                }

                ApplySelectedOption(unit, progress, grantOptionConfig, grantOptionId, null);
                excludedOptionIds.Add(grantOptionId);
            }
        }

        private static bool TryGetFirstAddBuffConfigId(RogueEffectGroupConfig groupConfig, out int buffConfigId)
        {
            buffConfigId = 0;
            if (groupConfig?.Entries == null)
            {
                return false;
            }

            foreach (RogueEffectEntryConfig entry in groupConfig.Entries)
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

        private static bool ShouldRecordRuntime(int executeType)
        {
            return executeType == RogueEffectExecuteType.AddBuff ||
                    executeType == RogueEffectExecuteType.AddKillGold ||
                    executeType == RogueEffectExecuteType.OnKillHeal ||
                    executeType == RogueEffectExecuteType.OnKillStackAttack ||
                    executeType == RogueEffectExecuteType.GoldDamageBonus ||
                    executeType == RogueEffectExecuteType.LowHpDamageBonus ||
                    executeType == RogueEffectExecuteType.DamageReduction ||
                    executeType == RogueEffectExecuteType.LifeSteal ||
                    executeType == RogueEffectExecuteType.InstantKillChance ||
                    executeType == RogueEffectExecuteType.AreaDiscoveryGold ||
                    executeType == RogueEffectExecuteType.ProbabilityMultiplier ||
                    executeType == RogueEffectExecuteType.SkillDisable ||
                    executeType == RogueEffectExecuteType.WeaponModifier ||
                    executeType == RogueEffectExecuteType.ScaleModifier ||
                    executeType == RogueEffectExecuteType.PeriodicHpScale ||
                    executeType == RogueEffectExecuteType.ExtendGameTime;
        }

        private static bool ShouldKeepBuff(BuffConfig buffConfig)
        {
            if (buffConfig == null)
            {
                return false;
            }

            if (buffConfig.TickTime > 0 || (buffConfig.Duration >= 0 && buffConfig.Duration < int.MaxValue))
            {
                return true;
            }

            foreach (EffectNode effect in buffConfig.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                if (effect is EffectServerBuffAdd addEffect)
                {
                    if (ContainsOnlyInstantAddActions(addEffect))
                    {
                        continue;
                    }

                    return true;
                }

                if (effect is EffectRogueReplaceAllCards ||
                    effect is EffectRogueTemporaryKey)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool ContainsOnlyInstantAddActions(EffectServerBuffAdd addEffect)
        {
            if (addEffect?.Children == null || addEffect.Children.Count == 0)
            {
                return true;
            }

            foreach (BTNode node in addEffect.Children)
            {
                if (node is BTRogueAddGold ||
                    node is BTRogueGrantRandomCard ||
                    node is BTRogueGrantItem ||
                    node is BTRogueReplaceAllCards ||
                    node is BTRogueHealMaxHpPermille)
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
