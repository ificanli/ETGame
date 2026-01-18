using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterXunLuoHandler: ABTCoroutineHandler<AI_MonsterXunLuo>
    {
        protected override int Check(Buff buff, AI_MonsterXunLuo node, BTEnv env)
        {
            Unit unit = buff.GetOwner();
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            if (threatComponent.GetCount() > 0)
            {
                return 1;
            }
            return 0;
        }

        protected override async ETTask RunAsync(Buff buff, AI_MonsterXunLuo node, BTEnv env)
        {
            Unit unit = buff.GetOwner();
            Scene root = buff.Root();
            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> unitRef = unit;

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;
            
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            float3 birthPos = new(numericComponent.GetAsFloat(NumericType.X), numericComponent.GetAsFloat(NumericType.Y), numericComponent.GetAsFloat(NumericType.Z));
            float aoi = numericComponent.GetAsFloat(NumericType.AOI);
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            // 暂时写死
            BuffHelper.RemoveBuffByConfigId(unit, 200111, BuffFlags.AIRemove);
            
            while (true)
            {
                // 找一个点
                pathfindingComponent = pathfindingComponentRef;
                float3 randomPos = pathfindingComponent.FindRandomPointWithRaduis(birthPos, 0, aoi);
                
                // 走过去
                unit = unitRef;
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
                await root.GetComponent<TimerComponent>().WaitAsync(RandomGenerator.RandomNumber(1000, 4000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}