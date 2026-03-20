using Unity.Mathematics;

namespace ET.Server
{
    public class AI_RobotPatrolHandler: ABTCoroutineHandler<AI_RobotPatrol>
    {
        protected override async ETTask RunAsync(AI_RobotPatrol node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (pathfindingComponent == null)
            {
                Log.Warning($"[RobotAI] patrol skipped: pathfinding missing, unitId={unit.Id}");
                return;
            }

            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            float3 birthPos = spawnPointComponent?.Position ?? unit.Position;
            float minRadius = math.max(0f, node.MinRadius);
            float maxRadius = math.max(minRadius + 0.1f, node.MaxRadius);
            int idleMinMs = math.max(100, node.IdleMinMs);
            int idleMaxMs = math.max(idleMinMs, node.IdleMaxMs);

            EntityRef<Scene> rootRef = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;
            EntityRef<TargetComponent> targetComponentRef = targetComponent;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            unit = unitRef;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (node.ExitCombatBuffConfigId > 0)
            {
                BuffHelper.RemoveBuffByConfigId(unit, node.ExitCombatBuffConfigId, BuffFlags.AIRemove);
            }

            while (true)
            {
                pathfindingComponent = pathfindingComponentRef;
                unit = unitRef;
                targetComponent = targetComponentRef;
                if (unit == null || unit.IsDisposed || pathfindingComponent == null)
                {
                    return;
                }

                if (targetComponent != null)
                {
                    targetComponent.Unit = null;
                }

                float3 randomPos = pathfindingComponent.FindRandomPointWithRaduis(birthPos, minRadius, maxRadius);
                await unit.FindPathMoveToAsync(randomPos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                Scene root = rootRef;
                if (root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(RandomGenerator.RandomNumber(idleMinMs, idleMaxMs + 1));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}
