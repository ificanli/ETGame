using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueProgressHelper
    {
        public static RogueProgressComponent EnsureProgress(Unit unit, bool syncToClient)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return null;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                progress = unit.AddComponent<RogueProgressComponent>();
            }

            if (syncToClient)
            {
                SendExpSync(unit, progress);
            }

            return progress;
        }

        public static void AddKillExp(Unit killer, Unit target)
        {
            if (killer == null || killer.IsDisposed || killer.UnitType != UnitType.Player)
            {
                return;
            }

            if (target == null || target.IsDisposed)
            {
                return;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Warning("[Rogue] AddKillExp failed: config category is null.");
                return;
            }

            int exp = configCategory.GetKillExp(target);
            if (exp <= 0)
            {
                return;
            }

            AddExp(killer, exp);
        }

        public static void AddExp(Unit unit, int addExp)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || addExp <= 0)
            {
                return;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Warning("[Rogue] AddExp failed: config category is null.");
                return;
            }

            RogueProgressComponent progress = EnsureProgress(unit, false);
            if (progress == null)
            {
                return;
            }

            progress.CurrentExp += addExp;

            while (progress.CurrentExp >= progress.NeedExp)
            {
                int oldLevel = progress.Level;
                int nextLevel = oldLevel + 1;

                if (!configCategory.TryGetLevel(nextLevel, out RogueLevelConfig nextLevelConfig))
                {
                    progress.CurrentExp = progress.NeedExp;
                    break;
                }

                progress.CurrentExp -= progress.NeedExp;
                progress.Level = nextLevel;
                progress.NeedExp = RogueProgressComponentSystem.NormalizeNeedExp(nextLevelConfig.NeedExp);

                List<RogueNumericDelta> gainedNumerics = ApplyLevelNumerics(unit, progress, nextLevelConfig);
                SendLevelUp(unit, oldLevel, nextLevel, gainedNumerics);

                if (nextLevelConfig.TriggerChoice)
                {
                    if (progress.ChoicePending)
                    {
                        progress.PendingChoiceLevels.Add(nextLevel);
                    }
                    else
                    {
                        TryOpenChoice(unit, progress);
                    }
                }
            }

            SendExpSync(unit, progress);
        }

        public static async ETTask<int> ChooseOption(Unit unit, long choiceSerial, int optionId, Action<int, long> onBuffApplied)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return ErrorCode.ERR_Cancel;
            }

            EntityRef<Unit> unitRef = unit;
            using (await unit.Root().CoroutineLockComponent.Wait(CoroutineLockType.RogueChoice, unit.Id))
            {
                unit = unitRef;
                if (unit == null || unit.IsDisposed)
                {
                    return ErrorCode.ERR_Cancel;
                }

                RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
                if (progress == null || !progress.ChoicePending)
                {
                    return ErrorCode.ERR_RogueChoiceNotPending;
                }

                if (progress.ChoiceSerial != choiceSerial)
                {
                    return ErrorCode.ERR_RogueChoiceSerialMismatch;
                }

                bool containsOption = progress.PendingOptionIds.Contains(optionId);
                if (!containsOption)
                {
                    return ErrorCode.ERR_RogueChoiceOptionInvalid;
                }

                RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
                if (configCategory == null)
                {
                    return ErrorCode.ERR_RogueConfigMissing;
                }

                if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig))
                {
                    return ErrorCode.ERR_RogueChoiceOptionInvalid;
                }

                if (optionConfig == null || !optionConfig.TryGetEffectBuffConfigId(out int effectBuffConfigId) || !BuffConfigCategory.Instance.Contain(effectBuffConfigId))
                {
                    return ErrorCode.ERR_RogueBuffConfigNotFound;
                }

                Buff buff = BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), effectBuffConfigId, null);
                if (buff != null)
                {
                    progress.AppliedBuffIds.Add(buff.Id);
                }

                progress.SelectedOptionIds.Add(optionId);
                onBuffApplied?.Invoke(effectBuffConfigId, buff?.Id ?? 0L);

                progress.ChoicePending = false;
                progress.PendingOptionIds.Clear();

                if (progress.PendingChoiceLevels.Count > 0)
                {
                    progress.PendingChoiceLevels.RemoveAt(0);
                    TryOpenChoice(unit, progress);
                }

                return ErrorCode.ERR_Success;
            }
        }

        private static List<RogueNumericDelta> ApplyLevelNumerics(Unit unit, RogueProgressComponent progress, RogueLevelConfig levelConfig)
        {
            List<RogueNumericDelta> gained = new List<RogueNumericDelta>();
            if (unit == null || progress == null || levelConfig == null || levelConfig.NumericDeltas == null || levelConfig.NumericDeltas.Count == 0)
            {
                return gained;
            }

            NumericComponent numeric = unit.NumericComponent;
            if (numeric == null)
            {
                return gained;
            }

            foreach (RogueNumericConfig numericConfig in levelConfig.NumericDeltas)
            {
                if (numericConfig == null || numericConfig.NumericType <= 0 || numericConfig.Value == 0)
                {
                    continue;
                }

                try
                {
                    long oldValue = numeric.GetAsLong(numericConfig.NumericType);
                    numeric.Set(numericConfig.NumericType, oldValue + numericConfig.Value);

                    if (progress.AppliedLevelNumericTotals.TryGetValue(numericConfig.NumericType, out long totalValue))
                    {
                        progress.AppliedLevelNumericTotals[numericConfig.NumericType] = totalValue + numericConfig.Value;
                    }
                    else
                    {
                        progress.AppliedLevelNumericTotals[numericConfig.NumericType] = numericConfig.Value;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[Rogue] apply numeric failed, unit={unit.Id}, numericType={numericConfig.NumericType}, value={numericConfig.Value}, error={e.Message}");
                    continue;
                }

                RogueNumericDelta delta = RogueNumericDelta.Create();
                delta.NumericType = numericConfig.NumericType;
                delta.Value = numericConfig.Value;
                gained.Add(delta);
            }

            return gained;
        }

        private static void SendExpSync(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return;
            }

            M2C_RogueExpSync msg = M2C_RogueExpSync.Create();
            msg.Level = progress.Level;
            msg.CurrentExp = progress.CurrentExp;
            msg.NeedExp = progress.NeedExp;
            MapMessageHelper.NoticeClient(unit, msg, NoticeType.Self);
        }

        private static void SendLevelUp(Unit unit, int oldLevel, int newLevel, List<RogueNumericDelta> gainedNumerics)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            M2C_RogueLevelUp msg = M2C_RogueLevelUp.Create();
            msg.OldLevel = oldLevel;
            msg.NewLevel = newLevel;
            if (gainedNumerics != null)
            {
                foreach (RogueNumericDelta delta in gainedNumerics)
                {
                    msg.GainedNumerics.Add(delta);
                }
            }

            MapMessageHelper.NoticeClient(unit, msg, NoticeType.Self);
        }

        private static bool TryOpenChoice(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return false;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                return false;
            }

            List<int> optionIds = RollOptions(configCategory);
            if (optionIds.Count == 0)
            {
                Log.Warning($"[Rogue] open choice failed: no valid options, unit={unit.Id}");
                return false;
            }

            progress.ChoiceSerial += 1;
            progress.ChoicePending = true;
            progress.PendingOptionIds.Clear();
            progress.PendingOptionIds.AddRange(optionIds);

            M2C_RogueChoicePopup popup = M2C_RogueChoicePopup.Create();
            popup.ChoiceSerial = progress.ChoiceSerial;
            foreach (int optionId in optionIds)
            {
                if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) || optionConfig == null)
                {
                    continue;
                }

                RogueOptionData optionData = RogueOptionData.Create();
                optionData.OptionId = optionId;
                optionData.BuffConfigId = optionConfig.TryGetEffectBuffConfigId(out int effectBuffConfigId) ? effectBuffConfigId : optionConfig.BuffConfigId;
                optionData.NameTextId = optionConfig.NameTextId;
                optionData.DescTextId = optionConfig.DescTextId;
                optionData.Icon = optionConfig.GetImagePath();
                popup.Options.Add(optionData);
            }

            if (popup.Options.Count == 0)
            {
                progress.ChoicePending = false;
                progress.PendingOptionIds.Clear();
                Log.Warning($"[Rogue] open choice failed: popup options empty, unit={unit.Id}");
                return false;
            }

            MapMessageHelper.NoticeClient(unit, popup, NoticeType.Self);
            Log.Info($"[Rogue] choice popup sent unit={unit.Id}, serial={popup.ChoiceSerial}, options={popup.Options.Count}");
            return true;
        }

        private static List<int> RollOptions(RogueRuntimeConfigCategory configCategory)
        {
            List<int> result = new List<int>();
            if (configCategory == null)
            {
                return result;
            }

            Dictionary<int, RogueOptionConfig> options = configCategory.GetOptions();
            if (options == null || options.Count == 0)
            {
                return result;
            }

            List<int> candidates = new List<int>();
            foreach (KeyValuePair<int, RogueOptionConfig> kv in options)
            {
                RogueOptionConfig optionConfig = kv.Value;
                if (optionConfig == null || !optionConfig.TryGetEffectBuffConfigId(out int effectBuffConfigId))
                {
                    continue;
                }

                if (!BuffConfigCategory.Instance.Contain(effectBuffConfigId))
                {
                    continue;
                }

                candidates.Add(kv.Key);
            }

            int needCount = configCategory.GetChoiceOptionCount();
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

        public static void ClearRogueRuntime(Unit unit, bool removeProgressComponent)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                return;
            }

            RevertAppliedLevelNumerics(unit, progress);
            RemoveAppliedRogueBuffs(unit, progress);

            progress.CurrentExp = 0;
            progress.ChoiceSerial = 0;
            progress.ChoicePending = false;
            progress.PendingOptionIds.Clear();
            progress.PendingChoiceLevels.Clear();
            progress.AppliedBuffIds.Clear();
            progress.SelectedOptionIds.Clear();
            progress.AppliedLevelNumericTotals.Clear();

            if (removeProgressComponent)
            {
                unit.RemoveComponent<RogueProgressComponent>();
            }
        }

        private static void RemoveAppliedRogueBuffs(Unit unit, RogueProgressComponent progress)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null || progress.AppliedBuffIds == null || progress.AppliedBuffIds.Count == 0)
            {
                return;
            }

            foreach (long buffId in progress.AppliedBuffIds.ToArray())
            {
                Buff buff = buffComponent.GetChild<Buff>(buffId);
                if (buff == null)
                {
                    continue;
                }

                buffComponent.RemoveBuff(buff);
            }
        }

        private static void RevertAppliedLevelNumerics(Unit unit, RogueProgressComponent progress)
        {
            NumericComponent numeric = unit.NumericComponent;
            if (numeric == null || progress.AppliedLevelNumericTotals == null || progress.AppliedLevelNumericTotals.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<int, long> kv in progress.AppliedLevelNumericTotals)
            {
                try
                {
                    long oldValue = numeric.GetAsLong(kv.Key);
                    numeric.Set(kv.Key, oldValue - kv.Value);
                }
                catch (Exception e)
                {
                    Log.Warning($"[Rogue] revert numeric failed, unit={unit.Id}, numericType={kv.Key}, value={kv.Value}, error={e.Message}");
                }
            }
        }
    }
}
