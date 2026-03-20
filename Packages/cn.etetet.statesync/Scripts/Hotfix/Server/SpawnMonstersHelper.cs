using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 刷怪辅助类，统一处理怪物生成逻辑
    /// </summary>
    public static class SpawnMonstersHelper
    {
        /// <summary>
        /// 在指定刷怪点生成怪物
        /// </summary>
        public static int SpawnAtPoint(Scene scene, Unit spawnPointUnit, string groupId, int count)
        {
            Log.Info($"[SpawnMonsters] SpawnAtPoint called: scene={scene?.Name}, pointId={spawnPointUnit?.Id}, groupId={groupId}, count={count}");

            if (scene == null || spawnPointUnit == null || string.IsNullOrWhiteSpace(groupId) || count <= 0)
            {
                Log.Warning($"[SpawnMonsters] Invalid parameters: scene={scene != null}, point={spawnPointUnit != null}, groupId={groupId}, count={count}");
                return 0;
            }

            // TODO: 这里需要根据 groupId 从配置中读取怪物配置
            // 目前使用简单的逻辑：groupId 直接作为 UnitConfigId
            // 实际项目中应该有一个 MonsterGroupConfig 来配置刷怪组

            int monstersSpawned = 0;

            // 尝试将 groupId 解析为 UnitConfigId
            if (!int.TryParse(groupId, out int unitConfigId))
            {
                Log.Warning($"[SpawnMonsters] Invalid groupId format: {groupId}");
                return 0;
            }

            Log.Info($"[SpawnMonsters] Parsed unitConfigId: {unitConfigId}");

            // 检查配置是否存在
            if (UnitConfigCategory.Instance.GetOrDefault(unitConfigId) == null)
            {
                Log.Warning($"[SpawnMonsters] UnitConfig not found: configId={unitConfigId}");
                return 0;
            }

            // 获取刷怪点位置
            float3 spawnPosition = spawnPointUnit.Position;
            Log.Info($"[SpawnMonsters] Spawn position: ({spawnPosition.x:F2}, {spawnPosition.y:F2}, {spawnPosition.z:F2})");

            // 生成怪物
            for (int i = 0; i < count; i++)
            {
                // 在刷怪点周围随机偏移位置
                float offsetX = RandomGenerator.RandFloat01() * 4f - 2f;
                float offsetZ = RandomGenerator.RandFloat01() * 4f - 2f;
                float3 monsterPosition = new float3(spawnPosition.x + offsetX, spawnPosition.y, spawnPosition.z + offsetZ);

                // 创建怪物
                long monsterId = IdGenerater.Instance.GenerateId();
                Unit monster = UnitFactory.Create(scene, monsterId, unitConfigId, monsterPosition, quaternion.identity, false);

                if (monster != null)
                {
                    float3 resolvedPosition = monsterPosition;
                    float unitRadius = monster.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
                    PathfindingComponent pathfinding = monster.GetComponent<PathfindingComponent>();
                    if (pathfinding != null)
                    {
                        if (pathfinding.TryRecastFindNearestPointForMovement(monsterPosition, unitRadius, out float3 projectedPos, out float projectedDistance))
                        {
                            resolvedPosition = projectedPos;
                            if (projectedDistance > 0.01f)
                            {
                                Log.Info($"[SpawnMonsters] Spawn position projected to navmesh: unitId={monsterId}, rawPos=({monsterPosition.x:F2}, {monsterPosition.y:F2}, {monsterPosition.z:F2}), projectedPos=({resolvedPosition.x:F2}, {resolvedPosition.y:F2}, {resolvedPosition.z:F2}), projectedDeltaXZ={projectedDistance:F3}");
                            }
                        }
                        else if (pathfinding.TryRecastFindNearestPointForSpawn(monsterPosition, unitRadius, out float3 spawnProjectedPos, out float spawnProjectedDistance))
                        {
                            resolvedPosition = spawnProjectedPos;
                            Log.Warning($"[SpawnMonsters] recover spawn point by wide projection: unitId={monsterId}, rawPos=({monsterPosition.x:F2}, {monsterPosition.y:F2}, {monsterPosition.z:F2}), projectedPos=({resolvedPosition.x:F2}, {resolvedPosition.y:F2}, {resolvedPosition.z:F2}), projectedDeltaXZ={spawnProjectedDistance:F3}");
                        }
                        else if (pathfinding.TryRecastFindNearestPointForSpawn(spawnPosition, unitRadius, out float3 centerProjectedPos, out float centerProjectedDistance))
                        {
                            resolvedPosition = centerProjectedPos;
                            Log.Warning($"[SpawnMonsters] recover spawn point by center projection: unitId={monsterId}, centerPos=({spawnPosition.x:F2}, {spawnPosition.y:F2}, {spawnPosition.z:F2}), projectedPos=({resolvedPosition.x:F2}, {resolvedPosition.y:F2}, {resolvedPosition.z:F2}), projectedDeltaXZ={centerProjectedDistance:F3}");
                        }
                        else if (pathfinding.TryRecastFindNearestPointForSpawn(float3.zero, unitRadius, out float3 originProjectedPos, out float originProjectedDistance))
                        {
                            resolvedPosition = originProjectedPos;
                            Log.Warning($"[SpawnMonsters] recover spawn point by origin projection: unitId={monsterId}, projectedPos=({resolvedPosition.x:F2}, {resolvedPosition.y:F2}, {resolvedPosition.z:F2}), projectedDeltaXZ={originProjectedDistance:F3}");
                        }
                        else
                        {
                            Log.Warning($"[SpawnMonsters] Spawn position not on navmesh: unitId={monsterId}, pos=({monsterPosition.x:F2}, {monsterPosition.y:F2}, {monsterPosition.z:F2})");
                            resolvedPosition = monsterPosition;
                        }
                    }

                    // 设置怪物位置
                    monster.Position = resolvedPosition;
                    MapUnitEnterHelper.TrySnapCurrentPositionToNavmesh(monster, "SpawnMonstersHelper", out float snapDeltaXZ, out float snapDeltaY);
                    monster.GetComponent<UnitSpawnPointComponent>()?.SetSpawnPoint(monster.Position, monster.Rotation);
                    MapUnitEnterHelper.EnsureConfiguredAIBuff(monster, true);

                    monstersSpawned++;
                    Log.Info($"[SpawnMonsters] Monster created: id={monsterId}, configId={unitConfigId}, pos=({monster.Position.x:F2}, {monster.Position.y:F2}, {monster.Position.z:F2}), snapDeltaXZ={snapDeltaXZ:F3}, snapDeltaY={snapDeltaY:F3}");
                }
                else
                {
                    Log.Warning($"[SpawnMonsters] Failed to create monster: configId={unitConfigId}");
                }
            }

            Log.Info($"[SpawnMonsters] Spawn completed: total={monstersSpawned}/{count}");
            return monstersSpawned;
        }
    }
}
