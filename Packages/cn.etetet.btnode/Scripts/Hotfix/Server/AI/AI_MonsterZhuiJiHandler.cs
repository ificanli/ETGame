using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterZhuiJiHandler: ABTCoroutineHandler<AI_MonsterZhuiJi>
    {
        protected override async ETTask RunAsync(AI_MonsterZhuiJi node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            Scene root = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            EntityRef<ThreatComponent> threatComponentRef = threatComponent;
            
            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            int thinkIntervalMs = math.max(50, node.ThinkIntervalMs);
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            unit = unitRef;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (node.PreCastSpellId > 0)
            {
                SpellHelper.Cast(unit, node.PreCastSpellId);
            }
            
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
                
                // 找到仇恨最大的作为自己的目标
                unit = unitRef;
                threatComponent = threatComponentRef;
                if (unit == null || unit.IsDisposed || threatComponent == null)
                {
                    return;
                }

                ThreatInfo threatInfo = threatComponent.GetMaxThreat();
                if (threatInfo == null)
                {
                    continue;
                }

                Unit target = threatInfo.Unit;
                if (target == null || target.IsDisposed)
                {
                    continue;
                }

                TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
                if (targetComponent != null)
                {
                    targetComponent.Unit = target;
                }

                // 选择技能，移动到技能攻击范围
                int spellId = node.MainSpellId;
                if (spellId <= 0)
                {
                    continue;
                }

                if (!SpellConfigCategory.Instance.Contain(spellId))
                {
                    Log.Warning($"[MonsterAI] combat spell config missing, unitId={unit.Id}, spellId={spellId}");
                    continue;
                }

                SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellId);
                float distance = math.distance(unit.Position, target.Position);
                float targetRadius = target.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                float d1 = distance - targetRadius - unitRadius;
                
                if (d1 > spellConfig.TargetSelector.MaxDistance / 1000f)
                {
                    // 走过去
                    unit.FindPathMoveToAsync(target.Position).Coroutine(cancellationToken);
                    continue;
                }
                
                if (spellConfig.TargetSelector.MinDistance > 0 && d1 < spellConfig.TargetSelector.MinDistance / 1000f)
                {
                    // 反向走
                    unit.FindPathMoveToAsync(unit.Position - target.Position + unit.Position).Coroutine(cancellationToken);
                    continue;
                }

                unit.Stop(0);

                // 同一个技能还未结束
                Buff current = unit.GetComponent<SpellComponent>().Current;
                if (current != null && spellConfig.BuffId == current.ConfigId)
                {
                    continue;
                }

                // 施放技能
                SpellHelper.Cast(unit, spellId);
            }
        }
    }
}
