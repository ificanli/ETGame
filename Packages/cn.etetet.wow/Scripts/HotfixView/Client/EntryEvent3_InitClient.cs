namespace ET.Client
{
    [Event(SceneType.WOW)]
    public class EntryEvent3_InitClient : AEvent<Scene, EntryEvent3>
    {
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            Fiber fiber = root.Fiber;
            await fiber.CreateFiber(SchedulerType.Main, 0, SceneType.Client, "Client");
        }
    }
}