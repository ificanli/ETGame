using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterXunLuo: AAIHandler
    {
        public override int Check(AIComponent aiComponent, AIConfig aiConfig)
        {
            return 0;
        }

        public override async ETTask Execute(AIComponent aiComponent, AIConfig aiConfig)
        {
            Scene root = aiComponent.Root();
            Unit unit = aiComponent.GetParent<Unit>();

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();

            UnitConfig unitConfig = unit.Config();
            
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            float3 birthPos = new(numericComponent.GetAsFloat(NumericType.X), numericComponent.GetAsFloat(NumericType.Y), numericComponent.GetAsFloat(NumericType.Z));
            float aoi = numericComponent.GetAsFloat(NumericType.AOI);
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            while (true)
            {
                // 找一个点
                
                float3 randomPos = pathfindingComponent.FindRandomPointWithRaduis(birthPos, 0, aoi);
                
                // 走过去
                await unit.FindPathMoveToAsync(randomPos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                // 等待一段时间
                await root.GetComponent<TimerComponent>().WaitAsync(RandomGenerator.RandomNumber(1000, 4000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}