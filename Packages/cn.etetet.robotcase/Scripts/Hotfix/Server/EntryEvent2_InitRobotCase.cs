namespace ET.Server
{
    [Event(SceneType.RobotCase)]
    public class EntryEvent2_InitRobotCase: AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            if (Options.Instance.Console)
            {
                root.AddComponent<ConsoleComponent>();
            }
            await ETTask.CompletedTask;
        }
    }
}