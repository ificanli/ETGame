using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(OperaComponent))]
    public static partial class OperaComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OperaComponent self)
        {
            self.mapMask = LayerMask.GetMask("Map");
        }

        [EntitySystem]
        private static void Update(this OperaComponent self)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                CodeLoader.Instance.Reload();
            }
        }

        /*
        private static async ETTask Test1(this OperaComponent self)
        {
            Log.Debug($"Croutine 1 start1 ");
            using (await self.Root().CoroutineLockComponent.Wait(1, 20000, 3000))
            {
                await self.Root().TimerComponent.WaitAsync(6000);
            }

            Log.Debug($"Croutine 1 end1");
        }
            
        private static async ETTask Test2(this OperaComponent self)
        {
            ETCancellationToken oldCancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            Log.Debug($"Croutine 2 start2");
            using (await self.Root().CoroutineLockComponent.Wait(1, 20000, 3000))
            {
                await self.Root().TimerComponent.WaitAsync(1000);
            }
            Log.Debug($"Croutine 2 end2");
        }
        
        private static async ETTask TestCancelAfter(this OperaComponent self)
        {
            ETCancellationToken oldCancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            Log.Debug($"TestCancelAfter start");
            ETCancellationToken newCancellationToken = new();
            await self.Root().TimerComponent.WaitAsync(3000).TimeoutAsync(newCancellationToken, 1000);
            if (newCancellationToken.IsCancel())
            {
                Log.Debug($"TestCancelAfter newCancellationToken is cancel!");
            }
            
            if (oldCancellationToken != null && !oldCancellationToken.IsCancel())
            {
                Log.Debug($"TestCancelAfter oldCancellationToken is not cancel!");
            }
            Log.Debug($"TestCancelAfter end");
        }
        */
    }
}