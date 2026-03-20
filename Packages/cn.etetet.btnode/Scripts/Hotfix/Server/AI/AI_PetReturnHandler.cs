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
            Scene root = unit.Root();
            EntityRef<Unit> unitRef = unit;
            EntityRef<Scene> rootRef = root;
            unit.GetComponent<TargetComponent>().Unit = null;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            while (true)
            {
                unit = unitRef;
                root = rootRef;
                if (unit == null || unit.IsDisposed || root == null)
                {
                    return;
                }

                await root.TimerComponent.WaitAsync(1000);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}
