using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterReturnHandler: ABTCoroutineHandler<AI_MonsterReturn>
    {
        protected override int Check(AI_MonsterReturn node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            NumericComponent numericComponent = unit.NumericComponent;
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

        protected override async ETTask RunAsync(AI_MonsterReturn node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            NumericComponent numericComponent = unit.NumericComponent;

            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();

            threatComponent.ClearThreat();

            TimerComponent timerComponent = unit.Root().TimerComponent;
            
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