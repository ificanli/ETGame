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
            Scene root = unit.Root();
            
            EntityRef<Unit> unitRef = unit;
            EntityRef<Unit> ownerRef = owner;
            EntityRef<Scene> rootRef = root;
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            unit = unitRef;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            SpellHelper.Cast(unit, 100110);
            
            while (true)
            {
                unit = unitRef;
                owner = ownerRef;
                if (unit == null || unit.IsDisposed || owner == null || owner.IsDisposed)
                {
                    return;
                }

                await unit.FindPathMoveToAsync(owner.Position);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
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
            }
        }
    }
}
