using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTDashToSpellTargetPositionHandler: ABTHandler<BTDashToSpellTargetPosition>
    {
        protected override int Run(BTDashToSpellTargetPosition node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (caster == null || caster.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 1;
            }

            if (!TryGetSpellTargetPosition(buff, out float3 targetPosition))
            {
                return 1;
            }

            float3 dashVector = targetPosition - caster.Position;
            dashVector.y = 0;
            float rawDistance = math.length(dashVector);
            if (rawDistance < 0.01f)
            {
                return 0;
            }

            float3 direction = math.normalize(dashVector);
            quaternion to = quaternion.LookRotation(direction, math.up());
            caster.Turn(to, math.max(0, node.TurnTimeMs));

            float maxDistance = node.MaxDistance > 0f ? node.MaxDistance : rawDistance;
            float dashDistance = math.min(rawDistance, maxDistance);
            dashDistance = math.max(0f, dashDistance - math.max(0f, node.MinStopDistance));
            if (dashDistance < 0.01f)
            {
                return 0;
            }

            float3 destination = caster.Position + direction * dashDistance;
            int durationMs = math.max(1, node.DurationMs);

            StartDashAsync(caster, destination, durationMs, math.max(0, node.TurnTimeMs)).Coroutine();
            return 0;
        }

        private static bool TryGetSpellTargetPosition(Buff buff, out float3 targetPosition)
        {
            Buff parentBuff = buff.GetParentBuff();
            if (parentBuff != null && !parentBuff.IsDisposed)
            {
                SpellTargetComponent parentSpellTargetComponent = parentBuff.GetBuffData().GetComponent<SpellTargetComponent>();
                if (parentSpellTargetComponent != null)
                {
                    targetPosition = parentSpellTargetComponent.Position;
                    return true;
                }
            }

            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            if (spellTargetComponent != null)
            {
                targetPosition = spellTargetComponent.Position;
                return true;
            }

            targetPosition = float3.zero;
            return false;
        }

        private static async ETTask StartDashAsync(Unit caster, float3 destination, int durationMs, int turnTimeMs)
        {
            EntityRef<Unit> casterRef = caster;

            MoveComponent moveComponent = caster.GetComponent<MoveComponent>();
            if (moveComponent == null)
            {
                caster.Position = destination;
                caster.SendStop(0);
                return;
            }

            float3 startPosition = caster.Position;
            float distance = math.distance(startPosition, destination);
            if (distance < 0.01f)
            {
                caster.SendStop(0);
                return;
            }

            M2C_PathfindingResult message = M2C_PathfindingResult.Create();
            message.Id = caster.Id;
            message.Points.Add(startPosition);
            message.Points.Add(destination);
            MapMessageHelper.NoticeClient(caster, message, NoticeType.Broadcast);

            float durationSeconds = math.max(durationMs / 1000f, 0.001f);
            float speed = distance / durationSeconds;
            bool ret = await moveComponent.MoveToAsync(new List<float3> { startPosition, destination }, speed, turnTimeMs);

            caster = casterRef;
            if (!ret || caster == null || caster.IsDisposed)
            {
                return;
            }

            caster.SendStop(0);
        }
    }
}
