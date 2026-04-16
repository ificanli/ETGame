using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(MoveValidationComponent))]
    [FriendOf(typeof(MoveValidationComponent))]
    public static partial class MoveValidationComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MoveValidationComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            self.LastValidPosition = unit.Position;
            self.LastValidateTimeMs = TimeInfo.Instance.ServerNow();
            self.LastClientSequence = 0;
            self.ViolationCount = 0;
        }

        [EntitySystem]
        private static void Destroy(this MoveValidationComponent self)
        {
        }

        public static bool Validate(this MoveValidationComponent self, float3 clientPos, float speed, uint sequence, long clientTimeMs, out float3 correctedPos)
        {
            correctedPos = self.LastValidPosition;
            Unit unit = self.GetParent<Unit>();

            // 1. 序列号检查
            if (sequence <= self.LastClientSequence)
            {
                return true; // 旧包，静默忽略（不算违规）
            }

            long now = TimeInfo.Instance.ServerNow();
            float deltaTimeMs = math.max(now - self.LastValidateTimeMs, 16f);
            float deltaTimeSec = deltaTimeMs / 1000f;

            // 2. 速度检查：实际位移 vs 最大允许位移
            float actualDistance = math.distance(clientPos, self.LastValidPosition);
            float maxSpeed = unit.GetComponent<NumericComponent>()?.GetAsFloat(NumericType.Speed) ?? 10f;
            float maxDistance = maxSpeed * deltaTimeSec * self.MaxSpeedTolerance;

            // 给一个最小容忍距离，防止浮点误差
            maxDistance = math.max(maxDistance, 0.5f);

            if (actualDistance > maxDistance)
            {
                self.ViolationCount++;
                if (self.ViolationCount <= 10 || self.ViolationCount % 50 == 0)
                {
                    Log.Warning($"[MoveValidation] Speed violation unitId={unit.Id}, actual={actualDistance:F2}, max={maxDistance:F2}, speed={speed:F2}, maxSpeed={maxSpeed:F2}, dt={deltaTimeSec:F3}, violations={self.ViolationCount}");
                }
                return false;
            }

            // 3. NavMesh 检查
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            if (pathfinding != null)
            {
                float unitRadius = unit.GetComponent<NumericComponent>()?.GetAsFloat(NumericType.Radius) ?? 0.5f;
                if (!pathfinding.TryRecastFindNearestPointForMovement(clientPos, unitRadius, out float3 projectedPos, out float projectedDistance))
                {
                    self.ViolationCount++;
                    if (self.ViolationCount <= 10 || self.ViolationCount % 50 == 0)
                    {
                        Log.Warning($"[MoveValidation] NavMesh violation unitId={unit.Id}, pos=({clientPos.x:F2},{clientPos.y:F2},{clientPos.z:F2}), violations={self.ViolationCount}");
                    }
                    return false;
                }
            }

            // 校验通过
            self.LastValidPosition = clientPos;
            self.LastValidateTimeMs = now;
            self.LastClientSequence = sequence;
            self.ViolationCount = math.max(self.ViolationCount - 1, 0); // 逐步恢复
            correctedPos = clientPos;
            return true;
        }
    }
}
