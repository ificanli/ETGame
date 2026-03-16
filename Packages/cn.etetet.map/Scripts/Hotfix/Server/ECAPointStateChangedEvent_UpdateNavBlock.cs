namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ECAPointStateChangedEvent_UpdateNavBlock : AEvent<Scene, ECAPointStateChangedEvent>
    {
        protected override ETTask Run(Scene scene, ECAPointStateChangedEvent args)
        {
            ECAPointNavBlockHelper.RefreshPointState(scene, args.PointId);
            return ETTask.CompletedTask;
        }
    }
}
