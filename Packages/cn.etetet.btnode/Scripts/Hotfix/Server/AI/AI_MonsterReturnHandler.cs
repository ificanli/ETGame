using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterReturnHandler: ABTCoroutineHandler<AI_MonsterReturn>
    {
        protected override int Check(Buff buff, AI_MonsterReturn node, BTEnv env)
        {
            Unit unit = buff.GetOwner();
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

        protected override async ETTask RunAsync(Buff buff, AI_MonsterReturn node, BTEnv env)
        {
            Unit unit = buff.GetOwner();
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();

            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();

            threatComponent.ClearThreat();

            TimerComponent timerComponent = unit.Root().GetComponent<TimerComponent>();
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
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