using ET.Client;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.AI)]
    public class AI_PetReturnHandler: AIHandler<AI_PetReturn>
    {
        protected override int Check(Unit unit, AI_PetReturn node, BTEnv env)
        {
            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) < 30f)
            {
                return 1;
            }
            return 0;
        }

        protected override async ETTask Execute(Unit unit, AI_PetReturn node, BTEnv env)
        {
            unit.GetComponent<TargetComponent>().Unit = default;

            TimerComponent timerComponent = unit.Root().GetComponent<TimerComponent>();
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
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