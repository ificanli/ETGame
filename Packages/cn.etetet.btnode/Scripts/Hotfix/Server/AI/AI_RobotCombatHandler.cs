using Unity.Mathematics;

namespace ET.Server
{
    public class AI_RobotCombatHandler: ABTCoroutineHandler<AI_RobotCombat>
    {
        protected override async ETTask RunAsync(AI_RobotCombat node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (targetComponent == null)
            {
                return;
            }

            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            EntityRef<Unit> unitRef = unit;
            EntityRef<TargetComponent> targetComponentRef = targetComponent;
            EntityRef<TargetSelectorComponent> selectorRef = selector;

            TimerComponent timerComponent = unit.Root().TimerComponent;
            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            int thinkIntervalMs = math.max(50, node.ThinkIntervalMs);

            if (node.PreCastSpellId > 0)
            {
                SpellHelper.Cast(unit, node.PreCastSpellId);
            }

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            while (true)
            {
                await timerComponent.WaitAsync(thinkIntervalMs);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                unit = unitRef;
                targetComponent = targetComponentRef;
                if (unit == null || unit.IsDisposed || targetComponent == null)
                {
                    return;
                }

                Unit target = targetComponent.Unit;
                selector = selectorRef;
                float maxRange = selector?.MaxRange ?? 0f;
                if (!TargetSelectorHelper.IsValidTarget(unit, target, maxRange))
                {
                    target = selector?.SelectTarget();
                    if (!TargetSelectorHelper.IsValidTarget(unit, target, maxRange))
                    {
                        targetComponent.Unit = null;
                        continue;
                    }

                    targetComponent.Unit = target;
                }

                if (node.MainSpellId <= 0)
                {
                    continue;
                }

                if (node.MainSpellId <= 0 || !SpellConfigCategory.Instance.Contain(node.MainSpellId))
                {
                    Log.Warning($"[RobotAI] combat spell config missing, unitId={unit.Id}, spellId={node.MainSpellId}");
                    continue;
                }

                SpellConfig spellConfig = SpellConfigCategory.Instance.Get(node.MainSpellId);

                float distance = math.distance(unit.Position, target.Position);
                float targetRadius = target.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                float deltaDistance = distance - targetRadius - unitRadius;
                float maxDistance = spellConfig.TargetSelector.MaxDistance / 1000f;
                float minDistance = spellConfig.TargetSelector.MinDistance / 1000f;

                if (deltaDistance > maxDistance)
                {
                    unit.FindPathMoveToAsync(target.Position).Coroutine(cancellationToken);
                    continue;
                }

                if (minDistance > 0f && deltaDistance < minDistance)
                {
                    unit.FindPathMoveToAsync(unit.Position - target.Position + unit.Position).Coroutine(cancellationToken);
                    continue;
                }

                unit.Stop(0);

                Buff current = unit.GetComponent<SpellComponent>()?.Current;
                if (current != null && spellConfig.BuffId == current.ConfigId)
                {
                    continue;
                }

                SpellHelper.Cast(unit, node.MainSpellId);
            }
        }
    }
}
