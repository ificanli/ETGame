using System;

namespace ET
{
    [Invoke(SceneType.LockStep)]
    public class FiberInit_LockStep: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            
            EntityRef<Scene> rootRef = root;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
        }
    }
}