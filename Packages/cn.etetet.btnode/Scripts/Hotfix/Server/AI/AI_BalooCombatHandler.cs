using Unity.Mathematics;

namespace ET.Server
{
    public class AI_BalooCombatHandler: ABTCoroutineHandler<AI_BalooCombat>
    {
        protected override async ETTask RunAsync(AI_BalooCombat node, BTEnv env)
        {
            if (!MonsterCombatCommonHelper.TryGetInitialContext(env, node.Buff, out Unit unit, out Scene root, out ThreatComponent threatComponent, out float unitRadius))
            {
                return;
            }

            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            EntityRef<ThreatComponent> threatComponentRef = threatComponent;
            int baseSpeed = unit.NumericComponent?.GetAsInt(NumericType.SpeedBase) ?? 0;
            bool nextUseAltSpell = false;
            bool enraged = false;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            while (true)
            {
                unit = unitRef;
                if (unit == null || unit.IsDisposed)
                {
                    return;
                }

                NumericComponent numeric = unit.NumericComponent;
                if (!enraged && MonsterCombatCommonHelper.IsLowHp(unit, math.clamp(node.EnrageHpPermille, 1, 1000)))
                {
                    enraged = true;
                    if (numeric != null && baseSpeed > 0)
                    {
                        int enragedSpeed = math.max(baseSpeed, baseSpeed * math.max(100, node.EnrageSpeedPct) / 100);
                        numeric.SetNoEvent(NumericType.SpeedBase, enragedSpeed);
                        numeric.SetNoEvent(NumericType.Speed, enragedSpeed);
                    }
                }

                root = rootRef;
                if (root == null)
                {
                    return;
                }

                int thinkIntervalMs = enraged
                    ? math.max(80, node.EnrageThinkIntervalMs)
                    : math.max(100, node.ThinkIntervalMs);
                await root.TimerComponent.WaitAsync(thinkIntervalMs);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                unit = unitRef;
                threatComponent = threatComponentRef;
                if (!MonsterCombatCommonHelper.TryRefreshTarget(unit, threatComponent, out Unit target))
                {
                    continue;
                }

                int spellId = nextUseAltSpell && node.AltSpellId > 0
                    ? node.AltSpellId
                    : node.MainSpellId;
                if (!MonsterCombatCommonHelper.TryGetSpellConfig(unit.Id, spellId, out SpellConfig spellConfig))
                {
                    continue;
                }

                if (MonsterCombatCommonHelper.TryMaintainCastRange(unit, target, unitRadius, spellConfig, cancellationToken))
                {
                    continue;
                }

                if (MonsterCombatCommonHelper.HasCastingSpell(unit))
                {
                    continue;
                }

                MonsterCombatCommonHelper.FaceTarget(unit, target);
                if (MonsterCombatCommonHelper.TryCast(unit, spellId))
                {
                    nextUseAltSpell = node.AltSpellId > 0 && !nextUseAltSpell;
                }
            }
        }
    }
}
