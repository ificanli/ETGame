namespace ET.Client
{
    [Event(SceneType.Client)]
    public class ECAPointStateChanged_ApplyViewState : AEvent<Scene, ECAPointStateChanged>
    {
        protected override async ETTask Run(Scene scene, ECAPointStateChanged args)
        {
            ECAPointViewHelper.ApplyPointState(scene, args.PointId, args.State);
            await ETTask.CompletedTask;
        }
    }
}
