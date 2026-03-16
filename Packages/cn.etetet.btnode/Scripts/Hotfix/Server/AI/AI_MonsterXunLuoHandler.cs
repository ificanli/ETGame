using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterXunLuoHandler: ABTCoroutineHandler<AI_MonsterXunLuo>
    {
        protected override async ETTask RunAsync(AI_MonsterXunLuo node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            Scene root = buff.Root();
            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> unitRef = unit;

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;

            NumericComponent numericComponent = unit.NumericComponent;
            UnitSpawnPointComponent spawnPointComponent = unit.GetComponent<UnitSpawnPointComponent>();
            float3 birthPos = spawnPointComponent?.Position ?? unit.Position;
            float aoi = numericComponent.GetAsFloat(NumericType.AOI);
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            // 暂时写死
            BuffHelper.RemoveBuffByConfigId(unit, 200111, BuffFlags.AIRemove);
            
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
                    if (TryAcquireThreatFromVisiblePlayer(unit, threatComponent, out Unit acquiredTarget))
                    {
                        unit.GetComponent<TargetComponent>().Unit = acquiredTarget;
                        Log.Info($"[MonsterAggro] acquire threat by sight, monster={unit.Id}, target={acquiredTarget.Id}, pos={unit.Position}");
                        return;
                    }
                }

                // 找一个点
                pathfindingComponent = pathfindingComponentRef;
                float3 randomPos = pathfindingComponent.FindRandomPointWithRaduis(birthPos, 0, aoi);
                
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
                await root.TimerComponent.WaitAsync(RandomGenerator.RandomNumber(1000, 4000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }

        private static bool TryAcquireThreatFromVisiblePlayer(Unit unit, ThreatComponent threatComponent, out Unit target)
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
