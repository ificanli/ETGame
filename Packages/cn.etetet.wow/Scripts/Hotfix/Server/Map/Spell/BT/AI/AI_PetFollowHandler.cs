using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.AI)]
    public class AI_PetFollowHandler: AIHandler<AI_PetFollow>
    {
        protected override int Check(Unit unit, AI_PetFollow node, BTEnv env)
        {
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

        protected override async ETTask Execute(Unit unit, AI_PetFollow node, BTEnv env)
        {
            Unit owner = PetHelper.GetOwner(unit);
            
            EntityRef<Unit> unitRef = unit;
            EntityRef<Unit> ownerRef = owner;

            TimerComponent timerComponent = unit.Root().GetComponent<TimerComponent>();
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            unit = unitRef;
            SpellHelper.Cast(unit, 100110);
            
            while (true)
            {
                unit = unitRef;
                owner = ownerRef;
                await unit.FindPathMoveToAsync(owner.Position);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                await timerComponent.WaitAsync(200);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}