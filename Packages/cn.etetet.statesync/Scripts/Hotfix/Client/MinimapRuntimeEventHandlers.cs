namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_SyncMinimapMarker : AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            MinimapRuntimeMarkerHelper.SyncUnit(runtime, args.Unit);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Current)]
    public class ChangePosition_SyncMinimapMarker : AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            MinimapRuntimeMarkerHelper.SyncPosition(runtime, args.Unit);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Current)]
    public class ChangeRotation_SyncMinimapMarker : AEvent<Scene, ChangeRotation>
    {
        protected override async ETTask Run(Scene scene, ChangeRotation args)
        {
            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            MinimapRuntimeMarkerHelper.SyncForward(runtime, args.Unit);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Current)]
    public class BeforeUnitRemove_RemoveMinimapMarker : AEvent<Scene, BeforeUnitRemove>
    {
        protected override async ETTask Run(Scene scene, BeforeUnitRemove args)
        {
            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            Unit unit = args.Unit;
            MinimapRuntimeMarkerHelper.RemoveMarker(runtime, unit?.Id ?? 0);
            await ETTask.CompletedTask;
        }
    }
}
