using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterReturnHandler: ABTCoroutineHandler<AI_MonsterReturn>
    {
        protected override async ETTask RunAsync(AI_MonsterReturn node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            Scene root = unit.Root();
            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            float3 birthPos = spawnPointComponent?.Position ?? unit.Position;
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            int waitIntervalMs = math.max(100, node.WaitIntervalMs);

            threatComponent?.ClearThreat();
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            unit = unitRef;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            await unit.FindPathMoveToAsync(birthPos);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            while (true)
            {
                unit = unitRef;
                root = rootRef;
                if (unit == null || unit.IsDisposed || root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(waitIntervalMs);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}
