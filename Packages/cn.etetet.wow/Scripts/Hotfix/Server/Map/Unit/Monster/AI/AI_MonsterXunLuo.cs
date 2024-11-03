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
            Log.Debug("开始巡逻");

            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();

            UnitConfig unitConfig = unit.Config();
            float3 birthPos = new float3(unitConfig.Position[0], unitConfig.Position[1], unitConfig.Position[2]) / 1000f;
            float aoi = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.AOI);
            
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
                await root.GetComponent<TimerComponent>().WaitAsync(4000);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}