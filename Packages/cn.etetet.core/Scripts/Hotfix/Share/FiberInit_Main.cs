using System;

namespace ET
{
    [Invoke(SceneType.Main)]
    public class FiberInit_Main: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            int sceneType = SceneTypeSingleton.Instance.GetSceneType(Options.Instance.SceneName);
            root.SceneType = sceneType;
            root.Name = Options.Instance.SceneName;
            EntityRef<Scene> rootRef = root;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
        }
    }
}