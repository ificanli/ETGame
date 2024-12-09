using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterReturn: AAIHandler
    {
        public override int Check(AIComponent aiComponent, AIConfig aiConfig)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            float3 originPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            if (math.distance(unit.Position, originPos) < 40f)
            {
                return 1;
            }
            return 0;
        }

        public override async ETTask Execute(AIComponent aiComponent, AIConfig aiConfig)
        {
            Unit unit = aiComponent.GetParent<Unit>();

            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();

            float3 originPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();

            threatComponent.ClearThreat();

            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            await unit.FindPathMoveToAsync(originPos);
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