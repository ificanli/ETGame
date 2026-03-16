namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterMyUnitCreate_BindMinimapRuntime : AEvent<Scene, AfterMyUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterMyUnitCreate args)
        {
            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            if (runtime == null)
            {
                runtime = scene.AddComponent<MinimapRuntimeComponent>();
            }

            runtime.BindMyUnit(args.Unit);
            await ETTask.CompletedTask;
        }
    }
}
