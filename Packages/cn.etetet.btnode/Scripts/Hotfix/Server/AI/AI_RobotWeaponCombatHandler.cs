using Unity.Mathematics;

namespace ET.Server
{
    public class AI_RobotWeaponCombatHandler : ABTCoroutineHandler<AI_RobotWeaponCombat>
    {
        protected override async ETTask RunAsync(AI_RobotWeaponCombat node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (targetComponent == null)
            {
                return;
            }

            Scene root = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<TargetComponent> targetComponentRef = targetComponent;
            EntityRef<Scene> rootRef = root;
            int thinkIntervalMs = math.max(50, node.ThinkIntervalMs);

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            while (true)
            {
                root = rootRef;
                if (root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(thinkIntervalMs);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                unit = unitRef;
                targetComponent = targetComponentRef;
                if (unit == null || unit.IsDisposed || targetComponent == null)
                {
                    return;
                }

                Unit target = targetComponent.Unit;
                TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
                float chaseRange = math.max(selector?.MaxRange ?? 0f, 10f);
                if (!TargetSelectorHelper.IsValidTarget(unit, target, math.max(chaseRange, node.PreferredMaxDistance + 6f)))
                {
                    targetComponent.Unit = null;
                    continue;
                }

                float preferredDistance = ResolvePreferredDistance(node, unit.Id, target.Id);
                float stopMoveTolerance = math.max(0.1f, node.StopMoveTolerance);
                float retreatDistance = math.max(0.1f, node.RetreatDistance);
                float retreatStepDistance = math.max(retreatDistance, node.RetreatStepDistance);

                float horizontalDistance = math.distance(
                    new float2(unit.Position.x, unit.Position.z),
                    new float2(target.Position.x, target.Position.z));

                if (horizontalDistance > preferredDistance + stopMoveTolerance)
                {
                    unit.FindPathMoveToAsync(target.Position).Coroutine(cancellationToken);
                    continue;
                }

                if (horizontalDistance < retreatDistance)
                {
                    float3 offset = unit.Position - target.Position;
                    offset.y = 0f;
                    float3 direction = math.normalizesafe(offset, new float3(1f, 0f, 0f));
                    float moveDistance = math.max(retreatStepDistance, preferredDistance - horizontalDistance + stopMoveTolerance);
                    float3 retreatPos = unit.Position + direction * moveDistance;
                    retreatPos.y = unit.Position.y;
                    unit.FindPathMoveToAsync(retreatPos).Coroutine(cancellationToken);
                    continue;
                }

                unit.Stop(0);
            }
        }

        private static float ResolvePreferredDistance(AI_RobotWeaponCombat node, long unitId, long targetId)
        {
            float minDistance = math.max(1f, node.PreferredMinDistance);
            float maxDistance = math.max(minDistance, node.PreferredMaxDistance);
            if (maxDistance <= minDistance + 0.01f)
            {
                return minDistance;
            }

            uint seed = (uint)(unitId ^ (targetId << 1) ^ 0x9E3779B9);
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            float ratio = (seed & 0xFFFFFF) / 16777215f;
            return math.lerp(minDistance, maxDistance, ratio);
        }
    }
}
