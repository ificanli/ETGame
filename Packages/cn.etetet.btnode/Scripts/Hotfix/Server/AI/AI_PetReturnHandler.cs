using ET.Client;
using Unity.Mathematics;

namespace ET.Server
{
    public class AI_PetReturnHandler: ABTCoroutineHandler<AI_PetReturn>
    {
        protected override async ETTask RunAsync(AI_PetReturn node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            unit.GetComponent<TargetComponent>().Unit = null;

            TimerComponent timerComponent = unit.Root().TimerComponent;
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