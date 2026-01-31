using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetFollowHandler: ABTCoroutineHandler<AI_PetFollow>
    {
        protected override async ETTask RunAsync(AI_PetFollow node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            Unit owner = PetHelper.GetOwner(unit);
            
            EntityRef<Unit> unitRef = unit;
            EntityRef<Unit> ownerRef = owner;

            TimerComponent timerComponent = unit.Root().TimerComponent;
            
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