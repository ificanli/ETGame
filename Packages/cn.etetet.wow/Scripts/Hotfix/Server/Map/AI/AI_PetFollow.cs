using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetFollow: AAIHandler
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
            if (math.distance(unit.Position, owner.Position) < 3f)
            {
                return 1;
            }
            
            return 0;
        }

        public override async ETTask Execute(AIComponent aiComponent, AIConfig aiConfig)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            Unit owner = PetHelper.GetOwner(unit);

            PathfindingComponent pathfindingComponent = owner.GetComponent<PathfindingComponent>();

            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            while (true)
            {
                float3 randomPos = pathfindingComponent.FindRandomPointWithRaduis(owner.Position, 2, 3);

                await unit.FindPathMoveToAsync(randomPos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                await timerComponent.WaitAsync(RandomGenerator.RandomNumber(5000, 10000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}