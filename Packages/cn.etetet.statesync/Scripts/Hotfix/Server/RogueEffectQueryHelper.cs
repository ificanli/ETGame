namespace ET.Server
{
    /// <summary>
    /// 肉鸽效果查询工具。统一从玩家当前生效的 Buff 节点中查询被动效果。
    /// </summary>
    public static class RogueEffectQueryHelper
    {
        /// <summary>
        /// 获取概率倍率千分比（默认1000=1.0倍）。用于"幸运女神"等概率提升效果。
        /// </summary>
        public static int GetProbabilityMultiplierPermille(Unit unit)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            return passiveRuntime.GetProbabilityMultiplierPermille();
        }

        /// <summary>
        /// 是否禁用技能。用于"火力进化2：技能无法使用"。
        /// </summary>
        public static bool IsSkillDisabled(Unit unit)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            return passiveRuntime.HasSkillDisable();
        }

        /// <summary>
        /// 获取体型缩放千分比变化总量。正=变大，负=变小。
        /// </summary>
        public static int GetScaleModifierPermille(Unit unit)
        {
            BuffComponent buffComponent = unit?.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                return 0;
            }

            using ListComponent<Buff> buffs = ListComponent<Buff>.Create();
            buffComponent.GetByEffectType<EffectRogueScaleModifier>(buffs);
            int total = 0;
            foreach (Buff buff in buffs)
            {
                EffectRogueScaleModifier effect = buff?.GetConfig().GetEffect<EffectRogueScaleModifier>();
                if (effect == null || effect.ScalePermille == 0)
                {
                    continue;
                }

                total += effect.ScalePermille;
            }

            return total;
        }

        /// <summary>
        /// 获取游戏延长时间（毫秒）。
        /// </summary>
        public static long GetExtendGameTimeMs(Unit unit)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            return passiveRuntime.GetExtendGameTimeMs();
        }

        public static bool HasFatalImmunity(Unit unit)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            return passiveRuntime.HasFatalImmunity();
        }

        public static int GetKillGoldBonus(Unit unit, int targetUnitType)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            int killGoldBonus = passiveRuntime.GetKillGoldBonus(targetUnitType);
            if (killGoldBonus != 0)
            {
                return killGoldBonus;
            }

            if (targetUnitType != (int)UnitType.Monster)
            {
                return 0;
            }

            RogueEffectRuntimeComponent runtimeComponent = unit?.GetComponent<RogueEffectRuntimeComponent>();
            return runtimeComponent?.GetTotalValue1ByExecuteType(RogueEffectExecuteType.AddKillGold) ?? 0;
        }

        public static int GetAreaDiscoveryGold(Unit unit)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            return passiveRuntime.GetAreaDiscoveryGold();
        }

        public static bool HasOutOfCombatStealth(Unit unit)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            return passiveRuntime.HasOutOfCombatStealth();
        }

        public static void GetAfterSkillSpeedBoostData(Unit unit, out int totalSpeedPct, out int maxDurationMs)
        {
            totalSpeedPct = 0;
            maxDurationMs = 0;

            RogueBuffPassiveRuntimeComponent passiveRuntime = unit?.GetComponent<RogueBuffPassiveRuntimeComponent>();
            passiveRuntime.GetAfterSkillSpeedBoostData(out totalSpeedPct, out maxDurationMs);
        }
    }
}
