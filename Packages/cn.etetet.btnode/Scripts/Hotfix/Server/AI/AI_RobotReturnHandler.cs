using Unity.Mathematics;

namespace ET.Server
{
    public class AI_RobotReturnHandler: ABTCoroutineHandler<AI_RobotReturn>
    {
        protected override async ETTask RunAsync(AI_RobotReturn node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            float3 birthPos = spawnPointComponent?.Position ?? unit.Position;
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            Scene root = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;

            if (node.ExitCombatBuffConfigId > 0)
            {
                BuffHelper.RemoveBuffByConfigId(unit, node.ExitCombatBuffConfigId, BuffFlags.AIRemove);
            }

            threatComponent?.ClearThreat();
            if (targetComponent != null)
            {
                targetComponent.Unit = null;
            }

            if (selector != null)
            {
                selector.CurrentTargetId = 0;
                selector.ManualTargetId = 0;
            }

            int waitIntervalMs = math.max(100, node.WaitIntervalMs);
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

                unit = unitRef;
                if (unit == null || unit.IsDisposed)
                {
                    return;
                }
            }
        }
    }
}
