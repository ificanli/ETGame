using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.AI)]
    public class AI_PetZhuiJiHandler: AIHandler<AI_PetZhuiJi>
    {
        protected override int Check(AIComponent aiComponent, AI_PetZhuiJi node, BTEnv env)
        {
            Unit pet = aiComponent.GetParent<Unit>();
            
            TargetComponent targetComponent = pet.GetComponent<TargetComponent>();
            Unit target = targetComponent.Target;
            if (target == null)
            {
                return 1;
            }
            
            // 自己阵营的不攻击
            if (PetHelper.GetOwner(pet).Id == target.Id)
            {
                return 1;
            }
            return 0;
        }

        protected override async ETTask Execute(AIComponent aiComponent, AI_PetZhuiJi node, BTEnv env)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            EntityRef<Unit> unitRef = unit;

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            EntityRef<TargetComponent> targetComponentRef = targetComponent;
            
            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            
            float unitRadius = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();

            SpellHelper.Cast(unit, 100110);
            
            while (true)
            {
                await timerComponent.WaitAsync(200);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                targetComponent = targetComponentRef;
                Unit target = targetComponent.Unit;
                if (target == null)
                {
                    continue;
                }
                
                // 选择技能，移动到技能攻击范围
                int spellId = 100100;
                SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellId);
                unit = unitRef;
                float distance = math.distance(unit.Position, target.Position);
                float targetRadius = target.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
                float d1 = distance - targetRadius - unitRadius;
                
                if (d1 > spellConfig.TargetSelector.MaxDistance / 1000f)
                {
                    // 走过去
                    unit.FindPathMoveToAsync(target.Position).WithContext(cancellationToken);
                    continue;
                }
                
                if (d1 < spellConfig.TargetSelector.MinDistance / 1000f)
                {
                    // 反向走
                    unit.FindPathMoveToAsync(unit.Position - target.Position + unit.Position).WithContext(cancellationToken);
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