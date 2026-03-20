using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetZhuiJiHandler: ABTCoroutineHandler<AI_PetZhuiJi>
    {
        protected override async ETTask RunAsync(AI_PetZhuiJi node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            Scene root = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            EntityRef<TargetComponent> targetComponentRef = targetComponent;
            
            float unitRadius = unit.NumericComponent.GetAsFloat(NumericType.Radius);
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            unit = unitRef;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            SpellHelper.Cast(unit, 100110);
            
            while (true)
            {
                root = rootRef;
                if (root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(200);
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
                if (target == null || target.IsDisposed)
                {
                    continue;
                }
                
                // 选择技能，移动到技能攻击范围
                int spellId = 100100;
                SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellId);
                float distance = math.distance(unit.Position, target.Position);
                float targetRadius = target.NumericComponent.GetAsFloat(NumericType.Radius);
                float d1 = distance - targetRadius - unitRadius;
                
                if (d1 > spellConfig.TargetSelector.MaxDistance / 1000f)
                {
                    // 走过去
                    unit.FindPathMoveToAsync(target.Position).Coroutine(cancellationToken);
                    continue;
                }
                
                if (d1 < spellConfig.TargetSelector.MinDistance / 1000f)
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
