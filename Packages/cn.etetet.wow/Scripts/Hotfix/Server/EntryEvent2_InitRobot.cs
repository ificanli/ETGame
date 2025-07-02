using MongoDB.Bson;
using Unity.Mathematics;

namespace ET.Server
{
    [Event(SceneType.Robot)]
    public class EntryEvent2_InitRobot: AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            if (Options.Instance.Console == 1)
            {
                root.AddComponent<ConsoleComponent>();
            }

            root.SceneType = SceneType.Client;
            await ETTask.CompletedTask;
        }
    }
}