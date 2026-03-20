using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetIdleHandler: ABTCoroutineHandler<AI_PetIdle>
    {
        protected override async ETTask RunAsync(AI_PetIdle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            Scene root = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            Unit owner = PetHelper.GetOwner(unit);
            EntityRef<Unit> ownerRef = owner;
            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            unit = unitRef;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            // 暂时写死
            BuffHelper.RemoveBuffByConfigId(unit, 200111, BuffFlags.AIRemove);
            
            while (true)
            {
                unit = unitRef;
                pathfindingComponent = pathfindingComponentRef;
                owner = ownerRef;
                if (unit == null || unit.IsDisposed || owner == null || owner.IsDisposed || pathfindingComponent == null)
                {
                    return;
                }

                float3 pos = pathfindingComponent.FindRandomPointWithRaduis(owner.Position, 1, 2);
                await unit.FindPathMoveToAsync(pos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                root = rootRef;
                if (root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(RandomGenerator.RandomNumber(10000, 20000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}
