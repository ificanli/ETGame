using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTRogueAddGoldHandler : ABTHandler<BTRogueAddGold>
    {
        protected override int Run(BTRogueAddGold node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            RogueProgressComponent progress = unit?.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                return 0;
            }

            int finalDelta = RogueGoldHelper.GetGoldDeltaBySource(progress, node.Amount, node.SourceType);
            if (finalDelta != 0)
            {
                RogueGoldHelper.AddGold(progress, finalDelta);
                if (node.SyncProgress)
                {
                    RogueProgressHelper.SyncProgress(unit, progress);
                }
            }

            return 0;
        }
    }

    public class BTRogueGrantRandomCardHandler : ABTHandler<BTRogueGrantRandomCard>
    {
        protected override int Run(BTRogueGrantRandomCard node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            RogueProgressComponent progress = unit?.GetComponent<RogueProgressComponent>();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (unit == null || unit.IsDisposed || progress == null || configCategory == null)
            {
                return 0;
            }

            int sourceOptionId = buff?.ConfigId ?? 0;
            HashSet<int> excludedOptionIds = new();
            if (sourceOptionId > 0)
            {
                excludedOptionIds.Add(sourceOptionId);
            }

            int grantCount = node.Count > 0 ? node.Count : 1;
            for (int i = 0; i < grantCount; ++i)
            {
                int optionId = node.FixedOptionId;
                if (optionId <= 0 &&
                    !RogueOptionRollHelper.TryRollOneOption(unit, configCategory, node.Quality, excludedOptionIds, out optionId))
                {
                    continue;
                }

                if (optionId <= 0 ||
                    !configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) ||
                    optionConfig == null)
                {
                    continue;
                }

                RogueEffectHelper.ApplySelectedOption(unit, progress, optionConfig, optionId, null);
                excludedOptionIds.Add(optionId);
            }

            return 0;
        }
    }

    public class BTRogueGrantItemHandler : ABTHandler<BTRogueGrantItem>
    {
        protected override int Run(BTRogueGrantItem node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            if (unit == null || unit.IsDisposed || node.ItemConfigId <= 0 || node.Count <= 0)
            {
                return 0;
            }

            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                itemComponent = unit.AddComponent<ItemComponent>();
            }

            try
            {
                ItemHelper.AddItem(itemComponent, node.ItemConfigId, node.Count, ItemChangeReason.QuestReward);
            }
            catch
            {
                return 0;
            }

            return 0;
        }
    }

    public class BTRogueRadarScanHandler : ABTHandler<BTRogueRadarScan>
    {
        protected override int Run(BTRogueRadarScan node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 0;
            }

            EffectRogueContainerRadar effect = buff.GetConfig().GetEffect<EffectRogueContainerRadar>();
            if (effect == null || effect.Radius <= 0)
            {
                return 0;
            }

            Scene scene = unit.Scene();
            ECAManagerComponent manager = scene?.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                return 0;
            }

            float radiusSqr = effect.Radius * effect.Radius;
            foreach (ECAPointComponent point in manager.GetAllECAPoints())
            {
                if (point == null || point.IsDisposed || point.PointType != ECAPointType.Container)
                {
                    continue;
                }

                Unit pointUnit = point.GetParent<Unit>();
                if (pointUnit == null || pointUnit.IsDisposed)
                {
                    continue;
                }

                float2 offset = pointUnit.Position.xz - unit.Position.xz;
                if (math.lengthsq(offset) > radiusSqr)
                {
                    continue;
                }

                ContainerRuntimeHelper.SendPointState(point, unit);
            }

            return 0;
        }
    }

    public class BTRogueReplaceAllCardsHandler : ABTHandler<BTRogueReplaceAllCards>
    {
        protected override int Run(BTRogueReplaceAllCards node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            RogueProgressComponent progress = unit?.GetComponent<RogueProgressComponent>();
            if (unit == null || unit.IsDisposed || progress == null)
            {
                return 0;
            }

            RogueEffectHelper.ClearSelectedOptions(unit, progress);
            return 0;
        }
    }

    public class BTRogueRegisterObjectiveHandler : ABTHandler<BTRogueRegisterObjective>
    {
        protected override int Run(BTRogueRegisterObjective node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 0;
            }

            RogueObjectiveComponent objectiveComponent = unit.GetComponent<RogueObjectiveComponent>();
            if (objectiveComponent == null)
            {
                objectiveComponent = unit.AddComponent<RogueObjectiveComponent>();
            }

            objectiveComponent.AddObjective(buff.ConfigId, buff.ConfigId, node.ObjectiveId, node.GoalValue);
            return 0;
        }
    }

    public class BTRogueUnregisterObjectiveHandler : ABTHandler<BTRogueUnregisterObjective>
    {
        protected override int Run(BTRogueUnregisterObjective node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            RogueObjectiveComponent objectiveComponent = unit?.GetComponent<RogueObjectiveComponent>();
            if (buff == null || buff.IsDisposed || objectiveComponent == null)
            {
                return 0;
            }

            objectiveComponent.RemoveOneObjectiveByOptionId(buff.ConfigId);
            if (objectiveComponent.ChildrenCount() == 0)
            {
                unit.RemoveComponent<RogueObjectiveComponent>();
            }

            return 0;
        }
    }

    public class BTRogueAddWeaponModifiersHandler : ABTHandler<BTRogueAddWeaponModifiers>
    {
        protected override int Run(BTRogueAddWeaponModifiers node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 0;
            }

            EffectRogueWeaponModifiers effect = buff.GetConfig().GetEffect<EffectRogueWeaponModifiers>();
            if (effect?.Entries == null || effect.Entries.Count == 0)
            {
                return 0;
            }

            RogueWeaponModifierComponent modifierComponent = unit.GetComponent<RogueWeaponModifierComponent>();
            if (modifierComponent == null)
            {
                modifierComponent = unit.AddComponent<RogueWeaponModifierComponent>();
            }

            foreach (RogueWeaponModifierEntry entry in effect.Entries)
            {
                if (entry == null || entry.ModType <= 0 || entry.ValuePermille == 0)
                {
                    continue;
                }

                modifierComponent.AddModifier(entry.ModType, entry.ValuePermille);
            }

            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);
            return 0;
        }
    }

    public class BTRogueRemoveWeaponModifiersHandler : ABTHandler<BTRogueRemoveWeaponModifiers>
    {
        protected override int Run(BTRogueRemoveWeaponModifiers node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            RogueWeaponModifierComponent modifierComponent = unit?.GetComponent<RogueWeaponModifierComponent>();
            if (buff == null || buff.IsDisposed || modifierComponent == null)
            {
                return 0;
            }

            EffectRogueWeaponModifiers effect = buff.GetConfig().GetEffect<EffectRogueWeaponModifiers>();
            if (effect?.Entries == null || effect.Entries.Count == 0)
            {
                return 0;
            }

            foreach (RogueWeaponModifierEntry entry in effect.Entries)
            {
                if (entry == null || entry.ModType <= 0 || entry.ValuePermille == 0)
                {
                    continue;
                }

                modifierComponent.RemoveModifier(entry.ModType, entry.ValuePermille);
            }

            if (modifierComponent.Modifiers.Count == 0)
            {
                unit.RemoveComponent<RogueWeaponModifierComponent>();
            }

            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);
            return 0;
        }
    }

    public class BTRogueApplyInteractRangeBonusHandler : ABTHandler<BTRogueApplyInteractRangeBonus>
    {
        protected override int Run(BTRogueApplyInteractRangeBonus node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 0;
            }

            EffectRogueInteractRangeBonus effect = buff.GetConfig().GetEffect<EffectRogueInteractRangeBonus>();
            if (effect == null || effect.Distance <= 0f)
            {
                return 0;
            }

            RogueInteractRangeBonusBuffStateComponent state = buff.GetBuffData().GetComponent<RogueInteractRangeBonusBuffStateComponent>() ??
                    buff.GetBuffData().AddComponent<RogueInteractRangeBonusBuffStateComponent>();
            if (state.AppliedBonusDistance > 0f)
            {
                return 0;
            }

            state.AppliedBonusDistance = effect.Distance;
            ECAInteractionModifierHelper.AddInteractRangeBonus(unit, state.AppliedBonusDistance);
            return 0;
        }
    }

    public class BTRogueRemoveInteractRangeBonusHandler : ABTHandler<BTRogueRemoveInteractRangeBonus>
    {
        protected override int Run(BTRogueRemoveInteractRangeBonus node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            RogueInteractRangeBonusBuffStateComponent state = buff?.GetBuffData().GetComponent<RogueInteractRangeBonusBuffStateComponent>();
            if (unit == null || unit.IsDisposed || state == null || state.AppliedBonusDistance <= 0f)
            {
                return 0;
            }

            ECAInteractionModifierHelper.RemoveInteractRangeBonus(unit, state.AppliedBonusDistance);
            state.AppliedBonusDistance = 0f;
            return 0;
        }
    }

    public class BTRogueUpdateBagSpaceHpBonusHandler : ABTHandler<BTRogueUpdateBagSpaceHpBonus>
    {
        protected override int Run(BTRogueUpdateBagSpaceHpBonus node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            if (buff == null || buff.IsDisposed || numeric == null)
            {
                return 0;
            }

            EffectRogueBagSpaceHpBonus effect = buff.GetConfig().GetEffect<EffectRogueBagSpaceHpBonus>();
            if (effect == null || effect.MaxHpPermilleAtFull <= 0)
            {
                return 0;
            }

            RogueBagSpaceHpBonusBuffStateComponent state = buff.GetBuffData().GetComponent<RogueBagSpaceHpBonusBuffStateComponent>() ??
                    buff.GetBuffData().AddComponent<RogueBagSpaceHpBonusBuffStateComponent>();

            long currentMaxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (currentMaxHp <= 0)
            {
                return 0;
            }

            long baseMaxHp = currentMaxHp - state.AppliedMaxHpDelta;
            if (baseMaxHp <= 0)
            {
                baseMaxHp = currentMaxHp;
            }

            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            int usedSlotCount = itemComponent?.GetUsedSlotCount() ?? 0;
            int capacity = itemComponent?.Capacity ?? 0;

            long targetDelta = 0;
            if (capacity > 0 && usedSlotCount > 0)
            {
                long fillPermille = (long)usedSlotCount * 1000 / capacity;
                if (fillPermille > 1000)
                {
                    fillPermille = 1000;
                }

                targetDelta = baseMaxHp * effect.MaxHpPermilleAtFull * fillPermille / 1000 / 1000;
            }

            long delta = targetDelta - state.AppliedMaxHpDelta;
            if (delta == 0)
            {
                return 0;
            }

            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd + delta);

            long currentHp = numeric.GetAsLong(NumericType.HP);
            if (delta > 0)
            {
                numeric.Set(NumericType.HP, currentHp + delta);
            }
            else
            {
                long newMaxHp = numeric.GetAsLong(NumericType.MaxHP);
                if (currentHp > newMaxHp)
                {
                    numeric.Set(NumericType.HP, newMaxHp);
                }
            }

            state.AppliedMaxHpDelta = targetDelta;
            return 0;
        }
    }

    public class BTRogueClearBagSpaceHpBonusHandler : ABTHandler<BTRogueClearBagSpaceHpBonus>
    {
        protected override int Run(BTRogueClearBagSpaceHpBonus node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            RogueBagSpaceHpBonusBuffStateComponent state = buff?.GetBuffData().GetComponent<RogueBagSpaceHpBonusBuffStateComponent>();
            if (numeric == null || state == null || state.AppliedMaxHpDelta == 0)
            {
                return 0;
            }

            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd - state.AppliedMaxHpDelta);

            long currentHp = numeric.GetAsLong(NumericType.HP);
            long newMaxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (currentHp > newMaxHp)
            {
                numeric.Set(NumericType.HP, newMaxHp);
            }

            state.AppliedMaxHpDelta = 0;
            return 0;
        }
    }

    public class BTRogueUpdateNearMonsterSpeedHandler : ABTHandler<BTRogueUpdateNearMonsterSpeed>
    {
        protected override int Run(BTRogueUpdateNearMonsterSpeed node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            if (buff == null || buff.IsDisposed || numeric == null)
            {
                return 0;
            }

            EffectRogueNearMonsterSpeedBonus effect = buff.GetConfig().GetEffect<EffectRogueNearMonsterSpeedBonus>();
            if (effect == null || effect.Radius <= 0 || effect.SpeedPct <= 0)
            {
                return 0;
            }

            RogueNearMonsterSpeedBuffStateComponent state = buff.GetBuffData().GetComponent<RogueNearMonsterSpeedBuffStateComponent>() ??
                    buff.GetBuffData().AddComponent<RogueNearMonsterSpeedBuffStateComponent>();

            int targetSpeedPct = HasMonsterInRadius(unit, effect.Radius) ? effect.SpeedPct : 0;
            int deltaPct = targetSpeedPct - state.AppliedSpeedPct;
            if (deltaPct == 0)
            {
                return 0;
            }

            int finalPct = numeric.GetAsInt(NumericType.SpeedFinalPct);
            numeric.Set(NumericType.SpeedFinalPct, finalPct + deltaPct);
            state.AppliedSpeedPct = targetSpeedPct;
            return 0;
        }

        private static bool HasMonsterInRadius(Unit unit, float radius)
        {
            AOIEntity ownerAoi = unit.GetComponent<AOIEntity>();
            if (ownerAoi == null || ownerAoi.IsDisposed)
            {
                return false;
            }

            float radiusSqr = radius * radius;
            foreach ((long _, AOIEntity aoiEntity) in ownerAoi.GetSeeUnits())
            {
                Unit target = aoiEntity?.Unit;
                if (target == null || target.IsDisposed || target.UnitType != UnitType.Monster)
                {
                    continue;
                }

                float2 offset = target.Position.xz - unit.Position.xz;
                if (math.lengthsq(offset) <= radiusSqr)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class BTRogueClearNearMonsterSpeedHandler : ABTHandler<BTRogueClearNearMonsterSpeed>
    {
        protected override int Run(BTRogueClearNearMonsterSpeed node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            RogueNearMonsterSpeedBuffStateComponent state = buff?.GetBuffData().GetComponent<RogueNearMonsterSpeedBuffStateComponent>();
            if (numeric == null || state == null || state.AppliedSpeedPct == 0)
            {
                return 0;
            }

            int finalPct = numeric.GetAsInt(NumericType.SpeedFinalPct);
            numeric.Set(NumericType.SpeedFinalPct, finalPct - state.AppliedSpeedPct);
            state.AppliedSpeedPct = 0;
            return 0;
        }
    }

    public class BTRogueClearOutOfCombatStealthHandler : ABTHandler<BTRogueClearOutOfCombatStealth>
    {
        protected override int Run(BTRogueClearOutOfCombatStealth node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            RogueOutOfCombatStealthStateComponent stealthState = unit?.GetComponent<RogueOutOfCombatStealthStateComponent>();
            if (unit == null || unit.IsDisposed || stealthState == null)
            {
                return 0;
            }

            stealthState.ClearConcealment();
            unit.RemoveComponent<RogueOutOfCombatStealthStateComponent>();
            return 0;
        }
    }

    public class BTRogueApplySilentSearchHandler : ABTHandler<BTRogueApplySilentSearch>
    {
        protected override int Run(BTRogueApplySilentSearch node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            if (unit == null || unit.IsDisposed)
            {
                return 0;
            }

            ECAInteractionModifierHelper.AddSilentSearch(unit);
            return 0;
        }
    }

    public class BTRogueRemoveSilentSearchHandler : ABTHandler<BTRogueRemoveSilentSearch>
    {
        protected override int Run(BTRogueRemoveSilentSearch node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            if (unit == null || unit.IsDisposed)
            {
                return 0;
            }

            ECAInteractionModifierHelper.RemoveSilentSearch(unit);
            return 0;
        }
    }

    public class BTRogueApplyTrapMasterHandler : ABTHandler<BTRogueApplyTrapMaster>
    {
        protected override int Run(BTRogueApplyTrapMaster node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 0;
            }

            EffectRogueTrapMaster effect = buff.GetConfig().GetEffect<EffectRogueTrapMaster>();
            if (effect == null || effect.IdleMs <= 0 || effect.BulletCount <= 0)
            {
                return 0;
            }

            RogueTrapMasterStateComponent trapMasterState = unit.GetComponent<RogueTrapMasterStateComponent>() ??
                    unit.AddComponent<RogueTrapMasterStateComponent>();
            trapMasterState.UpsertSource(buff.Id, effect.IdleMs, effect.BulletCount);
            return 0;
        }
    }

    public class BTRogueRemoveTrapMasterHandler : ABTHandler<BTRogueRemoveTrapMaster>
    {
        protected override int Run(BTRogueRemoveTrapMaster node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            RogueTrapMasterStateComponent trapMasterState = unit?.GetComponent<RogueTrapMasterStateComponent>();
            if (unit == null || unit.IsDisposed || buff == null || trapMasterState == null)
            {
                return 0;
            }

            trapMasterState.RemoveSource(buff.Id);
            if (trapMasterState.Sources.Count == 0)
            {
                unit.RemoveComponent<RogueTrapMasterStateComponent>();
            }

            return 0;
        }
    }

    public class BTRogueApplyPassiveEffectsHandler : ABTHandler<BTRogueApplyPassiveEffects>
    {
        protected override int Run(BTRogueApplyPassiveEffects node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 0;
            }

            RogueBuffPassiveRuntimeComponent passiveRuntime = unit.GetComponent<RogueBuffPassiveRuntimeComponent>() ??
                    unit.AddComponent<RogueBuffPassiveRuntimeComponent>();
            passiveRuntime.ApplyFromBuff(buff);

            if (buff.GetConfig().GetEffect<EffectRogueOutOfCombatStealth>() != null)
            {
                CombatStateComponent combat = unit.GetComponent<CombatStateComponent>();
                bool inCombat = combat != null && combat.InCombat;
                if (!inCombat)
                {
                    RogueOutOfCombatStealthStateComponent stealthState = unit.GetComponent<RogueOutOfCombatStealthStateComponent>() ??
                            unit.AddComponent<RogueOutOfCombatStealthStateComponent>();
                    stealthState.ApplyConcealment();
                }
            }

            if (buff.GetConfig().GetEffect<EffectRogueAfterSkillSpeedBoost>() != null)
            {
                RogueAfterSkillSpeedBoostComponent speedBoost = unit.GetComponent<RogueAfterSkillSpeedBoostComponent>();
                if (speedBoost != null)
                {
                    passiveRuntime.GetAfterSkillSpeedBoostData(out int totalSpeedPct, out int maxDurationMs);
                    if (totalSpeedPct > 0 && maxDurationMs > 0)
                    {
                        speedBoost.RefreshSpeedBoost(totalSpeedPct, maxDurationMs);
                    }
                }
            }

            return 0;
        }
    }

    public class BTRogueRemovePassiveEffectsHandler : ABTHandler<BTRogueRemovePassiveEffects>
    {
        protected override int Run(BTRogueRemovePassiveEffects node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            if (unit == null || unit.IsDisposed || buff == null || passiveRuntime == null)
            {
                return 0;
            }

            passiveRuntime.RemoveBySource(buff.Id);

            if (!passiveRuntime.HasOutOfCombatStealth())
            {
                RogueOutOfCombatStealthStateComponent stealthState = unit.GetComponent<RogueOutOfCombatStealthStateComponent>();
                if (stealthState != null)
                {
                    stealthState.ClearConcealment();
                    unit.RemoveComponent<RogueOutOfCombatStealthStateComponent>();
                }
            }

            if (passiveRuntime.AfterSkillSpeedBoostBySource.Count == 0)
            {
                RogueAfterSkillSpeedBoostComponent speedBoost = unit.GetComponent<RogueAfterSkillSpeedBoostComponent>();
                if (speedBoost != null)
                {
                    unit.RemoveComponent<RogueAfterSkillSpeedBoostComponent>();
                }
            }

            if (passiveRuntime.IsEmpty())
            {
                unit.RemoveComponent<RogueBuffPassiveRuntimeComponent>();
            }

            return 0;
        }
    }

    public class BTRogueApplyScaleModifierHandler : ABTHandler<BTRogueApplyScaleModifier>
    {
        protected override int Run(BTRogueApplyScaleModifier node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            if (buff == null || buff.IsDisposed || numeric == null || node.MaxHpPermille == 0)
            {
                return 0;
            }

            long maxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (maxHp <= 0)
            {
                return 0;
            }

            long hpDelta = maxHp * node.MaxHpPermille / 1000;
            RogueScaleModifierBuffStateComponent state = buff.GetBuffData().GetComponent<RogueScaleModifierBuffStateComponent>() ??
                    buff.GetBuffData().AddComponent<RogueScaleModifierBuffStateComponent>();
            state.AppliedMaxHpDelta = hpDelta;

            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd + hpDelta);

            long currentHp = numeric.GetAsLong(NumericType.HP);
            if (hpDelta > 0)
            {
                numeric.Set(NumericType.HP, currentHp + hpDelta);
            }
            else
            {
                long newMaxHp = numeric.GetAsLong(NumericType.MaxHP);
                if (currentHp > newMaxHp)
                {
                    numeric.Set(NumericType.HP, newMaxHp);
                }
            }

            return 0;
        }
    }

    public class BTRogueRevertScaleModifierHandler : ABTHandler<BTRogueRevertScaleModifier>
    {
        protected override int Run(BTRogueRevertScaleModifier node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            RogueScaleModifierBuffStateComponent state = buff?.GetBuffData().GetComponent<RogueScaleModifierBuffStateComponent>();
            if (numeric == null || state == null || state.AppliedMaxHpDelta == 0)
            {
                return 0;
            }

            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd - state.AppliedMaxHpDelta);

            long currentHp = numeric.GetAsLong(NumericType.HP);
            long newMaxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (currentHp > newMaxHp)
            {
                numeric.Set(NumericType.HP, newMaxHp);
            }

            return 0;
        }
    }

    public class BTRoguePeriodicHpScaleHandler : ABTHandler<BTRoguePeriodicHpScale>
    {
        protected override int Run(BTRoguePeriodicHpScale node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            if (buff == null || buff.IsDisposed || numeric == null || node.MaxHpPermille == 0)
            {
                return 0;
            }

            long maxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (maxHp <= 0)
            {
                return 0;
            }

            long hpDelta = maxHp * node.MaxHpPermille / 1000;
            if (hpDelta == 0)
            {
                return 0;
            }

            RoguePeriodicHpScaleBuffStateComponent state = buff.GetBuffData().GetComponent<RoguePeriodicHpScaleBuffStateComponent>() ??
                    buff.GetBuffData().AddComponent<RoguePeriodicHpScaleBuffStateComponent>();
            state.AppliedMaxHpDelta += hpDelta;

            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd + hpDelta);
            return 0;
        }
    }

    public class BTRogueRevertPeriodicHpScaleHandler : ABTHandler<BTRogueRevertPeriodicHpScale>
    {
        protected override int Run(BTRogueRevertPeriodicHpScale node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            NumericComponent numeric = unit?.NumericComponent;
            RoguePeriodicHpScaleBuffStateComponent state = buff?.GetBuffData().GetComponent<RoguePeriodicHpScaleBuffStateComponent>();
            if (numeric == null || state == null || state.AppliedMaxHpDelta == 0)
            {
                return 0;
            }

            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd - state.AppliedMaxHpDelta);

            long currentHp = numeric.GetAsLong(NumericType.HP);
            long newMaxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (currentHp > newMaxHp)
            {
                numeric.Set(NumericType.HP, newMaxHp);
            }

            return 0;
        }
    }

    public class BTRogueHealMaxHpPermilleHandler : ABTHandler<BTRogueHealMaxHpPermille>
    {
        protected override int Run(BTRogueHealMaxHpPermille node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            NumericComponent numeric = unit?.NumericComponent;
            if (numeric == null || node.HealPermille <= 0)
            {
                return 0;
            }

            long currentHp = numeric.GetAsLong(NumericType.HP);
            long maxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (currentHp <= 0 || maxHp <= 0 || currentHp >= maxHp)
            {
                return 0;
            }

            long healAmount = maxHp * node.HealPermille / 1000;
            if (healAmount <= 0)
            {
                healAmount = 1;
            }

            long newHp = currentHp + healAmount;
            if (newHp > maxHp)
            {
                newHp = maxHp;
            }

            numeric.Set(NumericType.HP, newHp);
            return 0;
        }
    }
}
