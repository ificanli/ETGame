using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.AI)]
    public class AI_PetIdleHandler: AIHandler<AI_PetIdle>
    {
        protected override int Check(AIComponent aiComponent, AI_PetIdle node, BTEnv env)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent.Target;
            if (target != null)
            {
                return 1;
            }

            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) > 3f)
            {
                return 1;
            }
            return 0;
        }

        protected override async ETTask Execute(AIComponent aiComponent, AI_PetIdle node, BTEnv env)
        {
            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            Unit unit = aiComponent.GetParent<Unit>();
            EntityRef<Unit> unitRef = unit;
            Unit owner = PetHelper.GetOwner(unit);
            EntityRef<Unit> ownerRef = owner;
            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            // 暂时写死
            BuffHelper.RemoveBuffByConfigId(unit, 200111, BuffFlags.AIRemove);
            
            while (true)
            {
                pathfindingComponent = pathfindingComponentRef;
                owner = ownerRef;
                float3 pos = pathfindingComponent.FindRandomPointWithRaduis(owner.Position, 1, 2);
                unit = unitRef;
                await unit.FindPathMoveToAsync(pos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                await timerComponent.WaitAsync(RandomGenerator.RandomNumber(10000, 20000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}