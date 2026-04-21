using System;
using Unity.Mathematics;

namespace ET.Server
{
    public class AI_BladeCatCombatHandler: ABTCoroutineHandler<AI_BladeCatCombat>
    {
        protected override async ETTask RunAsync(AI_BladeCatCombat node, BTEnv env)
        {
            if (!MonsterCombatCommonHelper.TryGetInitialContext(env, node.Buff, out Unit unit, out Scene root, out ThreatComponent threatComponent, out float unitRadius))
            {
                return;
            }

            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            EntityRef<ThreatComponent> threatComponentRef = threatComponent;
            int thinkIntervalMs = math.max(80, node.ThinkIntervalMs);
            int postCastRecoverMs = math.max(0, node.PostCastRecoverMs);
            long nextCastReadyTime = 0;
            bool wasCastingSpell = false;
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            while (true)
            {
                root = rootRef;
                if (root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(thinkIntervalMs);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                unit = unitRef;
                threatComponent = threatComponentRef;
                if (unit == null || unit.IsDisposed)
                {
                    return;
                }

                long now = TimeInfo.Instance.ServerNow();
                bool hasCastingSpell = MonsterCombatCommonHelper.HasCastingSpell(unit);
                if (!hasCastingSpell && wasCastingSpell && postCastRecoverMs > 0)
                {
                    nextCastReadyTime = Math.Max(nextCastReadyTime, now + postCastRecoverMs);
                    Log.Debug(
                        $"[MonsterAI] bladecat enter recover, unitId={unit.Id}, spellId={node.MainSpellId}, " +
                        $"recoverMs={postCastRecoverMs}, nextCastReadyTime={nextCastReadyTime}");
                }
                wasCastingSpell = hasCastingSpell;

                if (!MonsterCombatCommonHelper.TryRefreshTarget(unit, threatComponent, out Unit target))
                {
                    int threatCount = threatComponent?.GetCount() ?? 0;
                    if (threatCount > 0)
                    {
                        ThreatInfo maxThreat = threatComponent.GetMaxThreat();
                        Unit maxThreatUnit = maxThreat?.Unit;
                        Log.Debug(
                            $"[MonsterAI] bladecat refresh target blocked, unitId={unit?.Id ?? 0}, threatCount={threatCount}, " +
                            $"maxThreatUnitId={(maxThreatUnit != null && !maxThreatUnit.IsDisposed ? maxThreatUnit.Id : 0)}, " +
                            $"hasTargetComponent={(unit?.GetComponent<TargetComponent>() != null)}, " +
                            $"unitPos={(unit != null && !unit.IsDisposed ? unit.Position.ToString() : "Disposed")}");
                    }
                    continue;
                }

                if (!MonsterCombatCommonHelper.TryGetSpellConfig(unit.Id, node.MainSpellId, out SpellConfig spellConfig))
                {
                    continue;
                }

                float targetRadius = target.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                float distance = math.distance(unit.Position, target.Position);
                float edgeDistance = distance - targetRadius - unitRadius;
                if (MonsterCombatCommonHelper.TryMaintainCastRange(unit, target, unitRadius, spellConfig, cancellationToken))
                {
                    Log.Debug(
                        $"[MonsterAI] bladecat maintain range, unitId={unit.Id}, spellId={node.MainSpellId}, targetId={target.Id}, " +
                        $"distance={distance:F3}, edgeDistance={edgeDistance:F3}, unitPos={unit.Position}, targetPos={target.Position}");
                    continue;
                }

                if (hasCastingSpell)
                {
                    Buff current = unit.GetComponent<SpellComponent>()?.Current;
                    Log.Debug(
                        $"[MonsterAI] bladecat skip cast because current spell alive, unitId={unit.Id}, spellId={node.MainSpellId}, " +
                        $"targetId={target.Id}, currentBuffConfigId={(current != null && !current.IsDisposed ? current.ConfigId : 0)}, " +
                        $"currentBuffId={(current != null && !current.IsDisposed ? current.Id : 0)}");
                    continue;
                }

                if (now < nextCastReadyTime)
                {
                    continue;
                }

                MonsterCombatCommonHelper.FaceTarget(unit, target);
                MonsterCombatCommonHelper.TryCast(unit, node.MainSpellId);
            }
        }
    }
}
