using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterZhuiJi: AAIHandler
    {
        public override int Check(AIComponent aiComponent, AIConfig aiConfig)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            if (threatComponent == null)
            {
                return 1;
            }
            if (threatComponent.GetCount() == 0)
            {
                return 1;
            }
            
            return 0;
        }

        public override async ETTask Execute(AIComponent aiComponent, AIConfig aiConfig)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();

            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            
            float unitRadius = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            while (true)
            {
                await timerComponent.WaitAsync(200);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                // 找到仇恨最大的作为自己的目标
                ThreatInfo threatInfo = threatComponent.GetMaxThreat();
                Unit target = threatInfo.Unit;
                if (target == null)
                {
                    continue;
                }
                unit.GetComponent<TargetComponent>().Unit = target;
                
                // 选择技能，移动到技能攻击范围
                int spellId = 100100;
                SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellId);
                float distance = math.distance(unit.Position, target.Position);
                float targetRadius = target.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
                if (distance > spellConfig.TargetSelector.MaxDistance + targetRadius + unitRadius)
                {
                    // 走过去
                    unit.FindPathMoveToAsync(target.Position).WithContext(cancellationToken);
                    continue;
                }
                if (distance < spellConfig.TargetSelector.MinDistance + targetRadius + unitRadius)
                {
                    // 反向走
                    unit.FindPathMoveToAsync(unit.Position - target.Position + unit.Position).WithContext(cancellationToken);
                    continue;
                }
                
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