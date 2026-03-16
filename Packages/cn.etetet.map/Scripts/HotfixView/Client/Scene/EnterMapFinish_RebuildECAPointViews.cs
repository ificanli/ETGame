namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EnterMapFinish_RebuildECAPointViews : AEvent<Scene, EnterMapFinish>
    {
        protected override async ETTask Run(Scene scene, EnterMapFinish args)
        {
            ECAPointViewHelper.RebuildBindings(scene);
            await ETTask.CompletedTask;
        }
    }
}
