using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueProgressHelper
    {
        private const int InitialChoicePopupAfterEnterDelayMs = 300;

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
                SyncProgress(unit, progress);
            }

            return progress;
        }

        public static bool TryOpenInitialChoiceIfNeeded(Unit unit)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                Log.Info($"[RogueInit] TryOpenInitialChoiceIfNeeded skipped: invalid unit, unitId={unit?.Id ?? 0}, unitType={unit?.UnitType}");
                return false;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                Log.Info($"[RogueInit] TryOpenInitialChoiceIfNeeded skipped: progress missing, unit={unit.Id}");
                return false;
            }

            if (progress.ChoicePending || progress.PendingOptionIds.Count > 0 || progress.SelectedOptionIds.Count > 0)
            {
                Log.Info(
                    $"[RogueInit] TryOpenInitialChoiceIfNeeded skipped: existing choice state, unit={unit.Id}, choicePending={progress.ChoicePending}, pendingOptions={progress.PendingOptionIds.Count}, selectedOptions={progress.SelectedOptionIds.Count}, serial={progress.ChoiceSerial}");
                return false;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Warning($"[RogueInit] TryOpenInitialChoiceIfNeeded failed: config category null, unit={unit.Id}");
                return false;
            }

            if (!configCategory.TryGetLevel(progress.Level, out RogueLevelConfig levelConfig) || levelConfig == null || !levelConfig.TriggerChoice)
            {
                Log.Info($"[RogueInit] TryOpenInitialChoiceIfNeeded skipped: level no trigger, unit={unit.Id}, level={progress.Level}");
                return false;
            }

            bool opened = TryOpenChoice(unit, progress);
            if (opened)
            {
                Log.Info($"[Rogue] open initial choice, unit={unit.Id}, level={progress.Level}, serial={progress.ChoiceSerial}");
            }

            return opened;
        }

        public static async ETTask TryInitializeChoicePopupAfterEnter(Unit unit, int delayMs = InitialChoicePopupAfterEnterDelayMs)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || delayMs <= 0)
            {
                return;
            }

            Log.Info($"[RogueInit] schedule initial popup after enter unit={unit.Id}, delayMs={delayMs}");
            EntityRef<Unit> unitRef = unit;
            await unit.Root().TimerComponent.WaitAsync(delayMs);

            unit = unitRef;
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                Log.Info($"[RogueInit] initial popup after enter cancelled: unit invalid after wait, unitId={unit?.Id ?? 0}");
                return;
            }

            string mapName = unit.Scene()?.Name.GetSceneConfigName();
            if (mapName == "Home" || mapName == "GateMap")
            {
                Log.Info($"[RogueInit] initial popup after enter skipped by map, unit={unit.Id}, map={mapName}");
                return;
            }

            if (TryOpenInitialChoiceIfNeeded(unit))
            {
                Log.Info($"[RogueInit] initial popup after enter opened choice, unit={unit.Id}");
                return;
            }

            bool resent = TryResendPendingChoicePopup(unit);
            Log.Info($"[RogueInit] initial popup after enter resend result unit={unit.Id}, resent={resent}");
        }

        public static async ETTask TryEnsureChoicePopupVisibleLater(Unit unit, int delayMs = 2500)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || delayMs <= 0)
            {
                return;
            }

            Log.Info($"[RogueInit] schedule delayed popup check unit={unit.Id}, delayMs={delayMs}");
            EntityRef<Unit> unitRef = unit;
            await unit.Root().TimerComponent.WaitAsync(delayMs);

            unit = unitRef;
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                Log.Info($"[RogueInit] delayed popup check cancelled: unit invalid after wait, unitId={unit?.Id ?? 0}");
                return;
            }

            string mapName = unit.Scene()?.Name.GetSceneConfigName();
            if (mapName == "Home" || mapName == "GateMap")
            {
                Log.Info($"[RogueInit] delayed popup check skipped by map, unit={unit.Id}, map={mapName}");
                return;
            }

            if (TryOpenInitialChoiceIfNeeded(unit))
            {
                Log.Info($"[RogueInit] delayed popup check reopened initial choice, unit={unit.Id}");
                return;
            }

            bool resent = TryResendPendingChoicePopup(unit);
            Log.Info($"[RogueInit] delayed popup check resend result unit={unit.Id}, resent={resent}");
        }

        public static bool TryResendPendingChoicePopup(Unit unit)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return false;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null || !progress.ChoicePending || progress.ChoiceSerial <= 0 || progress.PendingOptionIds.Count == 0)
            {
                Log.Info(
                    $"[RogueInit] TryResendPendingChoicePopup skipped: no pending choice, unit={unit?.Id ?? 0}, hasProgress={progress != null}, choicePending={progress?.ChoicePending ?? false}, serial={progress?.ChoiceSerial ?? 0}, pendingOptions={progress?.PendingOptionIds.Count ?? 0}");
                return false;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Warning($"[RogueInit] TryResendPendingChoicePopup failed: config category null, unit={unit.Id}");
                return false;
            }

            bool sent = TrySendChoicePopup(unit, progress, configCategory, false);
            if (sent)
            {
                Log.Info($"[Rogue] resend pending choice popup unit={unit.Id}, serial={progress.ChoiceSerial}, options={progress.PendingOptionIds.Count}");
            }

            return sent;
        }

        public static void AddKillExp(Unit killer, Unit target)
        {
            if (killer == null || killer.IsDisposed || killer.UnitType != UnitType.Player)
            {
                Log.Info($"[RogueExp] skip kill exp: invalid killer, killerId={killer?.Id ?? 0}, killerType={killer?.UnitType}");
                return;
            }

            if (target == null || target.IsDisposed)
            {
                Log.Info($"[RogueExp] skip kill exp: target invalid, killerId={killer.Id}");
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
                Log.Info($"[RogueExp] skip kill exp: config returned non-positive exp, killerId={killer.Id}, targetId={target.Id}, targetType={target.UnitType}, exp={exp}");
                return;
            }

            Log.Info($"[RogueExp] kill exp resolved, killerId={killer.Id}, targetId={target.Id}, targetType={target.UnitType}, exp={exp}");
            AddExp(killer, exp);
        }

        public static void AddKillExp(Unit killer, int targetUnitType)
        {
            if (killer == null || killer.IsDisposed || killer.UnitType != UnitType.Player)
            {
                Log.Info($"[RogueExp] skip kill exp by type: invalid killer, killerId={killer?.Id ?? 0}, killerType={killer?.UnitType}, targetUnitType={targetUnitType}");
                return;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Warning("[Rogue] AddKillExp failed: config category is null.");
                return;
            }

            int exp = configCategory.GetKillExp(targetUnitType);
            if (exp <= 0)
            {
                Log.Info($"[RogueExp] skip kill exp by type: non-positive exp, killerId={killer.Id}, targetUnitType={targetUnitType}, exp={exp}");
                return;
            }

            Log.Info($"[RogueExp] kill exp resolved by type, killerId={killer.Id}, targetUnitType={targetUnitType}, exp={exp}");
            AddExp(killer, exp);
        }

        public static void AddExp(Unit unit, int addExp)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || addExp <= 0)
            {
                Log.Info($"[RogueExp] skip add exp: unitId={unit?.Id ?? 0}, unitType={unit?.UnitType}, addExp={addExp}");
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
                Log.Info($"[RogueExp] skip add exp: progress missing, unitId={unit.Id}, addExp={addExp}");
                return;
            }

            int oldLevelSnapshot = progress.Level;
            int oldExpSnapshot = progress.CurrentExp;
            int oldNeedExpSnapshot = progress.NeedExp;
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

            Log.Info($"[RogueExp] add exp applied, unitId={unit.Id}, addExp={addExp}, level={oldLevelSnapshot}->{progress.Level}, exp={oldExpSnapshot}->{progress.CurrentExp}, needExp={oldNeedExpSnapshot}->{progress.NeedExp}");
            SyncProgress(unit, progress);
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

                int applyError = RogueEffectHelper.ApplySelectedOption(unit, progress, optionConfig, optionId, onBuffApplied);
                if (applyError != ErrorCode.ERR_Success)
                {
                    return applyError;
                }

                progress.ChoicePending = false;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionRerollCounts.Clear();

                if (progress.PendingChoiceLevels.Count > 0)
                {
                    progress.PendingChoiceLevels.RemoveAt(0);
                    TryOpenChoice(unit, progress);
                }

                SyncProgress(unit, progress);

                return ErrorCode.ERR_Success;
            }
        }

        public static async ETTask<int> RerollOption(
            Unit unit,
            long choiceSerial,
            int optionIndex,
            int currentOptionId,
            Action<int, RogueOptionData> onOptionRerolled)
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

                EnsurePendingOptionRerollCounts(progress);
                if (optionIndex < 0 || optionIndex >= progress.PendingOptionIds.Count || currentOptionId <= 0)
                {
                    return ErrorCode.ERR_RogueChoiceOptionInvalid;
                }

                int oldOptionId = progress.PendingOptionIds[optionIndex];
                if (oldOptionId != currentOptionId)
                {
                    return ErrorCode.ERR_RogueChoiceOptionInvalid;
                }

                int remainingRerollCount = progress.PendingOptionRerollCounts[optionIndex];
                if (remainingRerollCount <= 0)
                {
                    Log.Info(
                        $"[Rogue] reroll option blocked: no reroll count, unit={unit.Id}, serial={choiceSerial}, index={optionIndex}, option={oldOptionId}");
                    return ErrorCode.ERR_RogueChoiceRerollExhausted;
                }

                RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
                if (configCategory == null)
                {
                    return ErrorCode.ERR_RogueConfigMissing;
                }

                if (!configCategory.TryGetOption(oldOptionId, out RogueOptionConfig oldOptionConfig) || oldOptionConfig == null)
                {
                    return ErrorCode.ERR_RogueChoiceOptionInvalid;
                }

                HashSet<int> excludedOptionIds = new(progress.PendingOptionIds);
                int rerolledOptionId = 0;
                bool rerolled = RogueOptionRollHelper.TryRollOneOption(unit, configCategory, oldOptionConfig.Quality, excludedOptionIds, false, out rerolledOptionId);
                if (!rerolled)
                {
                    rerolled = RogueOptionRollHelper.TryRollOneOption(unit, configCategory, oldOptionConfig.Quality, excludedOptionIds, true, out rerolledOptionId);
                }

                if (!rerolled || rerolledOptionId <= 0)
                {
                    Log.Warning(
                        $"[Rogue] reroll option failed: unit={unit.Id}, serial={choiceSerial}, index={optionIndex}, oldOption={oldOptionId}, quality={oldOptionConfig.Quality}");
                    return ErrorCode.ERR_RogueChoiceRerollFailed;
                }

                remainingRerollCount -= 1;
                if (!TryBuildChoiceOptionData(configCategory, rerolledOptionId, remainingRerollCount, out RogueOptionData optionData))
                {
                    return ErrorCode.ERR_RogueChoiceRerollFailed;
                }

                progress.PendingOptionIds[optionIndex] = rerolledOptionId;
                progress.PendingOptionRerollCounts[optionIndex] = remainingRerollCount;
                onOptionRerolled?.Invoke(oldOptionId, optionData);
                Log.Info(
                    $"[Rogue] reroll option success: unit={unit.Id}, serial={choiceSerial}, index={optionIndex}, oldOption={oldOptionId}, newOption={rerolledOptionId}, quality={oldOptionConfig.Quality}, rerollCount={remainingRerollCount}");
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

        public static void SyncProgress(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                return;
            }

            SyncProgress(unit, progress);
        }

        public static void SyncProgress(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return;
            }

            M2C_RogueExpSync msg = M2C_RogueExpSync.Create();
            msg.Level = progress.Level;
            msg.CurrentExp = progress.CurrentExp;
            msg.NeedExp = progress.NeedExp;
            msg.CurrentGold = progress.CurrentGold;
            MapMessageHelper.NoticeClient(unit, msg, NoticeType.Self);
            Log.Info($"[RogueExp] sync exp to client, unitId={unit.Id}, level={progress.Level}, exp={progress.CurrentExp}/{progress.NeedExp}, gold={progress.CurrentGold}");
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

            int choiceQuality = RogueChoiceQualityHelper.GetChoiceQuality(unit, progress.SelectedOptionIds.Count);
            if (choiceQuality <= 0)
            {
                Log.Warning($"[Rogue] open choice failed: invalid choice quality, unit={unit.Id}, choiceIndex={progress.SelectedOptionIds.Count}");
                return false;
            }

            int choiceIndex = progress.SelectedOptionIds.Count;
            int needCount = configCategory.GetChoiceOptionCount();
            int resolvedQuality = choiceQuality;
            bool resolvedQualityAssigned = false;
            List<int> optionIds = new List<int>(needCount);
            HashSet<int> selectedOptionIds = new HashSet<int>();

            int appendedCount = TryAppendRolledOptions(optionIds, selectedOptionIds, unit, configCategory, choiceQuality, false, needCount);
            if (appendedCount > 0)
            {
                resolvedQuality = choiceQuality;
                resolvedQualityAssigned = true;
            }

            if (optionIds.Count < needCount)
            {
                for (int quality = RogueChoiceQualityHelper.MaxQuality; quality >= RogueChoiceQualityHelper.MinQuality; --quality)
                {
                    if (quality == choiceQuality)
                    {
                        continue;
                    }

                    appendedCount = TryAppendRolledOptions(optionIds, selectedOptionIds, unit, configCategory, quality, false, needCount);
                    if (appendedCount == 0)
                    {
                        continue;
                    }

                    if (!resolvedQualityAssigned)
                    {
                        resolvedQuality = quality;
                        resolvedQualityAssigned = true;
                    }

                    Log.Warning(
                        $"[Rogue] open choice fallback quality fill, unit={unit.Id}, prefer={choiceQuality}, fallback={quality}, appended={appendedCount}, total={optionIds.Count}, choiceIndex={choiceIndex}");

                    if (optionIds.Count >= needCount)
                    {
                        break;
                    }
                }
            }

            if (optionIds.Count < needCount)
            {
                appendedCount = TryAppendRolledOptions(optionIds, selectedOptionIds, unit, configCategory, choiceQuality, true, needCount);
                if (appendedCount > 0)
                {
                    if (!resolvedQualityAssigned)
                    {
                        resolvedQuality = choiceQuality;
                        resolvedQualityAssigned = true;
                    }

                    Log.Warning(
                        $"[Rogue] open choice fallback ignore tags fill, unit={unit.Id}, prefer={choiceQuality}, fallback={choiceQuality}, appended={appendedCount}, total={optionIds.Count}, choiceIndex={choiceIndex}");
                }

                if (optionIds.Count < needCount)
                {
                    for (int quality = RogueChoiceQualityHelper.MaxQuality; quality >= RogueChoiceQualityHelper.MinQuality; --quality)
                    {
                        if (quality == choiceQuality)
                        {
                            continue;
                        }

                        appendedCount = TryAppendRolledOptions(optionIds, selectedOptionIds, unit, configCategory, quality, true, needCount);
                        if (appendedCount == 0)
                        {
                            continue;
                        }

                        if (!resolvedQualityAssigned)
                        {
                            resolvedQuality = quality;
                            resolvedQualityAssigned = true;
                        }

                        Log.Warning(
                            $"[Rogue] open choice fallback ignore tags fill, unit={unit.Id}, prefer={choiceQuality}, fallback={quality}, appended={appendedCount}, total={optionIds.Count}, choiceIndex={choiceIndex}");

                        if (optionIds.Count >= needCount)
                        {
                            break;
                        }
                    }
                }
            }

            if (optionIds.Count == 0)
            {
                Log.Warning($"[Rogue] open choice failed: no valid options, unit={unit.Id}, quality={choiceQuality}");
                return false;
            }

            if (optionIds.Count < needCount)
            {
                Log.Warning(
                    $"[Rogue] open choice insufficient options, unit={unit.Id}, prefer={choiceQuality}, resolved={resolvedQuality}, count={optionIds.Count}, need={needCount}, choiceIndex={choiceIndex}");
            }

            if (resolvedQuality != choiceQuality)
            {
                RogueChoiceQualityComponent qualityComponent = unit.GetComponent<RogueChoiceQualityComponent>();
                if (qualityComponent != null &&
                    choiceIndex >= 0 &&
                    choiceIndex < qualityComponent.ChoiceQualities.Count)
                {
                    qualityComponent.ChoiceQualities[choiceIndex] = resolvedQuality;
                }
            }

            progress.ChoiceSerial += 1;
            progress.ChoicePending = true;
            progress.PendingOptionIds.Clear();
            progress.PendingOptionIds.AddRange(optionIds);
            progress.PendingOptionRerollCounts.Clear();
            int defaultRerollCount = configCategory.GetChoiceOptionRerollCount();
            for (int i = 0; i < progress.PendingOptionIds.Count; ++i)
            {
                progress.PendingOptionRerollCounts.Add(defaultRerollCount);
            }

            bool popupSent = TrySendChoicePopup(unit, progress, configCategory, true);
            if (popupSent)
            {
                Log.Info($"[Rogue] choice popup sent unit={unit.Id}, serial={progress.ChoiceSerial}, options={progress.PendingOptionIds.Count}");
            }
            else
            {
                Log.Warning(
                    $"[RogueInit] choice popup send failed after roll, unit={unit.Id}, serial={progress.ChoiceSerial}, options=[{string.Join(",", progress.PendingOptionIds)}], quality={resolvedQuality}");
            }

            return popupSent;
        }

        private static int TryAppendRolledOptions(
            List<int> optionIds,
            HashSet<int> selectedOptionIds,
            Unit unit,
            RogueRuntimeConfigCategory configCategory,
            int requiredQuality,
            bool ignoreTagFilter,
            int needCount)
        {
            if (optionIds == null ||
                selectedOptionIds == null ||
                unit == null ||
                unit.IsDisposed ||
                configCategory == null ||
                needCount <= optionIds.Count)
            {
                return 0;
            }

            List<int> rolledOptionIds =
                    RogueOptionRollHelper.RollOptions(unit, configCategory, requiredQuality, selectedOptionIds, ignoreTagFilter, needCount - optionIds.Count);
            if (rolledOptionIds.Count == 0)
            {
                return 0;
            }

            int addedCount = 0;
            foreach (int optionId in rolledOptionIds)
            {
                if (optionId <= 0 || !selectedOptionIds.Add(optionId))
                {
                    continue;
                }

                optionIds.Add(optionId);
                ++addedCount;
                if (optionIds.Count >= needCount)
                {
                    break;
                }
            }

            return addedCount;
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
            RogueEffectHelper.RemoveRunEndEffectGroups(unit, progress);
            RogueEffectHelper.ClearAppliedEffects(unit, progress);
            RogueTagEffectHelper.ClearAppliedTagBuffs(unit, progress);

            RogueTemporaryItemStateComponent temporaryItemState = unit.GetComponent<RogueTemporaryItemStateComponent>();
            if (temporaryItemState != null)
            {
                temporaryItemState.ClearAll(unit);
                unit.RemoveComponent<RogueTemporaryItemStateComponent>();
            }

            RogueSummonedSpiritStateComponent summonedSpiritState = unit.GetComponent<RogueSummonedSpiritStateComponent>();
            if (summonedSpiritState != null)
            {
                summonedSpiritState.ClearAll(unit);
                unit.RemoveComponent<RogueSummonedSpiritStateComponent>();
            }

            progress.CurrentExp = 0;
            progress.CurrentGold = 0;
            progress.ChoiceSerial = 0;
            progress.ChoicePending = false;
            progress.PendingOptionIds.Clear();
            progress.PendingOptionRerollCounts.Clear();
            progress.PendingChoiceLevels.Clear();
            progress.AppliedBuffIds.Clear();
            progress.SelectedOptionIds.Clear();
            progress.AppliedOptionBuffIds.Clear();
            progress.ClaimedPointRewardIds.Clear();
            progress.AppliedLevelNumericTotals.Clear();
            progress.CommonShowTagCounts.Clear();
            progress.AppliedShowTagBuffIds.Clear();

            if (removeProgressComponent)
            {
                unit.RemoveComponent<RogueProgressComponent>();
                unit.RemoveComponent<RogueChoiceQualityComponent>();
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

        private static bool TrySendChoicePopup(Unit unit, RogueProgressComponent progress, RogueRuntimeConfigCategory configCategory, bool clearPendingWhenEmpty)
        {
            if (unit == null || unit.IsDisposed || progress == null || configCategory == null)
            {
                return false;
            }

            EnsurePendingOptionRerollCounts(progress);
            M2C_RogueChoicePopup popup = M2C_RogueChoicePopup.Create();
            popup.ChoiceSerial = progress.ChoiceSerial;
            for (int i = 0; i < progress.PendingOptionIds.Count; ++i)
            {
                int optionId = progress.PendingOptionIds[i];
                int rerollCount = progress.PendingOptionRerollCounts[i];
                if (!TryBuildChoiceOptionData(configCategory, optionId, rerollCount, out RogueOptionData optionData))
                {
                    continue;
                }

                popup.Options.Add(optionData);
            }

            if (popup.Options.Count == 0)
            {
                if (clearPendingWhenEmpty)
                {
                    progress.ChoicePending = false;
                    progress.PendingOptionIds.Clear();
                    progress.PendingOptionRerollCounts.Clear();
                }

                Log.Warning($"[Rogue] send choice popup failed: popup options empty, unit={unit.Id}, serial={progress.ChoiceSerial}, clearPending={clearPendingWhenEmpty}");
                return false;
            }

            Log.Info(
                $"[RogueInit] send choice popup to client unit={unit.Id}, serial={popup.ChoiceSerial}, optionIds=[{string.Join(",", progress.PendingOptionIds)}], clearPendingWhenEmpty={clearPendingWhenEmpty}");
            MapMessageHelper.NoticeClient(unit, popup, NoticeType.Self);
            return true;
        }

        private static bool TryBuildChoiceOptionData(
            RogueRuntimeConfigCategory configCategory,
            int optionId,
            int rerollCount,
            out RogueOptionData optionData)
        {
            optionData = null;
            if (configCategory == null ||
                optionId <= 0 ||
                !configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) ||
                optionConfig == null)
            {
                return false;
            }

            optionData = RogueOptionData.Create();
            optionData.OptionId = optionId;
            optionData.BuffConfigId = RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, optionConfig, out int effectBuffConfigId)
                ? effectBuffConfigId
                : optionConfig.BuffConfigId;
            optionData.NameTextId = optionConfig.NameTextId;
            optionData.DescTextId = optionConfig.DescTextId;
            optionData.Icon = optionConfig.GetImagePath();
            optionData.RerollCount = rerollCount;
            return true;
        }

        private static void EnsurePendingOptionRerollCounts(RogueProgressComponent progress)
        {
            if (progress == null)
            {
                return;
            }

            int defaultRerollCount = GetDefaultOptionRerollCount();
            progress.PendingOptionRerollCounts ??= new List<int>();
            int optionCount = progress.PendingOptionIds.Count;
            if (progress.PendingOptionRerollCounts.Count > optionCount)
            {
                progress.PendingOptionRerollCounts.RemoveRange(optionCount, progress.PendingOptionRerollCounts.Count - optionCount);
            }

            while (progress.PendingOptionRerollCounts.Count < optionCount)
            {
                progress.PendingOptionRerollCounts.Add(defaultRerollCount);
            }
        }

        private static int GetDefaultOptionRerollCount()
        {
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                return 1;
            }

            return configCategory.GetChoiceOptionRerollCount();
        }

    }
}
