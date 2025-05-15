using ET.Client;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.AI)]
    public class AI_PetReturnHandler: AIHandler<AI_PetReturn>
    {
        protected override int Check(AIComponent aiComponent, AI_PetReturn node, BTEnv env)
        {
            Unit unit = aiComponent.GetParent<Unit>();
            
            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) < 30f)
            {
                return 1;
            }
            return 0;
        }

        protected override async ETTask Execute(AIComponent aiComponent, AI_PetReturn node, BTEnv env)
        {
            Unit unit = aiComponent.GetParent<Unit>();

            unit.GetComponent<TargetComponent>().Unit = default;

            TimerComponent timerComponent = unit.Root().GetComponent<TimerComponent>();
            EntityRef<TimerComponent> timerRef = timerComponent;
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            while (true)
            {
                timerComponent = timerRef;
                await timerComponent.WaitAsync(1000);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}