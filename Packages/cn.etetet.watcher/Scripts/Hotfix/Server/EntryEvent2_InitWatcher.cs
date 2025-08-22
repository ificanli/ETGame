namespace ET.Server
{
    [Event(SceneType.Watcher)]
    public class EntryEvent2_InitWatcher: AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            if (Options.Instance.Console)
            {
                root.AddComponent<ConsoleComponent>();
            }

            root.AddComponent<WatcherComponent>();

            await ETTask.CompletedTask;
        }
    }
}