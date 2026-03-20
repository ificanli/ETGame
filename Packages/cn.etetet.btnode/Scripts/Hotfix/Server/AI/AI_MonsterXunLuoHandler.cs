using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterXunLuoHandler: ABTCoroutineHandler<AI_MonsterXunLuo>
    {
        protected override async ETTask RunAsync(AI_MonsterXunLuo node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            Scene root = buff.Root();
            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> unitRef = unit;

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;

            NumericComponent numericComponent = unit.NumericComponent;
            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            float3 birthPos = spawnPointComponent?.Position ?? unit.Position;
            float aoi = numericComponent?.GetAsFloat(NumericType.AOI) ?? 0f;
            float patrolMinRadius = math.max(0f, node.PatrolMinRadius);
            float configuredPatrolMaxRadius = node.PatrolMaxRadius > 0f ? node.PatrolMaxRadius : aoi;
            float patrolMaxRadius = math.max(patrolMinRadius + 0.1f, configuredPatrolMaxRadius);
            float aggroRange = node.AggroRange > 0f ? node.AggroRange : aoi;
            int idleMinMs = math.max(100, node.IdleMinMs);
            int idleMaxMs = math.max(idleMinMs, node.IdleMaxMs);
            
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
                unit = unitRef;
                if (unit == null || unit.IsDisposed)
                {
                    return;
                }

                ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
                if (threatComponent != null)
                {
                    // 已有仇恨时立刻结束巡逻，让行为树切换到追击分支。
                    if (threatComponent.GetCount() > 0)
                    {
                        return;
                    }

                    // 巡逻状态下主动感知可见玩家，建立仇恨后切到追击。
                    if (TryAcquireThreatFromVisiblePlayer(unit, threatComponent, aggroRange, out Unit acquiredTarget))
                    {
                        TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
                        if (targetComponent != null)
                        {
                            targetComponent.Unit = acquiredTarget;
                        }

                        Log.Info($"[MonsterAggro] acquire threat by sight, monster={unit.Id}, target={acquiredTarget.Id}, pos={unit.Position}");
                        return;
                    }
                }

                // 找一个点
                pathfindingComponent = pathfindingComponentRef;
                if (pathfindingComponent == null)
                {
                    return;
                }

                float3 randomPos = pathfindingComponent.FindRandomPointWithRaduis(birthPos, patrolMinRadius, patrolMaxRadius);
                
                // 走过去
                await unit.FindPathMoveToAsync(randomPos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                // 等待一段时间
                root = rootRef;
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

        private static bool TryAcquireThreatFromVisiblePlayer(Unit unit, ThreatComponent threatComponent, float aggroRange, out Unit target)
        {
            target = null;

            AOIEntity aoiEntity = unit.GetComponent<AOIEntity>();
            if (aoiEntity == null)
            {
                return false;
            }

            float bestDistance = float.MaxValue;
            foreach ((long _, AOIEntity aoi) in aoiEntity.GetSeeUnits())
            {
                Unit candidate = aoi?.Unit;
                if (candidate == null || candidate.IsDisposed || candidate.Id == unit.Id)
                {
                    continue;
                }

                if (candidate.UnitType != UnitType.Player)
                {
                    continue;
                }

                NumericComponent numericComponent = candidate.NumericComponent;
                if (numericComponent != null && numericComponent.GetAsFloat(NumericType.HP) <= 0f)
                {
                    continue;
                }

                float distance = math.distance(new float2(unit.Position.x, unit.Position.z), new float2(candidate.Position.x, candidate.Position.z));
                if (aggroRange > 0f && distance > aggroRange)
                {
                    continue;
                }

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                target = candidate;
            }

            if (target == null)
            {
                return false;
            }

            int threat = math.max(1, 100 - (int)math.floor(bestDistance));
            threatComponent.AddThreat(target, threat);
            return true;
        }
    }
}
