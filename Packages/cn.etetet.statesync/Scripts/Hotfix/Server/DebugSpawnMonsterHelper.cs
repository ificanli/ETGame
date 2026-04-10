using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 局内调试召唤怪物辅助逻辑。
    /// </summary>
    public static class DebugSpawnMonsterHelper
    {
        private const float DefaultSpawnDistance = 6f;
        private const float ForwardLengthSqEpsilon = 0.0001f;

        public static int TrySpawn(Unit owner, int unitConfigId, out Unit monster, out string errorMessage)
        {
            monster = null;
            errorMessage = string.Empty;

            if (owner == null || owner.IsDisposed)
            {
                errorMessage = "owner missing";
                return ErrorCode.ERR_DebugSpawnMonsterContextInvalid;
            }

            Scene scene = owner.Scene();
            if (scene == null || scene.IsDisposed)
            {
                errorMessage = "scene missing";
                return ErrorCode.ERR_DebugSpawnMonsterContextInvalid;
            }

            UnitConfig unitConfig = UnitConfigCategory.Instance?.GetOrDefault(unitConfigId);
            if (unitConfig == null)
            {
                errorMessage = $"unit config not found: {unitConfigId}";
                return ErrorCode.ERR_DebugSpawnMonsterInvalidConfig;
            }

            if (unitConfig.UnitType != UnitType.Monster)
            {
                errorMessage = $"unit config is not monster: unitConfigId={unitConfigId}, unitType={unitConfig.UnitType}";
                return ErrorCode.ERR_DebugSpawnMonsterInvalidConfig;
            }

            float3 desiredPosition = ResolveDesiredPosition(owner);
            long monsterId = IdGenerater.Instance.GenerateId();
            monster = UnitFactory.Create(scene, monsterId, unitConfigId, desiredPosition, owner.Rotation, false);
            if (monster == null || monster.IsDisposed)
            {
                errorMessage = $"create monster failed: unitConfigId={unitConfigId}";
                return ErrorCode.ERR_DebugSpawnMonsterCreateFailed;
            }

            float3 resolvedPosition = ResolveSpawnPosition(monster, desiredPosition, owner.Position);
            monster.Position = resolvedPosition;
            MapUnitEnterHelper.TrySnapCurrentPositionToNavmesh(monster, nameof(DebugSpawnMonsterHelper), out float snapDeltaXZ, out float snapDeltaY);
            monster.GetComponent<UnitSpawnPointComponent>()?.SetSpawnPoint(monster.Position, monster.Rotation);
            MapUnitEnterHelper.EnsureConfiguredAIBuff(monster, true);
            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevel(monster, false);

            Log.Info(
                $"[DebugSpawnMonster] success: owner={owner.Id}, monster={monster.Id}, unitConfigId={unitConfigId}, desiredPos=({desiredPosition.x:F2}, {desiredPosition.y:F2}, {desiredPosition.z:F2}), finalPos=({monster.Position.x:F2}, {monster.Position.y:F2}, {monster.Position.z:F2}), snapDeltaXZ={snapDeltaXZ:F3}, snapDeltaY={snapDeltaY:F3}");
            return ErrorCode.ERR_Success;
        }

        private static float3 ResolveDesiredPosition(Unit owner)
        {
            float3 fallback = owner.Position;
            float3 forward = owner.Forward;
            if (!math.all(math.isfinite(forward)) || math.lengthsq(forward) <= ForwardLengthSqEpsilon)
            {
                forward = math.mul(owner.Rotation, new float3(0f, 0f, 1f));
            }

            if (!math.all(math.isfinite(forward)))
            {
                return fallback;
            }

            forward.y = 0f;
            if (math.lengthsq(forward) <= ForwardLengthSqEpsilon)
            {
                forward = new float3(0f, 0f, 1f);
            }
            else
            {
                forward = math.normalize(forward);
            }

            float3 candidate = fallback + forward * DefaultSpawnDistance;
            return TryProjectSpawnPosition(owner, candidate, out float3 projected) ? projected : fallback;
        }

        private static float3 ResolveSpawnPosition(Unit monster, float3 desiredPosition, float3 fallbackPosition)
        {
            PathfindingComponent pathfinding = monster.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return desiredPosition;
            }

            float unitRadius = monster.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            if (pathfinding.TryRecastFindNearestPointForMovement(desiredPosition, unitRadius, out float3 projectedPos, out float projectedDistance))
            {
                if (projectedDistance > 0.01f)
                {
                    Log.Info(
                        $"[DebugSpawnMonster] projected by movement: monster={monster.Id}, desiredPos=({desiredPosition.x:F2}, {desiredPosition.y:F2}, {desiredPosition.z:F2}), projectedPos=({projectedPos.x:F2}, {projectedPos.y:F2}, {projectedPos.z:F2}), projectedDeltaXZ={projectedDistance:F3}");
                }

                return projectedPos;
            }

            if (pathfinding.TryRecastFindNearestPointForSpawn(desiredPosition, unitRadius, out projectedPos, out projectedDistance))
            {
                Log.Warning(
                    $"[DebugSpawnMonster] recover by wide projection: monster={monster.Id}, desiredPos=({desiredPosition.x:F2}, {desiredPosition.y:F2}, {desiredPosition.z:F2}), projectedPos=({projectedPos.x:F2}, {projectedPos.y:F2}, {projectedPos.z:F2}), projectedDeltaXZ={projectedDistance:F3}");
                return projectedPos;
            }

            if (pathfinding.TryRecastFindNearestPointForSpawn(fallbackPosition, unitRadius, out projectedPos, out projectedDistance))
            {
                Log.Warning(
                    $"[DebugSpawnMonster] recover by owner position: monster={monster.Id}, ownerPos=({fallbackPosition.x:F2}, {fallbackPosition.y:F2}, {fallbackPosition.z:F2}), projectedPos=({projectedPos.x:F2}, {projectedPos.y:F2}, {projectedPos.z:F2}), projectedDeltaXZ={projectedDistance:F3}");
                return projectedPos;
            }

            if (pathfinding.TryRecastFindNearestPointForSpawn(float3.zero, unitRadius, out projectedPos, out projectedDistance))
            {
                Log.Warning(
                    $"[DebugSpawnMonster] recover by origin projection: monster={monster.Id}, projectedPos=({projectedPos.x:F2}, {projectedPos.y:F2}, {projectedPos.z:F2}), projectedDeltaXZ={projectedDistance:F3}");
                return projectedPos;
            }

            Log.Warning(
                $"[DebugSpawnMonster] spawn position not on navmesh: monster={monster.Id}, desiredPos=({desiredPosition.x:F2}, {desiredPosition.y:F2}, {desiredPosition.z:F2})");
            return desiredPosition;
        }

        private static bool TryProjectSpawnPosition(Unit owner, float3 candidate, out float3 projected)
        {
            projected = candidate;
            PathfindingComponent pathfinding = owner.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return false;
            }

            float unitRadius = owner.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            return pathfinding.TryRecastFindNearestPointForMovement(candidate, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPointForSpawn(candidate, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPoint(owner.Position, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPointForSpawn(owner.Position, unitRadius, out projected, out _);
        }
    }
}
