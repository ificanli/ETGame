using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterReturnHandler: AIHandler<AI_MonsterReturn>
    {
        protected override int Check(AIComponent aiComponent, AI_MonsterReturn node, BTEnv env)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            if (math.distance(unit.Position, birthPos) < 30f)
            {
                return 1;
            }
            return 0;
        }

        protected override async ETTask Execute(AIComponent aiComponent, AI_MonsterReturn node, BTEnv env)
        {
            Unit unit = aiComponent.GetParent<Unit>();

            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();

            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();

            threatComponent.ClearThreat();

            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            await unit.FindPathMoveToAsync(birthPos);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            while (true)
            {
                await timerComponent.WaitAsync(1000);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}