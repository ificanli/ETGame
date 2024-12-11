using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetIdle: AAIHandler
    {
        public override int Check(AIComponent aiComponent, AIConfig aiConfig)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent.Target;
            if (target != null)
            {
                return 1;
            }
            
            long ownerId = unit.GetComponent<PetComponent>().OwnerId;
            Unit owner = unit.GetParent<UnitComponent>().Get(ownerId);
            if (math.distance(unit.Position, owner.Position) > 3f)
            {
                return 1;
            }
            return 0;
        }

        public override async ETTask Execute(AIComponent aiComponent, AIConfig aiConfig)
        {
            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            // 暂时写死
            BuffHelper.RemoveBuffByConfigId(aiComponent.GetParent<Unit>(), 200111, BuffFlags.OutBattleRemove);
            
            while (true)
            {
                await timerComponent.WaitAsync(100000);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}