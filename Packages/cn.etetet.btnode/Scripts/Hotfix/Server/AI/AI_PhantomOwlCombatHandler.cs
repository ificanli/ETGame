using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PhantomOwlCombatHandler: ABTCoroutineHandler<AI_PhantomOwlCombat>
    {
        protected override async ETTask RunAsync(AI_PhantomOwlCombat node, BTEnv env)
        {
            if (!MonsterCombatCommonHelper.TryGetInitialContext(env, node.Buff, out Unit unit, out Scene root, out ThreatComponent threatComponent, out float unitRadius))
            {
                return;
            }

            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            EntityRef<ThreatComponent> threatComponentRef = threatComponent;
            int thinkIntervalMs = math.max(60, node.ThinkIntervalMs);
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
                if (!MonsterCombatCommonHelper.TryRefreshTarget(unit, threatComponent, out Unit target))
                {
                    continue;
                }

                if (!MonsterCombatCommonHelper.TryGetSpellConfig(unit.Id, node.MainSpellId, out SpellConfig spellConfig))
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

                MonsterCombatCommonHelper.TryCast(unit, node.MainSpellId);
            }
        }
    }
}
